using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using LevelExtensionInfo = ILevelExtension.LevelExtensionInfo;
using System.IO;
using System.Xml.Linq;
using System.Collections;

[BerryPanelGroup("Content")]
[BerryPanelTab("Level Editor", "LevelEditorTabIcon", 10)]
public class LevelEditor : MetaEditor<LevelAssistant> {

    public static LevelEditor instance;

    public LevelDesignFile designFile;
    public LevelDesign design {
        get {
            return designFile == null ? null : designFile.Design;
        }
    }

    enum EditMode {
        Regular = 0,
        BigObjects = 1
    }
    EditMode editMode = EditMode.Regular;

    internal const float cellSize = 40;
    internal const float legendSize = 20;
    internal const float cellOffset = 4;
    internal static readonly Vector2 extraButtonSize = new Vector2(8, 60);
    internal int deepIndex = 0;

    public Event<SlotSettings> onSlotClick = new Event<SlotSettings>();
    public Event<SlotSettings, Rect> onSlotDraw = new Event<SlotSettings, Rect>();

    #region Styles
    static GUIStyle _labelStyle;
    public static GUIStyle labelStyle {
        get {
            if (_labelStyle == null) {
                _labelStyle = new GUIStyle(GUI.skin.button);
                _labelStyle.wordWrap = true;

                _labelStyle.normal.background = null;
                _labelStyle.focused.background = null;
                _labelStyle.active.background = null;

                _labelStyle.normal.textColor = Color.black;
                _labelStyle.focused.textColor = _labelStyle.normal.textColor;
                _labelStyle.active.textColor = _labelStyle.normal.textColor;

                _labelStyle.fontSize = 8;
                _labelStyle.margin = new RectOffset();
                _labelStyle.padding = new RectOffset();
            }
            return _labelStyle;
        }
    }

    static GUIStyle _tagStyle;
    public static GUIStyle tagStyle {
        get {
            if (_tagStyle == null) {
                _tagStyle = new GUIStyle();                
                _tagStyle.border = new RectOffset(1, 1, 1, 1);
                _tagStyle.normal.textColor = Color.white;
                _tagStyle.fontStyle = FontStyle.Bold;
                _tagStyle.alignment = TextAnchor.MiddleCenter;
                _tagStyle.fontSize = 8;
                _tagStyle.margin = new RectOffset();
                _tagStyle.padding = new RectOffset();
            }
            return _tagStyle;
        }
    }

    static GUIStyle _richLabelStyle;
    static GUIStyle richLabelStyle {
        get {
            if (_richLabelStyle == null) {
                _richLabelStyle = new GUIStyle(EditorStyles.label);
                _richLabelStyle.richText = true;
            }
            return _richLabelStyle;
        }
    }

    static GUIStyle _levelLayoutTitleStyle;
    static GUIStyle levelLayoutTitleStyle {
        get {
            if (_levelLayoutTitleStyle == null) {
                _levelLayoutTitleStyle = new GUIStyle(EditorStyles.whiteLargeLabel);
                _levelLayoutTitleStyle.alignment = TextAnchor.MiddleCenter;
            }
            return _levelLayoutTitleStyle;
        }
    }
    #endregion

    public Dictionary<int2, SlotSettings> slots = new Dictionary<int2, SlotSettings>();
    public static List<int2> selected = new List<int2>();

    GoalList goalList = null;
    ExtensionList extensionList = null;
    SelectionList selectionList = null;
    BigObjectsList bigObjectsList = null;

    GUIHelper.LayoutSplitter splitterH;
    #region Content
    static Dictionary<string, ISlotContent> slotContentInfo = new Dictionary<string, ISlotContent>();
    static Dictionary<string, ILevelGoal> goalInfo = new Dictionary<string, ILevelGoal>();
    static Dictionary<string, ILevelExtension> levelExtensionInfo = new Dictionary<string, ILevelExtension>();
    static Dictionary<string, ISlotContent> validatedContentInfo = new Dictionary<string, ISlotContent>();
    static Dictionary<string, IBlock> blockInfos;
    static Dictionary<string, IChip> chipInfos;
    static Dictionary<string, IBigObject> bigObjectInfos;
    static Dictionary<string, IBigModifier> bigModifierInfos;
    static Dictionary<string, IBigBlock> bigBlockInfos;
    static string[] chipPhysics;


    static List<ISlotContent> defaultContentInfo = new List<ISlotContent>();

    static List<LevelEditorExtension> extensions = new List<LevelEditorExtension>();
    static List<SlotTagRendererAttribute> tagRenderers = new List<SlotTagRendererAttribute>();

    static ContentEditor.Selector<CompleteBonus> bonusSelector;
    static ContentEditor.Selector<LevelRule> ruleSelector;
    #endregion

    #region Icons
    public static Texture2D slotIcon;
    public static Texture2D chipIcon;
    public static Texture2D blockIcon;
    public static Texture2D blockColorIcon;

    public static Texture2D listFolderIcon;
    public static Texture2D listLevelIcon;

    static Texture2D listChipIcon = null;
    static Texture2D listBlockIcon = null;
    static Texture2D listModifierIcon = null;
    static Texture2D listExtensionIcon = null;
    #endregion

    #region Colors
    Color missingSlotColor = new Color(1, 1, 1, .05f);
    static Color propertiesBackgroundColor = new Color(.8f, .8f, .8f, 1);
    Color selectionFaceColor = new Color(0, 1, 1, .3f);
    Color selectionEdgeColor = new Color(0, 1, 1, 1);
    Color creationFaceColor = new Color(0, .4f, .4f, .5f);
    #endregion

    public override bool Initialize() {
        if (!SessionAssistant.main) {
            Debug.LogError("SessionAssistant is missing");
            return false;
        }

        SessionAssistant.main.Initialize();

        if (!LevelAssistant.main) {
            Debug.LogError("LevelAssistant is missing");
            return false;
        }

        onSlotClick.RemoveAllListeners();
        onSlotDraw.RemoveAllListeners();

        splitterH = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, metaTarget.splitterH);
        splitterH.drawCursor = x => GUI.Box(x, "", Styles.separator);

        #region Icons
        slotIcon = EditorIcons.GetIcon("SlotIcon");
        chipIcon = EditorIcons.GetIcon("ChipIcon");
        blockIcon = EditorIcons.GetIcon("BlockIcon");
        blockColorIcon = EditorIcons.GetIcon("BlockColorIcon");
        
        listFolderIcon = EditorIcons.GetIcon("Folder");
        listLevelIcon = EditorIcons.GetIcon("LevelIcon");

        listChipIcon = EditorIcons.GetIcon("ChipIconMini");
        listBlockIcon = EditorIcons.GetIcon("BlockIconMini");
        listModifierIcon = EditorIcons.GetIcon("ModifierIconMini");
        listExtensionIcon = EditorIcons.GetIcon("ExtensionIcon");
        #endregion

        slots.Clear();
        
        ruleSelector = new ContentEditor.Selector<LevelRule>();

        slotContentInfo = Content.GetPrefabList<ISlotContent>().ToDictionary(x => x.name, x => x);
        defaultContentInfo = slotContentInfo.Values.Where(x => typeof(IDefaultSlotContent).IsAssignableFrom(x.GetType())).ToList();

        levelExtensionInfo = Content.GetPrefabList<ILevelExtension>().ToDictionary(x => x.name, x => x);
        extensions = LevelEditorExtension.GenerateEditors();
        tagRenderers = SlotTagRendererAttribute.Extract();
        chipPhysics = IChipPhysic.physics.Keys.ToArray();

        SlotGenerator.InitializeContent();

        instance = this;

        loading = Loading();

