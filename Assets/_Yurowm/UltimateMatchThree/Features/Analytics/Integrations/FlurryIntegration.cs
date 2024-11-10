using Yurowm.GameCore;
#if FLURRY
using Analytics;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlurryIntegration : IAnalyticsIntegration {
    public string ApiKey_iOS = "";
    public string ApiKey_Android = "";
    IAnalytics flurry;

    public override void Initialize() {
        flurry = Flurry.Instance;
        flurry.SetLogLevel(LogLevel.All);
        flurry.StartSession(ApiKey_iOS, ApiKey_Android);
    }

    public override void Event(string eventName) {
        flurry.LogEvent(eventName);
    }

    public override void Event(string eventName, float sum) {
        flurry.LogEvent(eventName, new Dictionary<string, string> { { "Sum", sum.ToString() } });
    }

    public override void Event(string eventName, Dictionary<string, string> segmentation) {
        flurry.LogEvent(eventName, segmentation);
    }

    public override void Event(string eventName, float sum, params string[] keys) {
        var segmentation = KeysToSementation(keys);
        segmentation.Set("Sum", sum.ToString());
        flurry.LogEvent(eventName, segmentation);

    }

    public override string GetName() {
        return "Flurry";
    }
}

#if UNITY_EDITOR
public class FlurryIntegrationEditor : AnalyticsIntegrationEditor<FlurryIntegration> {
    public override void DrawSettings(FlurryIntegration integration) {
        integration.ApiKey_Android = EditorGUILayout.TextField("Android API Key", integration.ApiKey_Android);
        integration.ApiKey_iOS = EditorGUILayout.TextField("iOS API Key", integration.ApiKey_iOS);
    }
}
#endif
#endif

public class FLURRY_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return null;
    }

    public override string GetDescription() {
        return "The implementation of Flurry analytics network. Requires Flurry SDK";
    }

    public override string GetSybmol() {
        return "FLURRY";
    }
}