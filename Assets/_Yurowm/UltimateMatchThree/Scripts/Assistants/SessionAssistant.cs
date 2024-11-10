using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Yurowm.GameCore;
using UnityEngine;

// Game logic class
public class SessionAssistant : MonoBehaviourAssistant<SessionAssistant> {
    public bool save_sessions = true;
    
    // Total
    public List<IChip> chipPrefabs = new List<IChip>();
    public List<IBlock> blockPrefabs = new List<IBlock>();
    public List<ISlotModifier> modifierPrefabs = new List<ISlotModifier>();
    public List<LevelRule> rules = new List<LevelRule>();
    
    public void Initialize() {
        chipPrefabs = Content.GetPrefabList<IChip>();
        blockPrefabs = Content.GetPrefabList<IBlock>();
        modifierPrefabs = Content.GetPrefabList<ISlotModifier>();
        rules = Content.GetPrefabList<LevelRule>();
    }

    void OnDestroy() {
        ILiveContent.KillEverything();
    }

    void Awake() {
        UIAssistant.onShowPage += OnShowPage;
        Initialize();
    }

    void OnShowPage(UIAssistant.Page page) {
        if (page.HasTag("SOUNDTRACK") && SessionInfo.current != null)
            AudioAssistant.main.PlayMusic(SessionInfo.current.rule.soundTrack);
    }

    void Start() {
        DebugPanel.AddDelegate("Complete the level", () => {
            if (SessionInfo.current.isPlaying && SessionInfo.current.rule.GetMode() == PlayingMode.Wait) {
                SessionInfo.current.ReachTheThiedStar();
                SessionInfo.current.rule.Complete();
            }
        });

        DebugPanel.AddDelegate("Fail the level", () => {
            if (SessionInfo.current.isPlaying && SessionInfo.current.rule.GetMode() == PlayingMode.Wait) {
                SessionInfo.current.rule.Fail();
            }
        });

        DebugPanel.AddDelegate("Add a bomb", () => {
            if (SessionInfo.current.isPlaying)
                FieldAssistant.main.Add(chipPrefabs.Where(x => x is IBomb).GetRandom());
        });

        #if AUTOPILOT
        DebugPanel.AddDelegate("Autopilot", () => {
            StartCoroutine(Autopilot());
        });
        #endif
    }

    void Update() {
        #if UNITY_EDITOR
        UIAssistant.Page page = UIAssistant.main.GetCurrentPage();
        if (page.HasTag("SLOWMO") && SessionInfo.current != null && SessionInfo.current.isPlaying) {
            Time.timeScale = Input.GetKey(KeyCode.Tab) ? 0.2f : 1f;
            if (Input.GetKeyDown(KeyCode.F7)) Debug.Break();
        }
        #endif
    }

