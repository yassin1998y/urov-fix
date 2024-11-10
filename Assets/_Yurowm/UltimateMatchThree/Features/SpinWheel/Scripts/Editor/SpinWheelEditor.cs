using UnityEngine;
using System.Collections;
using Yurowm.EditorCore;
using System;
using Reward = SpinWheelAssistant.Reward;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;

[BerryPanelGroup("Content")]
[BerryPanelTab("Spin Wheel")]
public class SpinWheelEditor : MetaEditor<SpinWheelAssistant> {
    Color rareColor = new Color(1, .8f, 0.4f, 1f);
    Color uniqueColor = new Color(.4f, .6f, 1, 1f);

    public override SpinWheelAssistant FindTarget() {
        return SpinWheelAssistant.main;
    }

    public override bool Initialize() {
        if (!metaTarget) return false;
        if (metaTarget.rewards == null || metaTarget.rewards.Count != SpinWheelAssistant.itemCount) {
            metaTarget.rewards = new List<Reward>(SpinWheelAssistant.itemCount);
            for (int i = 0; i < SpinWheelAssistant.itemCount; i++)
                metaTarget.rewards.Add(new Reward(ItemID.coin, 1, 1f));
        }
        return true;
    }

    public override void OnGUI() {
        float sum = metaTarget.rewards.Sum(x => x.weight);

        int index = 0;
        foreach (Reward reward in metaTarget.rewards) {
            //using (new GUIHelper.Horizontal(GUILayout.Width(300))) {
            //    using (new GUIHelper.BackgroundColor(Color.red))
            //        if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(20))) {
            //            metaTarget.rewards.Remove(reward);
            //            GUI.FocusControl("");
            //            break;
            //        }

            //    if (GUILayout.Button(string.Format("{0} ({1:F2}%)", reward.item, 100f * reward.weight / sum, GUILayout.Width(20)), EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true))) {
            //        current = current == reward ? null : reward;
            //        GUI.FocusControl("");
            //    }
            //}
            //if (reward == current)
            using (reward.type == Reward.Type.Regular ? null : new GUIHelper.BackgroundColor(reward.type == Reward.Type.Rare ? rareColor : uniqueColor))
                using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                    index++;
                    GUILayout.Label(string.Format("Reward #{0} ({1:F2}%)", index, 100f * reward.weight / sum), Styles.title);
                    reward.item = (ItemID) EditorGUILayout.EnumPopup("Item ID", reward.item);
                    reward.type = (Reward.Type) EditorGUILayout.EnumPopup("Reward Type", reward.type);
                    reward.count = Mathf.Max(1, EditorGUILayout.IntField("Count", reward.count));
                    reward.weight = Mathf.Max(0f, EditorGUILayout.FloatField("Weight", reward.weight));
                    reward.icon = (Sprite) EditorGUILayout.ObjectField("Icon", reward.icon, typeof(Sprite), false);
                }
        }
    }
}