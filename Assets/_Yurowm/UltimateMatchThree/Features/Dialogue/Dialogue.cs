using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;
using Yurowm.GameCore;
using System;
using System.Xml.Linq;
using System.Text.RegularExpressions;

[RequireComponent(typeof(ContentAnimator))]
public class Dialogue : ILevelExtension, IAnimated {

    ContentAnimator animator;
    List<ActionInfo> actions;
    internal Canvas canvas;

    public override void Setup(LevelExtensionInfo info) {
        animator = GetComponent<ContentAnimator>();
        actions = GetActionList(info);
        canvas = GetComponentInChildren<Canvas>();
        canvas.worldCamera = GameCamera.camera;
        canvas.gameObject.SetActive(false);
        actions.ForEach(x => x.OnDialogueCreated());
        Project.onLevelCreate.AddListener(Show);
    }

    void Show() {
        StartCoroutine(Showing());
    }

    IEnumerator Showing() {
        SessionInfo.current.settings.allowsToSave = false;
        yield return animator.PlayAndWait("Show");
        actions.ForEach(x => x.dialogue = this);
        actions.ForEach(x => x.OnDialogueStart());
        foreach (ActionInfo info in actions) {
            actions.ForEach(x => x.OnActionStart(info));
            yield return StartCoroutine(info.Logic());
            actions.ForEach(x => x.OnActionComplete(info));
        }
        actions.ForEach(x => x.OnDialogueComplete());
        actions = null;
        yield return animator.PlayAndWait("Hide");
        SessionInfo.current.settings.allowsToSave = true;
        Kill();
    }

    public IEnumerator GetAnimationNames() {
        yield return "Show";
        yield return "Hide";
    }

    public override void OnKill() {
        base.OnKill();
        if (actions != null)
            actions.ForEach(x => x.OnDialogueComplete());
    }

    public static List<ActionInfo> GetActionList(LevelExtensionInfo info) {
        LevelParameter parameter = GetActionParameter(info);

        List<ActionInfo> result = new List<ActionInfo>();
        try {
            XElement xml = XDocument.Parse(parameter.Text).Root;
            foreach (XElement element in xml.Elements()) {
                ActionInfo action = ActionInfo.Parse(element);
                if (action != null) result.Add(action);
            }
        } catch (Exception) {}
        return result;
    }

    public static LevelParameter GetActionParameter(LevelExtensionInfo info) {
        LevelParameter result = info.levelParameters.FirstOrDefault(x => x.name == "actions");
        if (result == null) {
            result = new LevelParameter("actions");
            info.levelParameters.Add(result);
        }
        return result;
    }
}

public abstract class ActionInfo {
    public readonly static Dictionary<string, Type> types;
    public Dialogue dialogue;
    static ActionInfo() {
        Type refType = typeof(ActionInfo);
        types = refType.Assembly.GetTypes().Where(x => !x.IsAbstract && refType.IsAssignableFrom(x)).
            OrderBy(x => x.Name).ToDictionary(x => x.Name, x => x);
    }

    public readonly string name;
    public static string GetName(string raw) {
        Regex prefixRemove = new Regex(@"^Action");
        Regex spaceAdd = new Regex(@"(?<=.)(?=[A-Z])");
        string name = raw;
        name = prefixRemove.Replace(name, "");
        name = spaceAdd.Replace(name, " ");
        return name;
    }

    public ActionInfo() {
        name = GetName(GetType().Name);
    }

    public static ActionInfo Parse(XElement xml) {
        try {
            Type targetType = types.Get(xml.Name.LocalName);
            if (targetType != null) {
                ActionInfo info = (ActionInfo) Activator.CreateInstance(targetType);
                info.Deserialize(xml);
                return info;
            }
        } catch (Exception) { }
        return null;
    }

    public abstract void Serialize(XElement xml);
    public abstract void Deserialize(XElement xml);

    public static XElement ToXML(ActionInfo info) {
        XElement element = new XElement(info.GetType().Name);
        info.Serialize(element);
        return element;
    }

    public abstract IEnumerator Logic();

    public override string ToString() {
        return name;
    }

    public virtual void OnDialogueStart() {}

    public virtual void OnActionStart(ActionInfo info) {}

    public virtual void OnActionComplete(ActionInfo info) {}

    public virtual void OnDialogueComplete() {}

    public virtual void OnDialogueCreated() {}

    public virtual IEnumerator<KeyValuePair<string, string>> GetState() {
        yield break;
    }

    public virtual string GetDetails() {
        return "";
    }
}

public class ActionDelay : ActionInfo {
    public float duration = 1;

