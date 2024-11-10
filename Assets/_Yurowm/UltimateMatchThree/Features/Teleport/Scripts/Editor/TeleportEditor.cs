using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class TeleportEditor : SlotEditorExtension {
    public override bool IsCompatibleWith(ILiveContent content) {
        return content is Teleport;
    }

    LevelDesign design;
    public override void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> content) {
        this.design = design;
        EUtils.DrawMixedProperty(content.Keys,
            mask: coord => true,
            getValue: coord => content[coord]["target"].Coordinate,
            setValue: (coord, value) => {
                if (coord == value || value == null)
                    content[coord]["target"].Coordinate = null;
                else
                    content[coord]["target"].Coordinate = value;
            },
            drawSingle: DrawFieldPreview,
            drawMixed: setDefault => {
                int2 result = DrawFieldPreview(null, null);
                if (result == null) return false;
                setDefault(result);
                return true;
            });
    }

    const int slotOffset = 2;
    const int slotSize = 15;
    int2 DrawFieldPreview(int2 position, int2 target) {
        int2 result = target;
        int2 size = design.fieldSize;

        if (target == null || !target.IsItHit(0, 0, size.x - 1, size.y - 1))
            result = null;

        Rect rect;
        #region Label
        using (new GUIHelper.Horizontal()) {
            GUILayout.Label("Target", GUILayout.ExpandWidth(true));
            rect = EditorGUILayout.GetControlRect(
                GUILayout.Width(size.x * slotSize + (size.x + 1) * slotOffset),
                GUILayout.Height(size.y * slotSize + (size.y + 1) * slotOffset));
            GUILayout.Space(10);
        }
        #endregion

        #region Background
        Handles.color = Color.cyan;
        Handles.DrawSolidRectangleWithOutline(rect, Color.black, Color.white);
        #endregion

        #region Field Preview
        Rect slotRect;
        foreach (int2 slot in LevelEditor.instance.slots.Keys) {
            slotRect = new Rect(
                slot.x * (slotSize + slotOffset) + slotOffset + rect.x,
                (size.y - slot.y - 1) * (slotSize + slotOffset) + slotOffset + rect.y,
                slotSize, slotSize);
            if (slot != position) {
                Handles.color = slot == target ? Color.cyan : Color.gray;
                Handles.DrawSolidRectangleWithOutline(slotRect, Color.white, Color.clear);
                if (GUI.Button(slotRect, "", LevelEditor.labelStyle)) {
                    result = slot.GetClone();
                }
            } else {
                Handles.color = Color.white;
                Handles.DrawSolidRectangleWithOutline(slotRect, Color.clear, target != null ? Color.cyan : Color.red);
            }

        }
        #endregion

        #region Arrow
        if (position != null && target != null) {
            Handles.color = Color.cyan;
            Handles.BeginGUI();
            Handles.DrawLine(
                new Vector3(
                    position.x * (slotSize + slotOffset) + slotOffset + rect.x + slotSize / 2,
                    (size.y - position.y - 1) * (slotSize + slotOffset) + slotOffset + rect.y + slotSize / 2),
                new Vector3(
                    target.x * (slotSize + slotOffset) + slotOffset + rect.x + slotSize / 2,
                    (size.y - target.y - 1) * (slotSize + slotOffset) + slotOffset + rect.y + slotSize / 2));
            Handles.EndGUI();
        }
        #endregion
        Handles.color = Color.white;
        return result;
    }
}