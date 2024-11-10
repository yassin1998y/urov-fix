using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.GameCore {
    public class Content : MonoBehaviourAssistant<Content> {
        [HideInInspector]
        public List<Item> cItems = new List<Item>();
        [HideInInspector]
        public List<TreeFolder> categories = new List<TreeFolder>();

        Dictionary<string, GameObject> content = new Dictionary<string, GameObject>();

        [HideInInspector]
        public string[] SDSymbols;

        void Awake() {
            Initialize();
        }

        [NonSerialized]
        bool initialized = false;
        public bool IsInitialized {
            get {
                return initialized;
            }
        }

        public void Initialize() {
            cItems.RemoveAll(x => x.item == null);
            content = cItems.ToDictionary(x => x.item.name, x => x.item);
            initialized = true;
        }

        public static T GetItem<T>(string key) where T : Component {
            GameObject obj = GetItem(key);
            if (obj)
                return obj.GetComponent<T>();
            return null;
        }

        public static T GetItem<T>() where T : Component {
            T prefab = GetPrefab<T>(p => p.GetComponent<T>());
            if (!prefab) return null;
            GameObject obj = GetItem(prefab.name);
            if (obj) return obj.GetComponent<T>();
            return null;
        }

        public static T GetPrefab<T>(string key) where T : Component {
            GameObject obj = GetPrefab(key);
            if (obj)
                return obj.GetComponent<T>();
            return null;
        }

        public static GameObject GetItem(string key) {
            if (main.content.ContainsKey(key))
                return Instantiate(main.content[key]);
            return null;
        }

        public static GameObject GetPrefab(string key) {
            if (!main) return null;
            if (!main.initialized) main.Initialize();
            if (main.content.ContainsKey(key))
                return main.content[key];
            return null;
        }

        public static L Emit<L>(L reference = null) where L : ILiveContent{
            if (!main) return null;
            if (!main.initialized) main.Initialize();
            L result;
            GameObject original = null;
            if (!reference) {
                result = GetPrefab<L>();
                if (result) original = result.gameObject;
            } else if (reference._original) {
                original = reference._original;
            } else {
                if (!main.content.Values.Contains(x => x.gameObject == reference.gameObject))
                    throw new Exception("This is a wrong reference. Use only original references from Content manager or instances which was created by original reference.");
                original = reference.gameObject;
            }

            result = Instantiate(original).GetComponent<L>();
            result.name = original.name;
            result._original = original;
            ILiveContent.Add(result);
            return result;
        }

        public static ILiveContent Emit(string name) {
            return Emit<ILiveContent>(name);
        }

        public static L Emit<L>(string name) where L : ILiveContent {
            if (!main)
                return null;
            if (!main.initialized)
                main.Initialize();
            if (!main.content.Keys.Contains(x => x == name))
                throw new Exception("This is a wrong name. Content manager dosn't contain anything like this.");

            L result = GetItem<L>(name);
            result._original = GetPrefab(name);
            result.name = name;
            ILiveContent.Add(result);
            return result;
        }

        public static ILiveContent Emit(Type refType, string name) {
            if (!main) return null;
            if (!main.initialized) main.Initialize();
            if (name != null && !main.content.Keys.Contains(x => x == name))
                throw new Exception("This is a wrong name. Content manager dosn't contain anything like this.");
            
            if (!typeof(ILiveContent).IsAssignableFrom(refType))
                throw new Exception("The ref type must be assignable from ILiveContent");

            GameObject original = main.content.First(x => (name == null || x.Key == name) && x.Value.GetComponent(refType)).Value;

            ILiveContent result = (ILiveContent) (Instantiate(original)).GetComponent(refType);

            result.name = name;
            result._original = original;
            ILiveContent.Add(result);
            return result;
        }

        public static T GetItem<T>(string key, Vector3 position) where T : Component {
            GameObject result = GetItem(key);
            result.transform.position = position;
            return result.GetComponent<T>();
        }

        public static GameObject GetItem(string key, Vector3 position) {
            GameObject result = GetItem(key);
            result.transform.position = position;
            return result;
        }

        public static GameObject GetItem(string key, Vector3 position, Quaternion rotation) {
            GameObject result = GetItem(key, position);
            result.transform.rotation = rotation;
            return result;
        }

        public static List<T> GetPrefabList<T>(Func<T, bool> condition = null) where T : Component {
            if (!main) return new List<T>();
            if (!main.initialized) main.Initialize();
            List<T> result = main.cItems.Select(x => x.item.GetComponent<T>()).Where(x => x != null).ToList();
            if (condition != null)
                result.RemoveAll(x => !condition(x));
            return result;
        }

        public static T GetPrefab<T>(Func<T, bool> condition = null) where T : Component {
            return GetPrefabList<T>(condition).FirstOrDefault();
        }

//        IEnumerator BlueScreen() {
//            if (!Application.isPlaying)
//                yield break;

//            GameObject canvas_go = new GameObject("BlueScreen");
//            canvas_go.hideFlags = HideFlags.HideAndDontSave;

//            Canvas canvas = canvas_go.AddComponent<Canvas>();
//            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//            canvas.sortingOrder = int.MaxValue;

//            CanvasScaler canvas_scaler = canvas_go.AddComponent<CanvasScaler>();
//            canvas_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//            canvas_scaler.referenceResolution = Vector2.one * 400;

//            GraphicRaycaster graphic_raycaster = canvas_go.AddComponent<GraphicRaycaster>();

//            GameObject background_go = new GameObject("Background");
//            background_go.hideFlags = HideFlags.HideAndDontSave;
//            background_go.transform.SetParent(canvas_go.transform);
//            background_go.transform.Reset();

//            RectTransform background_rect = background_go.AddComponent<RectTransform>();
//            background_rect.anchorMin = Vector2.zero;
//            background_rect.anchorMax = Vector2.one;
//            background_rect.offsetMin = Vector2.zero;
//            background_rect.offsetMax = Vector2.zero;

//            Image background = background_go.AddComponent<Image>();
//            background.color = new Color(.2f, 0.05f, 0f, 1);

//            GameObject text_go = new GameObject("Text");
//            text_go.hideFlags = HideFlags.HideAndDontSave;
//            text_go.transform.SetParent(background_go.transform);
//            text_go.transform.Reset();

//            RectTransform text_rect = text_go.AddComponent<RectTransform>();
//            text_rect.anchorMin = Vector2.zero;
//            text_rect.anchorMax = Vector2.one;
//            text_rect.offsetMin = Vector2.one * 10;
//            text_rect.offsetMax = -Vector2.one * 10;

//            Text text = text_go.AddComponent<Text>();
//            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
//            text.color = Color.red;
//            text.text = @"FATAL ERROR

//Technical information:
//*** STOP: 0x0000005 (0xF73B9D66, 0xF78D9EA0, 0xF78D9B9C)
//*** acpi.sys – Address F73B9D66 base at F73AE000, Date Stamp 480252b1";

//            yield return new WaitForSeconds(0.3f);
//            Application.Quit();
//        }

        [Serializable]
        public class Item {
            public Item(GameObject item) {
                this.item = item;
            }

            public GameObject item;
            public string path = "";
        }
    }

    public abstract class ILiveContent : MonoBehaviour {
        static List<ILiveContent> all = new List<ILiveContent>();

        internal GameObject _original = null;
        public GameObject original {
            get {
                return _original;
            }
        }

        public void Kill() {
            all.Remove(this);
            OnKill();
            Destroy(gameObject);
        }

        public bool EqualContent(ILiveContent content) {
            if (!content) return false;
            if (content == this) return true;
            if (content._original && _original) return content._original == _original;
            return content._original == gameObject || _original == content.gameObject;
        }

        #region Public Static

        static bool Search<T>(ILiveContent content, Type type, T original, Func<T, bool> condition) where T : ILiveContent {
            return (!original || content._original == original.gameObject) && type.IsAssignableFrom(content.GetType())
                && (condition == null || condition.Invoke((T) content));
        }
        public static List<T> GetAll<T>(Func<T, bool> condition = null, T original = null) where T : ILiveContent {
            Type type = typeof(T);
            return all.Where(x => Search(x, type, original, condition)).Cast<T>().ToList();
        }

        public static int Count<T>(Func<T, bool> condition = null, T original = null) where T : ILiveContent {
            Type type = typeof (T);
            return all.Count(x => Search(x, type, original, condition));
        }

        public static bool Contains<T>(Func<T, bool> condition = null, T original = null) where T : ILiveContent {
            Type type = typeof(T);
            return all.Contains(x => Search(x, type, original, condition));
        }
        
        public static void KillEverything() {
            new List<ILiveContent>(all).Where(x => x != null).ForEach(x => x.Kill());
            all.Clear();
        }
        #endregion

        internal static void Add(ILiveContent clone) {
            clone.Initialize();
            all.Add(clone);
        }

        #region Virtual
        virtual public void Initialize() {}
        virtual public void OnKill() {}
        #endregion
    }


    public class ContentSelector : PropertyAttribute {
        public Type targetType = null;

        public ContentSelector() {}
        public ContentSelector(Type type) {
            targetType = type;
        }
    }
}

