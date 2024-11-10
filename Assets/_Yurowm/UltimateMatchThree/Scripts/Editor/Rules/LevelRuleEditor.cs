using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class LevelRuleEditor<R> : MetaEditor where R : LevelRule {

    protected Dictionary<string, R> rules;
    protected R currentRule;

    AudioEditor.SoundSelector soundSelector;
    AudioEditor.TrackSelector trackSelector;

    ScoreBonusTree scoreBonusTree;

    string[] ruleNames;
    string currentRuleName = "";
    SerializedObject serializedRule;

    public override bool Initialize() {
        if (Content.main == null) {
            Debug.LogError("The Content manager is not found");
            return false;
        }
        rules = Content.GetPrefabList<R>().ToDictionary(x => x.name, x => x);
        if (rules.Count == 0) {
            Debug.LogError("The Content manager doesn't contain any suitable rules");
            return false;
        }
        ruleNames = rules.Keys.ToArray();
        currentRule = rules.First().Value;
        currentRuleName = ruleNames[0];
        serializedRule = new SerializedObject(currentRule);
        scoreBonusTree = new ScoreBonusTree(currentRule.scoreBonus);

        soundSelector = new AudioEditor.SoundSelector();
        trackSelector = new AudioEditor.TrackSelector();

        return true;
    }

    public override void OnGUI() {
        int currentRuleIndex = Mathf.Max(0, ruleNames.IndexOf(currentRuleName));
        using (new GUIHelper.Change(Refresh))
            currentRuleName = ruleNames.Get(EditorGUILayout.Popup("Rule", currentRuleIndex, ruleNames));
        currentRule = rules.Get(currentRuleName);

        if (serializedRule == null)
            serializedRule = new SerializedObject(currentRule);

        Undo.RecordObject(currentRule, "Rule Changed");

        EditorGUILayout.PropertyField(serializedRule.FindProperty("slotRenderer"));

        currentRule.soundTrack = trackSelector.Select("Sound Track", currentRule.soundTrack);
        currentRule.movesOfferCount = Mathf.Max(0, EditorGUILayout.IntField("Moves Offer Count", currentRule.movesOfferCount));
        if (currentRule.movesOfferCount > 0)
            currentRule.movesOfferPrice = Mathf.Max(1, EditorGUILayout.IntField("Moves Offer Price", currentRule.movesOfferPrice));

        using (new GUIHelper.Horizontal(Styles.area)) {
            scoreBonusTree.OnGUI(EditorGUILayout.GetControlRect(GUILayout.Width(150), GUILayout.Height(Mathf.Max(50, scoreBonusTree.totalHeight))));
            if (scoreBonusTree.selected != null) {
                using (new GUIHelper.Vertical()) {
                    scoreBonusTree.selected.name = EditorGUILayout.TextField("Name", scoreBonusTree.selected.name);
                    scoreBonusTree.selected.score = Mathf.Max(1, EditorGUILayout.IntField("Bonus", scoreBonusTree.selected.score));
                    scoreBonusTree.selected.text = EditorGUILayout.TextField("Text", scoreBonusTree.selected.text);
                    scoreBonusTree.selected.clip = soundSelector.Select("Sound", scoreBonusTree.selected.clip);
                }
            }
        }

        serializedRule.ApplyModifiedProperties();
    }

    protected void Refresh() {
        R rule = rules.Get(currentRuleName);
        serializedRule = rule ? new SerializedObject(rule) : null;
        scoreBonusTree = new ScoreBonusTree(rule.scoreBonus);
        OnSelectedAnotherRule(rule);
    }

    protected virtual void OnSelectedAnotherRule(R rule) {}

    class ScoreBonusTree : GUIHelper.NonHierarchyList<ScoreBonus> {

        public ScoreBonus selected = null;

        public ScoreBonusTree(List<ScoreBonus> collection) : base(collection, new TreeViewState(), "Score Bonuses") {
            onSelectedItemChanged += x => selected = x.FirstOrDefault();
        }

        public override ScoreBonus CreateItem() {
            ScoreBonus element = new ScoreBonus();
            element.id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            return element;
        }

        public override void DrawItem(Rect rect, ItemInfo info) {
            GUI.Label(rect, info.content.name.IsNullOrEmpty() ? "-" : info.content.name.ToString());
        }

        public override int GetUniqueID(ScoreBonus element) {
            return element.id;
        }

        public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
            result = null;
            return false;
        }

        protected override bool CanRename(TreeViewItem item) {
            return false;
        }
    }
}
