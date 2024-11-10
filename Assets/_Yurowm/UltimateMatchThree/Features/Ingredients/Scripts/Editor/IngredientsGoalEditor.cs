using System;
using UnityEditor;
using UnityEngine;
using Yurowm.GameCore;

public class IngredientsModeEditor : GoalEditorExtension {
    public override bool IsCompatibleWith(ILiveContent content) {
        return content is IngredientsGoal;
    }

    protected override void OnModeEditorGUI(Context context) {
        context.info[IngredientsGoal.targetCount_parameter].Int = Mathf.RoundToInt(EditorGUILayout.Slider("Ingredients Count", context.info[IngredientsGoal.targetCount_parameter].Int, 1, 100));
        context.info[IngredientsGoal.activeCount_parameter].Int = Mathf.RoundToInt(EditorGUILayout.Slider("Active Count", context.info[IngredientsGoal.activeCount_parameter].Int, 1, 10));
    }
}