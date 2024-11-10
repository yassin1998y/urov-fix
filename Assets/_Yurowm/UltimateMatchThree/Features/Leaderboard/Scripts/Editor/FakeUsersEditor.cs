using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Bot = Online.FakeUser;

[BerryPanelGroup("Content")]
[BerryPanelTab("Fake Users")]
public class FakeUsersEditor : MetaEditor<Online> {
    public override Online FindTarget() {
        return Online.main;
    }

    public override bool Initialize() {
        return metaTarget; 
    }

    public override void OnGUI() {
        Undo.RecordObject(metaTarget, "");

        GUILayout.Label("Offline Player", EditorStyles.boldLabel);
        DrawBot(ref metaTarget.me, () => metaTarget.me = new Bot());

        GUILayout.Label("Fake Users", EditorStyles.boldLabel);

        using (new GUIHelper.Horizontal()) {
            int line = 0;
            foreach (Bot user in metaTarget.fakeUsers) {
                line++;
                Bot b = user;
                bool breaker = false;
                DrawBot(ref b, () => breaker = true);
                if (breaker) break;

                if (line >= 4) {
                    line = 0;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

            }
            GUILayout.FlexibleSpace();
        }

        if (GUILayout.Button("Add", GUILayout.Width(80)))
            metaTarget.fakeUsers.Add(new Bot());
    }

    void DrawBot(ref Bot bot, Action remover) {
        using (new GUIHelper.Vertical()) {
            Rect rect = EditorGUILayout.GetControlRect(false, 80, GUILayout.Width(80));

            EditorGUI.DrawPreviewTexture(rect, bot.avatar == null ? Texture2D.blackTexture : bot.avatar.texture);
            rect.width = 20;
            rect.height = 20;

            if (GUI.Button(rect, "X")) {
                metaTarget.fakeUsers.Remove(bot);
                remover.Invoke();
            }

            bot.avatar = (Sprite) EditorGUILayout.ObjectField(bot.avatar, typeof(Sprite), false, GUILayout.Width(80));
            bot.name = EditorGUILayout.TextField(bot.name, GUILayout.Width(80));
        }
    }
}