    public override IEnumerator Logic() {
        yield return new WaitForSeconds(duration);
    }

    public override void Deserialize(XElement xml) {
        duration = float.Parse(xml.Value);
    }

    public override void Serialize(XElement xml) {
        xml.Value = duration.ToString();
    }

    public override string GetDetails() {
        return duration.ToString("F2") + " sec.";
    }
}

public class ActionAddBomb : ActionInfo {

    public IChip prefab = null;
    public List<int2> slots = new List<int2>();

    public override IEnumerator Logic() {
        if (prefab) {
            foreach (int2 slot in slots) {
                if (Slot.all.ContainsKey(slot)) {
                    FieldAssistant.main.Add(prefab, slot);
                    yield return new WaitForSeconds(.2f);
                }
            }
        }
        yield break;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("prefab");
        if (attribute != null) prefab = Content.GetPrefab<IChip>(attribute.Value);
        attribute = xml.Attribute("slots");
        if (attribute != null) slots = attribute.Value.Split(';').Select(int2.Parse).ToList() ;
    }

    public override void Serialize(XElement xml) {
        if (prefab) xml.Add(new XAttribute("prefab", prefab.name));
        if (slots.Count > 0) xml.Add(new XAttribute("slots", string.Join(";", slots.Select(x => x.ToString()).ToArray())));
    }

    public override string GetDetails() {
        return string.Format("{0} x{1}", prefab ? prefab.name : "N/A", slots.Count);
    }
}

public class ActionHighlightSlots : ActionInfo {

    static SlotHighlight highlight = null;

    public bool autohide = true;
    public List<int2> slots = new List<int2>();

    public override IEnumerator Logic() {
        if (highlight.IsShown())
            yield return highlight.StartCoroutine(highlight.Hide());
        if (slots.Count > 0) {
            highlight.autohide = autohide;
            highlight.StartCoroutine(highlight.Show(slots));
        }
    }

    public override void OnDialogueStart() {
        if (!highlight) {
            highlight = Content.Emit<SlotHighlight>();
            highlight.transform.SetParent(FieldAssistant.main.sceneFolder);
            highlight.transform.Reset();
        }
    }

    public override void OnDialogueComplete() {
        if (highlight && highlight.IsShown()) {
            highlight.HideAndKill();
            highlight = null;
        }
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("slots");
        if (attribute != null)
            slots = attribute.Value.Split(';').Select(int2.Parse).ToList();
        attribute = xml.Attribute("autohide");
        if (attribute != null)
            autohide = attribute.Value == "1";
    }

    public override void Serialize(XElement xml) {
        if (slots.Count > 0) xml.Add(new XAttribute("slots", string.Join(";", slots.Select(x => x.ToString()).ToArray())));
        xml.Add(new XAttribute("autohide", autohide ? 1 : 0));
    }
}

public class ActionLimitInteraction : ActionInfo {
    public List<int2> slots = new List<int2>();
    public bool disable = false;

    public override IEnumerator Logic() {
        if (disable)
            Slot.interactive = null;
        else 
            Slot.interactive = new List<int2>(slots);        
        yield break;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("slots");
        if (attribute != null) slots = attribute.Value.Split(';').Select(int2.Parse).ToList() ;
        attribute = xml.Attribute("disable");
        if (attribute != null) disable = attribute.Value == "1";
    }

    public override void Serialize(XElement xml) {
        if (slots.Count > 0) xml.Add(new XAttribute("slots", string.Join(";", slots.Select(x => x.ToString()).ToArray())));
        xml.Add(new XAttribute("disable", disable ? 1 : 0));
    }

    public override void OnDialogueStart() {
        Slot.interactive = new List<int2>();
    }

    public override void OnDialogueComplete() {
        Slot.interactive = null;
    }
}

public class ActionWaitNextMove : ActionInfo {
    public bool skipMatching = false;

    public override IEnumerator Logic() {
        int movesCount = SessionInfo.current.GetMovesCount();
        while (movesCount == SessionInfo.current.GetMovesCount())
            yield return 0;
        if (!skipMatching) {
            while (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity)
                yield return 0;
            while (SessionInfo.current.rule.GetMode() != PlayingMode.Wait)
                yield return 0;
        }

        yield return new WaitForSeconds(.1f);
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("skip");
        if (attribute != null) skipMatching = attribute.Value == "1";}

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute("skip", skipMatching ? 1 : 0));
    }
}

public class ActionHelper : ActionInfo {

    public List<int2> slots = new List<int2>();
    public DialogueHelper.LoopingMode looping;
    public bool autohide = true;

