using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using UnityEditor.IMGUI.Controls;
using Combination = MatchThreeSwapRule.Combination;
using System;

[BerryPanelGroup("Content")]
[BerryPanelTab("Match-Three")]
public class MatchThreeRuleEditor : LevelRuleEditor<MatchThreeSwapRule> {
    
    static List<IChip> bombs;
    static string[] bombNames;

    List<IChipMix> mixes;
    string[] mixNames;

    Pair<IChip> currentMixPair = null;
    BombMix currentMix = null;

    CombinationList combinationList;
    MixesList mixesList;

    public override bool Initialize() {
        if (!base.Initialize())
            return false;

        foreach (MatchThreeSwapRule rule in rules.Values)
            if (rule.combinations == null) 
                rule.combinations = new List<Combination>();

        bombs = Content.GetPrefabList<IChip>();
        bombNames = bombs.Select(x => (x as IChip).name).ToArray();

        mixes = Content.GetPrefabList<IChipMix>();
        mixNames = mixes.Select(x => x.name).ToArray();

        if (bombs.Count > 0)
            currentMixPair = new Pair<IChip>(bombs[0], bombs[0]);

        Refresh();
        return true;
    }

    protected override void OnSelectedAnotherRule(MatchThreeSwapRule rule) {
        BombMix.Comparer comparer = new BombMix.Comparer();
        rule.bombMixes.RemoveAll(x => !x.mix || !x.firstBomb || !x.secondBomb);
        rule.bombMixes = rule.bombMixes.Distinct(comparer).ToList();
        SortMixes(rule.bombMixes);
        Pair<string> pair = new Pair<string>(currentMixPair.a.name, currentMixPair.b.name);
        currentMix = rule.bombMixes.FirstOrDefault(x => x.pair == pair);

        combinationList = new CombinationList(rule.combinations);
        mixesList = new MixesList(rule.bombMixes);
        mixesList.edited = currentMixPair;
        mixesList.onSelectedItemChanged += x => {
            if (x.Count == 1) {
                currentMixPair.a = x[0].firstBomb;
                currentMixPair.b = x[0].secondBomb;
                var selection =  mixesList.GetSelection();
                Refresh();
                mixesList.SetSelection(selection);
            }
        };
    }

    public override void OnGUI() {
        base.OnGUI();

        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            GUILayout.Label("Parameters", Styles.title);
            currentRule.squares = EditorGUILayout.Toggle("Square combinations", currentRule.squares);
            currentRule.swapOnlyInWaitMode = EditorGUILayout.Toggle("Swap only in Wait mode", currentRule.swapOnlyInWaitMode);
            currentRule.swap_duration = EditorGUILayout.Slider("Swap Duration", currentRule.swap_duration, 0.01f, 1f);
            currentRule.hint_delay = EditorGUILayout.Slider("Delay of Showing Hints", currentRule.hint_delay, 0f, 30f);
        }

        if (bombs.Count == 0)
            EditorGUILayout.HelpBox("No powerups found", MessageType.Error, true);

        #region Combinations
        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            GUILayout.Label("Combinations", Styles.title);
            using (new GUIHelper.Horizontal()) {
                GUILayout.Label("Order", Styles.centeredMiniLabel, GUILayout.Width(45));
                GUILayout.Label("Type", Styles.centeredMiniLabel, GUILayout.Width(80));
                GUILayout.Label("Count", Styles.centeredMiniLabel, GUILayout.Width(40));
                GUILayout.Label("Bomb", Styles.centeredMiniLabel, GUILayout.Width(120));
            }

