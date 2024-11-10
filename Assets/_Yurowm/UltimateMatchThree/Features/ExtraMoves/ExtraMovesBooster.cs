using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

class ExtraMovesBooster : ISingleUseBooster {
    public int movesCount = 5;
    [ContentSelector]
    public NodeEffect effect;

    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }

    public override IEnumerator Logic() {
        int count = movesCount;
        GameObject emitter = ObjectTag.GetFirst("BottomDeep");
        GameObject target = ObjectTag.GetFirst("MovesCounter");

        yield return new WaitForSeconds(0.1f);
        int complete = count;
        while (count > 0) {
            count--;

            if (effect && emitter && target) {
                NodeEffect e = Content.Emit(effect);
                e.transform.SetParent(FieldAssistant.main.sceneFolder);
                e.transform.Reset();
                e.transform.position = emitter.transform.position;
                e.SetTarget(target.transform);
                e.onReach = () => {
                    SessionInfo.current.AddMove();
                    complete--;
                };
                e.Play();
            } else {
                SessionInfo.current.AddMove();
                complete--;
            }
            yield return new WaitForSeconds(0.2f);
        }

        while (complete > 0) yield return 0;
    }
}