using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.EditorCore;
using Yurowm.GameCore;

[BerryPanelTab("Localization", 1)]
public class LocalizationEditor : MetaEditor<LocalizationAssistant> {

    public static List<Entry> phrases = new List<Entry>();
    static List<Entry> phrases_ref = new List<Entry>();
    public SystemLanguage? language = null;
    public SystemLanguage? language_ref = null;

    Tree tree;
    LanguageList languageList;
    GUIHelper.LayoutSplitter splitter;
    GUIHelper.LayoutSplitter splitterRef;
    GUIHelper.SearchPanel searchPanel;

    public static Dictionary<SystemLanguage, Dictionary<string, string>> content;

    Entry selectedEntry = null;
    Entry refEntry = null;

    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("LocalizationAssistant is missing");
            return false;
        }

        if (language.HasValue) Refresh();

        if (language == null || (!language_ref.HasValue && LocalizationAssistant.main.languages.Count > 1)) {
            language = LocalizationAssistant.main.languages.First();
            language_ref = LocalizationAssistant.main.languages.FirstOrDefault(x => x != language);
            return Initialize();
        }

        if (searchPanel == null)
            searchPanel = new GUIHelper.SearchPanel("");

        splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, metaTarget.splitterSizes);
        splitter.drawCursor = x => GUI.Box(x, "", Styles.separator);

        splitterRef = new GUIHelper.LayoutSplitter(OrientationLine.Vertical, OrientationLine.Vertical, metaTarget.splitterRefSizes);
        splitterRef.drawCursor = splitter.drawCursor;

        languageList = new LanguageList("Languages", LocalizationAssistant.main.languages, metaTarget.languageTreeState);
        languageList.onSelectedItemChanged = x => {
            if (x.Count == 1 && x[0] != language)
                Edit(x[0], searchPanel.value);
        };
        languageList.onRebuild = () => {
            if (language.HasValue && !metaTarget.languages.Contains(language.Value)) {
                language = metaTarget.languages.First();
                Edit(language.Value);
            }
        };

        CreateTree();

        return true;
    }

    private void CreateTree() {
        tree = new Tree(phrases, metaTarget.entryTreeState);
        tree.onSelectedItemChanged = x => {
            if (x.Count == 1)
                selectedEntry = x[0];
            else
                selectedEntry = null;

            if (phrases_ref != null && selectedEntry != null) {
                refEntry = phrases_ref.FirstOrDefault(e => e.fullPath == selectedEntry.fullPath);
            } else
                refEntry = null;
        };
        tree.onChanged = Save;
        if (searchPanel != null && searchPanel.value != "")
            tree.SetSearchFilter(searchPanel.value);
        else
            tree.Reload();
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "localization changings");

        if (metaTarget.languages.Count == 0)
            metaTarget.languages.Add(SystemLanguage.English);
        
        #region Default Language
        metaTarget.use_system_language_by_default = GUILayout.Toggle(metaTarget.use_system_language_by_default, "Use system language by default");
        if (!metaTarget.use_system_language_by_default) {
            GUILayout.Space(5);
            List<string> languages = metaTarget.languages.Select(x => x.ToString()).ToList();
            int id = Mathf.Max(0, languages.IndexOf(metaTarget.default_language.ToString()));
            id = EditorGUILayout.Popup("Default", id, languages.ToArray(), GUILayout.ExpandWidth(true));
            metaTarget.default_language = (SystemLanguage) Enum.Parse(typeof(SystemLanguage), languages[id]);
        }
        #endregion

        DrawToolbar();

        using (splitter.Start(true, true)) {
            if (splitter.Area(Styles.area)) {
                languageList.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(languageList.totalHeight)));

                Handles.color = Color.white;
                Handles.DrawSolidRectangleWithOutline(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(2)), EditorGUIUtility.isProSkin ? new Color(.2f, .2f, .2f) : new Color(.64f, .64f, .64f), Color.clear);

                using (new GUIHelper.Change(Save)) 
                    tree.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)));
            }
            if (splitter.Area()) {
                if (selectedEntry != null) {
                    if (language.HasValue)
                        GUILayout.Label(language.Value.ToString(), Styles.title, GUILayout.ExpandWidth(true));
                    GUILayout.Label(selectedEntry.fullPath, Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
                    scrollEntryEditor = EditorGUILayout.BeginScrollView(scrollEntryEditor, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    using (new GUIHelper.Change(Save)) 
                        selectedEntry.value = EditorGUILayout.TextArea(selectedEntry.value, Styles.textAreaLineBreaked, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndScrollView();

                    if (refEntry != null) {
                        scrollRefEditor = EditorGUILayout.BeginScrollView(scrollRefEditor, GUILayout.MaxHeight(150), GUILayout.ExpandWidth(true));
                        EditorGUILayout.HelpBox(refEntry.value, MessageType.None, true);
                        EditorGUILayout.EndScrollView();
                    }
                } else
                    GUILayout.Label("Select an entry to edit", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }
        }

    }

    Vector2 scrollEntryEditor = Vector2.zero;
    Vector2 scrollRefEditor = Vector2.zero;
        
    void DrawToolbar() {
        using (new GUIHelper.Horizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
            List<string> languages = LocalizationAssistant.main.languages.Select(x => x.ToString()).ToList();
            using (new GUIHelper.Change(() => Initialize())) {
                int id = Mathf.Max(0, languages.IndexOf(language.ToString()));
                int _id = EditorGUILayout.Popup(id, languages.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100));
                if (id != _id)
                    language = (SystemLanguage) Enum.Parse(typeof(SystemLanguage), languages[_id]);

                if (languages.Count > 1) {
                    GUILayout.Label("Ref:", EditorStyles.toolbarButton, GUILayout.Width(30));
                    languages.RemoveAt(_id);

                    id = languages.IndexOf(language_ref.ToString());
                    _id = EditorGUILayout.Popup(id, languages.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100));
                    if (id != _id)
                        language_ref = (SystemLanguage) Enum.Parse(typeof(SystemLanguage), languages[_id]);
                }
            }

            searchPanel.OnGUI(x => tree.SetSearchFilter(x), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Refresh();

            if (GUILayout.Button("Sort", EditorStyles.toolbarButton, GUILayout.Width(50))) {
                phrases.Sort((x, y) => x.fullPath.CompareTo(y.fullPath));
                Save();
                tree.Reload();
            }

            if (GUILayout.Button("Get Missed Keys", EditorStyles.toolbarButton, GUILayout.Width(110))) {
                bool changed = false;
                foreach (string key in GetKeyList())
                    if (!phrases.Contains(x => x.fullPath == key)) {
                        phrases.Add(new Entry(key, ""));
                        changed = true;
                    }
                Type target_interface = typeof(ILocalized);
                Type monoBehaviour = typeof(MonoBehaviour);
                GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                MonoBehaviour[] assets = AssetDatabase.GetAllAssetPaths()
                    .Select(p => AssetDatabase.LoadAssetAtPath<Transform>(p)).Where(t => t)
                    .SelectMany(t => t.AndAllChild(true).SelectMany(c => 
                    c.gameObject.GetComponents<MonoBehaviour>().Where(m => m is ILocalized))).ToArray();
                foreach (var asset in assets) {
                    IEnumerator<string> keys = Utils.Collect<string>((asset as ILocalized).RequriedLocalizationKeys());
                    while (keys.MoveNext()) {
                        if (!phrases.Contains(x => x.fullPath == keys.Current)) {
                            phrases.Add(new Entry(keys.Current, ""));
                            changed = true;
                        }
                    }
                }
                foreach (Type type in (typeof(LocalizationAssistant)).Assembly.GetTypes())
                    if (type != target_interface && target_interface.IsAssignableFrom(type))
                        if (monoBehaviour.IsAssignableFrom(type)) {
                            foreach (GameObject root in roots) {
                                foreach (var comp in root.GetComponentsInChildren(type, true)) {
                                    IEnumerator<string> keys = Utils.Collect<string>((comp as ILocalized).RequriedLocalizationKeys());
                                    while (keys.MoveNext()) {
                                        if (!phrases.Contains(x => x.fullPath == keys.Current)) {
                                            phrases.Add(new Entry(keys.Current, ""));
                                            changed = true;
                                        }
                                    }
                                }
                            }
                        } else {
                            try {
                                IEnumerator<string> keys = Utils.Collect<string>((Activator.CreateInstance(type) as ILocalized).RequriedLocalizationKeys());
                                while (keys.MoveNext())
                                    if (!phrases.Contains(x => x.fullPath == keys.Current)) {
                                        phrases.Add(new Entry(keys.Current, ""));
                                        changed = true;
                                    }
                            } catch (Exception) {

                            }
                        }
                if (changed) {
                    Save();
                    Refresh();
                }

            }
        }
    }

    void Refresh() {
        LoadContent();
        phrases = content[language.Value].Select(x => new Entry(x.Key, x.Value)).ToList();
        if (metaTarget.languages.Count > 1) {
            if (!language_ref.HasValue || language_ref == language)
                language_ref = metaTarget.languages.First(x => x != language);
            phrases_ref = content[language_ref.Value].Select(x => new Entry(x.Key, x.Value)).ToList();
        } else {
            language_ref = null;
            phrases_ref = null;
        }
        CreateTree();
    }

    public static string[] GetKeyList() {
        XDocument document;
        List<string> result = new List<string>();

        foreach (SystemLanguage language in LocalizationAssistant.main.languages) {
            if (!(new FileInfo(string.Format(LocalizationAssistant.filepath, language)).Exists))
                continue;
            document = XDocument.Load(string.Format(LocalizationAssistant.filepath, language));
            foreach (XElement element in document.Element("language").Elements("phrase")) {
                XAttribute key = element.Attribute("key");
                if (key != null && !result.Contains(key.Value))
                    result.Add(key.Value);
            }
        }

        result.Sort();

        return result.ToArray();
    }

    public static void LoadContent() {
        content = null;

        if (!LocalizationAssistant.main)
            return;

        content = new Dictionary<SystemLanguage, Dictionary<string, string>>();
        foreach (SystemLanguage language in LocalizationAssistant.main.languages)
            content.Add(language, LocalizationAssistant.Load(language));
    }

    public override LocalizationAssistant FindTarget() {
        return LocalizationAssistant.main;
    }
    
    public void Save() {
        Save(language.Value);
    }

    public static void Save(SystemLanguage language) {
        XmlDocument document = new XmlDocument();

        document.LoadXml("<language></language>");
        XmlElement root = document.DocumentElement;

        content[language] = phrases.ToDictionary(x => x.fullPath, x => x.value);

        foreach (KeyValuePair<string, string> phrase in content[language]) {
            XmlElement note = document.CreateElement("phrase");
            note.SetAttribute("key", phrase.Key);
            note.SetAttribute("text", phrase.Value);
            root.AppendChild(note);
        }

        string path = string.Format(LocalizationAssistant.filepath, language);
        document.Save(path);
    }

    public static void Edit(string _search = "") {
        if (LocalizationAssistant.main && LocalizationAssistant.main.languages.Count > 0)
            Edit(LocalizationAssistant.main.languages[0], _search);
    }

    public static void Edit(SystemLanguage _language, string _search = "") {
        BerryPanel panel = BerryPanel.CreateBerryPanel();
        LocalizationEditor editor;

        panel.ShowTab();

        if (panel.CurrentEditor is LocalizationEditor)
            editor = (LocalizationEditor) panel.CurrentEditor;
        else {
            editor = new LocalizationEditor();
            panel.Show(editor);
        }
        
        editor.language = _language;
        editor.searchPanel.value = _search;
        editor.Initialize();
        Repaint();
    }

    class Tree : GUIHelper.HierarchyList<Entry> {
        public Tree(List<Entry> collection, TreeViewState state) : base(collection, null, state) {}
        const string newItemNameFormat = "id{0}";
        const string newGroupNameFormat = "group{0}";

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            if (selected.Count == 0) {
                menu.AddItem(new GUIContent("New Entry"), false, () => AddNewItem(root, newItemNameFormat));
                menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(root, newGroupNameFormat));

            } else {
                if (selected.Count == 1 && selected[0].isFolderKind) {
                    FolderInfo parent = selected[0].asFolderKind;

                    menu.AddItem(new GUIContent("Add New Entry"), false, () => AddNewItem(parent, newItemNameFormat));
                    menu.AddItem(new GUIContent("Add New Group"), false, () => AddNewFolder(parent, newGroupNameFormat));
                } else {
                    FolderInfo parent = selected[0].parent;
                    if (selected.All(x => x.parent == parent))
                        menu.AddItem(new GUIContent("Group"), false, () => Group(selected, parent, newGroupNameFormat));
                    else 
                        menu.AddItem(new GUIContent("Group"), false, () => Group(selected, root, newGroupNameFormat));
                   
                }

                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
            }
        }
        
        public override void DrawItem(Rect rect, ItemInfo info) {
            if (string.IsNullOrEmpty(searchFilter)) 
                GUI.Label(rect, info.content.name);
            else
                GUI.Label(rect, GUIHelper.SearchPanel.Format(info.content.fullPath, searchFilter), GUIHelper.SearchPanel.keyItemStyle);
        }

        public override string GetPath(Entry element) {
            return element.path;
        }

        public override int GetUniqueID(Entry element) {
            return ("/" + element.fullPath).GetHashCode();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public override void SetPath(Entry element, string path) {
            element.path = path;
        }

        protected override bool CanRename(TreeViewItem item) {
            return true;
        }

        public override void SetName(Entry element, string name) {
            element.name = name;
        }

        public override string GetName(Entry element) {
            return element.name;
        }

        public override Entry CreateItem() {
            return new Entry("", "");
        }
    }

    class LanguageList : GUIHelper.NonHierarchyList<SystemLanguage> {
        public LanguageList(string name, List<SystemLanguage> collection, TreeViewState state) : base(collection, state, name) {}

        SystemLanguage newLanguage = SystemLanguage.Unknown;
        public override SystemLanguage CreateItem() {
            return newLanguage;
        }

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            if (selected.Count == 0) {
                foreach (SystemLanguage language in Enum.GetValues(typeof(SystemLanguage))) {
                    if (itemCollection.Contains(language)) continue;
                    SystemLanguage _l = language;
                    menu.AddItem(new GUIContent("Add/" + language.ToString()), false, () => {
                        newLanguage = _l;
                        AddNewItem(headFolder, null);
                    });
                }
            }
            else
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            GUI.Label(rect, info.content.ToString());
        }

        public override int GetUniqueID(SystemLanguage element) {
            return (int) element;
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }
    }

    public class Entry {
        public Entry(string path, string value) {
                
            this.value = value;
            int sep = path.LastIndexOf('/');
            if (sep >= 0) {
                this.path = path.Substring(0, sep);
                name = path.Substring(sep + 1, path.Length - sep - 1);
            } else
                name = path;
        }

        public string value = "";
        public string name = "";
        public string path = "";
        public string fullPath {
            get {
                return (path.Length > 0 ? path + "/" : "") + name;
            }
        }
    }
}