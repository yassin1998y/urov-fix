using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.Security.Cryptography;
using UnityEngine.UI;
using UnityEditor.IMGUI.Controls;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Xml.Linq;
using UnityEditor.SceneManagement;

[BerryPanelTab("UI", "UITabIcon", 2)]
public class UIAssistantEditor : MetaEditor<UIAssistant> {
    
    UIAssistant.Page edit = null;
    AudioEditor.TrackSelector trackSelector = new AudioEditor.TrackSelector();

    public override void OnGUI() {
        if (!metaTarget)
            return;
        
        Color defColor = GUI.color;

        #region UI Modules

        GUILayout.Label("UI Modules", GUILayout.ExpandWidth(true));
        using (new GUIHelper.Vertical()) {
			for (int i = 0; i < metaTarget.UImodules.Count; i++) {
				using (new GUIHelper.Horizontal()) {
					metaTarget.UImodules[i] = (Transform) EditorGUILayout.ObjectField(metaTarget.UImodules[i], typeof(Transform), true, GUILayout.Width(200));
					if (metaTarget.UImodules[i] == null) {
						metaTarget.UImodules.RemoveAt(i);
						break;
					} else {
						EditorGUILayout.LabelField(metaTarget.UImodules[i].GetComponentsInChildren<CPanel>(true).Length.ToString() + " panel(s)", EditorStyles.miniBoldLabel, GUILayout.Width(100));
					}
				}
			}
			Transform new_module = (Transform) EditorGUILayout.ObjectField(null, typeof(Transform), true, GUILayout.Width(150));
			if (new_module)
				metaTarget.UImodules.Add(new_module);
        }
        #endregion

        #region Pages

        GUILayout.Space(20);
        GUILayout.Label("Pages", GUILayout.ExpandWidth(true));
        metaTarget.ArraysConvertation();
        using (new GUIHelper.Vertical()) {
			using (new GUIHelper.Horizontal()) {
				GUILayout.Space(10);
				GUILayout.Label("Edit", Styles.centeredMiniLabel, GUILayout.Width(35));
				GUILayout.Label("Name", Styles.centeredMiniLabel, GUILayout.Width(200));
			}

			foreach (UIAssistant.Page page in metaTarget.pages) {
				using (new GUIHelper.Horizontal()) {
                    using (new GUIHelper.BackgroundColor(Color.red))
                        if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(20))) {
                            metaTarget.pages.Remove(page);
                            break;
                        }
                    using (new GUIHelper.BackgroundColor(Color.cyan))
                    if (GUILayout.Button("Edit", EditorStyles.miniButtonRight, GUILayout.Width(35))) {
						if (edit == page) edit = null;
						else edit = page;
                        GUI.FocusControl("");
					}
					page.name = EditorGUILayout.TextField(page.name, GUILayout.Width(200));

					UIAssistant.Page default_page = metaTarget.pages.Find(x => x.default_page);

					if (default_page == null) {
						default_page = page;
						page.default_page = true;
					}

					if (page.default_page && default_page != page)
						page.default_page = false;


					if (page.default_page)
						GUILayout.Label("DEFAULT", GUILayout.Width(80));
					else
						if (GUILayout.Button("Make default", EditorStyles.miniButton, GUILayout.Width(80))) {
						default_page.default_page = false;
						default_page = page;
						page.default_page = true;
					}

				}

				if (edit == page) {
					using (new GUIHelper.Horizontal()) {
						GUILayout.Space(40);
						using (new GUIHelper.Vertical(Styles.area, GUILayout.Width(350))) {
                            page.soundtrack = trackSelector.Select("Soundtrack", page.soundtrack);

						    string tags = string.Join(",", page.tags.ToArray());
						    using (new GUIHelper.Change(() => page.tags = tags.Split(',')))
						        tags = EditorGUILayout.TextField("Tags", tags);
						    
							using (new GUIHelper.Horizontal()) {
								page.setTimeScale = EditorGUILayout.Toggle(page.setTimeScale, GUILayout.Width(20));
								GUILayout.Label("Time Scale", GUILayout.Width(100));
								if (page.setTimeScale)
									page.timeScale = EditorGUILayout.Slider(page.timeScale, 0, 1, GUILayout.Width(200));
							}

							using (new GUIHelper.Horizontal()) {
								GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(150));
								GUILayout.Label("Show", EditorStyles.boldLabel, GUILayout.Width(60));
								GUILayout.Label("Ignor", EditorStyles.boldLabel, GUILayout.Width(60));
								GUILayout.Label("Hide", EditorStyles.boldLabel, GUILayout.Width(60));
							}

							using (new GUIHelper.Vertical()) {
								Dictionary<CPanel, int> mask = new Dictionary<CPanel, int>();
								foreach (CPanel panel in metaTarget.panels) {
									mask.Add(panel, -1);
									if (page.panels.Contains(panel))
										mask[panel] = 1;
									else if (page.ignoring_panels.Contains(panel))
										mask[panel] = 0;
								}

							    foreach (CPanel panel in metaTarget.panels) {
								    using (new GUIHelper.Horizontal()) {
									    switch (mask[panel]) {
										    case -1:
											    GUI.color = Color.red;
											    break;
										    case 0:
											    GUI.color = Color.yellow;
											    break;
										    case 1:
											    GUI.color = Color.green;
											    break;
									    }
									    EditorGUILayout.LabelField(panel.name, GUILayout.Width(150));
									    GUI.color = defColor;

									    if (EditorGUILayout.Toggle(mask[panel] == 1, GUILayout.Width(60)))
										    mask[panel] = 1;
									    if (EditorGUILayout.Toggle(mask[panel] == 0, GUILayout.Width(60)))
										    mask[panel] = 0;
									    if (EditorGUILayout.Toggle(mask[panel] == -1, GUILayout.Width(60)))
										    mask[panel] = -1;
								    }
							    }
						        page.panels.Clear();
						        page.ignoring_panels.Clear();
						        foreach (KeyValuePair<CPanel, int> pair in mask) {
							        if (pair.Value == 1)
								        page.panels.Add(pair.Key);
							        else if (pair.Value == 0)
								        page.ignoring_panels.Add(pair.Key);
						        }
						    }
						}
					}
				}
			}
            using (new GUIHelper.BackgroundColor(Color.cyan))
			    if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.Width(60)))
				    metaTarget.pages.Add(new UIAssistant.Page());
        }
        #endregion

        GUI.color = defColor;
    }

    public override UIAssistant FindTarget() {
        return UIAssistant.main;
    }

    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("UIAssistant is missing!");
            return false;
        }

        if (metaTarget.UImodules == null)
            metaTarget.UImodules = new List<Transform>();

        if (metaTarget.pages == null)
            metaTarget.pages = new List<UIAssistant.Page>();

        return true;
    }
}

