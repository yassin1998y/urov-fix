using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using UnityEngine;
using Condition = GeneratedWithCondition;
using Probability = GeneratedWithProbability;
using Generated = GeneratedChipController;
using System.Xml.Linq;

[SlotTagRenderer('G', priority: 100)]
public class SlotGenerator : ISlotModifier, IDefaultSlotContent, INeedToBeSetup {

    #region Static 
    static bool initialized = false;
    public static Dictionary<IChip, Probability> probabilityPrefabs;
    public static Dictionary<IChip, Condition> conditionPrefabs;
    public static Dictionary<IChip, Generated> generatedChips;
    public static IChip mainChip;

    public static void InitializeContent() {
        List<IChip> chips = Content.GetPrefabList<IChip>(x => x is IGenerated);

        mainChip = Content.GetPrefab<IChip>(x => x is IDefaultSlotContent);
        if (!mainChip) Debug.LogError("A main chip is not found! Use IDefault interface to declare a main chip.");

        probabilityPrefabs = chips.Where(x => x is IGeneratedWithProbability).ToDictionary(x => x, x => new Probability((IGeneratedWithProbability) x));

        conditionPrefabs = chips.Where(x => x is IGeneratedWithCondition).ToDictionary(x => x, x => new Condition((IGeneratedWithCondition) x));

        generatedChips = new Dictionary<IChip, Generated>();
        foreach (var chip in conditionPrefabs)
            generatedChips.Set(chip.Key, chip.Value);

        foreach (var chip in probabilityPrefabs)
            generatedChips.Set(chip.Key, chip.Value);

        initialized = true;
    }
    #endregion

    List<ItemColor> charge = new List<ItemColor>();

    public static bool IsTagVisible(SlotSettings slot) {
        return slot.HasContent("Generator");
    }
    
    [HideInInspector]
	public IChip chip;

    readonly float delay = 0.2f;

    void  Awake (){
        if (!initialized)
            InitializeContent();
	}

    IEnumerator Routine() {
        while (true) {
            yield return new WaitForSeconds(delay);

            if (!SessionAssistant.main.enabled) continue;
		    if (slot.chip) continue;
		    if (slot.block) continue;
            if (gravityLockers.Count > 0) continue;
            if (SessionInfo.current.rule.GetMode() == PlayingMode.Wait)
                SessionInfo.current.rule.SetMode(PlayingMode.Gravity);
            if (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity) continue;

            if (charge.Count > 0) {
                FieldAssistant.main.CreateNewContent(mainChip, slot, Vector3.zero, charge[0]);
                charge.RemoveAt(0);
                goto CONTINUE;
            }

            foreach (var chip in generatedChips)
                if (chip.Value.CanItBeGenerated(slot.position)) {
                    FieldAssistant.main.CreateNewContent(chip.Key, slot, Vector3.zero);
                    goto CONTINUE;
                }

            FieldAssistant.main.CreateNewContent(mainChip, slot, Vector3.zero); // creating new chip

            CONTINUE:
            continue;
        }
    }

    public bool CanBeSetInNewSlot(LevelDesign design, SlotSettings slot) {
        return slot.position.y == (design.deep ? design.deepHeight : design.height) - 1;
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        string key;

        foreach (var probability in probabilityPrefabs) {
            key = string.Format(Probability.keyFormat, probability.Key.name);
            probability.Value.SetProbabilityMask(slot.position, info[key].Float);
        }

        foreach (var condition in conditionPrefabs) {
            key = string.Format(Condition.keyFormat, condition.Key.name);
            condition.Value.SetConditionMask(slot.position, info[key].Bool);
        }
        
        StartCoroutine(Routine());
    }

    public override void Serialize(XElement xContent) {
        string key;
        foreach (var probability in probabilityPrefabs) {
            key = string.Format(Probability.keyFormat, probability.Key.name);
            XElement xml = new XElement(key);
            xml.Add(new XAttribute("value", probability.Value.GetMask(slot.position).ToString("F2")));
            xContent.Add(xml);
        }

        foreach (var condition in conditionPrefabs) {
            key = string.Format(Condition.keyFormat, condition.Key.name);
            XElement xml = new XElement(key);
            xContent.Add(xml);
            xml.Add(new XAttribute("value", condition.Value.GetMask(slot.position) ? 1 : 0));
        }

        if (charge.Count > 0)
            xContent.Add(new XElement("charge", string.Join(";", charge.Select(x => ((int) x).ToString()).ToArray())));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        if (!initialized) InitializeContent();
        string key;

        foreach (var probability in probabilityPrefabs) {
            key = string.Format(Probability.keyFormat, probability.Key.name);
            var e = xContent.Element(key);
            if (e != null)
                slotContent[key].Float = float.Parse(e.Attribute("value").Value);
        }

        foreach (var condition in conditionPrefabs) {
            key = string.Format(Condition.keyFormat, condition.Key.name);
            var e = xContent.Element(key);
            if (e != null)
                slotContent[key].Bool = int.Parse(e.Attribute("value").Value) == 1;
        }

        XElement element = xContent.Element("charge");
        if (element != null) charge = element.Value.Split(';').Select(x => (ItemColor) int.Parse(x)).ToList();
    }

    public void OnSetup(Slot slot) {}
}