    public override IEnumerator Logic() {
        DialogueHelper handler = null;

        handler = ILiveContent.GetAll<DialogueHelper>().FirstOrDefault();
        if (slots.Count > 0) {
            if (!handler) {
                handler = Content.Emit<DialogueHelper>();
                handler.transform.SetParent(FieldAssistant.main.sceneFolder);
            } else
                yield return handler.StartCoroutine(handler.Hide());

            handler.loopingMode = looping;
            handler.autohide = autohide;
            handler.StartCoroutine(handler.Show(slots));
        } else if (handler)
            handler.StartCoroutine(handler.Hide());
        yield break;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("slots");
        if (attribute != null) slots = attribute.Value.Split(';').Select(int2.Parse).ToList();
        attribute = xml.Attribute("looping");
        if (attribute != null) looping = (DialogueHelper.LoopingMode) int.Parse(attribute.Value);
        attribute = xml.Attribute("autohide");
        if (attribute != null) autohide = attribute.Value == "1";
    }

    public override void Serialize(XElement xml) {
        if (slots.Count > 0) xml.Add(new XAttribute("slots", string.Join(";", slots.Select(x => x.ToString()).ToArray())));
        xml.Add(new XAttribute("looping", (int) looping));
        xml.Add(new XAttribute("autohide", autohide ? 1 : 0));
    }
}

public class ActionCharacter : ActionInfo {

    public static Dictionary<Corner, Dictionary<string, Character>> characters = null;
    public static Dictionary<Corner, Character> current = null;
    public Character prefab;
    public Corner corner;
    public ActionType type;
    public string pose = "";

    public enum Corner {Left, Right}
    public enum ActionType {Show, Hide}

    public override IEnumerator Logic() {
        if (current[corner] != null)
            while (current[corner].animator.IsPlaying())
                yield return 0;

        switch (type) {
            case ActionType.Show: {
                    Character character = characters[corner][prefab.name];
                    if (current[corner] != character) {
                        if (current[corner] != null) {
                            yield return dialogue.StartCoroutine(current[corner].Hide());
                            current[corner] = null;
                        }
                    }
                    character.SetPose(pose);
                    if (!current[corner]) {
                        current[corner] = character;
                        yield return dialogue.StartCoroutine(character.Show());
                    }
                } break;
            case ActionType.Hide: {
                    if (current[corner] != null) {
                        yield return dialogue.StartCoroutine(current[corner].Hide());
                        current[corner] = null;
                    }
                } break;
        }



    }

    public override void OnDialogueStart() {
        if (current == null) {
            current = new Dictionary<Corner, Character>();
            foreach (Corner corner in Enum.GetValues(typeof(Corner)))
                current.Add(corner, null);
        }
        if (characters == null)
            characters = new Dictionary<Corner, Dictionary<string, Character>>();
        if (!characters.ContainsKey(corner))
            characters.Add(corner, new Dictionary<string, Character>());
        if (!characters[corner].ContainsKey(prefab.name)) {
            Character character = Content.Emit(prefab);
            character.transform.SetParent(dialogue.canvas.transform);
            RectTransform rect = character.transform.rect();
            rect.anchorMin = GetAnchoredPosition(corner);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = new Vector3(corner == Corner.Left ? 1 : -1, 1, 1);
            character.gameObject.SetActive(false);
            characters[corner].Add(prefab.name, character);
        }
    }

    public override void OnDialogueComplete() {
        if (characters != null) {
            characters.ForEach(l => l.Value.ForEach(c => c.Value.Kill()));
            characters = null;
        }
        if (current != null) current = null;
    }

    static Vector2 GetAnchoredPosition(Corner corner) {
        switch (corner) {
            case Corner.Left: return Vector2.zero;
            case Corner.Right: return Vector2.right;
        }
        return Vector2.zero;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("prefab");
        if (attribute != null) prefab = Content.GetPrefab<Character>(attribute.Value);
        attribute = xml.Attribute("corner");
        if (attribute != null) corner = (Corner) int.Parse(attribute.Value);
        attribute = xml.Attribute("type");
        if (attribute != null) type = (ActionType) int.Parse(attribute.Value);
        attribute = xml.Attribute("pose");
        if (attribute != null) pose = attribute.Value;
    }

    public override void Serialize(XElement xml) {
        if (prefab) xml.Add(new XAttribute("prefab", prefab.name));
        xml.Add(new XAttribute("corner", (int) corner));
        xml.Add(new XAttribute("type", (int) type));
        xml.Add(new XAttribute("pose", pose));
    }