            combinationList.rule = currentRule;
            combinationList.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(combinationList.totalHeight)));

            if (GUILayout.Button("New", EditorStyles.miniButton, GUILayout.Width(60))) {
                currentRule.combinations.Add(new Combination());
                combinationList.Reload();
            }
        }
        #endregion

        #region Mixes
        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            GUILayout.Label("Bomb Mixes", Styles.title);
            if (mixes.Count == 0) {
                EditorGUILayout.HelpBox("No mix assets found", MessageType.Error, true);
            } else {
                using (new GUIHelper.Horizontal()) {
                    using (new GUIHelper.Change(Refresh)) {
                        currentMixPair.a = ContentPopup(currentMixPair.a, bombs, bombNames, GUILayout.Width(100));
                        GUILayout.Label("+", GUILayout.Width(12));
                        currentMixPair.b = ContentPopup(currentMixPair.b, bombs, bombNames, GUILayout.Width(100));
                        GUILayout.Label("=>", GUILayout.Width(20));
                    }
                    if (currentMix == null) {
                        if (!(currentMixPair.a is IDefaultSlotContent) || !(currentMixPair.b is IDefaultSlotContent))
                            using (new GUIHelper.BackgroundColor(Color.cyan))
                                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20))) {
                                    currentMix = new BombMix();
                                    currentMix.firstBomb = currentMixPair.a;
                                    currentMix.secondBomb = currentMixPair.b;
                                    currentMix.mix = mixes[0];
                                    currentRule.bombMixes.Add(currentMix);
                                    SortMixes(currentRule.bombMixes);
                                    mixesList.Reload();
                                }
                    } else {
                        using (new GUIHelper.BackgroundColor(Color.red))
                            if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(20))) {
                                currentRule.bombMixes.Remove(currentMix);
                                currentMix = null;
                                mixesList.Reload();
                            }
                        if (currentMix != null)
                            currentMix.mix = ContentPopup(currentMix.mix, mixes, mixNames, EditorStyles.miniButtonRight, GUILayout.Width(150));
                    }
                }

                mixesList.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(mixesList.totalHeight)));
            }
        }
        
        #endregion

        EditorUtility.SetDirty(currentRule);
    }

    void SortMixes(List<BombMix> mixes) {
        foreach (BombMix mix in mixes) {
            if (mix.firstBomb.name.CompareTo(mix.secondBomb.name) > 0) {
                var z = mix.firstBomb;
                mix.firstBomb = mix.secondBomb;
                mix.secondBomb = z;
            }
        }
        mixes.Sort((x, y) => x.firstBomb.name.CompareTo(y.firstBomb.name) * 2 +
            x.secondBomb.name.CompareTo(y.secondBomb.name));
    }

    static C ContentPopup<C>(C current, List<C> bombs, string[] names, params GUILayoutOption[] options) where C : ILiveContent {
        return ContentPopup(current, bombs, names, EditorStyles.miniButton, options);
    }

    static C ContentPopup<C>(C current, List<C> bombs, string[] names, GUIStyle style, params GUILayoutOption[] options) where C : ILiveContent {
        if (bombs.Count > 0) {
            int bombIndex = Mathf.Max(0, bombs.IndexOf(current));
            return bombs.Get(EditorGUILayout.Popup(bombIndex, names, style, options));
        }
        return null;
    }

    static C ContentPopup<C>(Rect rect, C current, List<C> bombs, string[] names, GUIStyle style = null) where C : ILiveContent {
        if (bombs.Count > 0) {
            int bombIndex = Mathf.Max(0, bombs.IndexOf(current));
            return bombs.Get(style == null ? EditorGUI.Popup(rect, bombIndex, names) : EditorGUI.Popup(rect, bombIndex, names, style));
        }
        return null;
    }


    class CombinationList : GUIHelper.NonHierarchyList<Combination> {
        public MatchThreeSwapRule rule;

        public CombinationList(List<Combination> collection) : base(collection, new TreeViewState()) {}

        public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            if (selected.Count > 0 && !selected.Any(x => x.isFolderKind))
                menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
        }

        public override Combination CreateItem() {
            return new Combination();
        }

        public override void DrawItem(Rect rect, ItemInfo info) {

            Rect r = new Rect(rect);
            using (!rule.squares && info.content.square ? new GUIHelper.BackgroundColor(Color.red) : null) {
                r.width = 30;
                GUI.Label(r, (info.index + 1).ToString() + ".", Styles.centeredMiniLabel);
                r.x += r.width;

                r.width = 80;
                info.content.type = (Combination.Type) EditorGUI.EnumPopup(r, info.content.type);
                r.x += r.width + 3;

                r.width = 40;
                info.content.minCount = Mathf.Clamp(EditorGUI.IntField(r, info.content.minCount), 4, 9);
                r.x += r.width + 3;

                r.width = 120;
                info.content.bomb = ContentPopup(r, info.content.bomb, bombs, bombNames);
            }
        }

        public override int GetUniqueID(Combination element) {
            return element.uniqueID;
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

class MixesList : GUIHelper.NonHierarchyList<BombMix> {
    internal Pair<IChip> edited;

    public MixesList(List<BombMix> collection) : base(collection, new TreeViewState()) {}

    public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
        if (selected.Count > 0 && !selected.Any(x => x.isFolderKind))
            menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
    }

    public override BombMix CreateItem() {
        return new BombMix();
    }

    public override void DrawItem(Rect rect, ItemInfo info) {
        string result = "{0} + {1} => {2}";

        if (info.content.refPair == edited)
            result = string.Format(Styles.highlightStrongBlue, string.Format(result, info.content.firstBomb.name,
                info.content.secondBomb.name, info.content.mix.name));
        else {
            if (info.content.firstBomb == edited.a)
                result = result.Replace("{0}", string.Format(Styles.highlightGreen, info.content.firstBomb.name));
            else if (info.content.firstBomb == edited.b)
                result = result.Replace("{0}", string.Format(Styles.highlightRed, info.content.firstBomb.name));
            else
                result = result.Replace("{0}", info.content.firstBomb.name);

            if (info.content.secondBomb == edited.a)
                result = result.Replace("{1}", string.Format(Styles.highlightGreen, info.content.secondBomb.name));
            else if (info.content.secondBomb == edited.b)
                result = result.Replace("{1}", string.Format(Styles.highlightRed, info.content.secondBomb.name));
            else
                result = result.Replace("{1}", info.content.secondBomb.name);

            result = result.Replace("{2}", info.content.mix.name);
        }

        GUI.Label(rect, result, Styles.richLabel);
    }
        
    public override int GetUniqueID(BombMix element) {
        return element.firstBomb.GetInstanceID() * element.secondBomb.GetInstanceID();
    }

    public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
        result = null;
        return false;
    }

    protected override bool CanRename(TreeViewItem item) {
        return false;
    }
}
