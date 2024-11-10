using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class BombsBonus : CompleteBonus, IDefault {
    [Range(2, 20)]
    public int bombsCount = 8;

    [ContentSelector(typeof (IBomb))]
    public List<IChip> bombTypes;

    [ContentSelector]
    public CollectionEffect nodeEffect;

    internal override IEnumerator Logic() {
        int newBombs = Mathf.Min(SessionInfo.current.GetMovesCount(), bombsCount);

        GameObject emitter = ObjectTag.GetFirst("MovesCounter");

        List<Slot> targets = new List<Slot>(Slot.allActive.Values);
        targets.RemoveAll(x => x.block || !x.chip || !(x.chip is IDefaultSlotContent));
        while (targets.Count > newBombs) 
            targets.RemoveAt(UnityEngine.Random.Range(0, targets.Count - 1));
        targets = targets.Unsort().ToList();

        foreach (Slot target in targets.ToArray()) {
            SessionInfo.current.BurnMove();
            if (nodeEffect && emitter) {
                CollectionEffect effect = Content.Emit(nodeEffect);
                effect.transform.position = emitter.transform.position;
                Slot slot = target;
                effect.SetTarget(target.transform);
                IChip bomb = bombTypes.GetRandom();
                effect.SetItem(bomb, slot.color);
                effect.onReach = () => {
                    targets.Remove(slot);
                    FieldAssistant.main.Add(bomb, slot);
                };
                effect.Play();
            } else {
                targets.Remove(target);
                FieldAssistant.main.Add(bombTypes.GetRandom(), target);
            }

            yield return new WaitForSeconds(0.05f);
        }

        while (targets.Count > 0) yield return 0;

        SessionInfo.current.rule.matchDate++;

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(CollapseAllPowerups());
    }

    // Coroutine of activation all bombs in playing field
    internal IEnumerator CollapseAllPowerups() {
        List<IBomb> powerUp = FindPowerups();
        while (powerUp.Count > 0) {
            powerUp = powerUp.FindAll(x => !x.destroying);
            if (powerUp.Count > 0)
                powerUp.GetRandom().Explode();
            yield return new WaitForSeconds(0.2f);
            powerUp = FindPowerups();
        }

        yield return new WaitForSeconds(0.2f);
    }

    // Finding bomb function
    List<IBomb> FindPowerups() {
        return GetAll<ISlotContent>(x => x.isActiveContent && x is IBomb).Cast<IBomb>().ToList();
    }

    internal override bool IsComplete() {
        return bombTypes.Count == 0 || (SessionInfo.current.OutOfLimit() && !Contains<ISlotContent>(x => x.isActiveContent && x is IBomb));
    }
}
