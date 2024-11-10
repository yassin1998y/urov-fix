using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.GameCore;
using System.Reflection;
using UnityEditorInternal;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor.IMGUI.Controls;
using System.Xml.Linq;
using UnityEditor.SceneManagement;

namespace Yurowm.EditorCore {
    [BerryPanelTab("Item IDs")]
    public class ItemIDEditor : MetaEditor {
        #region Patterns
        const string fileName = "ItemID.cs";
        const string defaultCode = @"// Auto Generated script. Use ""Berry Panel > Item IDs"" to edit it.
public enum ItemID {
    coin = 0,
    life = 1,
    lifeslot = 2,
    extramoves = 10,
    paint = 11,
    surprise = 12,
    ring = 13,
    needle = 100,
    flower = 101,
    shuffle = 102,
    happylife = 200
}";

        const string codeFormat = @"// Auto Generated script. Use ""Berry Panel > Item IDs"" to edit it.
public enum ItemID {
    *IDS*
}";
        #endregion
        FileInfo scriptFile;
        string code;
        Dictionary<string, int> ids = new Dictionary<string, int>();
        Regex id_format = new Regex(@"^[a-zA-Z]{1,32}$");

        public override bool Initialize() {

            scriptFile = EUtils.ProjectFiles(Application.dataPath).FirstOrDefault(x => x.Name == fileName);
            DirectoryInfo defaultDirectory = new DirectoryInfo(Path.Combine(Application.dataPath, "Generated Scriptes"));

            if (scriptFile == null || !scriptFile.Exists) {
                if (!defaultDirectory.Exists) defaultDirectory.Create();
                scriptFile = new FileInfo(Path.Combine(defaultDirectory.FullName, fileName));
                using (var stream = scriptFile.CreateText())
                    stream.Write(defaultCode);
                code = defaultCode;
            } else
                code = File.ReadAllText(scriptFile.FullName);

            if (!scriptFile.Exists) return false;

            Regex searcher = new Regex(@"^\/\/.*\s*public enum ItemID \{(?<ids>(?:\s*\w+\s*=\s*\d+,?)*)\s*\}");
            if (!searcher.IsMatch(code)) code = defaultCode;

            Match match = searcher.Match(code);
            if (match.Success) {
                ids.Clear();
                searcher = new Regex(@"(?<id>\w+)\s*=\s*(?<num>\d+)");
                foreach (Match info in searcher.Matches(match.Groups["ids"].Value))
                    ids.Set(info.Groups["id"].Value, int.Parse(info.Groups["num"].Value));

                _num = Mathf.Max(10, ids.Values.Max() + 1);
                _id = "";

                return true;
            }
            return false;
        }

        string _id = "";
        int _num = 0;

        bool changed = false;

