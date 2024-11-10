using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class AirstrikeBonus : CompleteBonus {

    [Range(2, 20)]
    public int bombsCount = 8;

    [ContentSelector]
    public RocketEffect rocketEffect;

    internal override bool IsComplete() {
        return SessionInfo.current.OutOfLimit();
    }

    internal override IEnumerator Logic() {
        int newBombs = Mathf.Min(SessionInfo.current.GetMovesCount(), bombsCount);

        List<Slot> targets = new List<Slot>(Slot.allActive.Values);

        while (targets.Count > newBombs)
            targets.RemoveAt(Random.Range(0, targets.Count));

        targets.Sort((x, y) => (y.x + y.y).CompareTo(x.x + x.y));

        List<StrikeEffect> effects = new List<StrikeEffect>();

        foreach (Slot target in targets) {
            StrikeEffect effect = Content.Emit(rocketEffect);
            effect.transform.position = target.transform.position + new Vector3(4, 12, 0) * Project.main.slot_offset;
            effect.SetTarget(target.transform);
            effect.Play();
            Slot slot = target;
            effect.onReach += () => Hit(slot);

            effects.Add(effect);

            yield return new WaitForSeconds(0.1f);
        }

        SessionInfo.current.rule.matchDate++;

        while (effects.Contains(x => x))
            yield return new WaitForSeconds(0.3f);
    }

    void Hit(Slot slot) {
        SessionInfo.current.BurnMove();
        IChip.Explode(slot.transform.position, 3, 100);
        HitContext context = new HitContext(HitReason.BombExplosion);
        int score = slot.Hit();
        foreach (Side side in Utils.allSides) {
            Slot _slot = Slot.allActive.Get(slot.position + side);
            if (_slot) score += _slot.Hit(context);
        }
        ScoreEffect.ShowScore(slot.transform.position, score, slot.color, 2);
    }
}