[BerryPanelGroup("Content")]
[BerryPanelTab("Audio", 3)]
public class AudioEditor : MetaEditor<AudioAssistant> {

    SoundTree soundTree;
    SoundFileTree soundFileTree;

    MusicTree trackTree;
    GUIHelper.LayoutSplitter splitter = null;
    GUIHelper.Scroll scroll = null;
    static object selected = null;

    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("AudioAssistant is missing");
            return false;
        }
        if (metaTarget.tracks == null)
            metaTarget.tracks = new List<AudioAssistant.MusicTrack>();

        if (metaTarget.sounds == null)
            metaTarget.sounds = new List<AudioAssistant.Sound>();

        selected = null;

        int id = 0;
        metaTarget.sounds.ForEach(x => {
            x.clips.RemoveAll(c => !c);
            x.id = id++;
        });
        metaTarget.tracks.ForEach(x => x.id = id++);

        splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 220);
        splitter.drawCursor += x => GUI.Box(x, "", Styles.separator);

        scroll = new GUIHelper.Scroll(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        soundTree = new SoundTree(metaTarget.sounds);
        soundTree.onSelectedItemChanged += x => {
            if (x.Count == 1) {
                selected = x[0];
                SelectionChanged();
            }
        };

        trackTree = new MusicTree(metaTarget.tracks);
        trackTree.onSelectedItemChanged += x => {
            if (x.Count == 1) {
                selected = x[0];
                SelectionChanged();
            }
        };

        soundFileTree = null;

        return true;
    }

    private void SelectionChanged() {
        if (selected == null)
            return;
        if (selected is AudioAssistant.Sound) {
            AudioAssistant.Sound sound = (selected as AudioAssistant.Sound);
            soundFileTree = new SoundFileTree(sound.clips);
            return;
        }
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");

        using (splitter.Start(true, true)) {
            if (splitter.Area(Styles.area)) {
                using (scroll.Start()) {
                    using (new GUIHelper.Horizontal()) {
                        GUILayout.Label("Music", Styles.miniLabel, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(35)))
                            trackTree.AddNewItem(null, "NewMusic{0}");
                    }
                    trackTree.OnGUI();
                    EditorGUILayout.Space();
                    using (new GUIHelper.Horizontal()) {
                        GUILayout.Label("SFX", Styles.miniLabel, GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(35)))
                            soundTree.AddNewItem(null, "NewSound{0}");
                    }
                    soundTree.OnGUI();
                }
            }

            if (splitter.Area(Styles.area)) {
                if (selected != null) {
                    if (selected is AudioAssistant.Sound) {
                        EditorGUILayout.HelpBox("Use drag and drop to add new AudioClip", MessageType.Info, true);
                        soundFileTree.OnGUI(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    }
                    else if (selected is AudioAssistant.MusicTrack) {
                        AudioAssistant.MusicTrack music = selected as AudioAssistant.MusicTrack;
                        music.track = (AudioClip) EditorGUILayout.ObjectField("Audio Clip", music.track, typeof(AudioClip), false);
                        GUILayout.FlexibleSpace();
                    }
                } else
                    GUILayout.Label("Nothing Selected", Styles.centeredLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }
        }
    }

    public override AudioAssistant FindTarget() {
        return AudioAssistant.main;
    }

    public class SoundSelector {
        string[] sounds;
        string[] soundNames;

        public SoundSelector() {
            if (AudioAssistant.main) {
                AudioAssistant.main.UpdatePath();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Set("<Null>", "");
                foreach (var sound in AudioAssistant.main.sounds)
                    if (!dictionary.ContainsKey(sound.fullName))
                        dictionary.Add(sound.fullName, sound.fullName);
                soundNames = dictionary.Keys.ToArray();
                sounds = dictionary.Values.ToArray();
            }
        }

        public string Select(string name, string selected) {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (Event.current.type == EventType.Layout)
                return selected;

            Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent(name));
            rect.xMin = rect2.x;

            Rect objectRect = new Rect(rect);
            return sounds.Get(EditorGUI.Popup(objectRect, sounds.IndexOf(selected), soundNames));
        }
    }

    public class TrackSelector {
        string[] tracks;
        string[] trackNames;

        public TrackSelector() {
            if (AudioAssistant.main) {
                AudioAssistant.main.UpdatePath();
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Set("-", "-");
                dictionary.Set("None", "None");
                foreach (var track in AudioAssistant.main.tracks)
                    if (!dictionary.ContainsKey(track.fullName))
                        dictionary.Add(track.fullName, track.fullName);
                trackNames = dictionary.Keys.ToArray();
                tracks = dictionary.Values.ToArray();
            }
        }

        public string Select(string name, string selected) {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (Event.current.type == EventType.Layout)
                return selected;

            Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent(name));
            rect.xMin = rect2.x;

            Rect objectRect = new Rect(rect);
            return tracks.Get(EditorGUI.Popup(objectRect, tracks.IndexOf(selected), trackNames));
        }
    }

    class SoundTree : GUIHelper.HierarchyList<AudioAssistant.Sound> {
        static Texture2D icon = null;

        public SoundTree(List<AudioAssistant.Sound> collection) : base(collection, null, new TreeViewState()) {
            icon = EditorGUIUtility.FindTexture("AudioSource Icon");
        }

        internal const string newGroupNameFormat = "Folder{0}";
        internal const string newSoundNameFormat = "Sound{0}";
        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            if (selected.Count == 0) {
                menu.AddItem(new GUIContent("New Sound"), false, () => AddNewItem(null, newSoundNameFormat));
                menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(null, newGroupNameFormat));
            } else {
                if (selected.Count == 1 && selected[0].isFolderKind) {
                    FolderInfo parent = selected[0].asFolderKind;

                    menu.AddItem(new GUIContent("New Sound"), false, () => AddNewItem(parent, newSoundNameFormat));
                    menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(parent, newGroupNameFormat));

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

        public override AudioAssistant.Sound CreateItem() {
            var sound = new AudioAssistant.Sound();
            sound.id = UnityEngine.Random.Range(int.MinValue, -1);
            return sound;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, icon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);
            GUI.Label(_rect, info.name);
            if (info.content == selected) {
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.cyan);
                _rect = new Rect(rect);
                _rect.width = 20;
                _rect.x = rect.xMax - _rect.width;
                if (GUI.Button(_rect, ">")) {
                    if (info.content.clips.Count > 0) {
                        StopAllClips();
                        PlayClip(info.content.clips.GetRandom());
                    }
                }

            }
        }

        static void PlayClip(AudioClip clip) {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip) }, null);
            method.Invoke(null, new object[] { clip });
        }

        static void StopAllClips() {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod("StopAllClips",
                BindingFlags.Static | BindingFlags.Public, null, new Type[0], null);
            method.Invoke(null, new object[0]);
        }

        public override string GetPath(AudioAssistant.Sound element) {
            return element.path;
        }

        public override string GetName(AudioAssistant.Sound element) {
            return element.name;
        }

        public override void SetName(AudioAssistant.Sound element, string name) {
            element.name = name;
        }

        public override int GetUniqueID(AudioAssistant.Sound element) {
            return element.id;
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public override void SetPath(AudioAssistant.Sound element, string path) {
            element.path = path;
        }
    }

    class SoundFileTree : GUIHelper.NonHierarchyList<AudioClip> {
        public static Texture2D icon = null;

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            selected = selected.Where(x => x.isItemKind).ToList();
            if (selected.Count > 0)
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }

        public SoundFileTree(List<AudioClip> collection) : base(collection, new TreeViewState()) {
            icon = EditorGUIUtility.FindTexture("AudioClip Icon");
            onSelectedItemChanged += s => Selection.objects = s.ToArray();
        }

        public override AudioClip CreateItem() {
            return null;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, icon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);
            GUI.Label(_rect, info.content.name);
        }

        public override int GetUniqueID(AudioClip element) {
            return element.GetHashCode();
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            if (o is AudioClip) {
                ItemInfo item = new ItemInfo(0, null);
                item.content = o as AudioClip;
                result = item;
                return true;
            }

            result = null;
            return false;
        }
    }

    class MusicTree : GUIHelper.HierarchyList<AudioAssistant.MusicTrack> {
        public static Texture2D icon = null;

        public MusicTree(List<AudioAssistant.MusicTrack> collection) : base(collection, null, new TreeViewState()) {
            icon = EditorGUIUtility.FindTexture("AudioListener Icon");
        }

        internal const string newGroupNameFormat = "Folder{0}";
        internal const string newMusicNameFormat = "Music{0}";
        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            if (selected.Count == 0) {
                menu.AddItem(new GUIContent("New Music"), false, () => AddNewItem(null, newMusicNameFormat));
                menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(null, newGroupNameFormat));
            } else {
                if (selected.Count == 1 && selected[0].isFolderKind) {
                    FolderInfo parent = selected[0].asFolderKind;

                    menu.AddItem(new GUIContent("New Music"), false, () => AddNewItem(parent, newMusicNameFormat));
                    menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(parent, newGroupNameFormat));
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

        public override AudioAssistant.MusicTrack CreateItem() {
            var sound = new AudioAssistant.MusicTrack();
            sound.id = UnityEngine.Random.Range(int.MinValue, -1);
            return sound;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            if (info.content == selected)
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.cyan);
            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, icon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);
            GUI.Label(_rect, info.name);
        }
        
        public override string GetPath(AudioAssistant.MusicTrack element) {
            return element.path;
        }

        public override string GetName(AudioAssistant.MusicTrack element) {
            return element.name;
        }

        public override void SetName(AudioAssistant.MusicTrack element, string name) {
            element.name = name;
        }

        public override int GetUniqueID(AudioAssistant.MusicTrack element) {
            return element.id;
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        public override void SetPath(AudioAssistant.MusicTrack element, string path) {
            element.path = path;
        }
    }
}

