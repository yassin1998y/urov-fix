using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class LavaChip : StoneChip, IMixable {
    [ContentSelector]
    public StrikeEffect rocketEffect;

    public static bool crushed = false;

    public override void OnEndDestroying() {
        base.OnEndDestroying();
        crushed = true;
    }

    public int GetMixingLogicPriority() {
        return 10;
    }

    public IEnumerator Mixing(IChip secondChip) {
        List<Slot> targets = new List<Slot>();

        if (destroyingEffect) {
            IEffect effect = Content.Emit(destroyingEffect);
            effect.transform.position = transform.position;
            effect.Play();
        }

        Explode(transform.position, 5, 60);

        int2 position = slot.position;

        HitContext context = new HitContext(HitReason.BombExplosion);
        int score = 0;
        foreach (Slot slot in Slot.allActive.Values)
            if (slot.position.EightSideDistanceTo(position) == 1)
                score += slot.Hit(context);

        ScoreEffect.ShowScore(Slot.allActive[position].transform.position, score, ItemColor.Uncolored, 2);

        int targetCount = UnityEngine.Random.Range(3, 9);

        while (targets.Count < targetCount) {
            var slots = Slot.allActive.Where(x => !x.Value.block && x.Value != slot && x.Value.GetCurrentContent() is IChip &&
                x.Value != secondChip.slot && x.Value.GetCurrentContent().destroyable != null &&
                !(x.Value.GetCurrentContent() is LavaChip) &&
                !targets.Contains(x.Value)).ToList();
            if (slots.Count == 0)
                break;
            targets.Add(slots.GetRandom().Value);
        }

        foreach (Slot target in targets) {
            StrikeEffect effect = Content.Emit(rocketEffect);
            effect.transform.position = transform.position;
            effect.SetTarget(target.transform);
            effect.Play();

            Slot slot = target;
            effect.onReach = () => FieldAssistant.main.Add<LavaChip>(slot);
        }

        yield break;
    }
}


public class LavaChipReaction : Reaction {

    public override int GetPriority() {
        return 0;
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Move;
    }

    public override IEnumerator React() {
        if (LavaChip.crushed) {
            LavaChip.crushed = false;
            yield break;
        }

        List<Slot> fireSlots = Slot.allActive.Values.Where(x => x.GetCurrentContent() is LavaChip).ToList();
        if (fireSlots.Count == 0) yield break;

        List<Slot> targets = fireSlots.SelectMany(x => x.nearSlot.Where(y => y.Key.IsStraight()).Select(z => z.Value)).Distinct()
            .Where(x => x != null && x.isActiveSlot && !x.block && x.chip && x.chip is IDestroyable && !fireSlots.Contains(x)).ToList();
        if (targets.Count == 0) yield break;

        Slot target = targets.GetRandom();

        target.chip.Kill();
        FieldAssistant.main.Add<LavaChip>(target.position);

        yield return ReactionResult.Gravity;
    }
}