        return true;
    }

    List<LevelDesign> tlevels;

    IEnumerator loading = null;
    IEnumerator Loading() {
        LevelAssistant assistant = LevelAssistant.main;
        if (!LevelList.directory.Exists) LevelList.directory.Create();
        
        #region Migrate Levels from LevelAssistant.designs to Resource folder
        if (!Application.isPlaying && assistant != null && assistant.designs.Count > 0 && EditorUtility.DisplayDialog("New Level Storage",
            "The level editor now uses Resource folder for storing the levels. Do you want to put all your levels in this folder? If yes, all levels from the old storage (LevelAssistant.designs) will be removed.", "Yes", "No")) {
            List<LevelDesignFile> files = new List<LevelDesignFile>();
            foreach (LevelDesign design in assistant.designs) {
                LevelDesignFile file = new LevelDesignFile(design, LevelDesignFile.NewKey());
                file.Number = design.number;
                files.Add(file);
            }
            if (files.Count == assistant.designs.Count) {
                files.ForEach(x => x.Save(true, true));
                List<string> f = File.ReadAllLines(LevelList.foldersFile.FullName).ToList();
                f.AddRange(assistant.folders.Select(x => x.fullPath));
                f = f.Distinct().ToList();
                File.WriteAllText(LevelList.foldersFile.FullName, string.Join("\n", f.ToArray()));
                Undo.RecordObject(assistant, "Removing levels from old storage");
                assistant.designs.Clear();
                assistant.folders.Clear();
                EditorUtility.SetDirty(assistant);
            }
        }
        #endregion

        List<LevelDesignFile> levels = new List<LevelDesignFile>();
        foreach (FileInfo file in LevelList.directory.GetFiles()) {
            if (file.Extension != ".xml") continue;
            if (instance != this) yield break;
            if (EditorApplication.isCompiling) {
                Debug.LogError("Aaaaaa!");
                yield break;
            }
            levels.Add(new LevelDesignFile(file));
            yield return null;
        }

        levels.Sort((a, b) => a.Number.CompareTo(b.Number));
        int number = 0;
        levels.ForEach(l => l.Number = ++number);

        List<TreeFolder> folders = File.ReadAllLines(LevelList.foldersFile.FullName).Select(p => new TreeFolder() { fullPath = p }).ToList();

        levelList = new LevelList(levels, folders, assistant ? assistant.levelListState : new TreeViewState()) { levelEditor = this};
        levelList.onSelectedItemChanged += x => {
            if (x.Count == 1 && x[0] != designFile)
                SelectLevel(x[0]);
        };

        SelectLevel(lastSelectedLevel.Int);
    }

    public override void OnGUI() {
        if (loading != null) {
            GUILayout.Box("Loading...", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) {
                if (!loading.MoveNext()) loading = null;
                Repaint();
            }
            return;
        }

        if (levelList.itemCollection.Count == 0)
            levelList.AddNewItem(null, null);

        if (design == null) metaTarget.levelListShown = true;

        using (new GUIHelper.Horizontal()) {
            using (splitterH.Start(metaTarget.levelListShown, true, true)) {
                if (splitterH.Area()) {
                    GUILayout.Label("Levels List", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
                    using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandHeight(true))) {
                        metaTarget.levelListScroll = EditorGUILayout.BeginScrollView(metaTarget.levelListScroll, GUILayout.ExpandHeight(true));
                        Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.MinHeight(levelList.totalHeight + 200), GUILayout.ExpandHeight(true));
                        levelList.OnGUI(rect);
                        EditorGUILayout.EndScrollView();
                    }
                }

                using (new GUIHelper.Change(() => Vacuum(false))) {
                    if (splitterH.Area()) {
                        if (design != null) {
                            metaTarget.parametersScroll = EditorGUILayout.BeginScrollView(metaTarget.parametersScroll, GUILayout.ExpandHeight(true));

                            slots = design.slots.ToDictionary(x => x.position, x => x);

                            DrawLevelParameters();

                            using (new GUIHelper.Change(() => bigObjectsList.Highlight(selected)))
                                editMode = (EditMode) GUILayout.Toolbar((int) editMode, Enum.GetNames(typeof(EditMode)), EditorStyles.miniButton, GUILayout.ExpandWidth(true));

                            switch (editMode) {
                                case EditMode.Regular: DrawSlotSettings(); break;
                                case EditMode.BigObjects: DrawBigObjectsSettings(); break;
                            }
                            EditorGUILayout.EndScrollView();
                        } else
                            GUILayout.Box("", EditorStyles.label, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    }

                    if (splitterH.Area())
                        DrawFieldView(true, true, true);
                }
            }
        }
        if (designFile != null && designFile.dirty)
            designFile.Save(true, true);
    }

    void DrawLevelParameters() {
        if (design != null) {
            #region Navigation Panel
            using (new GUIHelper.Horizontal()) {
                if (GUILayout.Button("<<", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
                    SelectLevel(1);
                if (GUILayout.Button("<", EditorStyles.miniButtonMid, GUILayout.Width(30)))
                    SelectLevel(designFile.Number - 1);

                metaTarget.levelListShown = GUILayout.Toggle(metaTarget.levelListShown,
                    "Level #" + designFile.Number, EditorStyles.miniButtonMid, GUILayout.ExpandWidth(true));

                if (GUILayout.Button(">", EditorStyles.miniButtonMid, GUILayout.Width(30)))
                    SelectLevel(designFile.Number + 1);
                if (GUILayout.Button(">>", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                    SelectLevel(levelList.itemCollection.Max(d => d.Number));
            }
            #endregion

            #region Action Buttons
            using (new GUIHelper.Horizontal()) {
                GUILayout.FlexibleSpace();
                using (new GUIHelper.Lock(EditorApplication.isPlayingOrWillChangePlaymode))
                    using (new GUIHelper.BackgroundColor(Color.Lerp(Color.white, Color.green, 0.6f)))
                        if (GUILayout.Button("Run", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                            RunLevel();
                using (new GUIHelper.BackgroundColor(Color.Lerp(Color.white, Color.red, 0.6f)))
                    if (GUILayout.Button("Reset", EditorStyles.miniButtonRight, GUILayout.Width(50)) &&
                        EditorUtility.DisplayDialog("Reset", "Are you sure want to reset the level", "Reset", "Cancel"))
                        ResetField();
            }
            #endregion
        }

        GUILayout.Label("Level Parameters", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
        using (new GUIHelper.Vertical(Styles.area)) {
            EditorGUILayout.LabelField("Level ID", designFile.Key);

            using (new GUIHelper.Change(UpdateSelectors))
                design.type = ruleSelector.Select("Level Type", design.type);


            if (ruleSelector.Count > 0) {
                design.bonus = bonusSelector.Select("Bonus Type", design.bonus);
                using (new GUIHelper.Change(Vacuum)) {
                    design.width = Mathf.RoundToInt(EditorGUILayout.Slider("Width", design.width, LevelDesign.minSize, LevelDesign.maxSize));
                    design.height = Mathf.RoundToInt(EditorGUILayout.Slider("Height", design.height, LevelDesign.minSize, LevelDesign.maxSize));
                    design.deep = EditorGUILayout.Toggle("Deep Level", design.deep);
                    if (design.deep)
                        design.deepHeight = Mathf.RoundToInt(EditorGUILayout.Slider("Deep Height", design.deepHeight, design.height + 1, LevelDesign.maxDeepHeight));
                }
                design.colorCount = Mathf.RoundToInt(EditorGUILayout.Slider("Colors Count", 1f * design.colorCount, 2f, ItemColorUtils.physiscalColors.Count));
                design.randomizeColors = EditorGUILayout.Toggle("Random Colors", design.randomizeColors);
                design.movesCount = Mathf.RoundToInt(EditorGUILayout.Slider("Moves Count", 1f * design.movesCount, 5f, 100f));
                if (chipPhysics.Length > 0)
                    design.chipPhysic = chipPhysics.ElementAt(EditorGUILayout.Popup("Chip Physic", chipPhysics.IndexOf(design.chipPhysic), chipPhysics));
                DrawStarPanel();

                #region Goals
                using (new GUIHelper.BackgroundColor(propertiesBackgroundColor)) {
                    using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                        GUILayout.Label("Level Goals", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
                        using (new GUIHelper.Change(UpdateSelectors))
                            goalList.OnGUI();

                        EditorGUILayout.Space();

                        goalList.DrawExtension(design);
                    }
                }
                #endregion

                EditorGUILayout.Space();

                #region Level Extensions
                design.extensions.RemoveAll(x => !levelExtensionInfo.Contains(y => y.Value == x.prefab));
                using (new GUIHelper.BackgroundColor(propertiesBackgroundColor)) {
                    using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                        GUILayout.Label("Extensions", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
                        using (new GUIHelper.Change(UpdateSelectors))
                            extensionList.OnGUI();

                        EditorGUILayout.Space();

                        extensionList.DrawLevelExtension(design);
                    }
                }
                
                #endregion
            }

            EditorGUILayout.Space();
        }
    }
    
    void DrawSlotSettings() {
        GUILayout.Label("Slot Parameters", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            if (design != null) {
                #region Slot Selection Buttons
                using (new GUIHelper.Horizontal()) {
                    if (GUILayout.Button("All Slots", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(true))) {
                        selected = new List<int2>(slots.Keys);
                        OnSelectionChanged();
                    }

                    if (GUILayout.Button("Whole Field", EditorStyles.miniButtonMid, GUILayout.ExpandWidth(true))) {
                        selected.Clear();
                        for (int x = 0; x < design.width; x++)
                            for (int y = 0; y < design.height; y++)
                                selected.Add(new int2(x, y));
                        OnSelectionChanged();
                    }

                    if (GUILayout.Button("Clear", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true))) {
                        selected.Clear();
                        OnSelectionChanged();
                    }
                }
                #endregion
                EditorGUILayout.Space();
            }

            if (selected.Count > 0 && ruleSelector.Count > 0) {
                if (selectionList != null) {
                    using (new GUIHelper.Vertical(Styles.area)) {
                        #region Slot
                        using (new GUIHelper.Change(OnSelectionChanged))
                            EUtils.DrawMixedProperty(selected,
                                mask: coord => true,
                                getValue: coord => slots.ContainsKey(coord),
                                setValue: (coord, value) => {
                                    if (value && !slots.ContainsKey(coord))
                                        slots[coord] = NewSlotSettings(design, coord, true);
                                    if (!value && slots.ContainsKey(coord))
                                        slots.Remove(coord);
                                },
                                drawSingle: (position, value) => EditorGUILayout.Toggle("Active", value),
                                drawMixed: setDefault => {
                                    if (!EditorGUILayout.Toggle("Active", false)) return false;
                                    setDefault(true);
                                    return true;
                                });
                        #endregion
                        selectionList.OnGUI();
                    }
                    EditorGUILayout.Space();
                    using (new GUIHelper.Vertical(Styles.area))
                        selectionList.DrawSelected();
                }

                EditorGUILayout.Space();
            }
        }
    }

    void DrawBigObjectsSettings() {
        GUILayout.Label("Big Objects Parameters", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            if (ruleSelector.Count > 0) {
                if (bigObjectsList != null) {
                    using (new GUIHelper.Vertical(Styles.area)) 
                        bigObjectsList.OnGUI();
                    EditorGUILayout.Space();
                    using (new GUIHelper.Vertical(Styles.area))
                        bigObjectsList.DrawSelected();
                }
                EditorGUILayout.Space();
            }
        }
    }

    void UpdateSelectors() {
        if (designFile == null) return;
        goalInfo = Content.GetPrefabList<ILevelGoal>(x => !(x is ILevelRuleExclusive) || (x as ILevelRuleExclusive).IsCompatibleWith(design.type))
            .ToDictionary(x => x.name, x => x);
        design.goals.RemoveAll(x => !x.prefab || !goalInfo.Contains(y => y.Value == x.prefab));
        bonusSelector = new ContentEditor.Selector<CompleteBonus>(ValidateContent);

        validatedContentInfo = Content.GetPrefabList<ISlotContent>().Where(x => ValidateContent(x))
            .ToDictionary(x => x.name, x => x);

        blockInfos = validatedContentInfo.Where(x => x.Value is IBlock).ToDictionary(x => x.Key, x => x.Value as IBlock);
        chipInfos = validatedContentInfo.Where(x => x.Value is IChip).ToDictionary(x => x.Key, x => x.Value as IChip);
        bigObjectInfos = validatedContentInfo.Where(x => x.Value is IBigObject).ToDictionary(x => x.Key, x => x.Value as IBigObject);
        bigModifierInfos = bigObjectInfos.Where(x => x.Value is IBigModifier).ToDictionary(x => x.Key, x => x.Value as IBigModifier);
        bigBlockInfos = bigObjectInfos.Where(x => x.Value is IBigBlock).ToDictionary(x => x.Key, x => x.Value as IBigBlock);
        foreach (SlotSettings slot in design.slots)
            slot.content.RemoveAll(x => !validatedContentInfo.ContainsKey(x.name));
        design.bigObjects.RemoveAll(x => !validatedContentInfo.ContainsKey(x.content.name));

        OnSelectionChanged();
    }

    bool ValidateContent(object content) {
        return ValidateContent(content, design);
    }

    public static bool ValidateContent(object content, LevelDesign design) {
        if (content is ILevelRuleExclusive && !(content as ILevelRuleExclusive).IsCompatibleWith(design.type)) return false;
        if (content is IGoalExclusive && !goalInfo.Values.Contains(m => design.goals.Contains(x => x.prefab == m) && (content as IGoalExclusive).IsCompatibleWithGoal(m))) return false;
        return true;
    }

    void DrawStarPanel() {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent("Score Stars"));
        rect.xMin = rect2.x;

        Rect fieldRect = new Rect(rect);
        fieldRect.width /= 3;

        design.firstStarScore = Mathf.Max(EditorGUI.IntField(fieldRect, design.firstStarScore), 1);
        fieldRect.x += fieldRect.width;

        design.secondStarScore = Mathf.Max(EditorGUI.IntField(fieldRect, design.secondStarScore), design.firstStarScore + 1);
        fieldRect.x += fieldRect.width;

        design.thirdStarScore = Mathf.Max(EditorGUI.IntField(fieldRect, design.thirdStarScore), design.secondStarScore + 1);
    }

    public void SelectLevel(LevelDesignFile designFile) {
        if (designFile == null || designFile.Design == null)
            return;
        this.designFile = designFile;
        designFile.Title = design.ToString();
        levelList.Select(x => x.Number == designFile.Number);
        lastSelectedLevel.Int = designFile.Number;
        slots = design.slots.ToDictionary(x => x.position, x => x);
        deepIndex = design.deep ? design.deepHeight - design.height : 0;
        design.goals.RemoveAll(x => !x.prefab);
        design.extensions.RemoveAll(x => x == null || !x.prefab);
        if (!chipPhysics.Contains(design.chipPhysic))
            design.chipPhysic = IChipPhysic.GetName(IChipPhysic.defaultPhysic);
        goalList = new GoalList(design.goals, this);
        bigObjectsList = new BigObjectsList(design.bigObjects, this);
        extensionList = new ExtensionList(design.extensions, this);
        UpdateSelectors();
        selected.Clear();
        OnSelectionChanged();
    }

    public void SelectLevel(int number) {
        var level = levelList.itemCollection.FirstOrDefault(x => x.Number == number);
        if (level == null) level = levelList.itemCollection.FirstOrDefault();
        SelectLevel(level);
    }

    void ResetField() {
        design.bigObjects.Clear();
        area field = design.area;
        foreach (int2 coord in field.GetPoints())
            NewSlotSettings(design, coord, true);
        slots = design.slots.ToDictionary(x => x.position, x => x);
        foreach (ILevelExtension.LevelExtensionInfo extension in design.extensions) {
            extension.ClearSlots();
            extension.ClearSlots();
        }
    }

    internal void RunLevel() {
        EditorApplication.isPlaying = true;
        PlayerPrefs.SetInt("TestLevel", designFile.Number);
    }

    static SlotSettings NewSlotSettings(LevelDesign design, int2 coord, bool newSlot = false) {
        SlotSettings slot = design.slots.Find(x => x.position == coord);
        if (slot != null || newSlot) {
            int index = -1;
            if (slot == null) {
                design.slots.Add(new SlotSettings(coord));
                index = design.slots.Count - 1;
            } else
                index = design.slots.IndexOf(slot);
            slot = new SlotSettings(coord);
            foreach (ISlotContent content in defaultContentInfo) {
                IDefaultSlotContent def = content as IDefaultSlotContent;
                if (def.CanBeSetInNewSlot(design, slot)) {
                    if (content is IChip) slot.content.Add(new SlotContent(content.name, SlotContent.Type.Chip));
                    else if (content is IBlock) slot.content.Add(new SlotContent(content.name, SlotContent.Type.Block));
                    else if (content is ISlotModifier) slot.content.Add(new SlotContent(content.name, SlotContent.Type.Modifier));
                }
            }
            design.slots[index] = slot;
            return slot;
        }
        return null;
    }

    public static LevelDesign NewLevelDesign() {
        LevelDesign newLevelDesign = new LevelDesign();
        for (int x = 0; x < newLevelDesign.width; x++)
            for (int y = 0; y < newLevelDesign.width; y++)
                NewSlotSettings(newLevelDesign, new int2(x, y), true);

        ILevelGoal[] goals = Content.GetPrefabList<ILevelGoal>(x => x is IDefault).ToArray();
        if (goals.Length > 0) newLevelDesign.goals = goals.Select(x => new LevelGoalInfo(x)).ToList();
        else newLevelDesign.goals.Add(new LevelGoalInfo(goalInfo.Values.First()));
        
        LevelRule rule = Content.GetPrefab<LevelRule>(x => x is IDefault);
        newLevelDesign.type = rule;

        CompleteBonus bonus = Content.GetPrefab<CompleteBonus>(x => x is IDefault);
        newLevelDesign.bonus = bonus;

        ILevelExtension[] extensions = Content.GetPrefabList<ILevelExtension>(x => x is IDefault).ToArray();
        if (extensions.Length > 0) newLevelDesign.extensions = extensions.Select(x => new ILevelExtension.LevelExtensionInfo(x)).ToList();

        return newLevelDesign;
    }

    #region Level Selector
    public LevelList levelList;

    public static PrefVariable lastSelectedLevel = new PrefVariable("Editor_lastSelectedLevel");

    public class LevelDesignFile {
        public const string fileNameFormat = "level_{0}.xml";

        public readonly FileInfo file;
        LevelDesign design = null;

        public LevelDesignFile(FileInfo file) {
            this.file = file;
        }

        public LevelDesignFile(LevelDesign design, string key) {
            this.design = design;
            this.key = new XAttribute("key", key);
            path = new XAttribute("path", design.path ?? "");
            file = new FileInfo(System.IO.Path.Combine(LevelList.directory.FullName, fileNameFormat.FormatText(key)));
        }

        public bool IsLoaded() {
            return design != null;
        }
        public LevelDesign Design {
            get {
                if (!IsLoaded()) Load(true, false);
                return design;
            }
        }
        public string Path {
            get {
                if (path == null) Load(false, true);
                return path == null || path.Value == null ? "" : path.Value;
            }
            set {
                if (path == null) path = new XAttribute("path", value);
                else if (path.Value != value) path.Value = value;
                else return;
                Save(false, true);
            }
        }
        public string Key {
            get {
                if (key == null) Load(false, true);
                return key == null || key.Value == null ? "" : key.Value;
            }
        }
        public string Title {
            get {
                if (title == null) Load(false, true);
                return title == null || title.Value == null ? "" : title.Value;
            }
            set {
                if (title == null) title = new XAttribute("title", value);
                else if (title.Value != value) title.Value = value;
                else return;
                Save(false, true);
            }
        }
        public int Number {
            get {
                if (number == null) Load(false, true);
                int result = IsLoaded() ? design.number : 0;
                if (number != null && number.Value != null && int.TryParse(number.Value, out result))
                    return result;
                return 0;
            }
            set {
                if (number == null) Load(true, true);
                if (IsLoaded()) {
                    if (number != null) {
                        if (number.Value == value.ToString()) return;
                        number.Value = value.ToString();
                    }
                    design.number = value;
                    Save(true, true);
                }
            }
        }
        public XElement Xml {
            get {
                if (xml == null) Load(false, false);
                return xml;
            }
        }

        XElement xml = null;
        XElement xmlAttributes = null;
        XAttribute key = null;
        XAttribute path = null;
        XAttribute number = null;
        XAttribute title = null;
        internal bool dirty;

        public void Save(bool saveDesign, bool saveAttributes) {
            if (!IsLoaded()) return;
            bool save = false;
            if (xml == null || saveDesign || dirty) {
                xml = design.Serialize("level");
                number = xml.Attribute("number");
                xmlAttributes = null;
                saveAttributes = true;
                Title = design.ToString();
                save = true;
            }

            if (saveAttributes || xmlAttributes == null || dirty) {
                if (xmlAttributes == null) {
                    xmlAttributes = new XElement("editor");
                    xml.Add(xmlAttributes);
                }
                xmlAttributes.RemoveAttributes();
                xmlAttributes.Add(key);
                xmlAttributes.Add(path);
                xmlAttributes.Add(title);
                save = true;
            }

            if (save) {
                if (!file.Directory.Exists) file.Directory.Create();
                File.WriteAllText(file.FullName, xml.ToString());
            }
            dirty = false;
        }

        public void Load(bool loadDesign, bool loadAttributes) {
            if (!file.Exists) return;
            if (xml == null || loadDesign) {
                xml = XElement.Parse(File.ReadAllText(file.FullName));
                number = xml.Attribute("number");
                if (number == null) {
                    number = new XAttribute("number", "0");
                    xml.Add(number);
                }
                loadAttributes = true;
                loadDesign = true;
            }

            if (loadAttributes) {
                xmlAttributes = xml.Element("editor");
                key = xmlAttributes.Attribute("key");
                path = xmlAttributes.Attribute("path");
                title = xmlAttributes.Attribute("title");
            }

            if (loadDesign) {
                design = new LevelDesign();
                try {
                    design = LevelDesign.Deserialize(xml);
                } catch (Exception e) {
                    Debug.LogException(e);
                    Debug.Log(file.FullName);
                }
            }
        }

        public static string NewKey() {
            return Utils.GenerateKey(16);
        }

        public override bool Equals(object obj) {
            return obj is LevelDesignFile ? file.Equals((obj as LevelDesignFile).file) : false;
        }

        public override int GetHashCode() {
            return file.GetHashCode();
        }
    }

    public class LevelList : GUIHelper.HierarchyList<LevelDesignFile> {
        List<IInfo> highlighted = new List<IInfo>();
        public LevelEditor levelEditor;

        static DirectoryInfo _directory = null;
        public static DirectoryInfo directory {
            get {
                if (_directory == null || !_directory.Exists) _directory = null;
                if (_directory == null) {
                    _directory = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources"));
                    if (!directory.Exists) _directory.Create();
                        _directory = new DirectoryInfo(Path.Combine(_directory.FullName, "Levels"));
                    if (!_directory.Exists) _directory.Create();
                }
                return _directory;
            }
        }
        static FileInfo _foldersFile;
        public static FileInfo foldersFile {
            get {
                if (_foldersFile != null && !_foldersFile.Exists) _foldersFile = null;
                if (_foldersFile == null) {
                    _foldersFile = new FileInfo(Path.Combine(directory.FullName, "folders.info"));
                    if (!_foldersFile.Exists) File.WriteAllText(_foldersFile.FullName, "");
                }
                return _foldersFile;
            }
        }

        public LevelList(List<LevelDesignFile> collection, List<TreeFolder> folders, TreeViewState state) : base(collection, folders, state) {
            onChanged += UpdateNumbers;
            onChanged += SaveFolders;
            onRebuild += () => {
                if (CurrentLevel != null) {
                    IInfo info = root.Find(GetUniqueID(CurrentLevel));
                    highlighted.Clear();
                    if (info != null) {
                        while (info != root) {
                            highlighted.Add(info);
                            info = info.parent;
                        }
                    }
                }
            };
            onSelectionChanged += x => {
                if (x.Count == 1 && x[0].isItemKind) {
                    IInfo info = x[0];
                    highlighted.Clear();
                    while (info != root) {
                        highlighted.Add(info);
                        info = info.parent;
                    }
                }
            };
            onRemove += x => {
                foreach (IInfo i in x)
                    if (i.isItemKind) i.asItemKind.content.file.Delete();
                SaveFolders();
            };
        }

        void SaveFolders() {
            File.WriteAllLines(foldersFile.FullName, folderCollection.Select(x => x.fullPath).ToArray());            
        }

        public void Sync(List<LevelDesignFile> designs) {
            if (itemCollection == designs)
                return;
            itemCollection = designs;
            Reload();
            onChanged();
        }

        void UpdateNumbers() {
            int levelNumber = 0;
            root.GetAllChild().Where(x => x.isItemKind).Select(x => x.asItemKind.content).ForEach(x => x.Number = ++levelNumber);
        }

        const string newFolderNameFormat = "Group{0}";
        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {

            if (selected.Count == 0) {
                menu.AddItem(new GUIContent("New Level"), false, () => AddNewItem(root, null));
                menu.AddItem(new GUIContent("New Folder"), false, () => AddNewFolder(root, newFolderNameFormat));
                menu.AddItem(new GUIContent("Import..."), false, () => Import());
            } else {
                if (selected.Count == 1 && selected[0].isFolderKind) {
                    FolderInfo parent = selected[0].asFolderKind;

                    menu.AddItem(new GUIContent("Add New Entry"), false, () => AddNewItem(parent, null));
                    menu.AddItem(new GUIContent("Add New Folder"), false, () => AddNewItem(parent, newFolderNameFormat));
                } else {
                    FolderInfo parent = selected[0].parent;
                    if (selected.All(x => x.parent == parent)) {
                        menu.AddItem(new GUIContent("Group"), false, () => Group(selected, parent, newFolderNameFormat));
                        if (selected.All(x => x.isItemKind))
                            menu.AddItem(new GUIContent("Dublicate"), false, () => Dublicate(selected.Select(x => x.asItemKind).ToList(), parent));
                    }
                    else
                        menu.AddItem(new GUIContent("Group"), false, () => Group(selected, root, newFolderNameFormat));

                }
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
                menu.AddItem(new GUIContent("Export..."), false, () => Export(selected.ToArray()));

            }
        }

        const string levelPackExtension = "umtelevels";

        public override void OnGUI(Rect rect) {
            var t = this;
            base.OnGUI(rect);
        }

        public virtual LevelDesignFile CurrentLevel {
            get {
                return levelEditor.designFile;
            }
        }

        void Import() {
            var path = EditorUtility.OpenFilePanel(
                "Open levels pack",
                "",
                levelPackExtension);

            if (path.Length == 0)
                return;

            var xml = System.Xml.Linq.XDocument.Load(path);

            List<LevelDesign> levels = new List<LevelDesign>();

            foreach (var xLevel in xml.Root.Elements()) {
                LevelDesign level = LevelDesign.Deserialize(xLevel);
                levels.Add(level);
            }

            if (levels.Count == 0) {
                EditorUtility.DisplayDialog("Error", "No levels found", "Ok");
            } else if (EditorUtility.DisplayDialog("Import", "The levels pack contains {0} new level(s). Do you want to add them into the game?".FormatText(levels.Count), "Yes", "No")) {
                var file = new System.IO.FileInfo(path);
                string folderName = file.Name.Substring(0, file.Name.Length - levelPackExtension.Length - 1);
                for (int i = 0; true; i++) {
                    string f = folderName + " (Imported)";
                    if (i > 0) f += " " + i;
                    if (FindFolder(f) == null) {
                        folderName = f;
                        AddFolder(folderName);
                        break;
                    }
                }

                foreach (LevelDesign level in levels) {
                    level.number += 1000000;
                    LevelDesignFile ldfile = new LevelDesignFile(level, LevelDesignFile.NewKey());
                    SetPath(ldfile, folderName + "/" + level.path);
                    itemCollection.Add(ldfile);
                }

                Reload();
                onChanged();
            }
        }

        void Export(IInfo[] selection) {
            var xml = new System.Xml.Linq.XElement("levels");
            bool save = false;
            foreach (var info in selection) {
                if (!info.isItemKind) continue;
                xml.Add(info.asItemKind.content.Design.Serialize("level"));
                save = true;
            }
            if (!save) {
                EditorUtility.DisplayDialog("Error", "Nothing to export!", "Ok");
                return;
            }
            var path = EditorUtility.SaveFilePanel(
                "Save levels pack",
                "",
                "Untitle." + levelPackExtension,
                levelPackExtension);

            if (path.Length != 0)
                System.IO.File.WriteAllText(path, xml.ToString());
        }

        void Dublicate(List<ItemInfo> levels, FolderInfo folder) {
            List<LevelDesign> clones = levels.Select(x => x.content.Design.Clone()).ToList();

            string path = folder.fullPath;
            foreach (LevelDesign design in clones) {
                LevelDesignFile ldfile = new LevelDesignFile(design, LevelDesignFile.NewKey());
                SetPath(ldfile, path);
                ldfile.Save(true, true);
                itemCollection.Add(ldfile);
            }

            Reload();
            onChanged();
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            if (highlighted.Contains(info))
                Highlight(rect, true);
            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, listLevelIcon);
            _rect.x += 16;
            _rect = new Rect(_rect.x, rect.y, rect.width - rect.x + _rect.x, rect.height);
            GUI.Label(_rect, info.content.Number + ". " + info.content.Title, richLabelStyle);
        }

        public override void DrawFolder(Rect rect, FolderInfo info) {
            if (highlighted.Contains(info))
                Highlight(rect, false);
            base.DrawFolder(rect, info);
        }

        public override int GetUniqueID(LevelDesignFile element) {
            return element.Key.GetHashCode();
        }

        public override int GetUniqueID(TreeFolder element) {
            return element.GetHashCode();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public override void SetPath(LevelDesignFile element, string path) {
            element.Path = path;
        }

        public override string GetPath(LevelDesignFile element) {
            return element.Path;
        }

        public override LevelDesignFile CreateItem() {
            var file = new LevelDesignFile(NewLevelDesign(), LevelDesignFile.NewKey());
            if (!file.file.Exists) file.Save(true, true);
            return file;
        }

        public override bool CanRename(ItemInfo info) {
            return false;
        }

        public override bool CanBeChild(IInfo parent, IInfo child) {
            return parent.isFolderKind;
        }

        public void Select(Func<LevelDesignFile, bool> func) {
            List<IInfo> infos = root.GetAllChild().Where(x => x.isItemKind && func(x.asItemKind.content)).ToList();
            SetSelection(infos.Select(x => GetUniqueID(x.asItemKind.content)).ToList());
            onSelectedItemChanged(infos.Select(x => x.asItemKind.content).ToList());
            onSelectionChanged(infos);
        }

        Color hightlightFace = new Color(0, 1, 1, 0.05f);
        void Highlight(Rect rect, bool outline = false) {
            Handles.DrawSolidRectangleWithOutline(rect, hightlightFace, outline ? Color.cyan : Color.clear);
        }
    }
    #endregion

    class GoalList : GUIHelper.NonHierarchyList<LevelGoalInfo> {
        static Texture2D goalIcon = null;
        ILevelGoal newGoal = null;
        LevelGoalInfo selectedGoal = null;

        public GoalList(List<LevelGoalInfo> collection, LevelEditor editor) : base(collection, new TreeViewState()) {
            onSelectedItemChanged = x => {
                if (x.Count == 1) selectedGoal = x[0];
            };
            onChanged += editor.UpdateSelectors;
            onChanged += () => editor.designFile.dirty = true;
        }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            foreach (ILevelGoal prefab in goalInfo.Values) {
                if (itemCollection.Contains(x => x.prefab == prefab))
                    continue;
                ILevelGoal goal = prefab;
                menu.AddItem(new GUIContent("Add/" + prefab.name.NameFormat(null, "Goal", true)), false, () => {
                    newGoal = goal;
                    AddNewItem(headFolder, null);
                });
            }
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }

        public override LevelGoalInfo CreateItem() {
            if (!newGoal) return null;
            return new LevelGoalInfo(newGoal);
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            if (goalIcon == null) goalIcon = EditorIcons.GetIcon("GoalIcon");

            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, goalIcon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);

            GUI.Label(_rect, info.content.prefab.name.NameFormat(null, "Goal", true));
        }

        public override int GetUniqueID(LevelGoalInfo element) {
            return element.prefab.GetInstanceID();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public void DrawExtension(LevelDesign design) {
            if (selectedGoal == null)
                return;
            GUILayout.Label("Selected Goal Settings", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
            foreach (var e in extensions)
                if (e is GoalEditorExtension && (e as GoalEditorExtension).IsCompatibleWith(selectedGoal.prefab))
                    (e as GoalEditorExtension).Draw(design, selectedGoal);
        }
    }

    class ExtensionList : GUIHelper.NonHierarchyList<LevelExtensionInfo> {
        static Texture2D extensionIcon = null;
        ILevelExtension newExtension = null;
        LevelExtensionInfo selectedExtension = null;

        public ExtensionList(List<LevelExtensionInfo> collection, LevelEditor editor) : base(collection, new TreeViewState()) {
            onSelectedItemChanged = x => {
                if (x.Count == 1) selectedExtension = x[0];
            };
            onChanged += editor.UpdateSelectors;
            onChanged += () => editor.designFile.dirty = true;
        }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            foreach (var prefab in levelExtensionInfo.Values) {
                if (itemCollection.Contains(x => x.prefab == prefab))
                    continue;
                ILevelExtension extension = prefab;
                menu.AddItem(new GUIContent("Add/" + prefab.name.NameFormat()), false, () => {
                    newExtension = extension;
                    AddNewItem(headFolder, null);
                });
            }
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }

        public override LevelExtensionInfo CreateItem() {
            if (!newExtension) return null;
            return new LevelExtensionInfo(newExtension);
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            if (extensionIcon == null) extensionIcon = EditorIcons.GetIcon("ExtensionIcon");

            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, extensionIcon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);

            GUI.Label(_rect, info.content.prefab.name.NameFormat());
        }

        public override int GetUniqueID(LevelExtensionInfo element) {
            return element.prefab.GetInstanceID();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public void DrawLevelExtension(LevelDesign design) {
            if (selectedExtension == null)
                return;
            GUILayout.Label("Selected Extension Settings", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));

            foreach (var e in extensions)
                if (e is LevelExtensionEditorExtension && (e as LevelExtensionEditorExtension)
                    .IsCompatibleWith(selectedExtension.prefab))
                    (e as LevelExtensionEditorExtension).DrawLevelParameter(design, selectedExtension);
        }

        internal void DrawSlotExtension(LevelDesign design, Dictionary<int2, SlotSettings> slots, List<int2> selected) {
            GUILayout.Label("Extensions", Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));

            foreach (var info in levelExtensionInfo) {
                var content = design.extensions.FirstOrDefault(x => info.Value == x.prefab);
                if (content == null) continue;
                foreach (var e in extensions.Where(x => x.IsCompatibleWith(info.Value)))
                    if (e is LevelExtensionEditorExtension)
                        (e as LevelExtensionEditorExtension).DrawSlotParameter(design, slots, selected, content);
            }
            
            EditorGUILayout.Space();
        }
    }

    class SelectionList : GUIHelper.NonHierarchyList<ILiveContent> {
        LevelEditor editor = null;
        public ILiveContent selectedContent = null;
        static Dictionary<ILiveContent, Dictionary<int2, SlotContent>> references = null;
        Dictionary<int2, SlotSettings> slots;

        public SelectionList(List<int2> coord, LevelEditor editor)
            : base(ExtractContent(ExtractSlots(coord, editor), editor.design), new TreeViewState()) {
            slots = ExtractSlots(coord, editor);
            this.editor = editor;
            onSelectedItemChanged = x => {
                if (x.Count == 1) selectedContent = x[0];
            };
            onChanged += editor.UpdateSelectors;
            onChanged += () => editor.designFile.dirty = true;
        }

        static Dictionary<int2, SlotSettings> ExtractSlots(List<int2> coord, LevelEditor editor) {
            return coord.ToDictionary(x => x, x => editor.slots.Get(x)).RemoveAll(x => x.Value == null);
        }

        static List<ILiveContent> ExtractContent(Dictionary<int2, SlotSettings> slots, LevelDesign design) {
            List<ILiveContent> result = validatedContentInfo.Values.Cast<ILiveContent>().ToList();

            references = new Dictionary<ILiveContent, Dictionary<int2, SlotContent>>();

            foreach (ILiveContent prefab in result) {
                Dictionary<int2, SlotContent> content = new Dictionary<int2, SlotContent>();
                foreach (SlotSettings slot in slots.Values) {
                    SlotContent c = slot.content.FirstOrDefault(x => x.name == prefab.name);
                    if (c != null) content.Set(slot.position, c);
                }

                if (content.Count > 0)
                    references.Set(prefab, content);
            }

            result = references.Keys.ToList();

            foreach (var info in design.extensions) {
                if (extensions.Contains(e => e is LevelExtensionEditorExtension &&
                    e.IsCompatibleWith(info.prefab) && (e as LevelExtensionEditorExtension).slotEditor))
                    result.Add(info.prefab as ILiveContent);
            }

            return result;
        }

        ISlotContent newPrefab = null;
        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            List<ISlotContent> newPrefabs = validatedContentInfo.Values.ToList();
            newPrefabs.RemoveAll(x => references.ContainsKey(x) || x is IBigObject);

            selected = selected.Where(x => x.isItemKind).ToList();
            foreach (var prefab in newPrefabs) {
                ISlotContent _prefab = prefab;
                string path = SlotContent.GetContentType(prefab).ToString();
                menu.AddItem(new GUIContent("Add/{0}/{1}".FormatText(path, prefab.name.NameFormat())), false, () => {
                    newPrefab = _prefab;
                    AddNewItem(headFolder, null);
                });
            }
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        new void Remove(IInfo[] selected) {
            foreach (IInfo info in selected) 
                foreach (SlotSettings slot in slots.Values) 
                    slot.content.RemoveAll(x => x.name == info.asItemKind.content.name);
            editor.OnSelectionChanged();
            onChanged();
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args) {
            return false;
        }

        public override ILiveContent CreateItem() {
            if (!newPrefab) return null;

            var type = SlotContent.GetContentType(newPrefab);

            List<SlotSettings> slots = this.slots.Values.ToList();

            if (type != SlotContent.Type.Modifier && !this.slots.Values.Contains(x => !x.content.Contains(c => c.type == type)) &&
                EditorUtility.DisplayDialog("Replace", "Do you want to replace existing content to this one?",
                "Replace", "Cancel")) {
                slots.ForEach(s => s.content.RemoveAll(c => c.type == type));
            }

            foreach (SlotSettings slot in slots) {
                switch (type) {
                    case SlotContent.Type.Chip: {
                            if (slot.chip == null) slot.chip = new SlotContent(newPrefab.name, type);
                        } break;
                    case SlotContent.Type.Block: {
                            if (slot.block == null)
                                slot.block = new SlotContent(newPrefab.name, type);
                        } break;
                    case SlotContent.Type.Modifier: {
                            if (!slot.content.Contains(x => x.name == newPrefab.name))
                                slot.content.Add(new SlotContent(newPrefab.name, type));
                        } break;
                }
            }

            if (type == SlotContent.Type.Block) {
                foreach (SlotSettings slot in slots) {
                    if (slot.chip != null && slot.block != null &&
                        !(slotContentInfo[slot.block.name] as IBlock).CanItContainChip())
                        slot.chip = null;
                }
            }

            selectedContent = newPrefab;
            editor.OnSelectionChanged();
            return newPrefab;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);

            if (info.content is IChip) GUI.DrawTexture(_rect, listChipIcon);
            else if (info.content is IBlock) GUI.DrawTexture(_rect, listBlockIcon);
            else if (info.content is ISlotModifier) GUI.DrawTexture(_rect, listModifierIcon);
            else GUI.DrawTexture(_rect, listExtensionIcon);

            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);

            GUI.Label(_rect, info.content.name.NameFormat());
        }

        public void DrawSelected() {
            if (!selectedContent) {
                GUILayout.Label("Nothing Selected", Styles.centeredMiniLabel);
            } else {
                GUILayout.Label(selectedContent.name.NameFormat(), Styles.centeredMiniLabel);
                if (references.ContainsKey(selectedContent)) {
                    foreach (LevelEditorExtension extension in extensions)
                        if (extension is SlotEditorExtension &&
                            extension.IsCompatibleWith(selectedContent))
                            (extension as SlotEditorExtension).Draw(editor.design, references[selectedContent]);

                } else if (selectedContent is ILevelExtension) {
                    LevelExtensionInfo info = editor.design.extensions.FirstOrDefault(x => x.prefab == selectedContent);
                    if (info != null) {
                        foreach (LevelEditorExtension extension in extensions) {
                            if (extension is LevelExtensionEditorExtension &&
                                extension.IsCompatibleWith(selectedContent))
                                (extension as LevelExtensionEditorExtension)
                                    .DrawSlotParameter(editor.design, editor.slots, selected, info);
                        }
                    }
                } 
            }

            EditorGUILayout.Space();
        }

        public override int GetUniqueID(ILiveContent element) {
            return element.GetInstanceID();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public void Select(ILiveContent content) {
            selectedContent = content;
            SetSelection(new List<int>() { GetUniqueID(content) });
        }
    }

    class BigObjectsList : GUIHelper.NonHierarchyList<BigObjectSettings> {
        LevelEditor editor = null;
        ILiveContent selectedContent = null;
        Dictionary<int2, SlotContent> references = new Dictionary<int2, SlotContent>();
        public BigObjectsList(List<BigObjectSettings> collection, LevelEditor editor) : base(collection, new TreeViewState()) {
            this.editor = editor;
            onSelectedItemChanged += (List<BigObjectSettings> items) => {
                selectedContent = items.Count > 0 && items.All(i => i.content.name == items[0].content.name) ? Content.GetPrefab<ILiveContent>(items[0].content.name) : null;
                references.Clear();
                if (selectedContent)
                    foreach (BigObjectSettings item in items)
                        references.Add(item.position, item.content);
            };
            onChanged += () => editor.designFile.dirty = true;
        }

        ISlotContent newContent = null;
        public override BigObjectSettings CreateItem() {
            if (!newContent && selected.Count == 1) return null;

            BigObjectSettings result = new BigObjectSettings(selected[0]);
            result.content = new SlotContent(newContent.name, SlotContent.GetContentType(newContent as ISlotContent));

            if (newContent is IBlock) {
                foreach (int2 coord in (newContent as IBigObject).Shape().ToList()) {
                    SlotSettings slot = editor.slots.Get(result.position + coord);
                    if (!(newContent as IBlock).CanItContainChip())
                        if (slot != null)
                            slot.chip = null;
                    slot.block = null;
                }
            }

            return result;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            using (IsHighlighted(info.content) ? new GUIHelper.Color(Color.cyan) : null) {
                Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
                GUI.DrawTexture(_rect, listExtensionIcon);
                _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);
                GUI.Label(_rect, info.content.content.name.NameFormat() + " " + info.content.position, EditorStyles.label);
            }
        }

        public void DrawSelected() {
            if (selectedContent == null) {
                GUILayout.Label("Nothing Selected", Styles.centeredMiniLabel);
            } else {
                GUILayout.Label(selectedContent.name.NameFormat(), Styles.centeredMiniLabel);
                if (references.Count > 0) {
                    foreach (LevelEditorExtension extension in extensions)
                        if (extension is SlotEditorExtension &&
                            extension.IsCompatibleWith(selectedContent))
                            (extension as SlotEditorExtension).Draw(editor.design, references);

                } else if (selectedContent is ILevelExtension) {
                    LevelExtensionInfo info = editor.design.extensions.FirstOrDefault(x => x.prefab == selectedContent);
                    if (info != null) {
                        foreach (LevelEditorExtension extension in extensions) {
                            if (extension is LevelExtensionEditorExtension &&
                                extension.IsCompatibleWith(selectedContent))
                                (extension as LevelExtensionEditorExtension)
                                    .DrawSlotParameter(editor.design, editor.slots, selected, info);
                        }
                    }
                }
            }

            EditorGUILayout.Space();
        }

        List<BigObjectSettings> highlighted = new List<BigObjectSettings>();
        bool IsHighlighted(BigObjectSettings item) {
            return highlighted.Contains(item);
        }

        public void Highlight(ICollection<int2> coords) {
            highlighted.Clear();
            foreach (BigObjectSettings item in itemCollection) {
                IBigObject content = bigObjectInfos.Get(item.content.name);
                if (content == null) continue;
                foreach (int2 coord in coords) {
                    foreach (int2 sCoord in content.Shape().ToList())
                        if (sCoord + item.position == coord) {
                            highlighted.Add(item);
                            goto CONTINUE;
                        }

                }
                CONTINUE: continue;
            }
        }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            if (LevelEditor.selected.Count == 1) {
                List<IBigObject> options = new List<IBigObject>(bigObjectInfos.Values);
                editor.GetBigObjectsFromSlot(LevelEditor.selected[0]).ForEach(ob => options.RemoveAll(o => o == ob));
                foreach (var modifier in options) {
                    ISlotContent newContent = modifier as ISlotContent;
                    menu.AddItem(new GUIContent("Add/Big " + SlotContent.GetContentType(newContent) +  "/" + newContent.name), false, () => AddNew(newContent));
                }
            }
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        void AddNew(ISlotContent content) {
            newContent = content;
            AddNewItem(root, "");
        }
             
        public override int GetUniqueID(BigObjectSettings element) {
            return element.GetHashCode();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }
    }

    void OnSelectionChanged() {
        ILiveContent selectedContent = selectionList != null ? selectionList.selectedContent : null;
        selectionList = new SelectionList(selected, this);
        if (selectedContent) selectionList.Select(selectedContent);
    }
    
    internal void DrawFieldView(bool selection, bool coordinates, bool resizers) {
        Rect rect = new Rect();
        using (new GUIHelper.Vertical(Styles.levelArea)) {
            GUILayout.FlexibleSpace();
            using (new GUIHelper.Horizontal()) {
                GUILayout.FlexibleSpace();
                if (design != null)
                    rect = EditorGUILayout.GetControlRect(
                        GUILayout.Width(design.width * (cellSize + cellOffset) + legendSize + Styles.area.padding.left + Styles.area.padding.right + extraButtonSize.x * 2),
                        GUILayout.Height(design.height * (cellSize + cellOffset) + legendSize + Styles.area.padding.top + Styles.area.padding.bottom + extraButtonSize.x * 2));
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        if (Event.current.type == EventType.Layout) return;

        if (design == null) return;

        if (slotIcon == null) Initialize();

        #region Title
        Rect _rect = new Rect(rect.x, rect.y - 20, rect.width, 20);
        GUI.Label(_rect, "Level #" + designFile.Number, levelLayoutTitleStyle);
        #endregion

        #region Draw Slots
        area drawArea = new area(design.deep ? int2.up * deepIndex : int2.zero, design.fieldSize);

        DrawSlots(drawArea, rect);
        #endregion

        #region Draw Selection Outline
        if (selection) {
            if (selected.Count == 1) {
                DrawOutlinedShape(rect, selected, creationFaceColor, selectionEdgeColor);
                Vector3 center = CoordToSlotRect(selected[0], rect).center;
                Handles.color = selectionEdgeColor;
                Handles.DrawLine(center + Vector3.left * 5, center + Vector3.right * 5);
                Handles.DrawLine(center + Vector3.up * 5, center + Vector3.down * 5);
                Handles.color = Color.white;
            } else
                DrawOutlinedShape(rect, selected, selectionFaceColor, selectionEdgeColor);
        }
        #endregion

        #region Cell Coordinates
        if (coordinates) {
            for (int x = 0; x < design.width; x++)
                GUI.Box(new Rect(rect.xMin + x * (cellSize + cellOffset) + legendSize + extraButtonSize.x,
                                rect.yMin + (design.height) * (cellSize + cellOffset) + extraButtonSize.x, cellSize, legendSize), x.ToString(), Styles.centeredMiniLabelWhite);
            for (int y = 0; y < design.height; y++)
                GUI.Box(new Rect(rect.xMin + extraButtonSize.x, rect.yMin + (design.height - y - 1) * (cellSize + cellOffset) + cellOffset + extraButtonSize.x,
                    legendSize, cellSize), (y + (design.deep ? deepIndex : 0)).ToString(), Styles.centeredMiniLabelWhite);
        }
        #endregion

        #region Field Resizer
        if (resizers) {
            FieldResizeButton(new Rect(rect.xMin, rect.center.y - extraButtonSize.y / 2, extraButtonSize.x, extraButtonSize.y), Side.Left);
            FieldResizeButton(new Rect(rect.xMax - extraButtonSize.x, rect.center.y - extraButtonSize.y / 2, extraButtonSize.x, extraButtonSize.y), Side.Right);

            FieldResizeButton(new Rect(rect.center.x - extraButtonSize.y / 2, rect.yMin, extraButtonSize.y, extraButtonSize.x), Side.Top);
            FieldResizeButton(new Rect(rect.center.x - extraButtonSize.y / 2, rect.yMax - extraButtonSize.x, extraButtonSize.y, extraButtonSize.x), Side.Bottom);

            if (fieldResizerSide != Side.Null && extraSize != 0) {
                area field = new area();
                bool removeMode = false;
                switch (fieldResizerSide) {
                    case Side.Left: {
                            extraSize = Mathf.Clamp(extraSize, design.width - LevelDesign.maxSize, design.width - LevelDesign.minSize);
                            field.position.x = Mathf.Min(extraSize, 0);
                            field.position.y = 0;
                            field.size.x = Mathf.Abs(extraSize);
                            field.size.y = design.height;
                            removeMode = extraSize > 0;
                        }
                        break;
                    case Side.Right: {
                            extraSize = Mathf.Clamp(extraSize, LevelDesign.minSize - design.width, LevelDesign.maxSize - design.width);
                            field.position.x = design.width + Mathf.Min(extraSize, 0);
                            field.position.y = 0;
                            field.size.x = Mathf.Abs(extraSize);
                            field.size.y = design.height;
                            removeMode = extraSize < 0;
                        }
                        break;
                    case Side.Top: {
                            extraSize = Mathf.Clamp(extraSize, LevelDesign.minSize - design.height, LevelDesign.maxSize - design.height);
                            field.position.y = design.height + Mathf.Min(extraSize, 0);
                            field.position.x = 0;
                            field.size.y = Mathf.Abs(extraSize);
                            field.size.x = design.width;
                            removeMode = extraSize < 0;
                        }
                        break;
                    case Side.Bottom: {
                            extraSize = Mathf.Clamp(extraSize, design.height - LevelDesign.maxSize, design.height - LevelDesign.minSize);
                            field.position.y = Mathf.Min(extraSize, 0);
                            field.position.x = 0;
                            field.size.y = Mathf.Abs(extraSize);
                            field.size.x = design.width;
                            removeMode = extraSize > 0;
                        }
                        break;
                }

                Handles.color = removeMode ? Color.red : Color.green;
                foreach (int2 pos in field.GetPoints()) {
                    Handles.DrawSolidRectangleWithOutline(new Rect(rect.xMin + pos.x * (cellSize + cellOffset) + legendSize + extraButtonSize.x,
                            rect.yMin + (design.height - pos.y - 1) * (cellSize + cellOffset) + cellOffset + extraButtonSize.x,
                            cellSize, cellSize), new Color(1, 1, 1, .2f), Color.white);
                }
            }
        }
        #endregion

        #region Deep Level Scroll Bar
        if (design.deep) {
            Rect deepScrollRect = new Rect(rect.xMax + 5, rect.yMin + extraButtonSize.x + cellOffset,
                10, design.height * (cellSize + cellOffset) - cellOffset);
            deepIndex = Mathf.RoundToInt(GUI.VerticalScrollbar(deepScrollRect, deepIndex,
                design.height, design.deepHeight, 0));
        }
        #endregion
    }

    void DrawOutlinedShape(Rect fieldRect, ICollection<int2> coords, Color faceColor, Color outlineColor) {
        if (Event.current.type == EventType.Repaint) {
            if (faceColor.a <= 0 && outlineColor.a <= 0)
                return;

            Color defaultColor = Handles.color;
            float border = cellOffset / 2;

            Rect r;
            Vector3[] verts = new Vector3[4];
            List<Vector3> edges = outlineColor.a > 0 ? new List<Vector3>() : null;

            foreach (int2 coord in coords) {
                Handles.color = faceColor * defaultColor * GUI.color;
                r = CoordToSlotRect(coord, fieldRect);
                verts[0] = new Vector3(r.xMin - border, r.yMin - border);
                verts[1] = new Vector3(r.xMax + border, r.yMin - border);
                verts[2] = new Vector3(r.xMax + border, r.yMax + border);
                verts[3] = new Vector3(r.xMin - border, r.yMax + border);
                if (faceColor.a > 0f)
                    Handles.DrawAAConvexPolygon(verts);
                if (outlineColor.a > 0) {
                    if (!coords.Contains(coord + Side.Top)) {
                        edges.Add(verts[0]);
                        edges.Add(verts[1]);
                    }
                    if (!coords.Contains(coord + Side.Bottom)) {
                        edges.Add(verts[2]);
                        edges.Add(verts[3]);
                    }
                    if (!coords.Contains(coord + Side.Left)) {
                        edges.Add(verts[0]);
                        edges.Add(verts[3]);
                    }
                    if (!coords.Contains(coord + Side.Right)) {
                        edges.Add(verts[1]);
                        edges.Add(verts[2]);
                    }
                }
            }
            if (outlineColor.a > 0) {
                Handles.color = outlineColor * defaultColor * GUI.color;
                for (int i = 0; i < edges.Count; i += 2)
                    Handles.DrawLine(edges[i], edges[i+1]);
            }
            Handles.color = defaultColor;
        }
    }

    void DrawSlots(area drawArea, Rect rect) {
        foreach (int2 coord in drawArea.GetPoints()) {
            switch (DrawSlotButton(coord, CoordToSlotRect(coord, rect))) {
                case ClickType.Main: {
                        SelectionControl(coord);
                        bigObjectsList.Highlight(selected);
                    } break;
                case ClickType.Secondary: {
                        if (selected.Contains(coord)) {
                            GenericMenu menu = new GenericMenu();
                            selectionList.ContextMenu(menu, new List<GUIHelper.HierarchyList<ILiveContent, TreeFolder>.IInfo>());
                            if (selected.Count == 1)
                                bigObjectsList.ContextMenu(menu, new List<GUIHelper.HierarchyList<BigObjectSettings, TreeFolder>.IInfo>());
                            menu.AddItem(new GUIContent("Reset"), false, () => selected.ForEach(x => slots[x] = NewSlotSettings(design, x, false)));
                            menu.AddItem(new GUIContent("Clear"), false, () => selected.Where(x => slots.ContainsKey(x)).ForEach(x => slots[x].content.Clear()));
                            menu.ShowAsContext();
                        }
                    } break;
                case ClickType.None: break;
            }
        }

        List<int2> cells = new List<int2>();
        foreach (BigObjectSettings item in design.bigObjects) {
            IBigObject content = bigObjectInfos.Get(item.content.name);
            if (content == null)
                continue;
            cells = content.Shape().ToList();
            foreach (int2 coord in cells) {
                coord.x += item.position.x;
                coord.y += item.position.y;
            }
            using (new GUIHelper.Color(content.GetEditorColor()))
                DrawOutlinedShape(rect, cells, new Color(1, 1, 1, .3f), Color.white);
            GUI.Label(CoordToSlotRect(item.position, rect), (content as ISlotContent).shortName, labelStyle);
        }

    }

    void SelectionControl(int2 coord) {
        if (slots.ContainsKey(coord))
            onSlotClick.Invoke(slots[coord]);
        if (Event.current.shift && selected.Count > 0) {
            int2 start = selected.Last().GetClone();
            int2 delta = new int2();
            delta.x = start.x < coord.x ? 1 : -1;
            delta.y = start.y < coord.y ? 1 : -1;
            int2 cursor = new int2();
            for (cursor.x = start.x; cursor.x != coord.x + delta.x; cursor.x += delta.x)
                for (cursor.y = start.y; cursor.y != coord.y + delta.y; cursor.y += delta.y)
                    if (!selected.Contains(cursor))
                        selected.Add(cursor.GetClone());
        } else {
            if (!Event.current.control)
                selected.Clear();
            if (selected.Contains(coord))
                selected.Remove(coord);
            else
                selected.Add(coord);
        }
        OnSelectionChanged();
    }

    List<IBigObject> GetBigObjectsFromSlot(int2 coord) {
        List<IBigObject> result = new List<IBigObject>();
        foreach (BigObjectSettings item in design.bigObjects) {
            IBigObject content = bigObjectInfos.Get(item.content.name);
            if (content == null) continue;
            foreach (int2 sCoord in content.Shape().ToList())
                if (sCoord + item.position == coord) {
                    result.Add(content);
                    break;
                }
        }
        return result;
    }

    #region Field Resizer
    Side fieldResizerSide = Side.Null;
    int extraSize = 0;
    static readonly Color fieldResizeCursorFace = new Color(1, 1, 1, .2f);
    static readonly Color fieldResizeCursorBorder = new Color(1, 1, 1, .4f);

    void FieldResizeButton(Rect rect, Side side) {
        Handles.color = Color.white;
        Handles.DrawSolidRectangleWithOutline(rect, fieldResizeCursorFace, fieldResizeCursorBorder);

        bool horizontal = side.X() != 0;

        EditorGUIUtility.AddCursorRect(rect, horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

        if (fieldResizerSide == Side.Null && Event.current.type == EventType.mouseDown && rect.Contains(Event.current.mousePosition)) {
            fieldResizerSide = side;
            extraSize = 0;
        }

        if (fieldResizerSide == side) {
            if (Event.current.type == EventType.mouseDrag) {
                if (horizontal)
                    extraSize = Mathf.RoundToInt((Event.current.mousePosition.x - rect.center.x)/ (cellSize + cellOffset));
                else 
                    extraSize = Mathf.RoundToInt((rect.center.y - Event.current.mousePosition.y)/ (cellSize + cellOffset));
            }

            if (EditorWindow.mouseOverWindow)
                EditorWindow.mouseOverWindow.Repaint();

            if (Event.current.type == EventType.MouseUp) {
                ResizeField(fieldResizerSide, extraSize);
                fieldResizerSide = Side.Null;
            }
        }

    }

    void ResizeField(Side side, int count) {
        if (count == 0)
            return;
        bool horizontal = side.X() != 0;
        bool min = horizontal ? side.X() < 0 : side.Y() < 0;

        if (min)
            count *= -1;

        if (horizontal)
            design.width = Mathf.Clamp(design.width + count, LevelDesign.minSize, LevelDesign.maxSize);
        else
            design.height = Mathf.Clamp(design.height + count, LevelDesign.minSize, LevelDesign.maxSize);
        if (min)
            MoveField((horizontal ? int2.right : int2.up) * count);

        Vacuum();
    }

    void MoveField(int2 offset) {
        slots.ForEach(x => x.Value.position += offset);
        slots = slots.ToDictionary(x => x.Value.position, x => x.Value);
        foreach (SlotSettings slot in slots.Values) {
            foreach (SlotContent content in slot.content)
                foreach (LevelParameter parameter in content.parameters)
                    parameter.Coordinate += offset;
        }
        foreach (var extension in design.extensions)  {
            foreach (LevelParameter parameter in extension.levelParameters)
                parameter.Coordinate += offset;
            foreach (SlotExtension slot in extension) {
                slot.coord += offset;
                foreach (LevelParameter parameter in slot.parameters)
                    parameter.Coordinate += offset;
            }
            extension.Refresh();
        }
        selected = selected.Select(x => x + offset).ToList();

        Vacuum();
    }

    void Vacuum(bool newSlot) {
        area area = design.area;
        slots = slots.RemoveAll(x => !area.IsItInclude(x.Key));
        if (newSlot) {
            foreach (int2 cursor in area.GetPoints())
                if (!slots.ContainsKey(cursor)) {
                    var slot = NewSlotSettings(design, cursor, true);
                    slots.Add(slot.position, slot);
                }
        }
        design.slots = slots.Values.ToList();
        designFile.dirty = true;
    }

    void Vacuum() {
        Vacuum(false);
    }
    #endregion

    static Rect[] tagRects = new Rect[] {
        new Rect(0, 0, .25f, .25f),
        new Rect(.25f, 0, .25f, .25f),
        new Rect(.5f, 0, .25f, .25f),
        new Rect(.75f, 0, .25f, .25f),
        new Rect(0, .25f, .25f, .25f),
        new Rect(.75f, .25f, .25f, .25f),
    };

    enum ClickType { None, Main, Secondary }
    ClickType DrawSlotButton(int2 coord, Rect rect) {
        ClickType result;

        if (Event.current.type == EventType.mouseDown && rect.Contains(Event.current.mousePosition))
            result = Event.current.button == 0 ? ClickType.Main :
                Event.current.button == 1 ? ClickType.Secondary : ClickType.None;
        else result = ClickType.None;

        if (Event.current.type == EventType.Repaint) {
            using (new GUIHelper.Color(slots.ContainsKey(coord) ? Color.gray : missingSlotColor))
                GUI.DrawTexture(rect, slotIcon);

            if (slots.ContainsKey(coord)) {
                #region Draw Chip
                if (slots.ContainsKey(coord) && slots[coord].chip != null) {
                    if (chipInfos.ContainsKey(slots[coord].chip.name)) {
                        IChip info = chipInfos[slots[coord].chip.name];
                        if (info is IColored && (int) slots[coord].chip["color"].ItemColor > design.colorCount)
                            slots[coord].chip["color"].ItemColor = ItemColor.Unknown;

                        ItemColor color = slots[coord].chip["color"].ItemColor;
                        using (new GUIHelper.Color(Color.Lerp(RealColors.Get(color), Color.white, 0.4f))) {
                            GUI.DrawTexture(rect, chipIcon);
                            GUI.Box(rect, info.shortName, labelStyle);
                        }
                    } else
                        slots[coord].chip = null;
                }
                #endregion

                #region Draw Custom Icons
                foreach (SlotEditorExtension extension in extensions.Where(x => x is SlotEditorExtension).Cast<SlotEditorExtension>()) {
                    foreach (SlotContent info in slots[coord].content) {
                        ISlotContent prefab = FindContent(info);
                        if (prefab && extension.IsCompatibleWith(prefab)) 
                            extension.DrawSlotIcon(rect, info);
                    }
                }

                foreach (var content in design.extensions) {
                    ILevelExtension info = content.prefab;
                    foreach (var e in extensions.Where(x => x.IsCompatibleWith(info)))
                        if (e is LevelExtensionEditorExtension)
                            (e as LevelExtensionEditorExtension).DrawSlotIcon(rect, coord, design, slots, content);
                }
                #endregion

                #region Draw Block
                if (slots[coord].block != null) {
                    if (blockInfos.ContainsKey(slots[coord].block.name)) {
                        IBlock info = blockInfos[slots[coord].block.name];
                        if (info is IColored) {
                            ItemColor color = slots[coord].block["color"].ItemColor;
                            using (new GUIHelper.Color(Color.Lerp(RealColors.Get(color), Color.white, 0.4f)))
                                GUI.DrawTexture(rect, blockColorIcon);
                        } else
                            GUI.DrawTexture(rect, blockIcon);

                        GUI.Box(new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2),
                            info.shortName + (info is ILayered ? (":" + (slots[coord].block["layer"].Int).ToString()) : ""), labelStyle);
                    } else 
                        slots[coord].block = null;
                }
                #endregion

                #region Draw Tags
                int tagIndex = 0;

                foreach (var tag in tagRenderers) {
                    if (tag.Condition(slots[coord])) {
                        Rect tagRect = new Rect(
                            rect.x + tagRects[tagIndex].x * rect.width,
                            rect.y + tagRects[tagIndex].y * rect.height,
                            rect.width * tagRects[tagIndex].width,
                            rect.height * tagRects[tagIndex].height);
                        tagIndex++;
                        DrawTag(tagRect, tag.GetSymbol(), tag.GetSymbolColor(), tag.GetBackgroundColor());
                    }
                    if (tagIndex >= tagRects.Length)
                        break;
                }
                #endregion

                onSlotDraw.Invoke(slots[coord], rect);
            }

        }

        return result;
    }

    Rect CoordToSlotRect(int2 coord, Rect fieldRect) {
        int2 pos = coord.GetClone();
        if (design.deep) pos.y -= deepIndex;
        return new Rect(fieldRect.xMin + pos.x * (cellSize + cellOffset) + legendSize + extraButtonSize.x,
                        fieldRect.yMin + (design.height - pos.y - 1) * (cellSize + cellOffset) + cellOffset + extraButtonSize.x,
                        cellSize, cellSize);
    }

    public override LevelAssistant FindTarget() {
        return LevelAssistant.main;
    }

    void DrawTag(Rect rect, char symbol, Color color, Color background) {
        Handles.DrawSolidRectangleWithOutline(rect, background, Color.clear);
        using (new GUIHelper.Color(color))
            GUI.Box(rect, symbol.ToString(), tagStyle);
    }

    ISlotContent FindContent(SlotContent info) {
        if (info == null)
            return null;
        if (slotContentInfo.ContainsKey(info.name))
            return slotContentInfo[info.name];
        return null;
    }
}