[BerryPanelTab("Local Profile", 4)]
public class LocalProfileEditor : MetaEditor<ProfileAssistant> {

    AnimBool inventoryFade = new AnimBool(false);
    AnimBool userFade = new AnimBool(false);
    AnimBool sessionFade = new AnimBool(false);
    string timeOffsetText;
    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("ProfileAssistant is missing");
            return false;
        }
        userFade.valueChanged.AddListener(Repaint);
        inventoryFade.valueChanged.AddListener(Repaint);
        sessionFade.valueChanged.AddListener(Repaint);

        timeOffsetText = new TimeSpan(metaTarget.debugTimeOffset).ToString();

        return true;
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");
        
		if (GUILayout.Button("Clear Data", GUILayout.Width(80))) {
			metaTarget.ClearData();
		}

        TimeSpan timeOffset;
        timeOffsetText = EditorGUILayout.TextField("Debug Time Offset", timeOffsetText);
        if (TimeSpan.TryParse(timeOffsetText, out timeOffset)) {
            metaTarget.debugTimeOffset = timeOffset.Ticks;
        } else
            Handles.DrawSolidRectangleWithOutline(GUILayoutUtility.GetLastRect(), Color.clear, Color.red);
        
        userFade.target = GUILayout.Toggle(userFade.target, "Saved User", EditorStyles.foldout);
        if (EditorGUILayout.BeginFadeGroup(userFade.faded)) {
            EditorGUILayout.HelpBox(PlayerPrefs.GetString(UserUtils.userSaveKey), MessageType.None);
        }
        EditorGUILayout.EndFadeGroup();

        if (SessionAssistant.main.save_sessions) {
            sessionFade.target = GUILayout.Toggle(sessionFade.target, "Saved Session", EditorStyles.foldout);
            if (EditorGUILayout.BeginFadeGroup(sessionFade.faded)) {
                EditorGUILayout.HelpBox(PlayerPrefs.GetString(SessionInfo.sessionSaveKey), MessageType.None);
            }
            EditorGUILayout.EndFadeGroup();
        }
    }

    public override ProfileAssistant FindTarget() {
        return ProfileAssistant.main;
    }
}

[BerryPanelTab("Other", 6)]
public class ProjectParametersEditor : MetaEditor<Project> {

