using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class AutoPlayBonus : CompleteBonus, ILevelRuleExclusive {
    public bool IsCompatibleWith(LevelRule rule) {
        return rule is MatchThreeSwapRule;
    }

    internal override bool IsComplete() {
        return SessionInfo.current.OutOfLimit();
    }

    internal override IEnumerator Logic() {
        Time.timeScale = 10f;

        while (ISlotContent.gravityLockers.Count > 0)
            yield return 0;

        MatchThreeSwapRule rule = (MatchThreeSwapRule) SessionInfo.current.rule;
        
        var moves = rule.FindMoves();

        if (moves.Count == 0) {
            if (!rule.Shuffle()) {
                while (SessionInfo.current.BurnMove()) {
                    SessionInfo.current.AddScorePoint(10);
                    yield return new WaitForSeconds(.1f);
                }
                yield break;
            }
            moves = rule.FindMoves();
        }

        var bestMove = moves.GetMax(x => x.potencial);
        SessionInfo.current.BurnMove();
        yield return StartCoroutine(rule.SwapByPlayerRoutine(Slot.allActive[bestMove.from], Slot.allActive[bestMove.to]));
    }
}
