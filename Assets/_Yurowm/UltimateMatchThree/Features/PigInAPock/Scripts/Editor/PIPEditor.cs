using UnityEngine;
using System.Collections;
using Yurowm.EditorCore;
using System;
using Reward = PigInAPokeRewarder.Reward;
using System.Linq;
using UnityEditor;

[BerryPanelGroup("Content")]
[BerryPanelTab("PIP Rewards")]
public class PIPEditor : MetaEditor<PigInAPokeRewarder> {
    Reward current = null;

    public override PigInAPokeRewarder FindTarget() {
        return PigInAPokeRewarder.main;
    }

    public override bool Initialize() {
        if (metaTarget == null)
            return false;

        return true;
    }

    public override void OnGUI() {
        float sum = metaTarget.rewards.Sum(x => x.weight);

        foreach (Reward reward in metaTarget.rewards) {
            using (new GUIHelper.Horizontal(GUILayout.Width(300))) {
                using (new GUIHelper.BackgroundColor(Color.red))
                    if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(20))) {
                        metaTarget.rewards.Remove(reward);
                        GUI.FocusControl("");
                        break;
                    }

                if (GUILayout.Button(string.Format("{0} ({1:F2}%)", reward.item, 100f * reward.weight / sum, GUILayout.Width(20)), EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true))) {
                    current = current == reward ? null : reward;
                    GUI.FocusControl("");
                }
            }
            if (reward == current) 
                using (new GUIHelper.Vertical(Styles.area, GUILayout.Width(300))) {
                    reward.item = (ItemID) EditorGUILayout.EnumPopup("Item ID", reward.item);
                    reward.count = Mathf.Max(1, EditorGUILayout.IntField("Count", reward.count));
                    reward.weight = Mathf.Max(0f, EditorGUILayout.FloatField("Weight", reward.weight));
                    reward.icon = (Sprite) EditorGUILayout.ObjectField("Icon", reward.icon, typeof (Sprite), false);
                }
        }

        using (new GUIHelper.BackgroundColor(Color.green))
            if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(40))) {
                Reward newReward = null;
                if (current != null) {
                    newReward = new Reward(current.item, current.count, current.weight);
                    newReward.icon = current.icon;
                } else 
                    newReward = new Reward(default(ItemID), 1, 1f);
                current = newReward;
                metaTarget.rewards.Add(newReward);
                GUI.FocusControl("");
            }
    }
}
