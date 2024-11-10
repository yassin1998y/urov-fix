using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.GameCore;

namespace Yurowm.EditorCore {
    [BerryPanelGroup("Development")]
    [BerryPanelTab("S.D.Symbols", "ScriptingTabIcon")]
    public class ScriptingDefineSymbolsEditor : MetaEditor {

        List<IScriptingDefineSymbol> symbols;
        SymbolsTree tree;

        GUIHelper.LayoutSplitter splitter = null;
        GUIHelper.Scroll scroll = null;

        public override bool Initialize() {
            string sds = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Trim();
            List<string> defines = string.IsNullOrEmpty(sds) ? new List<string>() : sds.Split(';').Select(x => x.Trim().ToUpper()).ToList();
            defines.Sort();

            symbols = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsAbstract && (typeof(IScriptingDefineSymbol)).IsAssignableFrom(x))
                .Select(x => (IScriptingDefineSymbol) Activator.CreateInstance(x)).ToList();
            symbols.ForEach(s => {
                s.enable = defines.Contains(x => x == s.GetSybmol().ToUpper());
            });
            symbols.Sort((x, y) => x.GetSybmol().CompareTo(y.GetSybmol()));

            tree = new SymbolsTree(defines.Select(x => new Symbol(x)).ToList(), null);

            splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200);
            splitter.drawCursor = x => GUI.Box(x, "", Styles.separator);

            scroll = new GUIHelper.Scroll(GUILayout.ExpandHeight(true));
            return true;
        }

        Color greenButton = new Color(.5f, 1f, .5f);
        Color redButton = new Color(1f, .5f, .5f);
        public override void OnGUI() {
            using (new GUIHelper.Lock(EditorApplication.isCompiling)) {
                if (GUILayout.Button("Save", GUILayout.Width(100))) {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join("; ", tree.itemCollection.Select(x => x.symbol).ToArray()));
                    if (Content.main) Content.main.SDSymbols = tree.itemCollection.Select(x => x.symbol).ToArray();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                using (splitter.Start(true, symbols.Count > 0)) {
                    if (splitter.Area(Styles.area)) {
                        GUILayout.Label("Symbols", Styles.title);
                        tree.OnGUI(GUILayout.ExpandHeight(true));
                    }

                    if (splitter.Area()) {
                        using (scroll.Start()) {
                            foreach (var symbol in symbols) {
                                using (new GUIHelper.Vertical(Styles.levelArea, GUILayout.ExpandWidth(true))) {
                                    GUILayout.Label(symbol.GetSybmol(), Styles.whiteBoldLabel);
                                    GUILayout.Box(symbol.GetDescription(), Styles.textAreaLineBreaked, GUILayout.ExpandWidth(true));
                                    using (new GUIHelper.Horizontal()) {
                                        string berryLink = symbol.GetBerryLink();
                                        if (symbol.enable) {
                                            using (new GUIHelper.Color(redButton))
                                                if (GUILayout.Button("Disable", GUILayout.Width(100))) {
                                                    symbol.enable = false;
                                                    tree.itemCollection.RemoveAll(x => x.symbol == symbol.GetSybmol().ToUpper());
                                                    tree.Reload();
                                                }
                                        } else {
                                            using (new GUIHelper.Color(greenButton))
                                                if (GUILayout.Button("Enable", GUILayout.Width(100))) {
                                                    symbol.enable = true;
                                                    tree.itemCollection.Add(new Symbol(symbol.GetSybmol().ToUpper()));
                                                    tree.Reload();
                                                }
                                        }
                                    }
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                }
            }
        }

        class Symbol {
            public string symbol = null;
            public int id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            public Symbol(string symbol) {
                this.symbol = symbol;
                if (!symbol.IsNullOrEmpty())
                    id = symbol.GetHashCode();
            }
        }
        class SymbolsTree : GUIHelper.NonHierarchyList<Symbol> {

            public SymbolsTree(List<Symbol> collection, string name) : base(collection, new UnityEditor.IMGUI.Controls.TreeViewState(), name) {
            }

            public override Symbol CreateItem() {
                return new Symbol(null);
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                rect.xMin += 16;
                if (!info.content.symbol.IsNullOrEmpty())
                    GUI.Label(rect, info.content.symbol);
            }

            public override int GetUniqueID(Symbol element) {
                return element.id;
            }

            public override bool ObjectToItem(UnityEngine.Object o, out IInfo result) {
                result = null;
                return false;
            }

            public override string GetName(Symbol element) {
                return element.symbol;
            }

            public override void SetName(Symbol element, string name) {
                element.symbol = name;
            }

            protected override void RenameEnded(RenameEndedArgs args) {
                args.newName = args.newName.ToUpper();
                base.RenameEnded(args);
            }
        }
    }
}
