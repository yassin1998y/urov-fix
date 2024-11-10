using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.IO;
using Yurowm.GameCore;

namespace Yurowm.EditorCore {
    public class BerryPanel : EditorWindow {
        static BerryPanel instance = null;

        #region Styles
        static bool style_IsInitialized = false;
        static GUIStyle style_Exeption;
        static GUIStyle tabButtonStyle;

        void InitializeStyles() {
            style_Exeption = new GUIStyle(GUI.skin.label);
            style_Exeption.normal.textColor = new Color(0.5f, 0, 0, 1);
            style_Exeption.alignment = TextAnchor.UpperLeft;
            style_Exeption.wordWrap = true;

            tabButtonStyle = new GUIStyle(EditorStyles.miniButton);
            tabButtonStyle.normal.textColor = Color.white;
            tabButtonStyle.active.textColor = new Color(1, .8f, .8f, 1);

            style_IsInitialized = true;
        }
        #endregion

        public BerryPanelTabAttribute editorAttribute = null;
        Color selectionColor;
        Color bgColor;

        [MenuItem("Window/Berry Panel")]
        public static BerryPanel CreateBerryPanel() {
            BerryPanel window;
            if (instance == null) {
                window = GetWindow<BerryPanel>();
                window.Show();
                window.OnEnable();
            } else {
                window = instance;
                window.Show();
            }
            return window;
        }

        void OnFocus() {
            if (current_editor != null) 
                current_editor.OnFocus();
        }

        public static void RepaintAll() {
            if (instance)
                instance.Repaint();
        }

        void OnEnable() {
            instance = this;

            Styles.Initialize();

            titleContent.text = "Berry Panel";

            LoadEditors();

            ShowFirstEditor();

            selectionColor = Color.Lerp(Color.red, Color.white, 0.7f);
            bgColor = Color.Lerp(GUI.backgroundColor, Color.black, 0.3f);
            //EditorCoroutine.start(DownloadHelpLibraryRoutine());
        }

        private void ShowFirstEditor() {
            if (!save_CurrentEditor.IsEmpty()) {
                Type _interface = typeof(IMetaEditor);
                Type _base_type = typeof(MetaEditor<>);
                string editorName = save_CurrentEditor.String;
                Type savedEditor = editors.Values.SelectMany(x => x).FirstOrDefault(x => x.FullName == editorName);
                if (_interface.IsAssignableFrom(savedEditor) && savedEditor != _interface && savedEditor != _base_type) {
                    Show((IMetaEditor) Activator.CreateInstance(savedEditor));
                    return;
                }
            }

            Type defaultEditor = editors.Values.SelectMany(x => x).FirstOrDefault(x => x.GetCustomAttributes(true).FirstOrDefault(y => y is BerryPanelDefaultAttribute) != null);
            if (defaultEditor != null)
                Show((IMetaEditor) Activator.CreateInstance(defaultEditor));
        }
                
        Dictionary<string, List<Type>> editors = new Dictionary<string, List<Type>>();
        void LoadEditors() {
            Type _interface = typeof(IMetaEditor);
            Type _base_type = typeof(MetaEditor<>);

            List<Type> types = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] assemblyTypes;
                try {
                    assemblyTypes = assembly.GetTypes();
                    foreach (Type type in assemblyTypes)
                    if (_interface.IsAssignableFrom(type) && type != _interface && type != _base_type)
                        types.Add(type);
                } catch (ReflectionTypeLoadException) { }
            }

            types.RemoveAll(x => x.GetCustomAttributes(true).FirstOrDefault(y => y is BerryPanelTabAttribute) == null);

            types.Sort((Type a, Type b) => {
                BerryPanelTabAttribute _a = (BerryPanelTabAttribute) a.GetCustomAttributes(true).FirstOrDefault(x => x is BerryPanelTabAttribute);
                BerryPanelTabAttribute _b = (BerryPanelTabAttribute) b.GetCustomAttributes(true).FirstOrDefault(x => x is BerryPanelTabAttribute);
                return _a.Priority.CompareTo(_b.Priority);
            });