    public override Project FindTarget() {
        return Project.main;
    }

    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("SessionAssistant is missing");
            return false;
        }
        
        if (!ProfileAssistant.main) {
            Debug.LogError("ProfileAssistant is missing");
            return false;
        }

        return true;
    }

    public override void OnGUI() {

        Undo.RecordObject(metaTarget, "");

        ProfileAssistant.main.firstStartMenuSkiping = EditorGUILayout.Toggle("Skip menu on first start", ProfileAssistant.main.firstStartMenuSkiping);
        SessionAssistant.main.save_sessions = EditorGUILayout.Toggle("Save Sessions", SessionAssistant.main.save_sessions);

        EditorGUILayout.Space();
        metaTarget.chip_acceleration = EditorGUILayout.Slider("Chip Acceleration", metaTarget.chip_acceleration, 1f, 100f);
        metaTarget.chip_max_velocity = EditorGUILayout.Slider("Chip Velocity Limit", metaTarget.chip_max_velocity, 5f, 100f);
        metaTarget.chip_start_velosity = EditorGUILayout.Slider("Chip Start Velocity", metaTarget.chip_start_velosity, 0, metaTarget.chip_max_velocity);
        metaTarget.explosion_multiplier = EditorGUILayout.Slider("Explosion Multiplier", metaTarget.explosion_multiplier, 0, 2);

        EditorGUILayout.Space();
        metaTarget.refilling_time = Mathf.RoundToInt(EditorGUILayout.Slider("Life Refilling Hour (" + Mathf.FloorToInt(1f * metaTarget.refilling_time / 60).ToString("D2") + ":" + (metaTarget.refilling_time % 60).ToString("D2") + ")", metaTarget.refilling_time, 1, 24 * 60));
        metaTarget.dailyreward_hour = Mathf.RoundToInt(EditorGUILayout.Slider("Daily Reward Hour (" + metaTarget.dailyreward_hour.ToString("D2") + ":00)", metaTarget.dailyreward_hour, 00, 23));

        EditorGUILayout.Space();
        metaTarget.slot_offset = EditorGUILayout.Slider("Slot Offset", metaTarget.slot_offset, 0.01f, 2f);

        EditorGUILayout.Space();
        metaTarget.music_volume_max = EditorGUILayout.Slider("Max Music Volume", metaTarget.music_volume_max, 0f, 1f);
        #if ONLINE
        EditorGUILayout.Space();
        metaTarget.iosAppID = EditorGUILayout.TextField("iOS AppID", metaTarget.iosAppID);
        metaTarget.fbAppLink = EditorGUILayout.TextField("Facebook App Link", metaTarget.fbAppLink);
        metaTarget.invitePictureURL = EditorGUILayout.TextField("Invite Picture Link", metaTarget.invitePictureURL);
        metaTarget.permamentFacebookToken = EditorGUILayout.TextField("Facebook Token", metaTarget.permamentFacebookToken);
        #endif

        EditorGUILayout.Space();
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
            metaTarget.md5ProtectionType = (Project.MD5ProtectionType) EditorGUILayout.EnumPopup("MD5 Protection Type", metaTarget.md5ProtectionType);
            if (metaTarget.md5ProtectionType != Project.MD5ProtectionType.None) {
                metaTarget.md5EncryptionKey = EditorGUILayout.TextField("Encryption Key", metaTarget.md5EncryptionKey);
                metaTarget.md5KeysProviderUrl = EditorGUILayout.TextField("Keys Provider URL", metaTarget.md5KeysProviderUrl);
                if (GUILayout.Button("Read Key from Release APK...", GUILayout.Width(200))) {
                    string path = EditorUtility.OpenFilePanel("Select Release APK file", "", "apk");
                    if (path != null && path != "") {
                        FileInfo file = new FileInfo(path);
                        string result = null;
                        if (file.Exists)
                            using (var md5 = MD5.Create())
                            using (var stream = File.OpenRead(file.FullName))
                                result = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        result = Project.main.CryptMD5(result);
                        if (result == null)
                            EditorUtility.DisplayDialog("Failed", "Can't get MD5", "Ok");
                        else if (EditorUtility.DisplayDialog("Success", "MD5 key for this APK is:\n" + result, "Copy to clipboard", "Cancel"))
                            EditorGUIUtility.systemCopyBuffer = result;
                    }
                }
            }
        }
    }
}

[BerryPanelGroup("Monetization")]
[BerryPanelTab("In-App Purchases")]
public class InAppPurchasesEditor : MetaEditor<BerryStore> {
    public override BerryStore FindTarget() {
        return BerryStore.main;
    }
   
    public override bool Initialize() {
        if (!LocalizationAssistant.main) {
            Debug.LogError("LocalizationAssistant is missing");
        }

        if (!metaTarget) {
            Debug.LogError("BerryStoreAssistant is missing");
            return false;
        }

        if (metaTarget.iaps == null)
            metaTarget.iaps = new List<BerryStore.IAP>();
        return true;
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");

        using (new GUIHelper.Horizontal()) {
			GUILayout.Space(20);
			GUILayout.Label("ID", Styles.centeredMiniLabel, GUILayout.Width(100));
            GUILayout.Space(20);
            GUILayout.Label("SKU", Styles.centeredMiniLabel, GUILayout.Width(200));
        }

        foreach (BerryStore.IAP iap in metaTarget.iaps) {
            using (new GUIHelper.Horizontal()) {
				if (GUILayout.Button("X", GUILayout.Width(20))) {
					metaTarget.iaps.Remove(iap);
					break;
				}

				iap.id = EditorGUILayout.TextField(iap.id, GUILayout.Width(100));

                GUILayout.Space(20);

                iap.sku = EditorGUILayout.TextField(iap.sku, GUILayout.Width(200));
            }
        }

        using (new GUIHelper.Horizontal()) {
			if (GUILayout.Button("Add", GUILayout.Width(60)))
				metaTarget.iaps.Add(new BerryStore.IAP());
            if (GUILayout.Button("Sort", GUILayout.Width(60)))
                metaTarget.iaps.Sort((x, y) => x.id.CompareTo(y.id));
        }
    }
}

[BerryPanelGroup("Monetization")]
[BerryPanelTab("Store")]
public class StoreEditor : MetaEditor<BerryStore> {

    List<string> IAP_ids;

    StoreTree storeTree;
    GUIHelper.LayoutSplitter splitter;

    BerryStore.Stuff editable = null;
    string[] storeItemContentOptions = new string[0];
    string[] storeGroupContentOptions = new string[0];

    public override bool Initialize() {
        if (metaTarget == null)
            return false;

        if (metaTarget.items == null) metaTarget.items = new List<BerryStore.Item>();

        if (metaTarget.groups == null) metaTarget.groups = new List<BerryStore.Group>();

        if (metaTarget.iaps == null) metaTarget.iaps = new List<BerryStore.IAP>();

        storeTree = new StoreTree(metaTarget.items, metaTarget.groups, metaTarget.storeListState);
        storeTree.onSelectionChanged = x => {
            editable = null;
            if (x.Count == 1) {
                if (x[0] is StoreTree.ItemInfo)
                    editable = (x[0] as StoreTree.ItemInfo).content;
                if (x[0] is StoreTree.FolderInfo)
                    editable = metaTarget.groups.FirstOrDefault(y => (x[0] as StoreTree.FolderInfo).item.displayName == y.id);
            }
        };
        splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, new float[] { 200 });
        splitter.drawCursor = x => GUI.Box(x, "", Styles.separator);

