using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yurowm.EditorCore;
using System.Text.RegularExpressions;

[CustomEditor (typeof (DinaLabel))]
public class DinaLabelEditor : Editor {

    DinaLabel provider;
    List<string> mask_values;

    static string[] keys = new string[0];

    void OnEnable () {
        provider = (DinaLabel) target;

        if (LocalizationEditor.content == null)
            LocalizationEditor.LoadContent();

        if (!DinaLabel.initialized)
            DinaLabel.Initialize();

        if (LocalizationAssistant.main)
            keys = LocalizationEditor.GetKeyList();

        mask_values = DinaLabel.words.Keys.ToList();
        mask_values.Sort();
	}
	
	public override void OnInspectorGUI() {
        Undo.RecordObject(provider, "DinaLabel changes");

        provider.localized = EditorGUILayout.Toggle("Localized", provider.localized);

        List<string> masks = new List<string>();

        if (provider.localized) {
            #region Localized settings
            if (!LocalizationAssistant.main) {
                EditorGUILayout.HelpBox("Localization Assistant is missing", MessageType.Error);
            } else {
                using (new GUIHelper.Horizontal()) {
                    if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                        OnEnable();
                    if (LocalizationEditor.content.Count > 0 && GUILayout.Button("Set", GUILayout.Width(40))) {
                        provider.GetTargets();
                        provider.Set(LocalizationEditor.content.First().Value[provider.key]);
                    }
                }

                int id = Mathf.Max(0, System.Array.IndexOf(keys, provider.key));
                id = EditorGUILayout.Popup("key", id, keys);
                provider.key = keys[id];
                foreach (SystemLanguage language in LocalizationEditor.content.Keys.ToArray()) {
                    using (new GUIHelper.Horizontal()) {
                        EditorGUILayout.PrefixLabel(language.ToString());
                        if (LocalizationEditor.content[language].ContainsKey(provider.key)) {
                            using (new GUIHelper.Change(() => SaveLocalization(language)))
                                LocalizationEditor.content[language][provider.key] = EditorGUILayout.TextArea(LocalizationEditor.content[language][provider.key]);
                        } else if (GUILayout.Button("Add")) {
                            LocalizationEditor.content[language].Add(provider.key, "");
                            SaveLocalization(language);
                        }
                        masks.AddRange(GetMasks(LocalizationEditor.content[language][provider.key]));
                    }
                }
            }
            #endregion
        } else {
            #region Non localized settings
            provider.text = EditorGUILayout.TextArea(provider.text);
            masks.AddRange(GetMasks(provider.text));
            #endregion
        }

        masks = masks.Distinct().ToList();
        masks.Sort();

        Dictionary<string, DinaLabel.Mask> _masks = provider.masks.ToDictionary(x => x.key, x => x);

        foreach (string mask in masks) 
            if (!_masks.ContainsKey(mask))
                _masks.Add(mask, new DinaLabel.Mask(mask));

        if (_masks.Count > 0) {
            provider.update = EditorGUILayout.Toggle("Update Delay", provider.update);
            if (provider.update)
                provider.delay = EditorGUILayout.Slider("Delay", provider.delay, 0.1f, 3f);
        } else
            provider.update = false;

        #region Masks panel
        if (_masks.Count > 0) {
            foreach (string key in _masks.Keys.ToArray()) {
                if (!masks.Contains(key))
                    _masks.Remove(key);
                else {
                    _masks[key].item = key.Contains(":");
                    if (_masks[key].item) continue;
                    using (new GUIHelper.Horizontal()) {
                        EditorGUILayout.PrefixLabel(key);

                        int id = Mathf.Max(0, mask_values.IndexOf(_masks[key].value));
                        id = EditorGUILayout.Popup(id, mask_values.ToArray());
                        _masks[key].value = mask_values[id];
                    }
                }
            }
        }
        provider.masks = _masks.Values.ToList();
        #endregion
    }

    void SaveLocalization(SystemLanguage language) {
        LocalizationEditor.phrases = LocalizationEditor.content[language].Select(x => new LocalizationEditor.Entry(x.Key, x.Value)).ToList();
        LocalizationEditor.Save(language);
    }

    Regex maskFilter = new Regex(@"\{(?<key>[a-zA-Z0-9:]+)\}");
    List<string> GetMasks(string text) {
        List<string> result = new List<string>();
        foreach (Match match in maskFilter.Matches(text))
            result.Add(match.Groups["key"].Value);
        return result;
    }
}