    #if AUTOPILOT
    IEnumerator Autopilot() {
        ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.Autopilot);
        Time.timeScale = 2f;
        while (true) {
            yield return new WaitWithDelay(() => mode == PlayingMode.Wait, 0.2f);

            List<int2> move = MovesFinder.GetBestMove();
            if (move != null) {
                StartFillingStack();
                foreach (int2 coord in move) {
                    AddSlotToStack(Slot.all[coord]);
                    yield return new WaitForSeconds(0.1f);
                }
                ReleaseStack();
            }

            yield return new WaitForSeconds(1);

            if (SessionInfo.current.IsCompleteTarget() || SessionInfo.current.GetMovesCount() <= 0) {
                Time.timeScale = 1;
                ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.StackLine);
                Debug.Break();
                break;
            }
        }
    }
    #endif

    void OnApplicationPause(bool pauseStatus) {
        if (SessionInfo.current != null && SessionInfo.current.isPlaying)
            UIAssistant.main.ShowPage("Pause");
    }
        
	// Resumption of gameplay
	public void Continue () {
        UIAssistant.main.ShowPage("Field");
	}

	// Starting next level
	public void PlayNextLevel() {
		if (CPanel.uiAnimation > 0) return;
        StartCoroutine(PlayLevelRoutine(LevelDesign.selected.number + 1));
	}

    IEnumerator PlayLevelRoutine(int level) {
        UIAssistant.main.ShowPage("LevelList");
        while (CPanel.uiAnimation > 0) yield return 0;

        LevelAssistant.SelectDesign(level);
    }

	// Restart the current level
	public void RestartLevel() {
        if (CPanel.uiAnimation > 0) return;

        StartCoroutine(PlayLevelRoutine(SessionInfo.current.level));
	}

    // Starting a new game session

    public void StartSession(LevelDesign design) {
        StartSession(new SessionInfo(design));
    }

    public void StartSession(SessionInfo s = null) {
        StopAllCoroutines ();

        if (s == null) SessionInfo.current = new SessionInfo(LevelDesign.selected);
        else SessionInfo.current = s;

        SessionInfo.current.rule.Run();
    }
    
    // Ending the session at user
    public void Quit() {
        StopAllCoroutines ();

        Project.onLevelClose.Invoke();
        Project.onLevelEnd.Invoke();
        FieldAssistant.main.RemoveField();
        Project.ClearDelegates();
        SessionInfo.RemoveSavedSession();

        if (SessionInfo.current.isPlaying)
            CurrentUser.main.lifeSystem.BurnLife();

        CurrentUser.main.sessionCount++;
        CurrentUser.main.UpdateLevelStatistic(SessionInfo.current.level, s => {
            s.totalCount++;
            s.escapedCount++;
        });
        BerryAnalytics.Event("Level Session Escaped", SessionInfo.current.SessionEventKeys());

        UserUtils.WriteProfileOnDevice(CurrentUser.main);


        SessionInfo.current.isPlaying = false;
        UIAssistant.main.ShowPage("LevelList");
	}
}

public class SessionInfo {
    public static SessionInfo current;
    public Settings settings;
    public IChipPhysic chipPhysic;
    public int deepIndex = 0;

    public SessionInfo(LevelDesign levelDesign) : this() {
        SetDesign(levelDesign.Clone());
        SetMask();
        Bake();
    }

    public SessionInfo() {
        settings = new Settings();
    }

    public void SetDesign(LevelDesign design) {
        if (!IsBaked())
            _design = design;
    }

    public void SetMask() {
        if (!IsBaked() && design != null) {
            colorMask.Clear();
            URandom random = new URandom(Project.randomSeed * design.number);
            List<ItemColor> unsorted = ItemColorUtils.physiscalColors.Unsort(random).ToList();
            for (int key = 0; key < design.colorCount; key++)
                colorMask.Add(ItemColorUtils.physiscalColors[key], design.randomizeColors ? unsorted[key] : ItemColorUtils.physiscalColors[key]);
        }
    }

    public void SetMask(Dictionary<ItemColor, ItemColor> mask) {
        if (!IsBaked())
            colorMask = mask;
    }

    bool baked = false;
    public bool IsBaked() {
        return baked;
    }

    public bool DeepIndexUpdate(bool force) {
        if (_design.deep) {
            IDeepLevelGoal main = (IDeepLevelGoal) goals.FirstOrDefault(x => !x.IsComplete() && x is IDeepLevelGoal);
            if (main != null) {
                int newDeepIdex = Mathf.Clamp(deepIndex + main.ChangeDeepIndex(), 0,
                    _design.deepHeight - _design.height);
                if (deepIndex != newDeepIdex || force) {
                    deepIndex = newDeepIdex;
                    _activeArea = _design.activeArea;
                    _activeArea.position.y = deepIndex;
                    Slot.CalculateActiveSlots();
                    if (!force)
                        GameCamera.main.ShowField(3);
                    return true;
                }
            }
        }
        return false;
    }

