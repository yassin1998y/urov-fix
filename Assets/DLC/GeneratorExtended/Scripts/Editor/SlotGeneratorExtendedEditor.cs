using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

public class SlotGeneratorExtendedEditor : SlotEditorExtension {
    Dictionary<string, IChip> content;
    List<SlotEditorExtension> editorExtensions;
    Color removeColor = new Color(1, .5f, .5f);
    Color addColor = new Color(.5f, 1, 1);

    public SlotGeneratorExtendedEditor() {
        content = Content.GetPrefabList<IChip>().OrderBy(x => x.name).ToDictionary(c => c.name, c => c);
        Type refType = typeof(SlotEditorExtension);
        editorExtensions = refType.Assembly.GetTypes()
            .Where(x => !x.IsAbstract && refType.IsAssignableFrom(x) && x != typeof (SlotGeneratorExtendedEditor))
            .Select(x => (SlotEditorExtension) Activator.CreateInstance(x))
            .Where(x => content.Contains(c => x.IsCompatibleWith(c.Value)))
            .ToList();
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is SlotGeneratorExtended;
    }

    Dictionary<LevelParameter, SlotContent> casesDict = new Dictionary<LevelParameter, SlotContent>();
    string currentCaseName = "";
    List<LevelParameter> buffer = null;
    public override void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> info) {

        SlotContent current = info.Values.First();
        using (new GUIHelper.Horizontal()) {
            if (info.Count == 1) {
                using (new GUIHelper.Color(addColor))
                    if (GUILayout.Button("ADD...", EditorStyles.miniButton, GUILayout.Width(50))) {
                        GenericMenu menu = new GenericMenu();
                        foreach (var chip in content) {
                            IChip _chip = chip.Value;
                            menu.AddItem(new GUIContent(chip.Key), false, () => AddNewCase(current, _chip));
                        }
                        menu.ShowAsContext();
                    }
            }
            GUILayout.FlexibleSpace();

            using (new GUIHelper.Lock(info.Count > 1))
                if (GUILayout.Button("COPY", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    buffer = current.parameters;
            using (new GUIHelper.Lock(buffer == null))
                if (GUILayout.Button("PASTE", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    info.ForEach(i => i.Value.parameters = buffer.Select(x => x.Clone()).ToList());
        }

        if (info.Count > 1) {
            GUILayout.Label("Multi-object editing not supported");
            return;
        }
        
        LevelParameter weight;
        IChip prefab;

        casesDict.Clear();
        foreach (LevelParameter parameter in current.parameters)
            casesDict[parameter] = SlotContent.Deserialize(XElement.Parse(parameter.Text));

        float totalWeight = casesDict.Values.Sum(c => c[SlotGeneratorExtended.weight_parameter].Float);
        foreach (var c in casesDict) {
            using (new GUIHelper.Change(() => c.Key.Text = c.Value.Serialize("case").ToString())) {
                prefab = content[c.Value.name];

                weight = c.Value[SlotGeneratorExtended.weight_parameter];

                using (new GUIHelper.Horizontal()) {
                    using (new GUIHelper.Color(removeColor))
                        if (GUILayout.Button("X", EditorStyles.miniButtonLeft, GUILayout.Width(20))) {
                            current.parameters.Remove(c.Key);
                            GUI.FocusControl("");
                        }

                    if (GUILayout.Button(prefab.name + " (" + (100f * weight.Float / totalWeight).ToString("F1") + "%)", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true))) {
                        currentCaseName = c.Key.name;
                        GUI.FocusControl("");
                    }
                }

                if (currentCaseName == c.Key.name) {
                    weight.Float = Mathf.Max(1, EditorGUILayout.FloatField("Weight", weight.Float));
                    foreach (var editor in editorExtensions)
                        if (editor.IsCompatibleWith(prefab))
                            editor.OnSlotEditorGUI(design, ToInfo(c.Value));
                }
            }
        }
    }

    private void AddNewCase(SlotContent current, IChip _chip) {
        SlotContent slotContent = new SlotContent(_chip.name, SlotContent.Type.Chip);
        slotContent[SlotGeneratorExtended.weight_parameter].Float = 1;
        current.parameters.Add(new LevelParameter("new") {
            Text = slotContent.Serialize("case").ToString()
        });
        int counter = 0;
        current.parameters.ForEach(p => p.name = "case" + ++counter);
    }

    Dictionary<int2, SlotContent> subInfo = new Dictionary<int2, SlotContent>() {
        { int2.zero, null }
    };
    Dictionary<int2, SlotContent> ToInfo(SlotContent slotContent) {
        subInfo[int2.zero] = slotContent;
        return subInfo;
    }
}