        public override void OnGUI() {
            using (new GUIHelper.Lock(EditorApplication.isCompiling || EditorApplication.isPlaying)) {
                using (new GUIHelper.Lock(!changed)) {
                    using (new GUIHelper.Horizontal()) {
                        if (GUILayout.Button("Save", GUILayout.Width(75))) {
                            changed = false;
                            code = codeFormat.Replace("*IDS*", string.Join(",\r\n\t", ids.Select(x => string.Format("{0} = {1}", x.Key, x.Value)).ToArray()));
                            using (var stream = scriptFile.CreateText())
                                stream.Write(code);
                            AssetDatabase.Refresh(ImportAssetOptions.Default);
                        }
                        if (GUILayout.Button("Revert", GUILayout.Width(75))) {
                            changed = false;
                            Initialize();
                            return;
                        }
                    }
                }

                using (new GUIHelper.Horizontal()) {
                    GUILayout.Label("ID", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
                    GUILayout.Label("Num", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));
                }

                foreach (string key in ids.Keys) {
                    using (new GUIHelper.Horizontal()) {
                        GUILayout.Label(key, GUILayout.Width(100));
                        GUILayout.Label(ids[key].ToString(), GUILayout.Width(50));
                        if (ids[key] >= 10 && GUILayout.Button("X", GUILayout.Width(20))) {
                            _id = key;
                            _num = ids[key];
                            ids.Remove(key);
                            changed = true;
                            break;
                        }
                    }
                }

                GUILayout.Label("New:", GUILayout.Width(50));

                using (new GUIHelper.Horizontal()) {
                    _id = EditorGUILayout.TextField(_id, GUILayout.Width(100));
                    _num = EditorGUILayout.IntField(_num, GUILayout.Width(50));
                }

                if (_id == "")
                    return;

                if (!id_format.IsMatch(_id)) {
                    EditorGUILayout.HelpBox("Wrong ID format. It must contain only letters (a-z and A-Z). Length between 1 and 32.", MessageType.Error);
                    return;
                }

                if (ids.ContainsKey(_id)) {
                    EditorGUILayout.HelpBox("This ID is already existing", MessageType.Error);
                    return;
                }

                if (_num < 10 || _num >= 1000) {
                    EditorGUILayout.HelpBox("Number must be between 10 and 999", MessageType.Error);
                    return;
                }

                if (ids.ContainsValue(_num)) {
                    EditorGUILayout.HelpBox("This Number already existing", MessageType.Error);
                    return;
                }

                if (GUILayout.Button("Add", GUILayout.Width(150))) {
                    ids.Add(_id, _num);
                    ids = ids.OrderBy(x => x.Value).ToDictionary();
                    _id = "";
                    _num = ids.Values.Max() + 1;
                    GUI.FocusControl("");
                    changed = true;
                }
            }
        }
    }

    [BerryPanelTab("Item Colors")]
    public class ItemColorEditor : MetaEditor {
#region Patterns
        const string fileName = "ItemColor.cs";
        const string defaultCode = @"// Auto Generated script. Use ""Berry Panel > Item Colors"" to edit it.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemColor {
    Red = 0,
    Green = 1,
    Blue = 2,
    Yellow = 3,
    Purple = 4,
    Orange = 5,
    Unknown = 100,
    Uncolored = 101,
    Universal = 102
}

public static class RealColors {

    static Dictionary<ItemColor, Color> colors = new Dictionary<ItemColor, Color>() {
        {ItemColor.Red, new Color(1.00f, 0.50f, 0.50f, 1.00f)},
        {ItemColor.Green, new Color(0.50f, 1.00f, 0.60f, 1.00f)},
        {ItemColor.Blue, new Color(0.40f, 0.80f, 1.00f, 1.00f)},
        {ItemColor.Yellow, new Color(1.00f, 0.90f, 0.30f, 1.00f)},
        {ItemColor.Purple, new Color(0.80f, 0.40f, 1.00f, 1.00f)},
        {ItemColor.Orange, new Color(1.00f, 0.70f, 0.00f, 1.00f)}
    };

    public static Color Get(ItemColor color) {
        try {
            if (color.IsPhysicalColor())
                return colors[color];
        } catch (System.Exception) {
        }
        return Color.white;
    }
}";

        const string codeFormat = @"// Auto Generated script. Use ""Berry Panel > Item Colors"" to edit it.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ItemColor {
    *IDS*
    Unknown = 100,
    Uncolored = 101,
    Universal = 102
}

public static class RealColors {

    static Dictionary<ItemColor, Color> colors = new Dictionary<ItemColor, Color>() {
        *COLORS*
    };