    public void Bake(bool colorGeneration = true) {
        rule = Content.Emit(design.type);

        _activeArea = _design.area;

        if (colorGeneration) {
            ItemColor[] possibleColors = colorMask.Keys.ToArray();

            Dictionary<int2, SlotSettings> slots = _design.slots.ToDictionary(x => x.position, x => x);
            Dictionary<int2, ItemColor[]> colorSets = new Dictionary<int2, ItemColor[]>();

            // Generating first color sets for all current items with unknown color
            foreach (var slot in slots) {
                if (slot.Value.current == null || !slot.Value.current["color"].ItemColor.IsUnknown())
                    continue;

                var nears = Utils.allSides.ToDictionary(x => x, x => slots.Get(slot.Key + x))
                    .Where(x => x.Value != null && x.Value.current != null && x.Value.current["color"].ItemColor.IsMatchableColor()).ToDictionary(x => x.Key, x => x.Value.current["color"].ItemColor);

                colorSets.Add(slot.Key, rule.ColorGeneration(possibleColors, nears, LevelRule.GenerationType.AllUnkonwn));
            }


            // f: 0 - even slots (blind generation - all content items has unknown colors)
            // f: 1 - odd slots (colors generated in neighbors slots context)
            // f: 2 - even slots (final generation - repeats f(0) step, because it was blind generation)
            for (int f = 0; f <= 2; f++)
                foreach (var slot in colorSets) {
                    if ((slot.Key.x + slot.Key.y) % 2 == f % 2) continue;

                    LevelRule.GenerationType type = (slot.Key.x + slot.Key.y) % 2 == 0 ? LevelRule.GenerationType.EvenSlots : LevelRule.GenerationType.OddSlots;

                    var nears = Utils.allSides.ToDictionary(x => x, x => slots.Get(slot.Key + x))
                        .Where(x => x.Value != null && x.Value.current != null && x.Value.current["color"].ItemColor.IsMatchableColor())
                        .ToDictionary(x => x.Key, x => x.Value.current["color"].ItemColor);

                    ItemColor[] colors;
                    if (f == 2) colors = rule.ColorGeneration(possibleColors, nears, LevelRule.GenerationType.EvenFinal);
                    else colors = slot.Value;
                    if (colors != null)
                        slots[slot.Key].current["color"].ItemColor = colors.Length > 0 ? rule.ColorGeneration(colors, nears, type)[0] : possibleColors.GetRandom();
                }

            // now generates random colors for all non-current content items.
            foreach (SlotSettings settings in slots.Values)
                foreach (SlotContent content in settings.content)
                    if (content.HasParameter("color") && content["color"].ItemColor.IsUnknown())
                        content["color"].ItemColor = possibleColors.GetRandom();

            // Applying color mask
            foreach (SlotSettings slot in design.slots) {
                if (slot.chip != null && slot.chip["color"].ItemColor.IsPhysicalColor())
                    slot.chip["color"].ItemColor = colorMask.Get(slot.chip["color"].ItemColor);
                if (slot.block != null && slot.block["color"].ItemColor.IsPhysicalColor())
                    slot.block["color"].ItemColor = colorMask.Get(slot.block["color"].ItemColor);
            }
        }

        // Loading and initializing all required game modes
        foreach (LevelGoalInfo info in _design.goals) {
            ILevelGoal goal = Content.Emit(info.prefab);
            goal.transform.SetParent(FieldAssistant.main.sceneFolder);
            goal.transform.Reset();
            goal.SetInfo(info, this);
            goals.Add(goal);
        }
        goals.ForEach(x => x.Initialize());

        if (_design.deep) {
            IDeepLevelGoal deepGoal = (IDeepLevelGoal) goals.FirstOrDefault(x => x is IDeepLevelGoal);
            if (deepGoal != null && deepGoal.GetDirection() == DeepLevelDirection.Up)
                deepIndex = 0;
            else 
                deepIndex = _design.deepHeight - _design.height;
            _activeArea.position.y = deepIndex;
            Content.Emit<DeepLevelEdges>();
        }

        chipPhysic = IChipPhysic.physics.Get(design.chipPhysic);
        if (chipPhysic == null) chipPhysic = IChipPhysic.defaultPhysic;

        baked = true;

        BerryAnalytics.Event("Level Session Started", SessionEventKeys());
    }

