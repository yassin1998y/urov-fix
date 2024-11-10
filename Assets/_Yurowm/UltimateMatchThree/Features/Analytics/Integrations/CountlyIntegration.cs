using Yurowm.GameCore;
#if COUNTLY
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CountlyIntegration : IAnalyticsIntegration {

    public override void Event(string eventName) {
        CountlyManager.Emit(eventName, 1);
    }

    public override void Event(string eventName, float sum) {
        CountlyManager.Emit(eventName, 1, sum);
    }

    public override void Event(string eventName, Dictionary<string, string> segmentation) {
        CountlyManager.Emit(eventName, 1, segmentation);
    }

    public override void Event(string eventName, float sum, params string[] keys) {
        CountlyManager.Emit(eventName, 1, sum, KeysToSementation(keys));
    }

    public override string GetName() {
        return "Count.ly";
    }
}

#if UNITY_EDITOR
public class CountlyIntegrationEditor : AnalyticsIntegrationEditor<CountlyIntegration> {
    [System.NonSerialized]
    SerializedObject manager = null;

    public override void DrawSettings(CountlyIntegration integration) {
        if (CountlyManager.Instance == null) {
            EditorGUILayout.HelpBox("Countly Manager instance is not found. Use button below to create it.", MessageType.Error);
            if (GUILayout.Button("Create Instance", GUILayout.Width(120))) {
                new GameObject("CountlyManager").AddComponent<CountlyManager>();
            }
        } else {
            if (manager == null)
                manager = new SerializedObject(CountlyManager.Instance);
            var iterator = manager.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(true))
                EditorGUILayout.PropertyField(iterator, true);

            manager.ApplyModifiedProperties();
        }
    }
}
#endif
#endif

public class COUNTLY_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return null;
    }

    public override string GetDescription() {
        return "The implementation of Count.ly analytics network. Requires Countly SDK";
    }

    public override string GetSybmol() {
        return "COUNTLY";
    }
}