        IAP_ids = metaTarget.iaps.Select(x => x.id).Distinct().ToList();


        foreach (BerryStore.Item item in metaTarget.items)
            foreach (BerryStore.Group group in metaTarget.groups)
                if (item.group == group) item.group = group; // They must be the same objects, but not only equal.

        if (Content.main) {
            if (!Content.main.IsInitialized) Content.main.Initialize();
            storeItemContentOptions = Content.main.cItems.Where(x => x.item.GetComponent<BerryStoreItem>() != null).Select(x => x.item.name).ToArray();
            storeGroupContentOptions = Content.main.cItems.Where(x => x.item.GetComponent<BerryStoreGroup>() != null).Select(x => x.item.name).ToArray();
        }

        return true;
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");

        #region Parameters
        GUILayout.Label("Store Parameters", Styles.centeredLabel, GUILayout.ExpandWidth(true));
        metaTarget.container = (LayoutGroup) EditorGUILayout.ObjectField("Store Items Container", metaTarget.container, typeof(LayoutGroup), true);

        if (storeItemContentOptions.Length > 0)
            metaTarget.storeItemContent = storeItemContentOptions.Get(EditorGUILayout.Popup("Items Prefab", storeItemContentOptions.IndexOf(metaTarget.storeItemContent), storeItemContentOptions));
        else
            EditorGUILayout.HelpBox("Content Assistant isn't contain any BerryStoreItem prefabs.", MessageType.Error, false);

        if (storeGroupContentOptions.Length > 0)
            metaTarget.storeGroupContent = storeGroupContentOptions.Get(EditorGUILayout.Popup("Group Prefab", storeGroupContentOptions.IndexOf(metaTarget.storeGroupContent), storeGroupContentOptions));
        else
            EditorGUILayout.HelpBox("Content Assistant isn't contain any BerryStoreGroup prefabs.", MessageType.Error, false);
        #endregion