    public string[] SessionEventKeys() {
        return new string[] {
            "Level Number:" + design.number,
            "Score:" + score,
            "Best Score:" + CurrentUser.main.sessions.GetAndAdd(design.number).bestScore,
            "Total Session Count:" + CurrentUser.main.sessionCount,
            "Session Count:" + CurrentUser.main.sessions.GetAndAdd(design.number).totalCount,
            "Successed Count:" + CurrentUser.main.sessions.GetAndAdd(design.number).successedCount,
            "Failed Count:" + CurrentUser.main.sessions.GetAndAdd(design.number).failedCount,
            "Escaped Count:" + CurrentUser.main.sessions.GetAndAdd(design.number).escapedCount};
    }

    area _activeArea;
    public area activeArea {
        get {
            return _activeArea;
        }
    }
    List<ILevelGoal> goals = new List<ILevelGoal>();
    List<IReactionProvider> reactions = new List<IReactionProvider>();

    internal LevelRule rule;

    LevelDesign _design;
    internal LevelDesign design {
        get {
            return _design;
        }
    }

    public int level {
        get {
            return _design.number;
        }
    }

    public IBooster mBooster = null;

    public bool isPlaying = false;

    int score = 0;

    public int swapEvent = 0;

    public Dictionary<ItemColor, int> colorTarget;
    public Dictionary<ItemColor, ItemColor> colorMask = new Dictionary<ItemColor, ItemColor>();

    public bool boosterSelected = false;

    public bool OutOfLimit() {
        return _design.movesCount <= 0;
    }

    public bool BurnMove() {
        if (!OutOfLimit()) {
            _design.movesCount--;
            return true;
        }
        return false;
    }

    public int GetMovesCount() {
        return _design.movesCount;
    }

    public int GetScore() {
        return score;
    }

    int starCount = 0;

    public bool IsLevelGoal<T>() where T : ILevelGoal {
        Type refType = typeof(T);
        foreach (ILevelGoal goal in goals)
            if (goal.GetType() == refType)
                return true;
        return false;
    }

    public List<ILevelGoal> GetGoals() {
        return goals;
    }

    public void AddScorePoint(int count = 1) {
        score += count;
        Project.onScoreChanged.Invoke();
        if (starCount != GetStarCount()) {
            starCount = GetStarCount();
            Project.onReachedTheStar.Invoke();
        }
    }

    public void AddReaction(IReactionProvider provider) {
        reactions.Add(provider);
    }

    public List<IReactionProvider> GetReactions() {
        return reactions;
    }

    public void ReachTheThiedStar() {
        score = design.thirdStarScore;
    }

    public static readonly int maxBonus = 5;

    public int GetStarCount() {
        if (score >= _design.thirdStarScore) return 3;
        if (score >= _design.secondStarScore) return 2;
        if (score >= _design.firstStarScore) return 1;
        return 0;
    }

    public StarType GetStar() {
        return (StarType) GetStarCount();
    }

    bool _isCompleteTarget = false;
    public const string sessionSaveKey = "lastSession";
    public const string sessionCheckSumKey = "lastSession@";

    public bool IsFaildedTarget() {
        foreach (ILevelGoal goal in goals)
            if (goal.IsFailed())
                return true;
        return false;
    }

    public bool IsCompleteTarget() {
        if (_isCompleteTarget)
            return true;
        foreach (ILevelGoal goal in goals) 
            if (!goal.IsComplete())
                return false;
        _isCompleteTarget = true;
        return true;
    }

