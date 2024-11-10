using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.GameCore;

public class CollectionGoalEditor : GoalEditorExtension {
    public override bool IsCompatibleWith(ILiveContent content) {
        return content is CollectionGoal && !(content as CollectionGoal).toDestroyAllTargets;
    }

    protected override void OnModeEditorGUI(Context context) {
        context.info[CollectionGoal.targetCount_parameter].Int = Mathf.Clamp(EditorGUILayout.IntField("Count",
            context.info[CollectionGoal.targetCount_parameter].Int), 1, 999);
    }
}
