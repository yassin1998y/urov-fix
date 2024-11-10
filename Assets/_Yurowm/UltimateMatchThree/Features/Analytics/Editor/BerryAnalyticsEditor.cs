using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

[BerryPanelGroup("Monetization")]
[BerryPanelTab("Analytics")]
public class BerryAnalyticsEditor : MetaEditor<BerryAnalytics> {

    List<IAnalyticsIntegration> integrations = new List<IAnalyticsIntegration>();
    Dictionary<string, IAnalyticsIntegration.Parameters> parameters = new Dictionary<string, IAnalyticsIntegration.Parameters>();
    Dictionary<Type, AnalyticsIntegrationEditor> editors = new Dictionary<Type, AnalyticsIntegrationEditor>();

    public override BerryAnalytics FindTarget() {
        return BerryAnalytics.main;
    }

    public override bool Initialize() {
        if (!metaTarget) return false;
        List<AnalyticsIntegrationEditor> editors = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => !x.IsAbstract && (typeof(AnalyticsIntegrationEditor)).IsAssignableFrom(x))
            .Select(x => (AnalyticsIntegrationEditor) Activator.CreateInstance(x)).ToList();
        foreach (Type type in IAnalyticsIntegration.allTypes) {
            IAnalyticsIntegration.Parameters parameters = metaTarget.parameters.FirstOrDefault(x => x.typeName == type.FullName);
            IAnalyticsIntegration integration = null;
            if (parameters != null)
                integration = IAnalyticsIntegration.Deserialize(parameters);
            if (integration == null) {
                parameters = new IAnalyticsIntegration.Parameters(type.FullName, "");
                integration = (IAnalyticsIntegration) Activator.CreateInstance(type);
            }
            integrations.Add(integration);
            this.editors.Set(integration.GetType(), editors.FirstOrDefault(x => x.IsSuitable(integration)));
            this.parameters.Set(parameters.typeName, parameters);
        }
        this.editors.RemoveAll(x => x.Value == null);
        return true;
    }

    public override void OnGUI() {
        AnalyticsIntegrationEditor editor;
        Undo.RecordObject(metaTarget, "Analytics");

        foreach (IAnalyticsIntegration integration in integrations) {
            EditorGUILayout.Space();
            GUILayout.Label(integration.GetName(), Styles.title);
            using (new GUIHelper.Change(() => Save(integration))) {
                editor = editors.Get(integration.GetType());
                if (editor != null) editor.OnGUI(integration);
                else EditorGUILayout.HelpBox("No Editor", MessageType.Info);
            }
        }
    }

    void Save(IAnalyticsIntegration integration) {
        parameters[integration.GetType().FullName].json = JsonUtility.ToJson(integration);
        metaTarget.parameters = parameters.Values.ToList();
    }
}
