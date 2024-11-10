using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;
using Yurowm.GameCore;

[BerryPanelGroup("Monetization")]
[BerryPanelTab("Advertising")]
public class AdvertisingEditor : MetaEditor<Advertising> {

    public override Advertising FindTarget() {
        return Advertising.main;
    }

    List<IAdIntegration> integrations = new List<IAdIntegration>();
    Dictionary<string, IAdIntegration.Parameters> parameters = new Dictionary<string, IAdIntegration.Parameters>();
    Dictionary<Type, AdIntegrationEditor> editors = new Dictionary<Type, AdIntegrationEditor>();

    public override bool Initialize() {
        if (!metaTarget)
            return false;

        List<AdIntegrationEditor> editors = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => !x.IsAbstract && (typeof(AdIntegrationEditor)).IsAssignableFrom(x))
            .Select(x => (AdIntegrationEditor) Activator.CreateInstance(x)).ToList();
        foreach (Type type in IAdIntegration.allTypes) {
            IAdIntegration.Parameters parameters = metaTarget.parameters.FirstOrDefault(x => x.typeName == type.FullName);
            IAdIntegration integration = null;
            if (parameters != null)
                integration = IAdIntegration.Deserialize(parameters);
            if (integration == null) {
                parameters = new IAdIntegration.Parameters(type.FullName, "");
                integration = (IAdIntegration) Activator.CreateInstance(type);
            }
            integrations.Add(integration);
            this.editors.Set(integration.GetType(), editors.FirstOrDefault(x => x.IsSuitable(integration)));
            this.parameters.Set(parameters.typeName, parameters);
        }
        this.editors.RemoveAll(x => x.Value == null);
        return true;
    }

    public override void OnGUI() {
        AdIntegrationEditor editor;
        Undo.RecordObject(metaTarget, "Advertings");

        GUILayout.Label("Settings", Styles.title);
        metaTarget.adDelay = Mathf.RoundToInt(EditorGUILayout.Slider("Ad Minimal Delay (min)", metaTarget.adDelay, 0, 59));
        metaTarget.adFreeLevels = Mathf.RoundToInt(EditorGUILayout.Slider("Ad Free Levels", metaTarget.adFreeLevels, 1, 20));

        foreach (IAdIntegration integration in integrations) {
            EditorGUILayout.Space();
            GUILayout.Label(integration.GetName(), Styles.title);
            using (new GUIHelper.Change(() => Save(integration))) {
                integration.active = EditorGUILayout.Toggle("Active", integration.active);
                if (integration.active) {
                    integration.typeMask = (AdType) EditorGUILayout.EnumMaskField("Type Mask", integration.typeMask);
                    editor = editors.Get(integration.GetType());
                    if (editor != null) editor.OnGUI(integration);
                }
            }
        }
    }

    void Save(IAdIntegration integration) {
        parameters[integration.GetType().FullName].json = JsonUtility.ToJson(integration);
        metaTarget.parameters = parameters.Values.ToList();
    }
}
