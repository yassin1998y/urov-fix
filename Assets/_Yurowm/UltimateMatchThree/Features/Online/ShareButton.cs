using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;
#if ONLINE
using Facebook.Unity;
#endif

[RequireComponent (typeof (Button))]
public class ShareButton : MonoBehaviour, ILocalized {

    void Awake () {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick() {
        StartCoroutine(Share());
    }

    IEnumerator Share() {
        BerryAnalytics.Event("Share Button Press");
        #if ONLINE
        yield return StartCoroutine(Online.main.OfflineQuestion());

        if (Online.main.IsOnline()) {

            string caption = LocalizationAssistant.main["share/link-caption"]
                    .Replace("{level}", LevelDesign.selected.number.ToString())
                    .Replace("{score}", SessionInfo.current.GetScore().ToString());
            DebugPanel.Log("Shared", "{0}\n{1}\n{2}"
                .FormatText(LocalizationAssistant.main["share/link-name"],
                caption,
                LocalizationAssistant.main["share/link-description"]));

            FB.FeedShare(
                toId: string.Empty,
                link: new Uri(Project.main.storeAppLink),
                linkName: LocalizationAssistant.main["share/link-name"],
                linkCaption: caption,
                linkDescription: LocalizationAssistant.main["share/link-description"],
                picture: new Uri(Project.main.invitePictureURL),
                callback: r => {
                    Debug.Log(r.RawResult);
                    BerryAnalytics.Event("Share Result", r.ResultDictionary.ToDictionary(x => x.Key, x => x.Value.ToString()));
                });
        }
        #endif
        yield break;
    }

    public IEnumerator RequriedLocalizationKeys() {
        yield return "share/link-name";
        yield return "share/link-caption";
        yield return "share/link-description";
    }
}