    public bool IsCompleteTarget<T>() where T : ILevelGoal {
        if (_isCompleteTarget)
            return true;
        foreach (ILevelGoal goal in goals)
            if (goal is T)
                return goal.IsComplete();
        return true;
    }

    public string ToXml() {
        XDocument document = new XDocument();
        XElement root = new XElement("session");
        document.Add(root);

        root.Add(new XAttribute("level_number", level));

        root.Add(new XAttribute("score", score));

        root.Add(new XAttribute("moves_count", _design.movesCount));

        root.Add(new XAttribute("color_mask", string.Join(",", colorMask.Select(x => string.Format("{0}:{1}", (int) x.Key, (int) x.Value)).ToArray())));

        root.Add(new XAttribute("boosterSelected", boosterSelected.ToString()));

        root.Add(new XAttribute("deepIndex", deepIndex));

        XElement rule = new XElement("rule");
        XElement slots = new XElement("slots");
        XElement bigObjects = new XElement("bigObjects");
        XElement goals = new XElement("goals");
        XElement reactions = new XElement("reactions");
        XElement extensions = new XElement("extensions");

        root.Add(rule);
        root.Add(slots);
        root.Add(bigObjects);
        root.Add(goals);
        root.Add(reactions);
        root.Add(extensions);

        this.rule.Serialize(rule);

        foreach (ILevelGoal goal in GetGoals()) {
            XElement goal_element = new XElement(goal.name);
            goals.Add(goal_element);
            goal.Serialize(goal_element);
        }

        foreach (SlotSettings settings in _design.slots) {
            if (!Slot.all.ContainsKey(settings.position))
                continue;

            Slot slot = Slot.all[settings.position];

            XElement slot_element = new XElement("slot");
            slots.Add(slot_element);
            slot.Serialize(slot_element);

            foreach (ISlotContent bo in slot.Content().Where(c => c is IBigObject && c.slot == slot)) {
                XElement bo_element = new XElement(bo.original.name);
                bo_element.Add(new XAttribute("position", slot.position));
                bigObjects.Add(bo_element);
                bo.Serialize(bo_element);
            }
        }

        foreach (IReactionProvider reaction in this.reactions) {
            XElement reaction_element = new XElement(reaction.GetType().Name);
            reactions.Add(reaction_element);
            reaction.Serialize(reaction_element);
        }

        foreach (ILevelExtension extension in ILiveContent.GetAll<ILevelExtension>()) {
            XElement extension_element = new XElement(extension.name);
            extensions.Add(extension_element);
            extension.Serialize(extension_element);
        }

        return document.ToString();
    }

