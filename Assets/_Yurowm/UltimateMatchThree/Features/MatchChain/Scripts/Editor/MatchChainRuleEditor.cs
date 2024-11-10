using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;

[BerryPanelGroup("Content")]
[BerryPanelTab("Match-Chain")]
public class MatchChainRuleEditor : LevelRuleEditor<MatchChainRule> {

    public override void OnGUI() {
        base.OnGUI();
        EditorUtility.SetDirty(currentRule);
    }

}
