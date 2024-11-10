using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class Shell : IBigBlock, INeedToBeSetup {
    [ContentSelector]
    public NodeEffect nodeEffect;
    public Transform emitter;
    [ContentSelector]
    public IChip reference;

    public override bool CanItContainChip() {
        return false;
    }

    public override void Initialize() {
        base.Initialize();
        Project.onSlotContentPrepareToDestroy.AddListener(OnContentDestroyed);
    }

    public override void OnKill() {
        Project.onSlotContentPrepareToDestroy.RemoveListener(OnContentDestroyed);
    }


    List<Slot> hitterSlots;
    void OnContentDestroyed(ISlotContent content) {
        if (content.context == null || content.context.reason != HitReason.Matching) return;
        if (content is IChip && hitterSlots.Contains(content.slot))
            slot.HitAndScore();
    }

    public override IEnumerator GetAnimationNames() {
        yield return "Hit";
    }

    public override IEnumerator GetSoundNames() {
        yield return "Hit";
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public override void Serialize(XElement xContent) {}

    public override int Hit(HitContext context) {
        animator.Play("Hit");
        sound.Play("Hit");
        EditPearl();
        return base.Hit(context);
    }

    void EditPearl() {
        Slot target = Slot.allActive.Values.Select(s => s.GetCurrentContent())
            .Where(c => c is IChip && c is IDefaultSlotContent && !c.destroying)
            .Select(c => c.slot).GetRandom();
        if (!target) return;
        if (nodeEffect && emitter) {
            NodeEffect e = Content.Emit(nodeEffect);
            e.transform.SetParent(FieldAssistant.main.sceneFolder);
            e.transform.Reset();
            e.transform.position = emitter.transform.position;
            e.SetTarget(target.transform);
            e.onReach = () => FieldAssistant.main.Add(reference, target);
            e.Play();
        } else {
            FieldAssistant.main.Add(reference, target);
        }
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        OnSetup(slot);
    }

    public void OnSetup(Slot slot) {
        var slots = GetSlots();
        hitterSlots = slots.SelectMany(s => s.nearSlot).Where(s => s.Key.IsStraight())
            .Select(s => s.Value).Distinct().ToList();
        hitterSlots.RemoveAll(s => slots.Contains(s));
    }
}
