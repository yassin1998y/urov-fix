using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;

public class BerryStoreItem : MonoBehaviour {

    public TextMeshProUGUI price;
    public Button purchaseButton;
    public Image icon;
    public TextMeshProUGUI title;
    public string coinIcon = "Gem";

    [HideInInspector]
    public BerryStore.Item item;

    void Awake () {
        purchaseButton.onClick.AddListener(Purchase);
        ItemCounter.refresh += Refresh;
	}

    void OnEnable() {
        Refresh();
    }

	public void Purchase () {
        switch (item.purchaseType) {
            case BerryStore.Item.PurchaseType.IAP:
                BerryStore.main.PurchaseIAP(item.iap, item.pack, item.onPurchase); break;
            case BerryStore.Item.PurchaseType.SoftCurrency: {
                    BerryAnalytics.Event("Purchase",
                          "PurchaseID:" + item.id,
                          "Price:" + item.cost);
                    BerryStore.ItemPack.Purchase(item.pack, item.onPurchase, item.cost); break;
                }
            case BerryStore.Item.PurchaseType.RewardedVideo: {
                    BerryAnalytics.Event("Rewarded Ad Request");
                    Advertising.main.ShowAds(() => {
                        BerryAnalytics.Event("Rewarded Ad Reward", "PurchaseID:" + item.id);
                        BerryStore.ItemPack.Purchase(item.pack, item.onPurchase);
                    });
                } break;
        }
    }

    public void Refresh() {
        string name = item.localized ? LocalizationAssistant.main[item.localization_Name] : item.Name;
        string description = item.localized ? LocalizationAssistant.main[item.localization_Description] : item.Descrition;

        title.text = name;
        if (!string.IsNullOrEmpty(description))
            title.text += string.Format("\n<size=60%>{0}</size>", description);

        purchaseButton.interactable = item.alwaysAvaliable || item.avaliableWhen.GetResult();

        if (purchaseButton.interactable) {
            switch (item.purchaseType) {
                case BerryStore.Item.PurchaseType.SoftCurrency:
                    purchaseButton.interactable = CurrentUser.main[ItemID.coin] >= item.cost;
                    break;
            }
        }

        icon.sprite = item.icon;

        switch (item.purchaseType) {
            case BerryStore.Item.PurchaseType.IAP: {
                    if (BerryStore.main.marketItemPrices.ContainsKey(item.iap))
                        price.text = BerryStore.main.marketItemPrices[item.iap];
                    else
                        price.text = "N/A";
                }
                break;
            case BerryStore.Item.PurchaseType.SoftCurrency:
                price.text = string.Format("{0}<sprite name=\"{1}\">", item.cost, coinIcon);
                break;
            case BerryStore.Item.PurchaseType.RewardedVideo:
                price.text = LocalizationAssistant.main["store/watch"];
                break;
        }
    }
}
