using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BoosterButton : MonoBehaviour {

    Button button;
    public Text counter;
    public GameObject nullCounter;
    public Image icon;

    public Text title;
    public Text description;

    IBooster prefab;

    public void SetPrefab(IBooster prefab) {
        this.prefab = prefab;
        Refresh();
    }

    void Awake () {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        ItemCounter.refresh += Refresh;
	}

    void OnEnable() {
        Refresh();
    }

    void OnDisable() {
        ItemCounter.refresh -= Refresh;
    }

    void Refresh() {
        if (prefab != null) {
            if (ProfileAssistant.main && CurrentUser.main != null) {
                if (nullCounter) nullCounter.SetActive(CurrentUser.main[prefab.itemID] <= 0);
                counter.gameObject.SetActive(!nullCounter || CurrentUser.main[prefab.itemID] > 0);
                counter.text = CurrentUser.main[prefab.itemID].ToString();
            }
            if (title)
                title.text = prefab.localized ? LocalizationAssistant.main[string.Format(IBooster.titleLocalizationKey, prefab.itemID)] : prefab.title;
            if (description)
                description.text = prefab.localized ? LocalizationAssistant.main[string.Format(IBooster.descriptionLocalizationKey, prefab.itemID)] : prefab.description;
        }
    }

	void OnClick () {
        if (!SessionInfo.current.settings.sBoostersEnable && prefab is ISingleUseBooster) return;
        if (!SessionInfo.current.settings.mBoostersEnable && prefab is IMultipleUseBooster) return;
        BerryAnalytics.Event("Booster Button Pressed", "ItemID:" + prefab.itemID);
        if (CurrentUser.main[prefab.itemID] > 0) {
            SessionInfo.current.mBooster = prefab;
            SessionInfo.current.boosterSelected = true;
        } else
            UIAssistant.main.ShowPage("Store");
    }
}
