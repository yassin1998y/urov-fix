using UnityEngine;
using Yurowm.GameCore;
using System.Linq;
using System.Collections.Generic;

public class TargetPanel : MonoBehaviour {

    void OnEnable() {
        transform.AllChild(false).ForEach(x => Destroy(x.gameObject));

        List<TargetCounter> counters = EmitCounters();

        foreach (TargetCounter counter in counters) {
            counter.transform.SetParent(transform);
            counter.transform.localPosition = Vector3.zero;
            counter.transform.localScale = Vector3.one;
        }
    }

    public virtual List<TargetCounter> EmitCounters() {
        List<TargetCounter> result = new List<TargetCounter>();
        foreach (ILevelGoal mode in SessionInfo.current.GetGoals()) {
            var counters = mode.GetCurrentCounters(SessionInfo.current);
            foreach (var counter in counters)
                mode.SubsribeToUpdate(counter);
            result.AddRange(counters);
            mode.ForceUpdateCounters();
        }
                
        return result;
    }

}
