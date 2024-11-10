using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class BerryStoreGroup : MonoBehaviour {

    public TextMeshProUGUI title;
    public TextMeshProUGUI description;

    [HideInInspector]
    public BerryStore.Group group;

    void Awake() {
        ItemCounter.refresh += Refresh;
    }

    void OnEnable() {
        Refresh();
    }

    public void Refresh() {

        string name = group.localized ? LocalizationAssistant.main[group.localization_Name] : group.Name;
        string description = group.localized ? LocalizationAssistant.main[group.localization_Description] : group.Descrition;

        title.text = name;
        this.description.text = description;
    }
}