            editors.Clear();
            foreach (Type editor in types) {
                BerryPanelGroupAttribute attr = (BerryPanelGroupAttribute) editor.GetCustomAttributes(true).FirstOrDefault(x => x is BerryPanelGroupAttribute);
                string group = attr != null ? attr.Group : "";
                if (!editors.ContainsKey(group))
                    editors.Add(group, new List<Type>());
                editors[group].Add(editor);
            }
        }

        Color defalutColor;
        public Vector2 editorScroll, tabsScroll = new Vector2();
        IMetaEditor current_editor = null;
        public IMetaEditor CurrentEditor {
            get {
                return current_editor;
            }
        }

        Action editorRender;
        void OnGUI() {
            if (instance != this && instance != null)
                Close();

            try {
                Styles.Update();

                if (!style_IsInitialized)
                    InitializeStyles();

                EditorGUILayout.Space();
            
                if (editorRender == null || current_editor == null) {
                    editorRender = null;
                    current_editor = null;
                }

                defalutColor = GUI.backgroundColor;
                using (new GUIHelper.Horizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))) {
                    using (new GUIHelper.Vertical(Styles.berryArea, GUILayout.Width(150), GUILayout.ExpandHeight(true))) {
                        tabsScroll = EditorGUILayout.BeginScrollView(tabsScroll);

                        DrawTabs();

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndScrollView();

                        Rect editorRect = EditorGUILayout.BeginVertical(Styles.berryArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

                        if (current_editor != null && editorRender != null) {
                            if (editorAttribute != null)
                                DrawTitle(editorAttribute.Title);
                            try {
                                if (EditorApplication.isCompiling) {
                                    GUILayout.Label("Compiling...", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                                } else {
                                    if (Event.current.type == EventType.Repaint)
                                        currectEditorRect = editorRect;
                                    editorRender.Invoke();
                                }
                            } catch (Exception e) {
                                Debug.LogException(e);
                                GUILayout.Label(e.ToString(), style_Exeption);
                            }
                        } else
                            GUILayout.Label("Nothing selected", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    
                        EditorGUILayout.EndScrollView();
                    }
                }
                GUILayout.Label(string.Format("Ultimate Match-Three v1.0, Berry Panel \nYurov Viktor Copyright 2015 - {0}", DateTime.Now.Year),
                    Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
            } catch (ArgumentException e) {
                if (!e.Message.StartsWith("Getting control") || !e.Message.StartsWith("GUILayout"))
                    Debug.LogException(e);
            }
        }

        void DrawTabs() {
            DrawTabs("");

            foreach (var group in editors)
                if (!string.IsNullOrEmpty(group.Key))
                    DrawTabs(group.Key);
        }

        void DrawTabs(string group) {
            if (editors.ContainsKey(group)) {
                if (!string.IsNullOrEmpty(group))
                    DrawTabTitle(group);

                foreach (Type editor in editors[group]) {
                    BerryPanelTabAttribute attr = (BerryPanelTabAttribute) editor.GetCustomAttributes(true).FirstOrDefault(x => x is BerryPanelTabAttribute);
                    if (attr != null && DrawTabButton(attr))
                        Show((IMetaEditor) Activator.CreateInstance(editor));
                }
            }
        }

        bool DrawTabButton(BerryPanelTabAttribute tabAttribute) {
            bool result = false;
            if (tabAttribute != null) {
                using (new GUIHelper.BackgroundColor(editorAttribute != null && editorAttribute.Match(tabAttribute) ? selectionColor : Color.white))
                    using (new GUIHelper.ContentColor(Styles.centeredMiniLabel.normal.textColor))
                        result = GUILayout.Button(new GUIContent(tabAttribute.Title, tabAttribute.Icon), tabButtonStyle, GUILayout.ExpandWidth(true));

                if (editorAttribute != null && editorAttribute.Match(tabAttribute) && editorRender == null)
                    result = true;
            }

            return result;
        }

        void DrawTabTitle(string text) {
            GUILayout.Label(text, Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));
        }

        void DrawTitle(string text) {
            GUILayout.Label(text, Styles.largeTitle, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
        }

        public static void Scroll(float position) {
            if (instance != null)
                instance.editorScroll = new Vector2(0, position);
        }

        PrefVariable save_CurrentEditor = new PrefVariable(string.Format("27990_BerryPanel_CurrentEditor"));
        public static Rect currectEditorRect = new Rect();

        public void Show(IMetaEditor editor) {
            EditorGUI.FocusTextInControl("");
            if (editor.Initialize()) {
                current_editor = editor;
                save_CurrentEditor.String = editor.GetType().FullName;

                BerryPanelTabAttribute attribute = (BerryPanelTabAttribute) editor.GetType().GetCustomAttributes(true).FirstOrDefault(x => x is BerryPanelTabAttribute);
                editorAttribute = attribute;

                editorRender = current_editor.OnGUI;
            }
        }



        public static IMetaEditor GetCurrentEditor() {
            if (instance == null)
                return null;
            return instance.current_editor;
        }

        public void Show(string editorName) {
            Type editor = editors.SelectMany(x => x.Value).FirstOrDefault(x => x.Name == editorName);
            if (editor != null)
                Show((IMetaEditor) Activator.CreateInstance(editor));
        }
    }
}