        using (splitter.Start()) {
            if (splitter.Area(Styles.area)) {
                Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.MinHeight(storeTree.totalHeight + 200), GUILayout.ExpandHeight(true));
                storeTree.OnGUI(rect);
            }
            if (splitter.Area(Styles.area)) {

                if (editable != null) {
                    if (editable is BerryStore.Item)
                        DrawItem(editable as BerryStore.Item);
                    if (editable is BerryStore.Group)
                        DrawGroup(editable as BerryStore.Group);
                } else
                    GUILayout.Box("", EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            }
        }
    }

    Color removeColor = new Color(1, .5f, .5f, 1);
    Color addColor = new Color(.8f, 1, .8f, 1);

    int itemToolbarSelected = 0;
    readonly string[] itemToolbarTitles = new string[] { "Parameters", "Avaliable", "Reward", "Reward Action" };
    void DrawItem(BerryStore.Item item) {
        using (new GUIHelper.Horizontal(GUILayout.ExpandHeight(true))) {

            #region Item Icon
            if (item.icon != null) {
                Texture2D preview = AssetPreview.GetAssetPreview(item.icon);
                if (preview != null) {
                    using (new GUIHelper.Vertical(GUILayout.Width(100), GUILayout.Height(180))) {
                        GUILayout.Label("Icon", Styles.centeredLabel, GUILayout.ExpandWidth(true));
                        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(100), GUILayout.ExpandHeight(true));
                        Vector2 targetSize = rect.size;
                        Vector2 center = rect.center;
                        rect.size = new Vector2(preview.width, preview.height);
                        if (rect.width > targetSize.x)
                            rect.size *= targetSize.x / rect.width;
                        if (rect.height > targetSize.y)
                            rect.size *= targetSize.y / rect.height;
                        rect.center = center;
                        GUI.DrawTexture(rect, preview);
                    }
                }
            }
            #endregion

            using (new GUIHelper.Vertical(GUILayout.Height(180), GUILayout.ExpandWidth(true))) {
                GUILayout.Label("Item", Styles.centeredLabel, GUILayout.ExpandWidth(true));

                using (new GUIHelper.Change(() => GUI.FocusControl("")))
                    itemToolbarSelected = GUILayout.Toolbar(itemToolbarSelected, itemToolbarTitles);
							
                switch (itemToolbarSelected) {
                    case 0: {
                            #region Parameters
                            #if !UNITY_PURCHASING
                            if (item.purchaseType == BerryStore.Item.PurchaseType.IAP)
                                EditorGUILayout.HelpBox("There is no IAP plugin. This item will not be used!", MessageType.Error);
                            #endif

                            item.id = EditorGUILayout.TextField("Item ID", item.id, GUILayout.ExpandWidth(true));

                            item.purchaseType = (BerryStore.Item.PurchaseType) EditorGUILayout.EnumPopup("Purchase Type", item.purchaseType);

                            switch (item.purchaseType) {
                                case BerryStore.Item.PurchaseType.IAP: item.iap = IAP_ids.Get(EditorGUILayout.Popup("IAP ID", IAP_ids.IndexOf(item.iap), IAP_ids.ToArray(), GUILayout.ExpandWidth(true))); break;
                                case BerryStore.Item.PurchaseType.SoftCurrency: item.cost = Mathf.Max(1, EditorGUILayout.IntField("Cost (Coins)", item.cost, GUILayout.ExpandWidth(true))); break;
                                case BerryStore.Item.PurchaseType.RewardedVideo: break;
                            }


                                    
                            string group = item.group.id;
                            using (new GUIHelper.Change(() => {
                                item.group = metaTarget.groups.Find(x => x.id == group);
                                storeTree.Reload();
                            })) {
                                List<string> group_ids = metaTarget.groups.Select(x => x.id).Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();
                                group = group_ids.Get(EditorGUILayout.Popup("Group", group_ids.IndexOf(group), group_ids.ToArray(), GUILayout.ExpandWidth(true)));
                            }

                            item.localized = EditorGUILayout.Toggle("Localized", item.localized, GUILayout.ExpandWidth(true));
                            if (item.localized) {
                                EditorGUILayout.LabelField("Item Name ID:", item.localization_Name, GUILayout.ExpandWidth(true));
                                EditorGUILayout.LabelField("Item Description ID:", item.localization_Description, GUILayout.ExpandWidth(true));
                                if (GUILayout.Button("Edit Localization", GUILayout.Width(110)))
                                    LocalizationEditor.Edit(string.Format("item/{0}", item.id));
                            } else {
                                item.Name = EditorGUILayout.TextField("Item Name", item.Name, GUILayout.ExpandWidth(true));
                                item.Descrition = EditorGUILayout.TextField(string.Format("Item Description ({0}/40)", item.Descrition.Length), item.Descrition, GUILayout.ExpandWidth(true));
                            }

                            item.icon = (Sprite) EditorGUILayout.ObjectField("Icon", item.icon, typeof(Sprite), false, GUILayout.ExpandWidth(true));

                            GUILayout.FlexibleSpace();

                            using (new GUIHelper.Horizontal()) {
                                using (new GUIHelper.BackgroundColor(addColor))
                                    if (GUILayout.Button("Dublicate", GUILayout.Width(150))) {
                                        editable = (BerryStore.Item) item.Clone();
                                        string newID = item.id + "-clone";
                                        for (int i = 1; true; i++) {
                                            if (!metaTarget.items.Contains(x => x.id == newID))
                                                break;
                                            newID = item.id + "-clone" + i.ToString();
                                        }
                                        editable.id = newID ;
                                        metaTarget.items.Insert(metaTarget.items.IndexOf(item) + 1, editable as BerryStore.Item);
                                        storeTree.Reload();
                                    }
                                }
                            #endregion
                            break;
                        }
                    case 1: {
                            #region Avaliable

                            item.alwaysAvaliable = EditorGUILayout.Toggle("Always", item.alwaysAvaliable);

                            using (new GUIHelper.Lock(item.alwaysAvaliable))
                                DrawComparer(item.avaliableWhen);

                            #endregion
                            break;
                        }
                    case 2: {
                            #region Reward
                            if (item.pack.Count == 0)
                                GUILayout.Label("Empty");

                            for (int i = 0; i < item.pack.Count; i++) {
                                using (new GUIHelper.Horizontal()) {
                                    using (new GUIHelper.BackgroundColor(removeColor))
                                        if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(30))) {
                                            item.pack.RemoveAt(i);
                                            break;
                                        }
                                    item.pack[i].itemID = (ItemID) EditorGUILayout.EnumPopup(item.pack[i].itemID, EditorStyles.miniButtonRight, GUILayout.Width(100));
                                    item.pack[i].itemCount = Mathf.Max(1, EditorGUILayout.IntField(item.pack[i].itemCount, GUILayout.MaxWidth(100)));
                                }
                            }

                            using (new GUIHelper.BackgroundColor(addColor))
                                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(30)))
                                    item.pack.Add(new BerryStore.ItemPack());

                            #endregion
                            break;
                        }
                    case 3: {
                            #region Reward Action
                            SerializedProperty onPurchase = new SerializedObject(metaTarget).FindProperty("items");
                            onPurchase = onPurchase.GetArrayElementAtIndex(metaTarget.items.IndexOf(item));
                            onPurchase = onPurchase.FindPropertyRelative("onPurchase");

                            EditorGUILayout.PropertyField(onPurchase);
                            #endregion
                            break;
                        }
                }
            }
        }
    }

    void DrawComparer(Comparer comparer) {
        using (new GUIHelper.Vertical()) {
            using (new GUIHelper.Horizontal()) {
                GUILayout.Label("Value A:", GUILayout.Width(70));
                comparer.valueA.source = (Comparer.Value.ComparisonSource) EditorGUILayout.EnumPopup(comparer.valueA.source, GUILayout.Width(100));
                switch (comparer.valueA.source) {
                    case Comparer.Value.ComparisonSource.Item: comparer.valueA.compareItemID = (ItemID) EditorGUILayout.EnumPopup(comparer.valueA.compareItemID, GUILayout.Width(70)); break;
                    case Comparer.Value.ComparisonSource.Number: comparer.valueA.value = EditorGUILayout.IntField(comparer.valueA.value, GUILayout.Width(70)); break;
                    case Comparer.Value.ComparisonSource.Reference:
                        comparer.valueA.reference = Reference.Keys().Get(EditorGUILayout
                            .Popup(Mathf.Max(0, Reference.Keys().IndexOf(comparer.valueA.reference)), Reference.Keys().ToArray(), GUILayout.Width(70)));
                        break;
                }
            }
            using (new GUIHelper.Horizontal()) {
                GUILayout.Label("Must be", GUILayout.Width(70));
                comparer.comparionOperator = (Comparer.ComparisonOperator) EditorGUILayout.EnumPopup(comparer.comparionOperator, GUILayout.Width(100));
                GUILayout.Label("then", GUILayout.Width(30));
            }
            using (new GUIHelper.Horizontal()) {
                GUILayout.Label("Value B:", GUILayout.Width(70));
                comparer.valueB.source = (Comparer.Value.ComparisonSource) EditorGUILayout.EnumPopup(comparer.valueB.source, GUILayout.Width(100));
                switch (comparer.valueB.source) {
                    case Comparer.Value.ComparisonSource.Item: comparer.valueB.compareItemID = (ItemID) EditorGUILayout.EnumPopup(comparer.valueB.compareItemID, GUILayout.Width(70)); break;
                    case Comparer.Value.ComparisonSource.Number: comparer.valueB.value = EditorGUILayout.IntField(comparer.valueB.value, GUILayout.Width(70)); break;
                    case Comparer.Value.ComparisonSource.Reference:
                        comparer.valueA.reference = Reference.Keys().Get(EditorGUILayout
                            .Popup(Mathf.Max(0, Reference.Keys().IndexOf(comparer.valueA.reference)), Reference.Keys().ToArray(), GUILayout.Width(70)));
                        break;
                }
            }
        }
    }

    void DrawGroup(BerryStore.Group group) {
        using (new GUIHelper.Vertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))) {

            #region Parameters
            GUILayout.Label("Group",Styles.centeredLabel, GUILayout.ExpandWidth(true));

            group.id = EditorGUILayout.TextField("Group ID", group.id, GUILayout.ExpandWidth(true));

            group.localized = EditorGUILayout.Toggle("Localized", group.localized, GUILayout.ExpandWidth(true));
            if (group.localized) {
                EditorGUILayout.LabelField("Group Name ID:", group.localization_Name, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("Group Description ID:", group.localization_Description, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Edit Localization", GUILayout.Width(110)))
                    LocalizationEditor.Edit(string.Format("item/{0}", group.id));
            } else {
                group.Name = EditorGUILayout.TextField("Group Name", group.Name, GUILayout.ExpandWidth(true));
                group.Descrition = EditorGUILayout.TextField(string.Format("Group Description ({0}/40)", group.Descrition.Length), group.Descrition, GUILayout.ExpandWidth(true));
            }

            #endregion

        }
    }

    public override BerryStore FindTarget() {
        return BerryStore.main;
    }

    class StoreTree : GUIHelper.HierarchyList<BerryStore.Item, BerryStore.Group> {
        static Texture2D groupIcon = null;
        static Texture2D itemIcon = null;
        static Texture2D iapIcon = null;
        static Texture2D adsIcon = null;
        static Texture2D lockIcon = null;

        public StoreTree(List<BerryStore.Item> collection, List<BerryStore.Group> groups, TreeViewState state) : base(collection, groups, state) {}

        const string newGroupIdFormat = "untitled{0}-group";
        const string newItemIdFormat = "untitled{0}";

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(root, newGroupIdFormat));

            if (selected.Count == 1) {
                if (selected[0].isFolderKind) {
                    FolderInfo parent = selected[0].asFolderKind;
                    menu.AddItem(new GUIContent("New Entry"), false, () => AddNewItem(parent, newItemIdFormat));
                }

                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
            }
        }

        public override void DrawFolder(Rect rect, FolderInfo info) {
            if (!groupIcon) groupIcon = EditorIcons.GetIcon("StoreGroupIcon");

            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            GUI.DrawTexture(_rect, groupIcon);
            _rect = new Rect(rect.x + 16, rect.y, rect.width - 16, rect.height);
            GUI.Label(_rect, info.content.id);
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            if (!adsIcon) adsIcon = EditorIcons.GetIcon("StoreAdsIcon");
            if (!iapIcon) iapIcon = EditorIcons.GetIcon("StoreIAPIcon");
            if (!itemIcon) itemIcon = EditorIcons.GetIcon("StoreItemIcon");
            if (!lockIcon) lockIcon = EditorIcons.GetIcon("UnlockedIcon");

            Rect _rect = new Rect(rect.x, rect.y, 16, rect.height);
            switch (info.content.purchaseType) {
                case BerryStore.Item.PurchaseType.IAP: GUI.DrawTexture(_rect, iapIcon); break;
                case BerryStore.Item.PurchaseType.RewardedVideo: GUI.DrawTexture(_rect, adsIcon); break;
                case BerryStore.Item.PurchaseType.SoftCurrency: GUI.DrawTexture(_rect, itemIcon); break;
            }
            _rect.x += 16;
            if (!info.content.alwaysAvaliable) {
                GUI.DrawTexture(_rect, lockIcon);
                _rect.x += 16;
            }

            _rect = new Rect(_rect.x, rect.y, rect.width - rect.x + _rect.x, rect.height);

            GUI.Label(_rect, info.content.id);
        }

        public override string GetPath(BerryStore.Item element) {
            if (element.group == null) return "ungrouped";
            return element.group.id;
        }
        public override void SetPath(BerryStore.Item element, string path) {
            BerryStore.Group group = folderCollection.FirstOrDefault(x => x.id == path);
            if (group != null) element.group = group;
        }

        public override string GetName(BerryStore.Item element) {
            return element.id;
        }
        public override void SetName(BerryStore.Item element, string name) {
            element.id = name;
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }
        
        protected override bool CanRename(TreeViewItem item) {
            return true;
        }

        public override bool CanBeChild(IInfo parent, IInfo child) {
            if (parent == root && child.isFolderKind)
                return true;
            if (parent.isFolderKind && child.isItemKind)
                return true;
            return false;
        }

        public override string GetPath(BerryStore.Group element) {
            return "";
        }
        public override void SetPath(BerryStore.Group element, string path) {}

        public override string GetName(BerryStore.Group element) {
            return element.id;
        }
        public override void SetName(BerryStore.Group element, string name) {
            element.id = name.Replace("/", "");
        }

        public override BerryStore.Group CreateFolder() {
            return BerryStore.Group.New();
        }

        public override int GetUniqueID(BerryStore.Item element) {
            return element.instanceID.GetHashCode();
        }

        public override int GetUniqueID(BerryStore.Group element) {
            return element.instanceID.GetHashCode();
        }

        public override BerryStore.Item CreateItem() {
            return BerryStore.Item.New(null);
        }
    }
}

