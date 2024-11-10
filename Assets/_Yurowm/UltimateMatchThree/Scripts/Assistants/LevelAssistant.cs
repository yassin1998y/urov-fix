using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;
using System.Xml.Linq;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor.IMGUI.Controls;
#endif

public class LevelAssistant : MonoBehaviourAssistant<LevelAssistant> {

    // Level Editor State Serialization
    #if UNITY_EDITOR
    [HideInInspector]
    public TreeViewState levelListState = new TreeViewState();
    [HideInInspector]
    public bool levelListShown = true;
    [HideInInspector]
    public float[] splitterH = new float[2] { 200, 300 };
    [HideInInspector]
    public List<TreeFolder> folders = new List<TreeFolder>();
    [HideInInspector]
    public Vector2 levelListScroll = Vector2.zero;
    [HideInInspector]
    public Vector2 parametersScroll = Vector2.zero;
    #endif

    public List<LevelDesign> designs = new List<LevelDesign>();

    void Awake() {
        StartCoroutine(LoadLevels());
    }

    IEnumerator LoadLevels() {
        yield return 0;
        #if UNITY_EDITOR
        string assetsPath = System.IO.Path.Combine(Application.dataPath, "Resources");
        assetsPath = System.IO.Path.Combine(assetsPath, "Levels");
        string[] levelsRaw = new System.IO.DirectoryInfo(assetsPath).GetFiles()
            .Where(f => f.Extension == ".xml").Select(f => System.IO.File.ReadAllText(f.FullName)).ToArray();
        #else
        string[] levelsRaw = Resources.LoadAll("Levels").Cast<TextAsset>().Select(a => a.text).ToArray();
        #endif

        List<LevelDesign> designs = new List<LevelDesign>();
        DelayedAccess access = new DelayedAccess(1f / 20);
        foreach (string raw in levelsRaw) {
            designs.Add(LevelDesign.Deserialize(XElement.Parse(raw)));
            if (access.GetAccess()) yield return 0;
        }
        designs.Sort((a, b) => a.number.CompareTo(b.number));
        this.designs = designs;
        UpdateNumbers();

        #if UNITY_EDITOR
        if (PlayerPrefs.GetInt("TestLevel") != 0) {
            TestLevel(PlayerPrefs.GetInt("TestLevel"));
            SessionInfo.RemoveSavedSession();
        }
        #endif

        SessionInfo savedSession = SessionInfo.Load();
        if (savedSession != null)
            SessionAssistant.main.StartSession(savedSession);
        else
            UIAssistant.main.ShowPage("LevelList");
    }

    public static void SelectDesign(int levelNumber) {
        if (CPanel.uiAnimation > 0)
            return;

        Project.randomSeed = UnityEngine.Random.Range(9, 999);

        LevelDesign.selected = main.GetDesign(levelNumber);
        if (LevelDesign.selected != null) {
            if (CurrentUser.main.lifeSystem.HasLife())
                UIAssistant.main.ShowPage("LevelSelectedPopup");
            else
                UIAssistant.main.ShowPage("NotEnoughLifes");
        } else
            UIAssistant.main.ShowPage("MoreLevels");
    }

    public LevelDesign GetDesign(int levelNumber) {
        if (levelNumber < 1 || levelNumber > designs.Count)
            return null;
        return designs[levelNumber - 1];
    }

    public void UpdateNumbers() {
        for (int i = 0; i < designs.Count; i++) designs[i].number = i + 1;
    }

    public static void TestLevel(int number) {
        LevelDesign.selected = main.designs[number - 1];
        SessionAssistant.main.StartSession(LevelDesign.selected);
        PlayerPrefs.DeleteKey("TestLevel");
    }
}

[System.Serializable]
public class LevelDesign {
    public static LevelDesign selected; // current level

    public LevelRule type = null;
    public CompleteBonus bonus = null;
    public string chipPhysic = "";
    public bool randomizeColors = true;

    public const int maxSize = 12; // maximal playing field size
    public const int minSize = 4;// minimal playing field size
    public const int maxDeepHeight = 50;

    public int number = 0; // Level number
    // field size
    public int width {
        get {
            return fieldSize.x;
        }
        set {
            fieldSize.x = value;
        }
    }
    public int height {
        get {
            return fieldSize.y;
        }
        set {
            fieldSize.y = value;
        }
    }

    public bool deep = false;
    public int deepHeight = maxSize;

    public int colorCount = 4; // count of chip colors
    
    public int firstStarScore = 100; // number of score points needed to get a first stars
    public int secondStarScore = 200; // number of score points needed to get a second stars
    public int thirdStarScore = 300; // number of score points needed to get a third stars

