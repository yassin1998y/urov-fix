using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

[SlotTagRenderer('G', ConsoleColor.Green, priority: 100)]
public class SlotGeneratorExtended : ISlotModifier, INeedToBeSetup {
    public static bool IsTagVisible(SlotSettings slot) {
        return slot.HasContent("GeneratorExtended");
    }

    public const string weight_parameter = "_weight";

    [NonSerialized]
    public List<Case> cases = new List<Case>();

    public void OnSetup(Slot slot) {}

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        cases = info.parameters
            .Select(p => SlotContent.Deserialize(XElement.Parse(p.Text)))
            .Select(s => new Case() {
                content = s,
                weight = s[weight_parameter].Float
            }).ToList();
        StartCoroutine(Routine());
    }

    readonly float delay = 0.2f;

    IEnumerator Routine() {
        if (cases.Count == 0) yield break;
        float Sum = cases.Sum(c => c.weight);
        while (true) {
            yield return new WaitForSeconds(delay);

            if (!SessionAssistant.main.enabled) continue;
		    if (slot.chip) continue;
		    if (slot.block) continue;
            if (gravityLockers.Count > 0) continue;
            if (SessionInfo.current.rule.GetMode() == PlayingMode.Wait)
                SessionInfo.current.rule.SetMode(PlayingMode.Gravity);
            if (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity) continue;

            float sum = UnityEngine.Random.Range(0, Sum);
            foreach (Case c in cases) {
                sum -= c.weight;
                if (sum > 0) continue;

                SlotContent slotContent = c.content.Clone();
                IChip chip = Content.Emit<IChip>(slotContent.name);
                chip.name = slotContent.name;

                IColored colored = chip as IColored;
                if (colored != null && (colored.color.IsColored() || slotContent.HasParameter("color"))) {
                    LevelParameter color = slotContent["color"];
                    if (color.ItemColor.IsPhysicalColor() && SessionInfo.current.colorMask.ContainsKey(color.ItemColor))
                        color.ItemColor = SessionInfo.current.colorMask[color.ItemColor];
                    else
                        color.ItemColor = SessionInfo.current.colorMask.Values.GetRandom();
                }

                chip.transform.position = slot.transform.position;

                slot.AttachContent(chip);
                chip.transform.SetParent(slot.transform);
                chip.slot = slot;

                if (chip is INeedToBeSetup)
                    (chip as INeedToBeSetup).OnSetupByContentInfo(slot, slotContent);
                break; 
            }
        }
    }

    public override void Serialize(XElement xContent) {
        foreach (Case c in cases)
            xContent.Add(c.Serialize());
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        int counter = 0;
        foreach (XElement x in xContent.Elements("case"))
            slotContent["case" + ++counter].Text = x.ToString();
    }

    public class Case {
        public SlotContent content;
        public float weight = 1;
        public XElement Serialize() {
            content[weight_parameter].Float = weight;
            return content.Serialize("case");
        }
    }
}