[BerryPanelGroup("Monetization")]
[BerryPanelTab("Initial Inventory")]
public class InitialInventoryEditor : MetaEditor<ProfileAssistant> {

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");

        #region Header
        using (new GUIHelper.Horizontal()) {
			GUILayout.Label("Item ID", Styles.centeredMiniLabel, GUILayout.Width(120));
			GUILayout.Label("Count", Styles.centeredMiniLabel, GUILayout.Width(120));
        }
        #endregion

        Dictionary<ItemID, int> inventory = new Dictionary<ItemID, int>();

        inventory = metaTarget.firstStartInventory.ToDictionary(x => x.type, x => x.initialCount);

        bool isLife;
        foreach (ItemID item in Enum.GetValues(typeof(ItemID))) {
            if (!inventory.ContainsKey(item))
                inventory.Add(item, 0);

            isLife = item == ItemID.life || item == ItemID.lifeslot;
            using (new GUIHelper.Lock(isLife)) {
                using (new GUIHelper.Horizontal()) {
				    GUILayout.Label(item.ToString(), GUILayout.Width(120));
				    int count = inventory.ContainsKey(item) ? inventory[item] : 0;
				    if (isLife) count = 5;
				    count = Mathf.Max(0, EditorGUILayout.IntField(count, GUILayout.Width(120)));
                    inventory.Set(item, count);
                }
            }
        }

        metaTarget.firstStartInventory = inventory.Select(x => new InitialItem() {
            type = x.Key,
            initialCount = x.Value
        }).ToList();

        if (GUILayout.Button("Manage Item IDs", GUILayout.Width(150)))
            BerryPanel.CreateBerryPanel().Show("ItemIDEditor");
    }

    public override ProfileAssistant FindTarget() {
        return ProfileAssistant.main;
    }

    public override bool Initialize() {
        if (!metaTarget) {
            Debug.LogError("ProfileAssistant is missing");
            return false;
        }

        return true;
    }
}

[CustomEditor(typeof(ItemMask))]
public class ItemMaskEditor : Editor {

    ItemMask main;

    void OnEnable() {
        main = (ItemMask) target;
    }

