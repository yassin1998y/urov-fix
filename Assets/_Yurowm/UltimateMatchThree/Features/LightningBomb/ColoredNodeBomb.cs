using System.Linq;
using Yurowm.GameCore;
using System.Xml.Linq;
using System.Collections.Generic;

public class ColoredNodeBomb : NodeBomb, IColored, INeedToBeSetup {
    public ItemColor color { get; set; }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        color = info["color"].ItemColor;
        Repaint(this, color);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) color));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
    }

    public void OnSetup(Slot slot) {
        Repaint(this, color.IsPhysicalColor() ? color : SessionInfo.current.colorMask.Values.GetRandom());
    }
}
