using System;
using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;

public class GrayBomb : IChip, IBomb, ILevelRuleExclusive {
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

        yield return new WaitForSeconds(0.1f);

        List<Slot> hitGroup = new List<Slot>();
        foreach (Side side in Utils.allSides) {
            int2 coord = position + side.ToInt2();
            if (Slot.allActive.ContainsKey(coord))
                hitGroup.Add(Slot.all[coord]);
        }
        HitContext context = new HitContext(hitGroup, HitReason.BombExplosion);
        foreach (Slot slot in hitGroup)
            slot.HitAndScore(context);

        while (animator.IsPlaying("Destroying"))
            yield return 0;
    }

    public void Explode() {
        slot.HitAndScore();
    }

    public void OnSetup(Slot slot) {
        Repaint(this, SessionInfo.current.colorMask.Values.GetRandom());
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public bool IsCompatibleWith(LevelRule rule) {
        return rule is MatchClickRule;
    }
}
