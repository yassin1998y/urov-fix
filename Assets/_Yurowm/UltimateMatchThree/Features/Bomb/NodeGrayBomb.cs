using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class NodeGrayBomb : NodeBomb, IColored, INeedToBeSetup {
    public ItemColor color {
        get {
            return ItemColor.Unknown;
        }
        set {
            targetColor = value;
        }
    }
    new ItemColor targetColor = ItemColor.Unknown;

    public override List<Slot> GetTargets(IChip secondChip) {
        base.targetColor = targetColor;
        if (secondChip is NodeBomb)
            return Slot.allActive.Values.Where(x => x.GetCurrentContent() is IDestroyable).ToList();
        else
            return Slot.allActive.Values.Where(x => x.color == targetColor).ToList();
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) targetColor));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
    }

    public void OnSetup(Slot slot) {
        if (!targetColor.IsPhysicalColor()) {
            targetColor = SessionInfo.current.colorMask.Values.GetRandom();
            Repaint(this, targetColor);
        }
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        targetColor = info["color"].ItemColor;
        Repaint(this, targetColor);
    }

}