    public List<LevelParameter> parameters = new List<LevelParameter>();
    public LevelParameter this[string index] {
        get {
            LevelParameter result = parameters.Find(x => x.name == index);
            if (result == null) {
                result = new LevelParameter(index);
                parameters.Add(result);
            }
            return result;
        }
    }

    public List<LevelGoalInfo> goals = new List<LevelGoalInfo>();
    public List<ILevelExtension.LevelExtensionInfo> extensions =
        new List<ILevelExtension.LevelExtensionInfo>();

    public int movesCount = 30; // Count of moves in TargetScore and JellyCrush
    
    public List<SlotSettings> slots = new List<SlotSettings>();
    public List<BigObjectSettings> bigObjects = new List<BigObjectSettings>();
    public int2 fieldSize = new int2(5, 5);
    public string path = "";
    public area activeArea {
        get {
            return new area(int2.zero, new int2(width, height));
        }
    }

    public area area {
        get {
            return new area(int2.zero, new int2(width, deep ? deepHeight : height));
        }
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        xml.Add(new XAttribute("type", type ? type.name : ""));
        xml.Add(new XAttribute("bonus", bonus ? bonus.name : ""));
        xml.Add(new XAttribute("number", number));
        xml.Add(new XAttribute("deep", deep ? "1" : "0"));
        xml.Add(new XAttribute("deepHeight", deepHeight));
        xml.Add(new XAttribute("color", colorCount));
        xml.Add(new XAttribute("r_colors", randomizeColors ? 1 : 0));
        xml.Add(new XAttribute("size", fieldSize));
        xml.Add(new XAttribute("star1", firstStarScore));
        xml.Add(new XAttribute("star2", secondStarScore));
        xml.Add(new XAttribute("star3", thirdStarScore));
        xml.Add(new XAttribute("moves", movesCount));
        xml.Add(new XAttribute("path", path));
        xml.Add(new XAttribute("physic", chipPhysic));

        XElement child = new XElement("parameters");
        xml.Add(child);
        foreach (var p in parameters) child.Add(p.Serialize("param"));

        child = new XElement("goals");
        xml.Add(child);
        foreach (var g in goals) child.Add(g.Serialize("goal"));

        child = new XElement("extensions");
        xml.Add(child);
        foreach (var e in extensions) child.Add(e.Serialize("exten"));

        child = new XElement("slots");
        xml.Add(child);
        foreach (var s in slots) child.Add(s.Serialize("slot"));

        child = new XElement("bigObjects");
        xml.Add(child);
        foreach (var bo in bigObjects) child.Add(bo.Serialize("bigObject"));

        return xml;
    }

    public static LevelDesign Deserialize(XElement xml) {
        LevelDesign result = new LevelDesign();
        result.type = Content.GetPrefab<LevelRule>(xml.Attribute("type").Value);
        result.bonus = Content.GetPrefab<CompleteBonus>(xml.Attribute("bonus").Value);
        result.number = int.Parse(xml.Attribute("number").Value);
        result.deep = xml.Attribute("deep").Value == "1";
        result.deepHeight = int.Parse(xml.Attribute("deepHeight").Value);
        result.colorCount = int.Parse(xml.Attribute("color").Value);
        result.fieldSize = int2.Parse(xml.Attribute("size").Value);
        result.firstStarScore = int.Parse(xml.Attribute("star1").Value);
        result.secondStarScore = int.Parse(xml.Attribute("star2").Value);
        result.thirdStarScore = int.Parse(xml.Attribute("star3").Value);
        result.movesCount = int.Parse(xml.Attribute("moves").Value);
        result.path = xml.Attribute("path").Value;
        var attribute = xml.Attribute("physic");
        if (attribute != null) result.chipPhysic = attribute.Value;

        attribute = xml.Attribute("r_colors");
        if (attribute != null) result.randomizeColors = attribute.Value == "1";

        foreach (var e in xml.Element("parameters").Elements())
            result.parameters.Add(LevelParameter.Deserialize(e));

        foreach (var e in xml.Element("goals").Elements())
            result.goals.Add(LevelGoalInfo.Deserialize(e));

        foreach (var e in xml.Element("extensions").Elements())
            result.extensions.Add(ILevelExtension.LevelExtensionInfo.CteateAndDeserialize(e));

        foreach (var e in xml.Element("slots").Elements())
            result.slots.Add(SlotSettings.Deserialize(e));

        var boXML = xml.Element("bigObjects");
        if (boXML != null)
            foreach (var e in boXML.Elements())
                result.bigObjects.Add(BigObjectSettings.Deserialize(e));

        return result;
    }

