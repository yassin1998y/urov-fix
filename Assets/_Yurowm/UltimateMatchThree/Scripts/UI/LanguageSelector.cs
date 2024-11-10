using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour {

    public Button next;
    public Button previous;

    // Use this for initialization
    void Awake () {
        next.onClick.AddListener(() => Select(1));
        previous.onClick.AddListener(() => Select(-1));
    }

	void Select (int direction) {
        int current = LocalizationAssistant.main.languages.IndexOf(LocalizationAssistant.main.current_language);
        current += direction > 0 ? 1 : -1;
        if (current >= LocalizationAssistant.main.languages.Count) current = 0;
        if (current < 0) current = LocalizationAssistant.main.languages.Count - 1;

        LocalizationAssistant.main.LearnLanguage(LocalizationAssistant.main.languages[current]);
        ItemCounter.RefreshAll();
    }
}
