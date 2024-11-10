using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Question : MonoBehaviour {

    public static Question main;
    public TextMeshProUGUI descritpion;
    public TextMeshProUGUI title;
    public Button[] answerButtons;

    bool wainting = false;
    int Result = 0;

    void Awake() {
        main = this;
    }

    public void Ask(string title, string question, params string[] answers) {
        if (answers.Length == 0) {
            Debug.LogError("Question without answers!");
            return;
        }

        wainting = true;

        descritpion.text = question;
        this.title.text = title;

        foreach (Button button in answerButtons)
            button.gameObject.SetActive(false);
        for (int i = 0; i < Mathf.Min(answers.Length, answerButtons.Length); i++) {
            answerButtons[i].gameObject.SetActive(true);
            answerButtons[i].transform.GetComponentInChildren<TextMeshProUGUI>(true).text = answers[i];
        }
    
        UIAssistant.main.SetPanelVisible("Question", true);
    }

	public void SetResult (int i) {
        wainting = false;
        Result = i;
        UIAssistant.main.SetPanelVisible("Question", false);
	}

    public int GetResult() {
        return Result;
    }
	
	public bool Wait () {
        return wainting;
	}
}
