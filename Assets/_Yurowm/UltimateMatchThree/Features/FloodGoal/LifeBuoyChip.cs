using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System.Xml.Linq;

public class LifeBuoyChip : IChip, IColored, IDestroyable, INeedToBeSetup, IGoalExclusive, IGeneratedWithProbability {
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

    public bool IsCompatibleWithGoal(ILevelGoal mode) {
        return mode is FloodGoal;
    }
}
