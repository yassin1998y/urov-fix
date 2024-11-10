using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using LevelDesignFile = LevelEditor.LevelDesignFile;

[BerryPanelGroup("Content")]
[BerryPanelTab("Dialogues", "DialogueTabIcon", 10)]
public class DialogueEditor : MetaEditor<LevelAssistant> {

    public ILevelExtension.LevelExtensionInfo dialogueInfo;

    public override LevelAssistant FindTarget() {
        return LevelAssistant.main;
    }

    GUIHelper.LayoutSplitter splitterH;
    GUIHelper.LayoutSplitter splitterV;
    GUIHelper.Scroll scroll = new GUIHelper.Scroll(GUILayout.ExpandHeight(false));

    static LevelDesign design {
        get {
            return designFile == null ? null : designFile.Design;
        }
    }
    static LevelDesignFile designFile {
        get {
            return levelEditor.designFile;
        }
    }

    static LevelEditor levelEditor;
    ActionList actionList;
    
    List<ActionInfo> actions = new List<ActionInfo>();
    ActionInfo currentAction;
    ActionInfoEditor currentActionEditor;

    Dictionary<string, string> state = new Dictionary<string, string>();

    PrefVariable lastSelectedAction = new PrefVariable("Editor_lastSelectedAction");
    bool showState = true;

    public override bool Initialize() {
        if (!LevelAssistant.main) {
            Debug.LogError("LevelAssistant is missing");
            return false;
        }
        if (!Content.main) {
            Debug.LogError("Content Manager is missing");
            return false;
        }

        splitterH = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200, 200);
        splitterV = new GUIHelper.LayoutSplitter(OrientationLine.Vertical, OrientationLine.Vertical, 100);
        splitterH.drawCursor = x => GUI.Box(x, "", Styles.separator);
        splitterV.drawCursor = splitterH.drawCursor;

        levelEditor = new LevelEditor();
        if (!levelEditor.Initialize())
            return false;

        loading = Loading();

