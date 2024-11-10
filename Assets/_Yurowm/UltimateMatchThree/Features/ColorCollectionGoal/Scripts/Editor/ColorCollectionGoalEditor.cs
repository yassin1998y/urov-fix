using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class ColorCollectionGoalEditor : GoalEditorExtension {

    Color[] chipColor;
    static readonly string[] alphabet = { "A", "B", "C", "D" };

    public ColorCollectionGoalEditor() : base() {
        chipColor = Enum.GetValues(typeof(ItemColor)).Cast<ItemColor>().Where(x => x.IsPhysicalColor()).Select(x => Color.Lerp(RealColors.Get(x), Color.white, 0.4f)).ToArray();
    }

    protected override void OnModeEditorGUI(Context context) {
        int targetColorCount = context.info[ColorCollectionGoal.targetCount_parameter].Int;
        targetColorCount = Mathf.RoundToInt(EditorGUILayout.Slider("Collections Count", targetColorCount, 1, Mathf.Min(4, context.design.colorCount)));

        for (int i = 0; i < targetColorCount; i++) {
            string key = string.Format(ColorCollectionGoal.collectionSize_parameter, i);
            using (new GUIHelper.Color(chipColor[i]))
                context.info[key].Int = Mathf.Clamp(EditorGUILayout.IntField("Color " + alphabet[i].ToString(), context.info[key].Int), 1, 999);
        }

        context.info[ColorCollectionGoal.targetCount_parameter].Int = targetColorCount;
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is ColorCollectionGoal;
    }
}