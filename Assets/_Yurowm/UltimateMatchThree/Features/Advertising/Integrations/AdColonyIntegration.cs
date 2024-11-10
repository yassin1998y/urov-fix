using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;
#if ADCOLONY
using AdColony;
using AdColonyInterstitialAd = AdColony.InterstitialAd;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdColonyIntegration : IAdIntegration {
    public string AppID_Android = "";
    public string RewardedZoneID_Android = "";
    public string RegularZoneID_Android = "";
    public string AppID_iOS = "";
    public string RewardedZoneID_iOS = "";
    public string RegularZoneID_iOS = "";

    Dictionary<string, AdColonyAdAvailability> avaliable = new Dictionary<string, AdColonyAdAvailability>();

    class AdColonyAdAvailability {
        public bool requested = false;
        public bool avaliable = false;
        public AdColonyInterstitialAd ad;
        public void Clear() {
            requested = false;
            avaliable = false;
            ad = null;
        }
    }

    public override void Initialize() {
        AppOptions appOptions = new AppOptions();
        appOptions.UserId = SystemInfo.deviceUniqueIdentifier;
        appOptions.AdOrientation = AdOrientationType.AdColonyOrientationAll;
        List<string> zoneIDs = new List<string>();
        if ((typeMask & AdType.Regular) != 0) {
            zoneIDs.Add(GetZoneID(AdType.Regular));
            avaliable.Set(GetZoneID(AdType.Regular), new AdColonyAdAvailability());
        }
        if ((typeMask & AdType.Rewarded) != 0) {
            zoneIDs.Add(GetZoneID(AdType.Rewarded));
            avaliable.Set(GetZoneID(AdType.Rewarded), new AdColonyAdAvailability());
        }
        Ads.Configure(GetAppID(), appOptions, zoneIDs.ToArray());

        Ads.OnClosed += ad => {
            if (avaliable.ContainsKey(ad.ZoneId)) {
                avaliable[ad.ZoneId].requested = false;
                avaliable[ad.ZoneId].avaliable = false;
            }
            if (onComplete != null) onComplete();
        };
        Ads.OnRequestInterstitial += ad => {
            if (avaliable.ContainsKey(ad.ZoneId)) {
                avaliable[ad.ZoneId].avaliable = true;
                avaliable[ad.ZoneId].ad = ad;
            }
        };
        Ads.OnRequestInterstitialFailedWithZone += ad => {
            if (avaliable.ContainsKey(ad))
                avaliable[ad].Clear();
        };
        Ads.OnExpiring += ad => {
            if (avaliable.ContainsKey(ad.ZoneId))
                avaliable[ad.ZoneId].Clear();
            Ads.RequestInterstitialAd(ad.ZoneId, null);
            avaliable[ad.ZoneId].requested = true;
        };

        foreach (var ad in avaliable) {
            Ads.RequestInterstitialAd(ad.Key, null);
            ad.Value.requested = true;
        }
    }

    public override string GetAppID() {
        switch (Application.platform) {
            case RuntimePlatform.Android: return AppID_Android;
            case RuntimePlatform.IPhonePlayer: return AppID_iOS;
        }
        return "";
    }

    public override string GetZoneID(AdType type) {
        if (Application.platform == RuntimePlatform.Android)
            switch (type) {
                case AdType.Regular: return RegularZoneID_Android;
                case AdType.Rewarded: return RewardedZoneID_Android;
            } 
        if (Application.platform == RuntimePlatform.IPhonePlayer) 
            switch (type) {
                case AdType.Regular: return RegularZoneID_iOS;
                case AdType.Rewarded: return RewardedZoneID_iOS;
            } 
        return "";
    }

    public override bool IsReady(AdType type) {
        string zoneID = GetZoneID(type);
        return avaliable.ContainsKey(zoneID) && avaliable[zoneID].avaliable;
    }

    Action onComplete = null;
    public override void Show(AdType type, Action onComplete) {
        this.onComplete = onComplete;
        string zoneID = GetZoneID(type);
        if (avaliable[zoneID].avaliable)
            Ads.ShowAd(avaliable[zoneID].ad);
    }

    public override string GetName() {
        return "AdColony";
    }
}

#if UNITY_EDITOR
public class AdColonyIntegrationEditor : AdIntegrationEditor<AdColonyIntegration> {
    public override void DrawSettings(AdColonyIntegration integration) {
        integration.AppID_Android = EditorGUILayout.TextField("Android AppID", integration.AppID_Android);
        integration.RegularZoneID_Android = EditorGUILayout.TextField("Android Regular ZoneID", integration.RegularZoneID_Android);
        integration.RewardedZoneID_Android = EditorGUILayout.TextField("Android Rewarded ZoneID", integration.RewardedZoneID_Android);
        integration.AppID_iOS = EditorGUILayout.TextField("iOS AppID", integration.AppID_iOS);
        integration.RegularZoneID_iOS = EditorGUILayout.TextField("iOS Regular ZoneID", integration.RegularZoneID_iOS);
        integration.RewardedZoneID_iOS = EditorGUILayout.TextField("iOS Rewarded ZoneID", integration.RewardedZoneID_iOS);
    }
}
#endif
#endif

public class ADCOLONY_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return null;
    }

    public override string GetDescription() {
        return "The implementation of AdColony advertising network. Requires AdColony SDK";
    }

    public override string GetSybmol() {
        return "ADCOLONY";
    }
}