    public override void OnInspectorGUI() {
        Undo.RecordObject(main, "ItemMask changes");

        DrawValue(main.A);

        main.mustBe = (ComparisonOperator) EditorGUILayout.EnumPopup("Mast be", main.mustBe);

        DrawValue(main.B);

        GUILayout.Space(20);

        GUILayout.Label("Targets", EditorStyles.boldLabel);
        main.allChild = EditorGUILayout.Toggle("All Childs", main.allChild);

        if (!main.allChild) {
            for (int i = 0; i < main.targets.Count; i++) {
                using (new GUIHelper.Horizontal()) {
					if (GUILayout.Button("X", GUILayout.Width(30))) {
						main.targets.RemoveAt(i);
						break;
					}
					main.targets[i] = (GameObject) EditorGUILayout.ObjectField(main.targets[i], typeof(GameObject), true);
                }
            }

            GameObject newTarget = (GameObject) EditorGUILayout.ObjectField("Add new", null, typeof(GameObject), true);
            if (newTarget)
                main.targets.Add(newTarget);
        }

        main.action = (ComparisonAction) EditorGUILayout.EnumPopup("Action", main.action);
    }

    private void DrawValue(ItemMask.Value a) {
        using (new GUIHelper.Vertical(Styles.area)) {
			a.source = (ComparisonSource) EditorGUILayout.EnumPopup("Source", a.source);

			switch (a.source) {
				case ComparisonSource.Item:
					a.compareItemID = (ItemID) EditorGUILayout.EnumPopup("Value", a.compareItemID);
					break;
				case ComparisonSource.Number:
					a.value = EditorGUILayout.IntField("Value", a.value);
					break;
				case ComparisonSource.Reference: {
						List<string> references = Reference.Keys();
						int index = references.IndexOf(a.reference);
						if (index < 0)
							index = 0;
						a.reference = references[EditorGUILayout.Popup("Key", index, references.ToArray())];
						break;
					}
			}
        }
    }
}

public class DLCManifest {

    [InitializeOnLoadMethod]
    public static void DLCManifestMonitor() {
        if (Application.isPlaying || EditorApplication.isCompiling || !Content.main || !AudioAssistant.main)
            return;


        EditorCoroutine.start(Search());
    }

    static IEnumerator Search() {
        yield return 0;
        string dataPath = Path.Combine(Application.dataPath, "DLC");

        var files = EUtils.SearchFiles(dataPath).Where(f => f.Name == "DLCManifest.xml").ToList();

        List<Manifest> manifests = files.Select(f => Manifest.FromXML(f)).ToList();
        if (EditorApplication.isCompiling) yield break;
        if (manifests.Count > 0)
            EditorApplication.isPlaying = false;
        foreach (Manifest manifest in manifests) {
            manifest.Show();
            yield return 0;
        }
    }

    class Manifest {
        Dictionary<GameObject, string> content = new Dictionary<GameObject, string>();
        List<AudioAssistant.Sound> sounds = new List<AudioAssistant.Sound>();
        FileInfo file;
        public static Manifest FromXML(FileInfo file) {
            Manifest result = new Manifest();
            result.file = file;
            XElement root = XDocument.Parse(File.ReadAllText(file.FullName)).Root;
            foreach (XElement xContent in root.Elements("content")) {
                string path = xContent.Attribute("path").Value;
                string folder = xContent.Attribute("folder").Value;

                path = "Assets" + Path.Combine(file.Directory.FullName, path)
                    .Substring(Application.dataPath.Length);

                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                result.content.Add(go, folder);
            }

            foreach (XElement xContent in root.Elements("sound")) {
                string path = xContent.Attribute("path").Value;
                string folder = xContent.Attribute("folder").Value.Replace("\\", "/");
                string name = xContent.Attribute("name").Value;

                path = "Assets" + Path.Combine(file.Directory.FullName, path)
                    .Substring(Application.dataPath.Length);

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip) {
                    AudioAssistant.Sound sound = result.sounds.FirstOrDefault(s => s.path == folder && s.name == name);
                    if (sound == null) {
                        sound = new AudioAssistant.Sound();
                        sound.path = folder;
                        sound.name = name;
                        result.sounds.Add(sound);
                    }
                    sound.clips.Add(clip);
                }
            }

            return result;
        }

        internal void Show() {
            string message = "New DLC content is found! Do you want to add it into the game?";
            if (content.Count > 0) {
                message += "\n\n";
                message += "New prefabs:\n";
                message += string.Join("\n", content.Keys.Select(x => "+ " + x.name).ToArray());
            }
            if (sounds.Count > 0) {
                message += "\n\n";
                message += "New sounds:\n";
                message += string.Join("\n", sounds.SelectMany(x => x.clips).Select(x => "+ " + x.name).ToArray());
            }

            if (EditorUtility.DisplayDialog("New DLC found!",
                message,
                "Install", "No, don't ask again!")) {
                if (content.Count > 0) {
                    Undo.RecordObject(Content.main, "");
                    Content.main.Initialize();
                    foreach (var c in content) {
                        if (Content.main.cItems.Contains(i => i.item == c.Key)) continue;
                        Content.Item item = new Content.Item(c.Key);
                        item.path = c.Value;
                        Content.main.cItems.Add(item);
                    }
                }
                if (sounds.Count > 0) {
                    Undo.RecordObject(AudioAssistant.main, "");
                    foreach (var sound in sounds) {
                        AudioAssistant.Sound item = AudioAssistant.main.sounds.FirstOrDefault(c => c.path == sound.path && c.name == sound.name);
                        if (item == null)
                            AudioAssistant.main.sounds.Add(sound);
                        else {
                            item.clips.AddRange(sound.clips);
                            item.clips = item.clips.Distinct().ToList();
                        }
                    }
                }
                EditorSceneManager.SaveOpenScenes();
            }
            file.Delete();
        }
    }
}

[CustomEditor(typeof(IBooster), true)]
public class IBoosterEditor : Editor {
    IBooster main;

    void OnEnable() {
        main = (IBooster) target;
    }

    public override void OnInspectorGUI() {
        Undo.RecordObject(main, "IBooster changes");
        DrawDefaultInspector();

        using (new GUIHelper.Vertical()) {
            EditorGUILayout.LabelField("Type", main is ISingleUseBooster ? "Single Use" : "Multiple Use");
            main.icon = (Sprite) EditorGUILayout.ObjectField("Icon", main.icon, typeof(Sprite), false);
            main.localized = EditorGUILayout.Toggle("Localized", main.localized);
            if (main.localized) {
                EditorGUILayout.LabelField("Title", string.Format(IBooster.titleLocalizationKey, main.itemID));
                EditorGUILayout.LabelField("Desctiption", string.Format(IBooster.descriptionLocalizationKey, main.itemID));
                if (GUILayout.Button("Edit Localization", GUILayout.Width(120)))
                    LocalizationEditor.Edit(string.Format(IBooster.editLocalizationPattern, main.itemID));
            } else {
                main.title = EditorGUILayout.TextField("Title", main.title);
                main.description = EditorGUILayout.TextField("Desctiption", main.description);
            }
        }
    }

}