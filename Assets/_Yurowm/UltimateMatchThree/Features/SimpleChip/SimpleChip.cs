using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System;
using System.Xml.Linq;

public class SimpleChip : IChip, IColored, IDestroyable, IDefaultSlotContent, INeedToBeSetup, IShuffled {
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
            return 1;
        }
    }

	public IEnumerator Destroying (){
        SessionInfo.current.AddScorePoint();
        
        sound.Play("Destroying");
        animator.Play("Destroying");

        while (animator.IsPlaying("Destroying"))
            yield return 0;
	}

    public bool CanBeSetInNewSlot(LevelDesign profile, SlotSettings slot) {
        return true;
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        Repaint(this, info["color"].ItemColor);
    }

    public void OnSetup(Slot slot) {
        Repaint(this, SessionInfo.current.colorMask.Values.GetRandom());
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) _color));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
    }
}