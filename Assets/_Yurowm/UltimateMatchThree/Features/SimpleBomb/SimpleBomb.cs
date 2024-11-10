using System.Collections;
using Yurowm.GameCore;
using System.Xml.Linq;

public class SimpleBomb : IChip, IColored, IBomb, INeedToBeSetup {
    ItemColor _color;
    public ItemColor color {
        get {
            return _color;
        }

        set {
            _color = value;
        }
    }

    public int destroyReward {
        get {
            return 3;
        }
    }

    public IEnumerator Destroying() {
        Explode(transform.position, 3, 100);

        int2 position = slot.position;

        sound.Play("Destroying");
        animator.Play("Destroying");

        var hitGroup = new System.Collections.Generic.List<Slot>() { slot };
        foreach (Side side in Utils.allSides) {
            int2 coord = position + side.ToInt2();
            if (Slot.allActive.ContainsKey(coord))
                hitGroup.Add(Slot.allActive[coord]);
        }
        HitContext context = new HitContext(hitGroup, HitReason.BombExplosion);
        hitGroup.ForEach(s => s.HitAndScore(context));

        while (animator.IsPlaying("Destroying"))
            yield return 0;
    }

    public void Explode() {
        slot.HitAndScore();
    }

    public void OnSetup(Slot slot) {
        Repaint(this, color.IsPhysicalColor() ? color : SessionInfo.current.colorMask.Values.GetRandom());
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        color = info["color"].ItemColor;
        Repaint(this, color);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) _color));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
    }
}
