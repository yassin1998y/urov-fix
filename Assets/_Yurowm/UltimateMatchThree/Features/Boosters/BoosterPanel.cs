using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;

[RequireComponent (typeof (LayoutGroup))]
public class BoosterPanel : MonoBehaviour {

    [ContentSelector]
    public BoosterButton button;
    public Type type;

    public enum Type {
        SingleUse,
        MultipleUse
    }

    void OnEnable() {
        transform.DestroyChilds();

        List<IBooster> boosterPrefabs = Content.GetPrefabList<IBooster>(x => type == Type.SingleUse ? x is ISingleUseBooster : x is IMultipleUseBooster);
        boosterPrefabs.Sort((x, y) => x.name.CompareTo(y.name));

        foreach (IBooster prefab in boosterPrefabs) {
            if (!Validate(prefab)) continue;
            BoosterButton b = Content.GetItem<BoosterButton>(button.name);
            b.SetPrefab(prefab);
            b.icon.sprite = prefab.icon;
            b.transform.SetParent(transform);
            b.transform.Reset();
        }
    }

    public static bool Validate(IBooster prefab) {
        if (prefab is ILevelRuleExclusive && !(prefab as ILevelRuleExclusive).IsCompatibleWith(SessionInfo.current.rule))
            return false;
        if (prefab is IGoalExclusive) {
            foreach (var goal in SessionInfo.current.GetGoals())
                if ((prefab as IGoalExclusive).IsCompatibleWithGoal(goal))
                    return true;
            return false;
        }
        return true;
    }
}
