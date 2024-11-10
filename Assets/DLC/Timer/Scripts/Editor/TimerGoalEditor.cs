using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.GameCore;

public class TimerGoalEditor : GoalEditorExtension {
    public override bool IsCompatibleWith(ILiveContent content) {
        return content is TimerGoal;
    }

    protected override void OnModeEditorGUI(Context context) {
        int value = context.info[TimerGoal.timer_parameter].Int;
        value = Mathf.RoundToInt(EditorGUILayout.Slider("Time (" + TimerGoal.TimerFormat(value) + ")", value, 10, 600));
        context.info[TimerGoal.timer_parameter].Int = value; 
    }
}
