using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System;
using System.Xml.Linq;

public class StoneChip : IChip, IDestroyable, IGeneratedWithProbability {

    public override void Initialize() {
        base.Initialize();
        Project.onSlotContentPrepareToDestroy.AddListener(OnChipDestroyed);
    }

    public override void OnKill() {
        Project.onSlotContentPrepareToDestroy.RemoveListener(OnChipDestroyed);
    }

    public int destroyReward {
        get {
            return 1;
        }
    }

    public IEnumerator Destroying() {
        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");
	}

    void OnChipDestroyed(ISlotContent content) {
        if (destroying) return;
        if (content == this) return;
        if (content.context == null || content.context.reason != HitReason.Matching) return;
        if (content is StoneChip) return;
        if (content is IChip && slot.position.FourSideDistanceTo(content.slot.position) <= 1)
            slot.HitAndScore();
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}
}