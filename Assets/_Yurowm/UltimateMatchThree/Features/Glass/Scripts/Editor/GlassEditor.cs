using System;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class GlassEditor : SlotEditorExtension {
    Texture2D glassIcon;

    public GlassEditor() : base() {
        glassIcon = EditorIcons.GetIcon("Glass");
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is Glass;
    }

    public override void DrawSlotIcon(Rect rect, SlotContent content) {
        using (new GUIHelper.Color(new Color(1, 1, 1, (content["layer"].Float) / 3)))
            GUI.DrawTexture(rect, glassIcon);
    }
}