    public static SessionInfo FromXml(string raw) {
        SessionInfo session;
        try {
            XDocument document = XDocument.Parse(raw);
            XElement root = document.Root;

            int level_number = int.Parse(root.Attribute("level_number").Value);

            LevelDesign design = LevelAssistant.main.GetDesign(level_number).Clone();
            design.movesCount = int.Parse(root.Attribute("moves_count").Value);

            XElement slots = root.Element("slots");
            XElement bigObjects = root.Element("bigObjects");
            XElement goals = root.Element("goals");
            XElement reactions = root.Element("reactions");
            XElement extensions = root.Element("extensions");
            XElement rule = root.Element("rule");

            Dictionary<string, ISlotContent> contentPrefabs = Content.GetPrefabList<ISlotContent>().
                ToDictionary(x => x.name, x => x);

            design.slots.Clear();

            SlotContent.Type type;
            SlotContent slotContent;
            foreach (XElement xml in slots.Elements()) {
                int2 position = int2.Parse(xml.Attribute("position").Value);
                SlotSettings slot = new SlotSettings(position);

                foreach (XElement xContent in xml.Elements()) {
                    ISlotContent prefab = contentPrefabs.Get(xContent.Name.LocalName);
                    if (prefab) {
                        type = SlotContent.GetContentType(prefab);
                        slotContent = new SlotContent(prefab.name, type);
                        slot.content.Add(slotContent);
                        prefab.Deserialize(xContent, slotContent);
                    }
                }

                design.slots.Add(slot);
            }

            design.bigObjects.Clear();
            if (bigObjects != null)
            foreach (XElement xContent in bigObjects.Elements()) {
                ISlotContent prefab = contentPrefabs.Get(xContent.Name.LocalName);
                int2 position = int2.Parse(xContent.Attribute("position").Value);
                BigObjectSettings bigObject = new BigObjectSettings(position);
                bigObject.content = new SlotContent(prefab.name, SlotContent.GetContentType(prefab));
                prefab.Deserialize(xContent, bigObject.content);
                design.bigObjects.Add(bigObject);
            }

            List<ILevelGoal> goalPrefabs = Content.GetPrefabList<ILevelGoal>();
            design.goals.Clear();
            foreach (XElement xml in goals.Elements()) {
                ILevelGoal prefab = goalPrefabs.FirstOrDefault(x => x.name == xml.Name.LocalName);
                if (prefab != null) {
                    LevelGoalInfo info = new LevelGoalInfo(prefab);
                    prefab.Deserialize(xml, info);
                    design.goals.Add(info);
                }
            }

            session = new SessionInfo();
            session.SetDesign(design);
            session.SetMask(root.Attribute("color_mask").Value
                .Split(',')
                .Select(x => x.Split(':'))
                .ToDictionary(
                    x => (ItemColor) int.Parse(x[0]),
                    x => (ItemColor) int.Parse(x[1])));

            session.Bake(false);
            session.score = int.Parse(root.Attribute("score").Value);
            session.deepIndex = int.Parse(root.Attribute("deepIndex").Value);

            session.boosterSelected = bool.Parse(root.Attribute("boosterSelected").Value);
            
            session.reactions.Clear();
            List<Type> reactionProviderTypes = Utils.FindInheritorTypes<IReactionProvider>();
            foreach (XElement xml in reactions.Elements()) {
                Type targetType = reactionProviderTypes.FirstOrDefault(x => x.Name == xml.Name.LocalName);
                if (targetType != null) {
                    IReactionProvider provider = (IReactionProvider) Activator.CreateInstance(targetType);
                    provider.Deserizalie(xml);
                    session.reactions.Add(provider);
                }
            }

            design.extensions.Clear();
            List<ILevelExtension> extensionPrefabs = Content.GetPrefabList<ILevelExtension>();
            foreach (XElement xml in extensions.Elements()) {
                ILevelExtension prefab = extensionPrefabs.FirstOrDefault(x => x.name == xml.Name.LocalName);
                if (prefab != null) {
                    ILevelExtension.LevelExtensionInfo info = new ILevelExtension.LevelExtensionInfo(prefab);
                    prefab.Deserialize(xml, info);
                    design.extensions.Add(info);
                }
            }

            session.rule.Deserialize(rule);

            return session;
        } catch (Exception e) {
            Debug.LogException(e);
            FieldAssistant.main.RemoveField();
            return null;
        }
    }

    internal void Save(bool force = false) {
        if (!SessionAssistant.main.save_sessions)
            return;
        if (settings.allowsToSave || force) {
            string xml = ToXml();
            PlayerPrefs.SetString(sessionSaveKey, xml);
            PlayerPrefs.SetString(sessionCheckSumKey, xml.CheckSum().ToString());
            PlayerPrefs.Save();
        }
    }

    public static SessionInfo Load() {
        if (!SessionAssistant.main.save_sessions)
            return null;
        string raw = PlayerPrefs.GetString(sessionSaveKey);
        if (raw.IsNullOrEmpty()) return null;

        string checkSum = PlayerPrefs.GetString(sessionCheckSumKey);
        if (checkSum != raw.CheckSum().ToString()) return null;

        SessionInfo savedSession = FromXml(raw);
        if (savedSession != null) {
            LevelDesign.selected = savedSession.design;
            return savedSession;
        }

        return null;
    }

