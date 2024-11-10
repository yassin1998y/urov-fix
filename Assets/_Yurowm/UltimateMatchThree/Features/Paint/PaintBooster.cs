using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

class PaintBooster : ISingleUseBooster, ILevelRuleExclusive {

    public int chipsCount = 12;

    [ContentSelector]
    public IEffect paintEffect;


    [ContentSelector]
    public RocketEffect rocektEffect;

    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }

    public bool IsCompatibleWith(LevelRule rule) {
        return rule is MatchChainRule || rule is MatchClickRule;
    }

    public override IEnumerator Logic() {
        int count = chipsCount;
        List<Slot> all = Slot.allActive.Values.Where(x => x.color.IsPhysicalColor()).ToList();
        List<Slot> targets = new List<Slot>();

        Slot last = all.GetRandom();
        all.Remove(last);
        targets.Add(last);
        count--;

        List<Slot> next;
        while (count > 0 && all.Count > 0) {
            last = targets.Last();
            next = last.nearSlot.Values.Where(x => x != null && all.Contains(x)).ToList();

            if (next.Count == 0)
                last = all.GetRandom();
            else
                last = next.GetRandom();

            all.Remove(last);
            targets.Add(last);
            count--;
        }
        ItemColor color = SessionInfo.current.colorMask.Values.GetRandom();

        yield return new WaitForSeconds(0.1f);
        foreach (Slot target in targets.ToArray()) {
            //target.Repaint(color);
            //AudioAssistant.Shot("WeedCrush");
            //if (paintEffect) {
            //    IEffect effect = Content.Emit(paintEffect);
            //    effect.transform.position = target.transform.position;
            //    effect.Repaint(color);
            //    effect.Play();
            //}

            if (rocektEffect) {
                RocketEffect effect = Content.Emit(rocektEffect);
                effect.transform.position = ObjectTag.GetFirst("BottomDeep").transform.position;
                effect.Repaint(color);
                effect.SetTarget(target.transform);
                Slot slot = target;
                effect.onReach += () => {
                    slot.Repaint(color);
                    AudioAssistant.Shot("WeedCrush");
                    targets.Remove(slot);
                };
                effect.Play();
            } else {
                target.Repaint(color);
                AudioAssistant.Shot("WeedCrush");
                targets.Remove(target);
            }
            
            yield return new WaitForSeconds(0.1f);
        }

        while (targets.Count > 0) yield return 0;
    }
}