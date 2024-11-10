using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;
#if ADMOB

#if UNITY_EDITOR
using UnityEditor;
#endif

using GoogleMobileAds.Api;
using AdMobInterstitialAd = GoogleMobileAds.Api.InterstitialAd;

public class AdMobIntegration : IAdIntegration {

    public string Regular_Android = "";
    public string Rewarded_Android = "";
    public string Regular_iOS = "";
    public string Rewarded_iOS = "";
    Dictionary<AdType, AdMobInterstitialAd> interstitial = new Dictionary<AdType, AdMobInterstitialAd>();

    public override void Initialize() {
        if ((typeMask & AdType.Regular) != 0) interstitial.Add(AdType.Regular, null);
        if ((typeMask & AdType.Rewarded) != 0) interstitial.Add(AdType.Rewarded, null);
    }

    public override void OnUpdate() {
        foreach (AdType adType in Enum.GetValues(typeof (AdType))) {
            if (!interstitial.ContainsKey(adType) || interstitial[adType] == null) {
                AdMobInterstitialAd interstitial = new AdMobInterstitialAd(GetZoneID(adType));
                interstitial.LoadAd(new AdRequest.Builder().Build());
                interstitial.OnAdFailedToLoad += (a, b) => interstitial = null;
                interstitial.OnAdClosed += (a, b) => {
                    if (onComplete != null) onComplete();
                    interstitial.Destroy();
                };
                this.interstitial.Set(adType, interstitial);
            }
        }
    }

    public override string GetAppID() {
        return null;
    }

    public override string GetZoneID(AdType type) {
        switch (Application.platform) {
            case RuntimePlatform.Android: {
                    switch (type) {
                        case AdType.Regular: return Regular_Android;
                        case AdType.Rewarded: return Rewarded_Android;
                    }
                } break;
            case RuntimePlatform.IPhonePlayer: {
                    switch (type) {
                        case AdType.Regular: return Regular_iOS;
                        case AdType.Rewarded: return Rewarded_iOS;
                    }
                } break;
        }
        return "";
    }

    public override bool IsReady(AdType type) {
        return interstitial.ContainsKey(type) && interstitial[type] != null &&
            interstitial[type].IsLoaded();
    }

    Action onComplete = null;
    public override void Show(AdType type, Action onComplete) {
        if (interstitial.ContainsKey(type)) {
            this.onComplete = onComplete;
            interstitial[type].Show();
        }
    }

    public override string GetName() {
        return "AdMob";
    }
}

#if UNITY_EDITOR
public class AdMobIntegrationEditor : AdIntegrationEditor<AdMobIntegration> {
    public override void DrawSettings(AdMobIntegration integration) {
        integration.Regular_Android = EditorGUILayout.TextField("Android Regular ID", integration.Regular_Android);
        integration.Rewarded_Android = EditorGUILayout.TextField("Android Rewarded ID", integration.Rewarded_Android);
        integration.Regular_iOS = EditorGUILayout.TextField("iOS Regular ID", integration.Regular_iOS);
        integration.Rewarded_iOS = EditorGUILayout.TextField("iOS Rewarded ID", integration.Rewarded_iOS);
    }
}
#endif
#endif

public class ADMOB_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return null;
    }

    public override string GetDescription() {
        return "The implementation of Google AdMob advertising network. Requires AdMob SDK";
    }

    public override string GetSybmol() {
        return "ADMOB";
    }
}