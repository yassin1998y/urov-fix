using System;
using Yurowm.GameCore;
using UnityEngine;

#if CHARTBOOST
using ChartboostSDK;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using System.Reflection;
#endif

public class ChartboostIntegration : IAdIntegration {
    bool downloading = false;

    public override void Initialize() {
        Chartboost.setAutoCacheAds(true);
        Chartboost.didCloseInterstitial += a => {if (onComplete != null) onComplete();};
        Chartboost.didCacheInterstitial += a => downloading = false;
    }

    public override void OnUpdate() {
        if (!Chartboost.hasInterstitial(CBLocation.Default) && !downloading) {
            downloading = true;
            Chartboost.cacheInterstitial(CBLocation.Default);
        }
    }

    public override string GetAppID() {
        return null;
    }

    public override string GetZoneID(AdType type) {
        return "{0}_{1}".FormatText(UIAssistant.main.GetCurrentPage().name, type);
    }

    public override bool IsReady(AdType type) {
        return Chartboost.hasInterstitial(CBLocation.Default);
    }

    Action onComplete;
    public override void Show(AdType type, Action onComplete) {
        this.onComplete = onComplete;
        Chartboost.showInterstitial(CBLocation.locationFromName(GetZoneID(type)));
    }

    public override string GetName() {
        return "Chartboost";
    }
}

#if UNITY_EDITOR
public class ChartboostIntegrationEditor : AdIntegrationEditor<ChartboostIntegration> {
    public ChartboostIntegrationEditor() : base() {
        var instance = GameObject.FindObjectOfType<Chartboost>();
        if (!instance) Chartboost.Create();
    }

    public override void DrawSettings(ChartboostIntegration integration) {
        if (GUILayout.Button("Edit Settings", GUILayout.Width(200)))
            CBSettings.Edit();
    }
}
#endif
#endif

public class CHARTBOOST_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return null;
    }

    public override string GetDescription() {
        return "The implementation of Chartboost advertising network. Requires Chartboost SDK";
    }

    public override string GetSybmol() {
        return "CHARTBOOST";
    }
}