    public override IEnumerator<KeyValuePair<string, string>> GetState() {
        if (!prefab)
            yield break;
        switch (type) {
            case ActionType.Show: yield return new KeyValuePair<string, string>(corner.ToString() + " Character", prefab.name + ": " + pose); break;
            case ActionType.Hide: yield return new KeyValuePair<string, string>(corner.ToString() + " Character", ""); break;
        }
    }

    public override string GetDetails() {
        return corner.ToString() + ": " + type.ToString() + (type == ActionType.Show ? " " + (prefab ? prefab.name : "N/A") + " " + pose : "");
    }
}

public class ActionSay : ActionInfo {
    public const string localizationKeyFormatState = "Localization Key Format";
    public const string localizationKeyPath = "dialogue/{0}/{1}";

    public string text = "";
    public bool localized = false;
    public string key = "";
    public ActionCharacter.Corner corner;
    static SpeechBubble bubble = null;

    public override IEnumerator Logic() {
        string text = localized ? LocalizationAssistant.main[string.Format(localizationKeyPath, ActionSettings.KeyPath, key)] : this.text;
        bubble.Say(text, ActionCharacter.current[corner]);

        if (!bubble.gameObject.activeSelf)
            yield return dialogue.StartCoroutine(bubble.Show());
        
        while (!bubble.clicked)
            yield return 0;

        yield return dialogue.StartCoroutine(bubble.Hide());
    }

    public override void OnDialogueStart() {
        if (!bubble) {
            bubble = Content.Emit<SpeechBubble>();
            bubble.transform.SetParent(dialogue.canvas.transform);
            RectTransform rect = bubble.transform.rect();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            bubble.gameObject.SetActive(false);
        }
    }

    public override void OnDialogueComplete() {
        if (bubble) {
            bubble.Kill();
            bubble = null;
        }
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("corner");
        if (attribute != null) corner = (ActionCharacter.Corner) int.Parse(attribute.Value);
        attribute = xml.Attribute("localized");
        if (attribute != null) localized = attribute.Value == "1";
        if (localized) {
            attribute = xml.Attribute("key");
            if (attribute != null)
                key = attribute.Value;
        } else {
            attribute = xml.Attribute("text");
            if (attribute != null) text = attribute.Value;
        }
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute("corner", (int) corner));
        xml.Add(new XAttribute("localized", localized ? 1 : 0));
        if (localized) xml.Add(new XAttribute("key", key));
        else xml.Add(new XAttribute("text", text));
    }

    public override IEnumerator<KeyValuePair<string, string>> GetState() {
        yield return new KeyValuePair<string, string>("Last Speach", corner.ToString() + ": " + (localized ? "@" + key : text));
    }

    public override string GetDetails() {
        return corner.ToString() + ": " + (localized ? "@" + key : text);
    }
}

public class ActionSettings : ActionInfo {
    public bool mBooster = true;
    public bool sBooster = true;
    public bool rollback = true;
    public bool hints = true;
    public string keyPath = "untitled";
    public static string KeyPath = null;

    public override IEnumerator Logic() {
        Apply();
        yield break;
    }

    void Apply() {
        SessionInfo.current.settings.mBoostersEnable = mBooster;
        SessionInfo.current.settings.sBoostersEnable = sBooster;
        SessionInfo.current.settings.showHints = hints;
        KeyPath = keyPath;
    }

    public override void OnDialogueCreated() {
        Apply();
    }

    public override void OnDialogueComplete() {
        if (rollback) {
            SessionInfo.current.settings.mBoostersEnable = true;
            SessionInfo.current.settings.sBoostersEnable = true;
            SessionInfo.current.settings.showHints = true;
        }
        KeyPath = null;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("mBooster");
        if (attribute != null) mBooster = attribute.Value == "1";
        attribute = xml.Attribute("sBooster");
        if (attribute != null) sBooster = attribute.Value == "1";
        attribute = xml.Attribute("rollback");
        if (attribute != null) rollback = attribute.Value == "1";
        attribute = xml.Attribute("hints");
        if (attribute != null) hints = attribute.Value == "1";
        attribute = xml.Attribute("keyPath");
        if (attribute != null) keyPath = attribute.Value;
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute("mBooster", mBooster ? 1 : 0));
        xml.Add(new XAttribute("sBooster", sBooster ? 1 : 0));
        xml.Add(new XAttribute("rollback", rollback ? 1 : 0));
        xml.Add(new XAttribute("hints", hints ? 1 : 0));
        xml.Add(new XAttribute("keyPath", keyPath));
    }

    public override IEnumerator<KeyValuePair<string, string>> GetState() {
        yield return new KeyValuePair<string, string>(ActionSay.localizationKeyFormatState, keyPath);
    }
}