    public static Color Get(ItemColor color) {
        try {
            if (color.IsPhysicalColor())
                return colors[color];
        } catch (System.Exception) {
        }
        return Color.white;
    }
}";
#endregion
        FileInfo scriptFile;
        string code;
        Dictionary<string, ColorInfo> ids = new Dictionary<string, ColorInfo>();
        Regex id_format = new Regex(@"^[a-zA-Z]{1,32}$");
        
        public override bool Initialize() {
            scriptFile = EUtils.ProjectFiles(Application.dataPath).FirstOrDefault(x => x.Name == fileName);
            DirectoryInfo defaultDirectory = new DirectoryInfo(Path.Combine(Application.dataPath, "Generated Scriptes"));

            if (scriptFile == null || !scriptFile.Exists) {
                if (!defaultDirectory.Exists) defaultDirectory.Create();
                scriptFile = new FileInfo(Path.Combine(defaultDirectory.FullName, fileName));
                using (var stream = scriptFile.CreateText())
                    stream.Write(defaultCode);
                code = defaultCode;
            } else
                code = File.ReadAllText(scriptFile.FullName);

            if (!scriptFile.Exists)
                return false;

            Regex searcher = new Regex(@"^\/\/.*\s(using\s+[A-Za-z\.0-1]+;\s)*[\s\w]*\{(?<ids>(?:\s*\w+\s*=\s*\d+,?)*)\s*\}[\s\w]*\{[\s*\w<,>=()]*\{(?<colors>(?:\s*\{[\w.,\s]*\([\d.f,\s]*\)\},?)*)\s*\}");
            if (!searcher.IsMatch(code))
                code = defaultCode;

            Match match = searcher.Match(code);
            if (match.Success) {
                ids.Clear();
                searcher = new Regex(@"(?<id>\w+)\s*=\s*(?<num>\d+)");
                foreach (Match info in searcher.Matches(match.Groups["ids"].Value)) {
                    ColorInfo color = new ColorInfo();
                    color.id = info.Groups["id"].Value;
                    color.num = int.Parse(info.Groups["num"].Value);
                    ids.Set(color.id, color);
                }


                searcher = new Regex(@"\{ItemColor\.(?<id>[\w]+),\snew\sColor\((?<r>\d\.\d\d)f,\s(?<g>\d\.\d\d)f,\s(?<b>\d\.\d\d)f,\s(?<a>\d\.\d\d)f\)\}");
                foreach (Match info in searcher.Matches(match.Groups["colors"].Value)) {
                    string id = info.Groups["id"].Value;
                    if (ids.ContainsKey(id)) {
                        ids[id].color = new Color(
                            float.Parse(info.Groups["r"].Value),
                            float.Parse(info.Groups["g"].Value),
                            float.Parse(info.Groups["b"].Value),
                            float.Parse(info.Groups["a"].Value)
                            );
                    }
                }

                ids.Where(x => !x.Value.color.HasValue).ForEach(x => x.Value.color = Color.white);

                newInfo.num = ids.Values.Where(x => x.num < 100).Max(x => x.num) + 1;
                newInfo.id = "";

                return true;
            }
            return false;
        }

        ColorInfo newInfo = new ColorInfo();

        bool changed = false;

        public override void OnGUI() {
            using (new GUIHelper.Lock(EditorApplication.isCompiling || EditorApplication.isPlaying)) {
                using (new GUIHelper.Horizontal()) {
                    using (new GUIHelper.Lock(!changed)) {
                        if (GUILayout.Button("Save", GUILayout.Width(75))) {
                            changed = false;
                            List<ColorInfo> toSave = ids.Values.Where(x => x.num >= 0 && x.num < 100).ToList();
                            code = codeFormat.Replace("*IDS*", string.Join("\r\n\t", toSave.Select(x => string.Format("{0} = {1},", x.id, x.num)).ToArray()))
                                .Replace("*COLORS*", string.Join(",\r\n\t\t", toSave.Select(x => string.Format("{{ItemColor.{0}, new Color({1:F2}f, {2:F2}f, {3:F2}f, 1.00f)}}",
                                    x.id, x.color.Value.r, x.color.Value.g, x.color.Value.b)).ToArray()));
                            using (var stream = scriptFile.CreateText())
                                stream.Write(code);
                            AssetDatabase.Refresh(ImportAssetOptions.Default);
                        }
                        if (GUILayout.Button("Revert", GUILayout.Width(75))) {
                            changed = false;
                            Initialize();
                            AssetDatabase.Refresh(ImportAssetOptions.Default);
                            return;
                        }
                    }
                    if (GUILayout.Button("Default", GUILayout.Width(80))) {
                        changed = false;
                        scriptFile.Delete();
                        Initialize();
                        AssetDatabase.Refresh(ImportAssetOptions.Default);
                        return;
                    }
                }

                using (new GUIHelper.Horizontal()) {
                    GUILayout.Label("ID", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
                    GUILayout.Label("Num", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));
                    GUILayout.Label("Color", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100));
                }

                foreach (ColorInfo info in ids.Values) {
                    using (new GUIHelper.Horizontal()) {
                        using (new GUIHelper.Lock(info.num <= 5 || info.num >= 100)) {
                            if (GUILayout.Button("X", GUILayout.Width(20))) {
                                newInfo.id = info.id;
                                newInfo.num = info.num;
                                ids.Remove(info.id);
                                changed = true;
                                break;
                            }
                        }
                        GUILayout.Label(info.id, GUILayout.Width(100));
                        GUILayout.Label(info.num.ToString(), GUILayout.Width(50));
                        if (info.num >= 0 && info.num < 100)
                            using (new GUIHelper.Change(() => changed = true))
                                info.color = ColorField(info.color.HasValue ? info.color.Value : Color.white);
                    }
                }

                GUILayout.Label("New:", GUILayout.Width(50));

                using (new GUIHelper.Horizontal()) {
                    newInfo.id = EditorGUILayout.TextField(newInfo.id, GUILayout.Width(100));
                    newInfo.num = EditorGUILayout.IntField(newInfo.num, GUILayout.Width(50));
                }

                if (newInfo.id == "")
                    return;

                if (!id_format.IsMatch(newInfo.id)) {
                    EditorGUILayout.HelpBox("Wrong ID format. It must contain only letters (a-z and A-Z). Length between 1 and 32.", MessageType.Error);
                    return;
                }

                if (ids.ContainsKey(newInfo.id)) {
                    EditorGUILayout.HelpBox("This ID is already existing", MessageType.Error);
                    return;
                }

                if (newInfo.num <= 5 || newInfo.num >= 100) {
                    EditorGUILayout.HelpBox("Number must be between 6 and 99", MessageType.Error);
                    return;
                }

                if (ids.Contains(x => x.Value.num == newInfo.num)) {
                    EditorGUILayout.HelpBox("This Number already existing", MessageType.Error);
                    return;
                }

                if (GUILayout.Button("Add", GUILayout.Width(150))) {
                    ids.Add(newInfo.id, newInfo);
                    newInfo = new ColorInfo();
                    ids = ids.OrderBy(x => x.Value.num).ToDictionary();
                    newInfo.num = ids.Values.Where(x => x.num < 100).Max(x => x.num) + 1;
                    GUI.FocusControl("");
                    changed = true;
                }
            }
        }

        private Color ColorField(Color color) {
            try {
                return EditorGUILayout.ColorField(color, GUILayout.Width(100));
            } catch (ExitGUIException) {
                return color;
            }
        }

        class ColorInfo {
            public string id = "";
            public int num = 0;
            public Color? color = null;
        }
    }

    [BerryPanelGroup("Content")]
    [BerryPanelTab("Content", "ContentTabIcon", -9999)]
    public class ContentEditor : MetaEditor<Content> {

        [InitializeOnLoadMethod]
        internal static void Start() {
            Content contentManager = Content.main;
            if (contentManager) {
                string sds = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Trim();
                contentManager.SDSymbols = string.IsNullOrEmpty(sds) ? new string[0] : sds.Split(';').Select(x => x.Trim().ToUpper()).ToArray();
            }
        } 

        ContentList contentList;

        GUIHelper.SearchPanel searchPanel;

        Vector2 contentListScroll = new Vector2();
        static bool needToBeUpdated = false;

        public override bool Initialize() {
            metaTarget.cItems.RemoveAll(x => x.item == null);

            contentList = new ContentList(metaTarget.cItems, metaTarget.categories);
            contentList.onSelectedItemChanged = s => Selection.objects = s.Select(x => x.item).ToArray();

            searchPanel = new GUIHelper.SearchPanel("");

            return metaTarget;
        }

        [MenuItem("Assets/Add To/Content")]
        public static void AddTo() {
            if (Content.main == null) {
                Debug.Log("Content manager is missing");
                return;
            }

            Content.main.Initialize();
            
            foreach (GameObject go in Selection.gameObjects)
                if (PrefabUtility.GetPrefabType(go) == PrefabType.Prefab && !Content.main.cItems.Contains(x => x.item.name == go.name))
                    Content.main.cItems.Add(new Content.Item(go));

            needToBeUpdated = true;
            Repaint();
        }

        void Sort() {
            Content.main.cItems.Sort((x, y) => x.item.name.CompareTo(y.item.name));
            contentList.MarkAsChanged();
        }

        public override void OnGUI() {
            if (!metaTarget) {
                EditorGUILayout.HelpBox("ContentAssistant is missing", MessageType.Error);
                return;
            }

            using (new GUIHelper.Horizontal(EditorStyles.toolbar)) {
                if (GUILayout.Button("New Group", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    contentList.AddNewFolder(null, ContentList.newGroupNameFormat);

                if (GUILayout.Button("Sort", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    Sort();

                searchPanel.OnGUI(x => contentList.SetSearchFilter(x), GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.HelpBox("Use drag and drop function to add new prefab to the Content manager. It can't be scene object, and it must have unique name.", MessageType.Info);

            if (needToBeUpdated) {
                contentList.MarkAsChanged();
                needToBeUpdated = false;
            }

            Undo.RecordObject(metaTarget, "");

            using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))) {
                contentListScroll = EditorGUILayout.BeginScrollView(contentListScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.MinHeight(contentList.totalHeight + 200), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                contentList.OnGUI(rect);
                EditorGUILayout.EndScrollView();
            }
        }

        public override Content FindTarget() {
            return Content.main;
        }

        class ContentList : GUIHelper.HierarchyList<Content.Item> {

            public ContentList(List<Content.Item> collection, List<TreeFolder> folders) : base(collection, folders, new TreeViewState()) {}

            internal const string newGroupNameFormat = "Folder{0}";
            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                if (selected.Count == 0) {
                    menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(null, newGroupNameFormat));
                } else {
                    if (selected.Count == 1 && selected[0].isFolderKind) {
                        FolderInfo parent = selected[0].asFolderKind;

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

            Color transparentColor = new Color(1, 1, 1, .5f);
            public override void DrawItem(Rect rect, ItemInfo info) {
                if (string.IsNullOrEmpty(searchFilter)) 
                    GUI.Label(rect, info.content.item.name);
                else 
                   GUI.Label(rect, GUIHelper.SearchPanel.Format(info.fullPath, searchFilter), GUIHelper.SearchPanel.keyItemStyle);
            }

            public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
                result = null;

                if (!(o is GameObject))
                    return false;

                if (PrefabUtility.GetPrefabType(o) == PrefabType.Prefab && !Content.main.cItems.Contains(x => x.item.name == o.name)) {
                    ItemInfo item = new ItemInfo(0, null);
                    item.content = new Content.Item(o as GameObject);
                    result = item;
                    return true;
                }

                return false;
            }

            public override string GetPath(Content.Item element) {
                return element.path;
            }

            public override void SetPath(Content.Item element, string path) {
                element.path = path;
            }

            public override string GetName(Content.Item element) {
                return element.item.name;
            }

            public override int GetUniqueID(Content.Item element) {
                return element.item.GetInstanceID();
            }

            public override Content.Item CreateItem() {
                return null;
            }
        }

        public class Selector : ISelector<GameObject> {

            public Selector(Func<GameObject, bool> filter = null) : base(filter) {}

            protected override GameObject GetValue(GameObject gameObject) {
                return gameObject;
            }
        }

        public class Selector<T> : ISelector<T> where T : Component {
            public Selector(Func<T, bool> filter = null) : base (filter) {}

            protected override T GetValue(GameObject gameObject) {
                return gameObject.GetComponent<T>();
            }
        }

        public abstract class ISelector<I> : IEnumerable<I> where I : UnityEngine.Object {

            Func<I, bool> filter = null;
            List<I> values = new List<I>();
            string[] names;

            public ISelector(Func<I, bool> filter = null) {
                this.filter = filter;
                Refresh();
            }

            public void Refresh() {
                values.Clear();
                if (Content.main) {
                    if (!Content.main.IsInitialized)
                        Content.main.Initialize();
                    foreach (Content.Item item in Content.main.cItems) {
                        I o = GetValue(item.item);
                        if (o == null) continue;
                        if (filter != null && !filter(o)) continue;
                        values.Add(o);
                    }
                    values.Sort((x, y) => x.name.CompareTo(y.name));
                    names = values.Select(x => x.name).ToArray();
                }
            }

            protected abstract I GetValue(GameObject gameObject);

            public I Select(string label, I current, params GUILayoutOption[] options) {
                if (!Content.main) {
                    EditorGUILayout.LabelField(label, "Content provider is missing", options);
                    return null;
                }

                if (values.Count == 0) {
                    EditorGUILayout.LabelField(label, "There isn't any options", options);
                    return null;
                }

                int index = Mathf.Max(0, values.IndexOf(current));
                return values[EditorGUILayout.Popup(label, index, names, options)];
            }

            public IEnumerator<I> GetEnumerator() {
                foreach (I value in values)
                    yield return value;
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public I this[int i] {
                get {
                    if (i >= 0 && i < values.Count)
                        return values[i];
                    return null;
                }
            }

            public I this[string name] {
                get {
                    return this[names.IndexOf(name)];
                }
            }

            public int Count {
                get {
                    return values.Count;
                }
            }

            public bool Contains(I item) {
                return values.Contains(item);
            }

            public bool Contains(int index) {
                return index >= 0 && index < Count;
            }

            public bool Contains(string name) {
                return names.Contains(name);
            }

            public bool Contains(Func<I, bool> match) {
                return values.Contains(match);
            }
        }
    }

    [CustomPropertyDrawer(typeof(SortingLayerAndOrder))]
    public class SortingLayerProperty : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.hasMultipleDifferentValues)
                return;

            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect rect = new Rect(position);
            Rect rect2 = EditorGUI.PrefixLabel(rect, label);
            rect.xMin = rect2.x;

            rect.width /= 2;

            string[] layerNames = GetSortingLayerNames();
            List<int> layerIDs = GetSortingLayerUniqueIDs().ToList();
            int currentLayerID = property.FindPropertyRelative("layerID").intValue;
            int id = Mathf.Max(0, layerIDs.IndexOf(currentLayerID));

            int new_id = EditorGUI.Popup(rect, id, layerNames);
            if (new_id != id) property.FindPropertyRelative("layerID").intValue = layerIDs[new_id];
            rect.x += rect.width;

            property.FindPropertyRelative("order").intValue = EditorGUI.IntField(rect, property.FindPropertyRelative("order").intValue);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public static string[] GetSortingLayerNames() {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[]) sortingLayersProperty.GetValue(null, new object[0]);
        }

        public static int[] GetSortingLayerUniqueIDs() {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[]) sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        }

        public static void DrawSortingLayerAndOrder(string name, SortingLayerAndOrder sorting) {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent(name));
            rect.xMin = rect2.x;

            Rect fieldRect = new Rect(rect);
            fieldRect.width /= 2;

            string[] layerNames = GetSortingLayerNames();
            List<int> layerIDs = GetSortingLayerUniqueIDs().ToList();
            int id = Mathf.Max(0, layerIDs.IndexOf(sorting.layerID));
            sorting.layerID = layerIDs.Get(EditorGUI.Popup(fieldRect, id, layerNames));
            fieldRect.x += fieldRect.width;

            sorting.order = EditorGUI.IntField(fieldRect, sorting.order);
        }
    }

    [CustomPropertyDrawer(typeof(IntRange))]
    [CustomPropertyDrawer(typeof(FloatRange))]
    public class RangeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect minRect = new Rect(position.x, position.y, position.width / 2, position.height);
            Rect maxRect = new Rect(minRect.x + minRect.width, position.y, minRect.width, position.height);

            EditorGUI.PropertyField(minRect, property.FindPropertyRelative("min"), GUIContent.none);
            EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("max"), GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(int2))]
    public class int2Property : PropertyDrawer {
        List<string> pathes = new List<string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            Rect xl = new Rect(position.x, position.y, 12, position.height);
            Rect x = new Rect(xl.xMax, position.y, position.width / 2 - xl.width, position.height);

            Rect yl = new Rect(x.xMax, position.y, 12, position.height);
            Rect y = new Rect(yl.xMax, position.y, position.width / 2 - xl.width, position.height);

            // Draw fields - passs GUIContent.none to each so they are drawn without labels
            GUI.Label(xl, "X");
            EditorGUI.PropertyField(x, property.FindPropertyRelative("x"), GUIContent.none);
            GUI.Label(yl, "Y");
            EditorGUI.PropertyField(y, property.FindPropertyRelative("y"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(ContentSelector))]
    public class ContentSelectorProperty : PropertyDrawer {

        const int buttonSize = 20;

        List<Component> values;
        Type targetType = null;

        string[] names;

        public void Refresh(SerializedProperty property) {
            values = new List<Component>();
            if (Content.main) {
                Type componentType = (typeof(Component));
                Type type = (attribute as ContentSelector).targetType;
                targetType = fieldInfo.FieldType;
                if (!componentType.IsAssignableFrom(targetType)) {
                    var types = fieldInfo.FieldType.GetGenericArguments();
                    if (types.Length == 1) targetType = types[0];
                }
                if (!componentType.IsAssignableFrom(targetType))
                    targetType = null;

                if (targetType != null) {
                    Dictionary<Component, string> path = new Dictionary<Component, string>();
                    if (!Content.main.IsInitialized)
                        Content.main.Initialize();
                    foreach (Content.Item item in Content.main.cItems) {
                        Component component = null;
                        if (!item.item) continue;
                        if (type == null) component = item.item.GetComponent(targetType);
                        else component = item.item.GetComponents(targetType).FirstOrDefault(x => type.IsAssignableFrom(x.GetType()));
                        if (component == null) continue;
                        values.Add(component);
                        path.Set(component, (item.path.Length > 0 ? item.path + "/" : "") + item.item.name);
                    }
                    values.Sort((x, y) => x.name.CompareTo(y.name));
                    values.Insert(0, null);
                    names = values.Select(x => x ? path.Get(x) : "<Null>").ToArray();
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (values == null) Refresh(property);

            Rect popupRect = EditorGUI.PrefixLabel(position, label, property.prefabOverride ? EditorStyles.boldLabel : EditorStyles.label);
            popupRect.xMin -= property.depth * 16;

            if (!(typeof(Component)).IsAssignableFrom(targetType)) {
                EditorGUI.HelpBox(popupRect, "Wrong usage of ContentSelector", MessageType.None);
                return;
            }

            if (values.Count == 0)
                EditorGUI.HelpBox(popupRect, "No Values", MessageType.None);
            else {
                Component selected = null;

                if (property.isArray) {
                    while (property.NextVisible(true)) {

                        selected = (Component) property.objectReferenceValue;
                        if (DrawSelector(popupRect, selected, property, out selected))
                            property.objectReferenceValue = selected;

                        Rect myRect = GUILayoutUtility.GetRect(0f, buttonSize);
                    }
                } else {
                    selected = (Component) property.objectReferenceValue;
                    if (DrawSelector(popupRect, selected, property, out selected))
                        property.objectReferenceValue = selected;
                }




            }
        }

        bool DrawSelector(Rect position, Component selected, SerializedProperty property, out Component result) {
            int id = Mathf.Max(0, values.IndexOf(selected));

            if (property.hasMultipleDifferentValues) {
                id = EditorGUI.Popup(position, -1, names);
                if (id != -1) {
                    result = values[id];
                    return true;
                }
            } else {
                id = EditorGUI.Popup(position, id, names);
                result = values[id];
                return true;
            }

            result = null;
            return false;
        } 
    }
}