    public LevelDesign Clone() {
        LevelDesign clone = (LevelDesign) MemberwiseClone();
        clone.slots = slots.Select(x => x.Clone()).ToList();
        clone.extensions = extensions.Select(x => x.Clone()).ToList();
        clone.goals = goals.Select(x => x.Clone()).ToList();
        return clone;
    }

    public override string ToString() {
        #if UNITY_EDITOR
        try {
            return type.name + " (" + goals.Select(x => x.prefab.name).Join(", ") + ")";
        } catch {
            return "<color=red>Error</color>";
        }
        #else
        return base.ToString();
        #endif
    }
}

[Serializable]
public class SlotSettings {

    public SlotContent chip {
        get {
            return content.FirstOrDefault(x => x.type == SlotContent.Type.Chip);
        }
        set {
            content.RemoveAll(x => x.type == SlotContent.Type.Chip);
            if (value != null) {
                value.type = SlotContent.Type.Chip;
                content.Add(value);
            }
        }
    }
    public SlotContent block {
        get {
            return content.FirstOrDefault(x => x.type == SlotContent.Type.Block);
        }
        set {
            content.RemoveAll(x => x.type == SlotContent.Type.Block);
            if (value != null) {
                value.type = SlotContent.Type.Block;
                content.Add(value);
            }
        }
    }
    
    public SlotContent current {
        get {
            SlotContent current = block;
            if (current != null) return current;
            current = chip;
            if (current != null) return current;
            return null;
        }

    }

    public int2 position = new int2();
    public List<SlotContent> content = new List<SlotContent>();

    public SlotSettings(int x, int y) {
        position = new int2(x, y);
    }

    public SlotSettings(int2 position) {
        this.position = position;
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        xml.Add(new XAttribute("position", position));
        foreach (var c in content)
            xml.Add(c.Serialize("c"));
        return xml;
    }

    public static SlotSettings Deserialize(XElement xml) {
        SlotSettings result = new SlotSettings(int2.Parse(xml.Attribute("position").Value));
        foreach (var x in xml.Elements("c"))
            result.content.Add(SlotContent.Deserialize(x));
        return result;
    }

    public SlotSettings Clone() {
        SlotSettings result = (SlotSettings) MemberwiseClone();
        result.content = content.Select(x => x.Clone()).ToList();
        if (chip != null) result.chip = chip.Clone();
        if (block != null) result.block = block.Clone();
        return result;
    }

    public bool HasContent(string name) {
        for (int i = 0; i < content.Count; i++)
            if (content[i].name == name)
                return true;
        return false;
    }

    public SlotContent GetSlotContent(string name) {
        for (int i = 0; i < content.Count; i++)
            if (content[i].name == name)
                return content[i];
        return null;
    }
}

[Serializable]
public class BigObjectSettings {
    public SlotContent content;
    public int2 position = new int2();

    public BigObjectSettings(int2 position) {
        this.position = position;
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        xml.Add(new XAttribute("position", position));
        xml.Add(content.Serialize("c"));
        return xml;
    }

    public static BigObjectSettings Deserialize(XElement xml) {
        BigObjectSettings result = new BigObjectSettings(int2.Parse(xml.Attribute("position").Value));
        result.content = SlotContent.Deserialize(xml.Element("c"));
        return result;
    }

    public BigObjectSettings Clone() {
        BigObjectSettings result = (BigObjectSettings) MemberwiseClone();
        result.content = content.Clone();
        return result;
    }
}

[Serializable]
public class LevelParameter {
    public string name;

    public float Float = 0;
    public ItemColor ItemColor = ItemColor.Unknown;
    public int2 Coordinate = new int2();
    public string Text = "";

    public int Int {
        get {
            return Mathf.RoundToInt(Float);
        }
        set {
            Float = value;
        }
    }
    public bool Bool {
        get {
            return Float == 1;
        }
        set {
            Float = value ? 1 : 0;
        }
    }

    public LevelParameter(string name) {
        this.name = name;
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        Serialize(xml);
        return xml;
    }

    public LevelParameter Clone() {
        return (LevelParameter) MemberwiseClone();
    }

    public void Serialize(XElement xml) {
        if (!name.IsNullOrEmpty()) xml.Add(new XAttribute("name", name));
            xml.Add(new XAttribute("value", Float));
        if (Coordinate != null && Coordinate != int2.zero)
            xml.Add(new XAttribute("coord", Coordinate));
        xml.Add(new XAttribute("color", (int) ItemColor));
        if (!Text.IsNullOrEmpty())
            xml.Value = Text;
    }

