using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public enum PreviewType {Target, Complete, Current}
public class TargetPanelPreview : TargetPanel {

    public PreviewType type;

    public override List<TargetCounter> EmitCounters() {
        List<TargetCounter> counters = new List<TargetCounter>();

        List<ILevelGoal> goals;
        if (type == PreviewType.Current) goals = SessionInfo.current.GetGoals();
        else goals = Content.GetPrefabList<ILevelGoal>(m => LevelDesign.selected.goals.Contains(n => n.prefab == m));

        foreach (ILevelGoal goal in goals) {
            LevelGoalInfo info = LevelDesign.selected.goals.FirstOrDefault(x => x.prefab.EqualContent(goal));
            counters.AddRange(goal.GetPreviewCounters(goal, LevelDesign.selected, info, type));
        }

        return counters;
    }
}
