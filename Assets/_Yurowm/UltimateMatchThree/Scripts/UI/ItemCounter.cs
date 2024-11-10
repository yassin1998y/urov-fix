using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[RequireComponent (typeof (TextMeshProUGUI))]
public class ItemCounter : MonoBehaviour {

    TextMeshProUGUI label;
	public ItemID itemID; // Item ID
    public static System.Action refresh = delegate {};


	void Awake () {
		label = GetComponent<TextMeshProUGUI> ();
        refresh += Refresh;
	}
	
	void OnEnable () {
		Refresh (); // Updating when counter is activated
	}

	// Refreshing couter function
	public void Refresh() {
        if (!label)
            return;
        if (CurrentUser.main != null)
            label.text = CurrentUser.main[itemID].ToString();
        else
            label.text = "0";
	}

	// Refreshing all counters function
	public static void RefreshAll() {
        refresh.Invoke();
	}
}