    public static LevelParameter Deserialize(XElement xml) {
        LevelParameter result = new LevelParameter("");
        foreach (XAttribute a in xml.Attributes()) {
            switch (a.Name.LocalName) {
                case "name": result.name = a.Value; break;
                case "value": result.Float = float.Parse(a.Value); break;
                case "coord": result.Coordinate = int2.Parse(a.Value); break;
                case "color": result.ItemColor = (ItemColor) int.Parse(a.Value); break;
            }
        }
        result.Text = xml.Value;
        return result;
    }
}

[Serializable]
public class SlotExtension {
    public int2 coord;

    public SlotExtension(int2 coord) {
        this.coord = coord;
    }

    public List<LevelParameter> parameters = new List<LevelParameter>();
    public LevelParameter this[string index] {
        get {
            LevelParameter result = parameters.Find(x => x.name == index);
            if (result == null) {
                result = new LevelParameter(index);
                parameters.Add(result);
            }
            return result;
        }
    }
    public bool HasParameter(string index) {
        return parameters.Contains(x => x.name == index);
    }

    public SlotExtension Clone() {
        SlotExtension result = (SlotExtension) MemberwiseClone();
        result.parameters = parameters.Select(x => x.Clone()).ToList();
        return result;
    }

    public void Serialize(XElement xml) {
        xml.Add(new XAttribute("coord", coord));
        foreach (LevelParameter parameter in parameters) {
            XElement element = new XElement("param");
            parameter.Serialize(element);
            xml.Add(element);
        }
    }

    public static SlotExtension Deserialize(XElement xml) {
        SlotExtension result = new SlotExtension(int2.Parse(xml.Attribute("coord").Value));
        foreach (XElement element in xml.Elements("param"))
            result.parameters.Add(LevelParameter.Deserialize(element));
        return result;
    }
}

[Serializable]
public class SlotContent {
    public enum Type { Chip, Block, Modifier, Unknown }

    public static Type GetContentType(ISlotContent content) {
        return content is IChip ? Type.Chip : 
                content is IBlock ? Type.Block :
                content is ISlotModifier ? Type.Modifier :
                Type.Unknown;
    }

    public string name;
    public Type type;

    public SlotContent(string name, Type type) {
        this.name = name;
        this.type = type;
    }

    public List<LevelParameter> parameters = new List<LevelParameter>();
    public LevelParameter this[string index] {
        get {
            LevelParameter result = parameters.Find(x => x.name == index);
            if (result == null) {
                result = new LevelParameter(index);
                parameters.Add(result);
            }
            return result;
        }
    }
    public bool HasParameter(string index) {
        return parameters.Contains(x => x.name == index);
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        xml.Add(new XAttribute("name", this.name));
        xml.Add(new XAttribute("type", (int) type));
        foreach (var p in parameters)
            xml.Add(p.Serialize("p"));
        return xml;
    }

    public static SlotContent Deserialize(XElement xml) {
        string name = xml.Attribute("name").Value;
        Type type = (Type) int.Parse(xml.Attribute("type").Value);
        SlotContent result = new SlotContent(name, type);
        foreach (var x in xml.Elements("p"))
            result.parameters.Add(LevelParameter.Deserialize(x));
        return result;
    }

    public SlotContent Clone() {
        SlotContent result = (SlotContent) MemberwiseClone();
        result.parameters = parameters.Select(x => x.Clone()).ToList();
        return result;
    }
}

[Serializable]
public class LevelGoalInfo {
    public ILevelGoal prefab;

    public List<LevelParameter> parameters = new List<LevelParameter>();

    public LevelGoalInfo(ILevelGoal prefab) {
        this.prefab = prefab;
    }

    public LevelParameter this[string index] {
        get {
            LevelParameter result = parameters.Find(x => x.name == index);
            if (result == null) {
                result = new LevelParameter(index);
                parameters.Add(result);
            }
            return result;
        }
    }
    public bool HasParameter(string index) {
        return parameters.Contains(x => x.name == index);
    }

    public XElement Serialize(string name) {
        XElement xml = new XElement(name);
        xml.Add(new XAttribute("type", prefab ? prefab.name : ""));
        foreach (var p in parameters)
            xml.Add(p.Serialize("param"));
        return xml;
    }

    public static LevelGoalInfo Deserialize(XElement xml) {
        LevelGoalInfo result = new LevelGoalInfo(Content.GetPrefab<ILevelGoal>(xml.Attribute("type").Value));
        foreach (XElement p in xml.Elements("param"))
            result.parameters.Add(LevelParameter.Deserialize(p));
        return result;
    }

    public LevelGoalInfo Clone() {
        return (LevelGoalInfo) MemberwiseClone();
    }
}


