using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using UnityEditor.IMGUI.Controls;
using Combination = MatchClickRule.Combination;
using System;

[BerryPanelGroup("Content")]
[BerryPanelTab("Match-Click")]
public class MatchClickRuleEditor : LevelRuleEditor<MatchClickRule> {
      
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

        foreach (MatchClickRule rule in rules.Values)
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

    protected override void OnSelectedAnotherRule(MatchClickRule rule) {
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
            currentRule.hint_delay = EditorGUILayout.Slider("Delay of Showing Hints", currentRule.hint_delay, 0f, 30f);
        }

        if (bombs.Count == 0)
            EditorGUILayout.HelpBox("No powerups found", MessageType.Error, true);

        #region Combinations
        using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
            GUILayout.Label("Combinations", Styles.title);
            using (new GUIHelper.Horizontal()) {
                GUILayout.Label("Order", Styles.centeredMiniLabel, GUILayout.Width(45));
                GUILayout.Label("Count", Styles.centeredMiniLabel, GUILayout.Width(40));
                GUILayout.Label("Vert.", Styles.centeredMiniLabel, GUILayout.Width(40));
                GUILayout.Label("Horiz.", Styles.centeredMiniLabel, GUILayout.Width(40));
                GUILayout.Label("Bomb", Styles.centeredMiniLabel, GUILayout.Width(120));
                GUILayout.Label("Helper", Styles.centeredMiniLabel, GUILayout.Width(100));
            }

            combinationList.rule = currentRule;
            combinationList.OnGUI();

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
        public MatchClickRule rule;

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
            r.width = 30;
            GUI.Label(r, (info.index + 1).ToString() + ".", Styles.centeredMiniLabel);
            r.x += r.width;

            r.width = 40;
            info.content.minCount = Mathf.Max(EditorGUI.IntField(r, info.content.minCount), 4);
            r.x += r.width + 3;

            r.width = 40;
            info.content.minVCount = Mathf.Max(EditorGUI.IntField(r, info.content.minVCount), 1);
            r.x += r.width + 3;

            r.width = 40;
            info.content.minHCount = Mathf.Max(EditorGUI.IntField(r, info.content.minHCount), 1);
            r.x += r.width + 3;

            info.content.vert = info.content.minVCount >= info.content.minHCount;

            r.width = 120;
            info.content.bomb = ContentPopup(r, info.content.bomb, bombs, bombNames);
            r.x += r.width + 3;

            r.width = 100;
            info.content.helper = (Texture2D) EditorGUI.ObjectField(r, info.content.helper, typeof (Texture2D), false);
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
