using UnityEngine;
using System.Collections;
using UnityEditor;
using Yurowm.GameCore;
using Yurowm.EditorCore;

[CustomEditor(typeof(SetSpriteColor))]
public class SetSpriteColorEditor : Editor {

    SetSpriteColor component;

    void OnEnable() {
        component = target as SetSpriteColor;
        foreach (ItemColor color in ItemColorUtils.allColors) {
            if (color.IsPhysicalColor())
                if (component.sprites.Find(x => x.color == color) == null)
                    component.sprites.Add(new SetSpriteColor.SpriteInfo() { color = color });
        }

        component.sprites.Sort((x, y) => x.color.CompareTo(y.color));
    }

    public override void OnInspectorGUI() {
        Undo.RecordObject(component, "SetSpriteColor changed");

        component.target = (SetSpriteColor.Target) EditorGUILayout.EnumPopup("Target", component.target);
        component.action = (SetSpriteColor.Action) EditorGUILayout.EnumPopup("Action", component.action);

        switch (component.action) {
            case SetSpriteColor.Action.ChangeSprite: {
                    if (component.sprites.Count == 0)
                        OnEnable();
                    foreach (SetSpriteColor.SpriteInfo info in component.sprites) {
                        using (new GUIHelper.Horizontal()) {
                            info.sprite = (Sprite) EditorGUILayout.ObjectField(info.color.ToString(), info.sprite, typeof(Sprite), false);
                        }
                    }
                    break;
                }

            case SetSpriteColor.Action.ChangeColor: {
                    component.sprites.Clear();
                    break;
                }
        }
    }
}