using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class Ice : IBlock, ISounded, IDestroyable, INeedToBeSetup {

    [ContentSelector]
    public SlotRenderer slotRenderer;

    public static new SlotRenderer renderer = null;
    public static bool crushed = false;

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
        crushed = true;

        Rebuild();

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

    public override bool CanItContainChip() {
        return true;
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}
    public override void Serialize(XElement xContent) {}

    void EmitRenderer() {
        if (!renderer) {
            renderer = Content.Emit(slotRenderer);
            renderer.transform.SetParent(FieldAssistant.main.sceneFolder);
            renderer.transform.Reset();
            renderer.Rebuild(SessionInfo.current.design.slots.Where(x => x.block != null && x.block.name == name).Select(x => x.position).ToList());
        }
    }

    void Rebuild() {
        EmitRenderer();
        renderer.Rebuild(GetAll<Ice>().Where(x => x && !x.destroying).Select(x => x.slot.position).ToList());
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        EmitRenderer();
    }

    public void OnSetup(Slot slot) {
        StartCoroutine(Creating());
    }

    private IEnumerator Creating() {
        while (animator.IsPlaying())
            yield return 0;
        Rebuild();
    }
}

public class IceReaction : Reaction {

    public override int GetPriority() {
        return 1;
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Move;
    }

    public override IEnumerator React() {
        if (Ice.crushed) {
            Ice.crushed = false;
            yield break;
        }

        List<Slot> icedSlots = Slot.allActive.Values.Where(x => x.GetCurrentContent() is Ice).ToList();
        if (icedSlots.Count == 0)
            yield break;

        List<Slot> targets = icedSlots.SelectMany(x => x.nearSlot.Where(y => y.Key.IsStraight()).Select(z => z.Value)).Distinct()
            .Where(x => x != null && x.isActiveSlot && !x.block && x.chip && !icedSlots.Contains(x)).ToList();
        if (targets.Count == 0)
            yield break;

        Slot target = targets.GetRandom();

        FieldAssistant.main.Add<Ice>(target.position);

        yield return ReactionResult.Gravity;
    }
}
