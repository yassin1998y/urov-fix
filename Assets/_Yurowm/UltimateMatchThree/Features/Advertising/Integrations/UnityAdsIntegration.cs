using System;
#if UNITY_ADS
using UnityEngine.Advertisements;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnityAdsIntegration : IAdIntegration {

    public string regularZoneId = "video";
    public string rewardedZoneId = "rewardedVideo";

    public override string GetAppID() {
        return null;
    }

    public override string GetName() {
        return "UnityAds";
    }

    public override string GetZoneID(AdType type) {
        switch (type) {
            case AdType.Regular: return regularZoneId;
            case AdType.Rewarded: return rewardedZoneId;
        } 
        return "video";
    }

    public override bool IsReady(AdType type) {
        return Advertisement.IsReady(GetZoneID(type));
    }

    public override void Show(AdType type, Action onComplete) {
        Advertisement.Show(GetZoneID(type), new ShowOptions {
            resultCallback = result => {
                if (result == ShowResult.Finished)
                    if (onComplete != null) onComplete.Invoke();
            }
        });
    }
}

#if UNITY_EDITOR
public class UnityAdsIntegrationEditor : AdIntegrationEditor<UnityAdsIntegration> {
    public override void DrawSettings(UnityAdsIntegration integration) {
        EditorGUILayout.HelpBox(@"The plugin uses IDs from connected Unity Project. See the Services menu.", MessageType.Info);

        integration.regularZoneId = EditorGUILayout.TextField("Regular Zone ID", integration.regularZoneId);
        integration.rewardedZoneId = EditorGUILayout.TextField("Rewarded Zone ID", integration.rewardedZoneId);
    }
}
#endif
#endif