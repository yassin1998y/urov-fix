using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Yurowm.GameCore;

public class ServiceAssistant : MonoBehaviourAssistant<ServiceAssistant>, ILocalized {
    bool rate_it_showen = false;

    void Awake() {
        if (Application.isEditor)
            Application.runInBackground = true;
        
        UIAssistant.onShowPage += LevelMapPopup;
		
        rate_it_showen = PlayerPrefs.GetInt("Rated") == 1;

        #if UNITY_IOS
        UnityEngine.iOS.NotificationServices.RegisterForNotifications(
            UnityEngine.iOS.NotificationType.Alert |
            UnityEngine.iOS.NotificationType.Badge |
            UnityEngine.iOS.NotificationType.Sound);
        #endif
    }

    void LevelMapPopup(UIAssistant.Page page) {
        StartCoroutine(LevelMapPopupRoutine(page));
    }

    IEnumerator LevelMapPopupRoutine(UIAssistant.Page page) {
        if (!page.HasTag("POPUP")) yield break;
        
        yield return 0;

        while (CPanel.uiAnimation > 0)
            yield return 0;
        if (UIAssistant.main.GetCurrentPage() != page)
            yield break;

        yield return 0;

        // Rate It
        if (!rate_it_showen && PlayerPrefs.GetInt("rateit.rated") != 1) {
            if (PlayerPrefs.HasKey("rateit.next_show")) {
                DateTime next_show = new DateTime(long.Parse(PlayerPrefs.GetString("rateit.next_show")));
                if (next_show > DateTime.Now) yield break;
            }

            if (CurrentUser.main.level < 10) yield break;
            if (Time.unscaledTime < 15) yield break;

            yield return StartCoroutine(RateItRoutine());

            rate_it_showen = true;
            yield break;
        }
    }

    IEnumerator RateItRoutine() {
        Question.main.Ask(
               LocalizationAssistant.main["rateit/title"],
               LocalizationAssistant.main["rateit/description"],
               LocalizationAssistant.main["rateit/yes"],
               LocalizationAssistant.main["rateit/later"],
               LocalizationAssistant.main["rateit/no"]
               );

        while (Question.main.Wait())
            yield return 0;

        string result = "";
        switch (Question.main.GetResult()) {
            case 0: {// Yes 
                    result = "Yes";
                    OpenAppStore();
                    PlayerPrefs.SetInt("rateit.rated", 1);
                    break;
                }
            case 1: {// Later
                    result = "Later";
                    DateTime next_show = DateTime.Now.AddDays(1);

                    PlayerPrefs.SetString("rateit.next_show", next_show.Ticks.ToString());
                    PlayerPrefs.Save();
                    break;
                }
            case 2: {// No
                    result = "No";
                    DateTime next_show = DateTime.Now.AddDays(3);

                    PlayerPrefs.SetString("rateit.next_show", next_show.Ticks.ToString());
                    PlayerPrefs.Save();
                    break;
                }
        }

        BerryAnalytics.Event("Rate It",
            "Result:" + result);
    }

    public void OpenAppStore() {
        string link = Project.main.internalAppLink;
        if (link != "") Application.OpenURL(link);
    }

    public void Quit() {
        Application.Quit();
    }

    public IEnumerator RequriedLocalizationKeys() {
        yield return "rateit/title";
        yield return "rateit/description";
        yield return "rateit/yes";
        yield return "rateit/no";
        yield return "rateit/later";
    }
}


