using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using System.Text.RegularExpressions;

public class DinaLabel : MonoBehaviour {

    public static Dictionary<string, Word> words = new Dictionary<string,Word>();
    public static bool initialized = false;

    Text label;
    TextMeshProUGUI tmpLabelUI;
    TextMeshPro tmpLabel;
    TextMesh textMesh;

    public bool localized = false;
    public bool richText = false;
    public string key;
    public string text;
    public bool update = false;
    public float delay = 0.2f;
    float lastTime = 0;
    public List<Mask> masks = new List<Mask>();

	void Awake () {
        if (!initialized)
            Initialize();
        ItemCounter.refresh += UpdateLabel;
        GetTargets();
    }

    public void GetTargets() {
        label = GetComponent<Text>();
        textMesh = GetComponent<TextMesh>();
        tmpLabelUI = GetComponent<TextMeshProUGUI>();
        tmpLabel = GetComponent<TextMeshPro>();
    }

    public static void Initialize() {
        words.Add("CurrentLevel", () => LevelDesign.selected.number.ToString());
        words.Add("CurrentScore", () => SessionInfo.current.GetScore().ToString());
        words.Add("FirstStarScore", () => LevelDesign.selected.firstStarScore.ToString());
        words.Add("SecondStarScore", () => LevelDesign.selected.secondStarScore.ToString());
        words.Add("ThirdStarScore", () => LevelDesign.selected.thirdStarScore.ToString());
        words.Add("BestScore", () => CurrentUser.main.GetScore(LevelDesign.selected.number).ToString());
        words.Add("CurrentMoves", () => SessionInfo.current.GetMovesCount().ToString());
        words.Add("LifesCount", () => CurrentUser.main[ItemID.life].ToString());
        words.Add("ExtraLifesCount", () => Mathf.Max(0, Reference.Get("ExtraLifes")).ToString());
        words.Add("LifeTimer", () => CurrentUser.main.lifeSystem.GetTimer());
        words.Add("DailySpinTimer", () => CurrentUser.main.dailySpin.GetTimer());
        words.Add("RewardedSpinTimer", () => CurrentUser.main.rewardedSpin.GetTimer());
        words.Add("Curent User Level", () => CurrentUser.main.level.ToString());
        words.Add("ProductName", () => Application.productName);
        words.Add("ProductVersion", () => Application.version);
        words.Add("ProductCompany", () => Application.companyName);

        initialized = true;
    }

    public void Set(string text) {
        if (label) label.text = text;
        if (textMesh) textMesh.text = text;
        if (tmpLabel) tmpLabel.text = text;
        if (tmpLabelUI) tmpLabelUI.text = text;
    }

    void OnEnable () {
        UpdateLabel();
	}

    void Update () {
        if (!update) return;
        if (lastTime + delay > Time.unscaledTime) return;
        lastTime = Time.unscaledTime;
        UpdateLabel();
    }

    void UpdateLabel() {
        string result = GetText();
        foreach (Mask mask in masks)
            result = result.Replace("{" + mask.key + "}", mask.item ? GetItemWord(mask.key) : words[mask.value].Invoke());
        Set(result);
    }


    public string GetText() {
        return localized ? LocalizationAssistant.main[key] : text;
    }

    public delegate string Word();

    static readonly Regex itemWordFormat = new Regex(@"^(?<key>[A-Za-z]+):(?<value>[A-Za-z]+)");
    string GetItemWord(string key) {
        Match match = itemWordFormat.Match(key);
        if (match.Success) {
            switch (match.Groups["key"].Value) {
                case "item": {
                        try {
                            ItemID id = (ItemID) Enum.Parse(typeof(ItemID), match.Groups["value"].Value);
                            return CurrentUser.main[id].ToString();
                        } catch (Exception) {
                            return "{WrongKey}";
                        }
                    }
            }
        }
        return "{N/A}";
    }

    [System.Serializable]
    public class Mask {
        public string key = "";
        public string value = "";
        public bool item = false;

        public Mask(string _key) {
            key = _key;
        }
    }
}
