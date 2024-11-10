using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class BigExplosionMix : IChipMix {

    int2 position;
    ItemColor color = ItemColor.Unknown;

    int score = 0;

    public override void Prepare(IChip firstChip, IChip secondChip) {
        position = secondChip.slot.position;
        score += firstChip.destroyable.destroyReward;
        score += secondChip.destroyable.destroyReward;
        if (firstChip.colored != null)
            color = firstChip.colored.color;
    }

    public override IEnumerator Destroying() {
        IChip.Explode(transform.position, 8, 60);

        List<Slot> hitGroup = new List<Slot>();
        for (int distance = 1; distance <= 2; distance++) {
            foreach (Slot slot in Slot.allActive.Values)
                if (slot.position.EightSideDistanceTo(position) == distance)
                    hitGroup.Add(slot);
        }
        HitContext context = new HitContext(hitGroup, HitReason.BombExplosion);

        for (int distance = 1; distance <= 2; distance++) {
            foreach (Slot slot in Slot.allActive.Values)
                if (slot.position.EightSideDistanceTo(position) == distance)
                    slot.HitAndScore(context);
            yield return new WaitForSeconds(0.1f);
        }

        ScoreEffect.ShowScore(Slot.allActive[position].transform.position, score, color, 2);
    }
}