    internal static void RemoveSavedSession() {
        PlayerPrefs.DeleteKey(sessionSaveKey);
        PlayerPrefs.DeleteKey(sessionCheckSumKey);
        PlayerPrefs.Save();
    }

    internal void AddMove() {
        design.movesCount++;
    }

    public class Settings {
        public bool mBoostersEnable = true;
        public bool sBoostersEnable = true;
        public bool allowsToSave = true;
        public bool showHints = true;
    }
}

#if AUTOPILOT
public static class MovesFinder {

    public static List<int2> GetBestMove() {
        Dictionary<int2, Unit> units = Slot.all
            .Where(x => x.Value.color.IsMatchableColor())
            .ToDictionary(x => x.Key, x => new Unit(x.Key, x.Value.color));
        foreach (Unit unit in units.Values)
            foreach (Side side in Utils.straightSides)
                if (units.ContainsKey(unit.coord + side) && unit.color.IsMatchWith(units[unit.coord + side].color))
                    unit.nears.Add(units[unit.coord + side]);


        foreach (IGameMode mode in SessionInfo.current.GetModes()) {
            if (mode.IsComplete()) continue;

            foreach (Unit unit in units.Values)
                unit.potential += mode.GetAssess(Slot.all[unit.coord]);
        }



        List<Move> moves = new List<Move>();

        Func<Move, Unit, List<Move>> find = null;

        find = (Move current, Unit last) => {
            current.Add(last);
            List<Move> result = new List<Move>();
            result.Add(current);

            foreach (Unit next in last.nears) {
                if (current.units.Contains(next))
                    continue;
                if (current.color.IsMatchWith(next.color)) {
                    if (current.units.Count + 1 >= 7) {
                        Move super_move = new Move(new List<Unit>(current.units));
                        super_move.Add(next);
                        super_move.potential = units.Values.Sum(x => x.potential);
                        result.Add(super_move);
                    } else
                        result.AddRange(find(new Move(new List<Unit>(current.units)), next));
                }
            }

            return result;
        };

        foreach (Unit first in units.Values) {
            if (first.nears.Count == 0 || first.nears.Count > 2)
                continue;
            if (first.nears.Count == 2 && first.nears.Count(x => x.nears.Count == 1) > 0)
                continue;

            moves.AddRange(find(new Move(), first));
        }

        moves = moves.Unsort().ToList();

        Move best_move = null;
        foreach (Move move in moves)
            if (best_move == null || best_move.potential < move.potential)
                best_move = move;

        if (best_move != null)
            return best_move.units.Select(x => x.coord).ToList();

        return null;
    }

    class Unit {
        public ItemColor color;
        public int2 coord;
        public List<Unit> nears = new List<Unit>();
        public int potential = 1;

        public Unit(int2 coord, ItemColor color) {
            this.color = color;
            this.coord = coord;
        }

        public override string ToString() {
            return string.Format("Unit Coord:{0} Potential:{1}", coord, potential);
        }
    }

    class Move {
        public List<Unit> units;
        public int potential = 0;
        public ItemColor color = ItemColor.Universal;

        public Move(List<Unit> units) {
            this.units = units;
            foreach (var unit in units)
                potential += unit.potential;
            Unit tmp = units.Find(x => x.color.IsPhysicalColor());
            if (tmp != null)
                color = tmp.color;
        }

        public Move() {
            units = new List<Unit>();
        }

        public void Add(Unit unit) {
            units.Add(unit);
            potential += unit.potential;
            if (color.IsUniversalColor() && unit.color.IsPhysicalColor())
                color = unit.color;
        }

        public override string ToString() {
            return string.Format("Move Count:{0} Potential:{1}", units.Count, potential);
        }
    }

}
#endif





