        return true;
    }

    IEnumerator loading = null;
    IEnumerator Loading() {
        LevelAssistant assistant = LevelAssistant.main;

        List<LevelDesignFile> levels = new List<LevelDesignFile>();
        Dialogue dialogue = Content.GetPrefab<Dialogue>();
        foreach (FileInfo file in DLevelList.directory.GetFiles()) {
            if (file.Extension != ".xml") continue;
            var ldFile = new LevelDesignFile(file);
            var x = ldFile.Xml.Element("extensions");
            if (x == null) continue;
            x = x.Elements("exten").FirstOrDefault(e => e.Attribute("type").Value == dialogue.name);
            if (x == null) continue;

            levels.Add(new LevelDesignFile(file));
            yield return null;
        }

        levels.Sort((a, b) => a.Number.CompareTo(b.Number));

        List<TreeFolder> folders = File.ReadAllLines(DLevelList.foldersFile.FullName).Select(p => new TreeFolder() { fullPath = p }).ToList();

        levelEditor.levelList = new DLevelList(levels, folders, assistant ? assistant.levelListState : new TreeViewState()) { levelEditor = new LevelEditor() };
        levelEditor.levelList.onSelectedItemChanged += x => {
            if (x.Count == 1 && x[0] != designFile)
                SelectDesign(x[0]);
        };

        levelEditor.levelList.onSelectedItemChanged(new List<LevelDesignFile>() { levelEditor.levelList.itemCollection.FirstOrDefault(x => x.Number == LevelEditor.lastSelectedLevel.Int) });

        if (lastSelectedAction.Int >= 0 && lastSelectedAction.Int < actions.Count)
            SelectAction(actions[lastSelectedAction.Int]);
    }

    void SelectDesign(LevelDesignFile designFile) {
        if (designFile != null) {
            levelEditor.SelectLevel(designFile);
           
            dialogueInfo = design.extensions.FirstOrDefault(x => x.prefab is Dialogue);
            actions = Dialogue.GetActionList(dialogueInfo);
            actionList = new ActionList(actions);
            actionList.onChanged += Save;
            actionList.onSelectedItemChanged += x => {
                if (x.Count == 1)
                    SelectAction(x[0]);
            };

            currentAction = actions.FirstOrDefault();
            SelectAction(currentAction);
        }
    }

    void SelectAction(ActionInfo actionInfo) {
        currentAction = actionInfo;
        if (currentAction != null) {
            lastSelectedAction.Int = actions.IndexOf(currentAction);
            currentActionEditor = ActionInfoEditor.editors.FirstOrDefault(e => e.IsCompatibleWith(currentAction));
            ActionInfoEditor.levelEditor = levelEditor;
            actionList.selected = currentAction;
            currentActionEditor.info = currentAction;
            if (currentActionEditor != null)
                currentActionEditor.Initialize();
            state.Clear();
            foreach (ActionInfo action in actions) {
                if (action == currentAction)
                    break;
                var s = action.GetState();
                while (s.MoveNext())
                    state.Set(s.Current.Key, s.Current.Value);
            }
            state = state.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public override void OnFocus() {
        base.OnFocus();
        Initialize();
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

        DrawActionBar();

        using (splitterH.Start()) {
            if (splitterH.Area(Styles.area))
                levelEditor.levelList.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
            if (splitterH.Area(Styles.area)) {
                if (actionList != null)
                    actionList.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
                else
                    GUILayout.FlexibleSpace();
            }
            if (splitterH.Area() && currentAction != null) {
                using (new GUIHelper.Change(() => designFile.dirty = true)) {
                    using (splitterV.Start(showState && state.Count > 0, true)) {
                        if (splitterV.Area(Styles.area)) {
                            using (scroll.Start())
                                foreach (var s in state)
                                    if (!string.IsNullOrEmpty(s.Value))
                                        EditorGUILayout.LabelField(s.Key, s.Value);
                        }

                        if (splitterV.Area()) {
                            if (currentActionEditor != null) {
                                currentActionEditor.state = state;
                                using (new GUIHelper.Change(Save))
                                    currentActionEditor.OnGUI();
                            }
                            else 
                                GUILayout.Label("Nothing to edit", Styles.centeredLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        }
                    }
                }
            }
            if (designFile != null && designFile.dirty)
                designFile.Save(true, true);
        }
    }

    void DrawActionBar() {
        using (new GUIHelper.Horizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {

            GUILayout.FlexibleSpace();
            showState = GUILayout.Toggle(showState, "State", EditorStyles.toolbarButton, GUILayout.Width(60));
            if (design != null) {
                using (new GUIHelper.BackgroundColor(Color.Lerp(Color.white, Color.green, 0.6f)))
                    if (!EditorApplication.isPlayingOrWillChangePlaymode && GUILayout.Button("Run", EditorStyles.toolbarButton, GUILayout.Width(50)))
                        ActionInfoEditor.levelEditor.RunLevel();
            }
        }

    }

    void Save() {
        LevelParameter parameter = Dialogue.GetActionParameter(dialogueInfo);

        XElement xml = new XElement("actions");
        foreach (ActionInfo info in actions)
            xml.Add(ActionInfo.ToXML(info));

        parameter.Text = xml.ToString();
    }

    public class DLevelList : LevelEditor.LevelList {
        static Texture2D speechIcon;
        List<IInfo> highlighted = new List<IInfo>();

        public DLevelList(List<LevelDesignFile> collection, List<TreeFolder> folders, TreeViewState state) :
            base(collection, folders, state) {}

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {}

        public override LevelDesignFile CreateItem() {
            return null;
        }

        public override void SetPath(LevelDesignFile element, string path) {}

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        Color hightlightFace = new Color(0, 1, 1, 0.05f);
        void Highlight(Rect rect, bool outline = false) {
            Handles.DrawSolidRectangleWithOutline(rect, hightlightFace, outline ? Color.cyan : Color.clear);
        }

        protected override bool CanStartDrag(CanStartDragArgs args) {
            return false;
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }
    }

    class ActionList : GUIHelper.NonHierarchyList<ActionInfo> {
        GUIStyle detailsStyle = null;

        public ActionInfo selected = null;
        const float detailsHeight = 8f;

        public override float ItemRowHeight() {
            return base.ItemRowHeight() + detailsHeight;
        }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            foreach (var pair in ActionInfo.types)
                menu.AddItem(new GUIContent("New/" + ActionInfo.GetName(pair.Value.Name)), false, () => AddNewItem(pair.Value));

            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        public ActionList(List<ActionInfo> collection) : base(collection, new TreeViewState(), null) {
            onRebuild += OnRebuild;
            OnRebuild();
            highlightFaceColor = EditorGUIUtility.isProSkin ? new Color(.1f, .1f, .1f, 1) : new Color(.9f, .9f, .9f, 1);
            oddFace = EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, .06f) : new Color(1f, 1f, 1f, .06f);
            textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        }

        public void AddNewItem(Type type) {
            string path = headFolder.fullPath;
            ActionInfo newItem = (ActionInfo) Activator.CreateInstance(type);
            SetPath(newItem, path);
            itemCollection.Add(newItem);

            Reload();
            onChanged();
        }

        public override ActionInfo CreateItem() {
            return null;
        }

        public override void OnGUI(Rect rect) {
            if (detailsStyle == null) {
                detailsStyle = new GUIStyle(Styles.miniLabel);
                detailsStyle.fontSize = 9;
                detailsStyle.normal.textColor = new Color(1, 1, 1, .6f);
                detailsStyle.padding = new RectOffset();
            }
            defaultColor = GUI.color;
            base.OnGUI(rect);
            GUI.color = defaultColor;
        }

        Color highlightFaceColor;
        Color oddFace;
        Color textColor;
        Color defaultColor;
        public override void DrawItem(Rect rect, ItemInfo info) {
            if (info.content == selected)
                Handles.DrawSolidRectangleWithOutline(rect, highlightFaceColor, colors[info.content.name]);
            else if (info.index % 2 == 0)
                Handles.DrawSolidRectangleWithOutline(rect, oddFace, Color.clear);
            rect.height -= detailsHeight;

            GUI.color = colors[info.content.name];

            if (selected == null || info.content.name != selected.name)
                GUI.color = textColor;

            GUI.Label(rect, string.Format("{0}. {1}", (info.index + 1), info.content.ToString()),
                selected != null && info.content.name == selected.name ? Styles.whiteBoldLabel : Styles.whiteLabel);


            Rect _rect = new Rect(rect.x, rect.y, rect.width, rect.height);
            _rect.y += _rect.height - 4;
            _rect.height = detailsHeight + 4;
            GUI.Label(_rect, info.content.GetDetails(), detailsStyle);
            GUI.color = defaultColor;
        }

        public override int GetUniqueID(ActionInfo element) {
            return itemCollection.IndexOf(element);
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }

        Dictionary<string, Color> colors = new Dictionary<string, Color>();
        void OnRebuild() {
            colors.Clear();
            HSBColor color = EditorGUIUtility.isProSkin ? new HSBColor(0, .4f, .95f) : new HSBColor(0, .8f, .4f);
            float h = 0;
            foreach (var pair in ActionInfo.types) {
                color.h = h;
                colors.Add(ActionInfo.GetName(pair.Value.Name), color.ToColor());
                h += 1f / ActionInfo.types.Count;
            }
        }
    }

}

public abstract class ActionInfoEditor {

    public readonly static List<ActionInfoEditor> editors;
    public static LevelEditor levelEditor;

    public ActionInfo info;
    public Dictionary<string, string> state = new Dictionary<string, string>();

    static ActionInfoEditor() {
        Type refType = typeof(ActionInfoEditor);
        editors = refType.Assembly.GetTypes().Where(x => !x.IsAbstract && refType.IsAssignableFrom(x)).
            Select(x => (ActionInfoEditor) Activator.CreateInstance(x)).ToList();
    }

    public ActionInfoEditor() {
        // Each action editor should be created by constructor without parameters
    }

    public abstract bool IsCompatibleWith(ActionInfo info);

    public abstract void OnGUI();

    public virtual void Initialize() {}
}

public abstract class ActionInfoEditor<A> : ActionInfoEditor where A : ActionInfo {
    public override bool IsCompatibleWith(ActionInfo info) {
        return info is A;
    }

    public override void OnGUI() {
        OnGUI((A) info);
    }

    public abstract void OnGUI(A info);
}

public class ActionDelayEditor : ActionInfoEditor<ActionDelay> {
    public override void OnGUI(ActionDelay info) {
        info.duration = EditorGUILayout.Slider("Duration (sec.)", info.duration, 1f, 30f);
    }
}

public class ActionAddBombEditor : ActionInfoEditor<ActionAddBomb> {
    ContentEditor.Selector<IChip> selector;
    GUIStyle bombNumber = null;

    public override void Initialize() {
        selector = new ContentEditor.Selector<IChip>();
        levelEditor.onSlotClick.AddListener(OnSlotClick);
        levelEditor.onSlotDraw.AddListener(OnSlotDraw);
    }

    void OnSlotDraw(SlotSettings slot, Rect rect) {
        int index = info.slots.IndexOf(slot.position);
        if (index >= 0) {
            Handles.DrawSolidRectangleWithOutline(rect, new Color(1, 1, 0, 0.1f), Color.yellow);
            GUI.Box(rect, index.ToString(), bombNumber);
        }
    }

    void OnSlotClick(SlotSettings slot) {
        if (info.slots.Contains(slot.position))
            info.slots.Remove(slot.position);
        else
            info.slots.Add(slot.position);
    }

    new ActionAddBomb info = null;
    public override void OnGUI(ActionAddBomb info) {
        if (bombNumber == null) {
            bombNumber = new GUIStyle(EditorStyles.boldLabel);
            bombNumber.alignment = TextAnchor.MiddleCenter;
            bombNumber.normal.textColor = Color.black;
        }
        this.info = info;
        info.prefab = selector.Select("Bomb", info.prefab);
        GUILayout.Label("Choose, where the bomb should be added", GUILayout.ExpandWidth(true));
        levelEditor.DrawFieldView(false, true, false);
    }
}

public class ActionHighlightSlotsEditor : ActionInfoEditor<ActionHighlightSlots> {
    public override void Initialize() {
        levelEditor.onSlotClick.AddListener(OnSlotClick);
        levelEditor.onSlotDraw.AddListener(OnSlotDraw);
    }

    void OnSlotDraw(SlotSettings slot, Rect rect) {
        int index = info.slots.IndexOf(slot.position);
        if (index >= 0)
            Handles.DrawSolidRectangleWithOutline(rect, new Color(1, 1, 0, 0.1f), Color.yellow);
    }

    void OnSlotClick(SlotSettings slot) {
        if (info.slots.Contains(slot.position))
            info.slots.Remove(slot.position);
        else
            info.slots.Add(slot.position);
    }

    new ActionHighlightSlots info = null;
    public override void OnGUI(ActionHighlightSlots info) {
        this.info = info;
        if (info.slots.Count == 0)
            EditorGUILayout.HelpBox("No slots selected. The previous highlight will fadeout", MessageType.Info);
        else
            info.autohide = EditorGUILayout.Toggle("Autohide", info.autohide);
        GUILayout.Label("Choose slots, that should be highlighted", GUILayout.ExpandWidth(true));
        levelEditor.DrawFieldView(false, true, false);
    }
}

public class ActionLimitInteractionEditor : ActionInfoEditor<ActionLimitInteraction> {

    public override void Initialize() {
        levelEditor.onSlotClick.AddListener(OnSlotClick);
        levelEditor.onSlotDraw.AddListener(OnSlotDraw);
    }

    void OnSlotDraw(SlotSettings slot, Rect rect) {
        if (info.disable) return;
        int index = info.slots.IndexOf(slot.position);
        if (index >= 0)
            Handles.DrawSolidRectangleWithOutline(rect, new Color(.5f, 1, 0, 0.1f), Color.green);
    }

    void OnSlotClick(SlotSettings slot) {
        if (info.slots.Contains(slot.position))
            info.slots.Remove(slot.position);
        else
            info.slots.Add(slot.position);
    }

    new ActionLimitInteraction info = null;
    public override void OnGUI(ActionLimitInteraction info) {
        this.info = info;
        info.disable = EditorGUILayout.Toggle("All slots avaliable", info.disable);

        if (info.disable)
            EditorGUILayout.HelpBox("The mode is disabled. All slots will be interactive.", MessageType.Info);
        else if (info.slots.Count == 0)
            EditorGUILayout.HelpBox("No slots selected. The level field will be locked.", MessageType.Info);

        GUILayout.Label("Choose slots, which should be avaliable for interacting.", GUILayout.ExpandWidth(true));
        using (new GUIHelper.Lock(info.disable))
            levelEditor.DrawFieldView(false, true, false);
    }
}

public class ActionHelperEditor : ActionInfoEditor<ActionHelper> {
    GUIStyle bombNumber = null;

    public override void Initialize() {
        levelEditor.onSlotClick.AddListener(OnSlotClick);
        levelEditor.onSlotDraw.AddListener(OnSlotDraw);
    }

    void OnSlotDraw(SlotSettings slot, Rect rect) {
        int index = info.slots.IndexOf(slot.position);
        if (index >= 0) {
            Handles.DrawSolidRectangleWithOutline(rect, new Color(1, 1, 0, 0.1f), Color.yellow);
            GUI.Box(rect, index.ToString(), bombNumber);
        }
    }

    void OnSlotClick(SlotSettings slot) {
        if (info.slots.Contains(slot.position))
            info.slots.Remove(slot.position);
        else
            info.slots.Add(slot.position);
    }

    new ActionHelper info = null;
    public override void OnGUI(ActionHelper info) {
        if (bombNumber == null) {
            bombNumber = new GUIStyle(EditorStyles.boldLabel);
            bombNumber.alignment = TextAnchor.MiddleCenter;
            bombNumber.normal.textColor = Color.black;
        }
        this.info = info;
        info.looping = (DialogueHelper.LoopingMode) EditorGUILayout.EnumPopup("Looping Mode", info.looping);
        info.autohide = EditorGUILayout.Toggle("Autohide", info.autohide);
        GUILayout.Label("Choose slots for helper show", GUILayout.ExpandWidth(true));
        levelEditor.DrawFieldView(false, true, false);
    }
}

public class ActionWaitNextMoveEditor : ActionInfoEditor<ActionWaitNextMove> {

    public override void OnGUI(ActionWaitNextMove info) {
        info.skipMatching = EditorGUILayout.Toggle("Skip Matching", info.skipMatching);
    }
}

public class ActionCharacterEditor : ActionInfoEditor<ActionCharacter> {
    ContentEditor.Selector<Character> selector;

    public override void Initialize() {
        selector = new ContentEditor.Selector<Character>();
        ActionCharacter info = this.info as ActionCharacter;
        if (info.prefab == null)
            info.prefab = selector.FirstOrDefault();
        if (info.prefab != null)
            info.prefab.Initialize();
    }

    public override void OnGUI(ActionCharacter info) {
        info.corner = (ActionCharacter.Corner) EditorGUILayout.EnumPopup("Corner", info.corner);
        info.type = (ActionCharacter.ActionType) EditorGUILayout.EnumPopup("Action Type", info.type);
        if (info.type != ActionCharacter.ActionType.Hide) {
            info.prefab = selector.Select("Character", info.prefab);
            if (info.prefab) {
                if (info.prefab.poses == null)
                    info.prefab.FindPoses();
                var poses = info.prefab.poses.Select(x => x.name).ToArray();
                if (poses.Length > 0) {
                    int selected = Array.IndexOf(poses, info.pose);
                    selected = EditorGUILayout.Popup("Pose", selected, poses);
                    if (selected < 0) selected = 0;
                    info.pose = poses[selected];
                }
            }
        }
    }
}

public class ActionSayEditor : ActionInfoEditor<ActionSay> {
    public override void Initialize() {
        base.Initialize();
        if (LocalizationEditor.content == null)
            LocalizationEditor.LoadContent();
    }

    public override void OnGUI(ActionSay info) {
        info.corner = (ActionCharacter.Corner) EditorGUILayout.EnumPopup("Corner", info.corner);
        info.localized = EditorGUILayout.Toggle("Localized", info.localized);
        if (info.localized) {
            if (!state.ContainsKey(ActionSay.localizationKeyFormatState)) {
                EditorGUILayout.HelpBox("Localization key path is missing. Please, add Settings action to the top of the action list. Than write a path for localization keys", 
                    MessageType.Error, false);
                return;
            }
            string keyPath = state[ActionSay.localizationKeyFormatState];
            if (string.IsNullOrEmpty(keyPath)) {
                EditorGUILayout.HelpBox("Localization key format can't be empty. Please, write a path for localization keys in the Settings action", MessageType.Error, false);
                return;
            }
            using (new GUIHelper.Change(() => info.key = info.key.Replace("/", "")))
                info.key = EditorGUILayout.TextField("Key", info.key);
            if (string.IsNullOrEmpty(info.key)) {
                EditorGUILayout.HelpBox("Key is empty", MessageType.Error,
                    false);
                return;
            }
            keyPath = string.Format(ActionSay.localizationKeyPath, keyPath, info.key);
            EditorGUILayout.LabelField(" ", keyPath);
            foreach (SystemLanguage language in LocalizationEditor.content.Keys.ToArray()) {
                using (new GUIHelper.Horizontal()) {
                    EditorGUILayout.PrefixLabel(language.ToString());
                    if (LocalizationEditor.content[language].ContainsKey(keyPath)) {
                        using (new GUIHelper.Change(() => Save(language)))
                            LocalizationEditor.content[language][keyPath] = EditorGUILayout.TextArea(LocalizationEditor.content[language][keyPath]);

                    } else if (GUILayout.Button("Add")) {
                        LocalizationEditor.content[language].Add(keyPath, "");
                        Save(language);
                    }
                }
            }

        } else {
            GUILayout.Label("Text");
            info.text = EditorGUILayout.TextArea(info.text, GUILayout.ExpandHeight(true));
        }
    }

    void Save(SystemLanguage language) {
        LocalizationEditor.phrases = LocalizationEditor.content[language].Select(x => new LocalizationEditor.Entry(x.Key, x.Value)).ToList();
        LocalizationEditor.Save(language);
    }
}

public class ActionSettingsEditor : ActionInfoEditor<ActionSettings> {
    public override void OnGUI(ActionSettings info) {
        EditorGUILayout.HelpBox("This action calls only when level created", MessageType.Info);
        info.mBooster = EditorGUILayout.Toggle("Multiple Use Boosters", info.mBooster);
        info.sBooster = EditorGUILayout.Toggle("Single Use Boosters", info.sBooster);
        info.hints = EditorGUILayout.Toggle("Show Hints", info.hints);
        info.rollback = EditorGUILayout.Toggle("Rollback on completion", info.rollback);
        using (new GUIHelper.Change(() => info.keyPath = FormatPath(info.keyPath)))
            info.keyPath = EditorGUILayout.TextField("Localization Key Path", info.keyPath);
    }

    static Regex format = new Regex(@"\/+");
    string FormatPath(string path) {
        path = format.Replace(path, "/");
        if (path.StartsWith("/")) path = path.Substring(1);
        if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
        return path;
    }
}