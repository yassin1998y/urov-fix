using System;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class DirtEditor : SlotEditorExtension {
    Texture2D dirtIcon;

    public DirtEditor() : base() {
        dirtIcon = EditorIcons.GetIcon("Dirt");
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is Dirt;
    }

    public override void DrawSlotIcon(Rect rect, SlotContent content) {
        using (new GUIHelper.Color(new Color(1, 1, 1, (content["layer"].Float) / 3)))
            GUI.DrawTexture(rect, dirtIcon);
    }
}