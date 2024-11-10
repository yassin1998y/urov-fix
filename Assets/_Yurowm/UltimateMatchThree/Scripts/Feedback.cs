using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;
using TMPro;

[RequireComponent (typeof (Animation))]
public class Feedback : MonoBehaviourAssistant<Feedback> {

    new Animation animation;
    public TextMeshProUGUI text;

    void Awake() {
        animation = GetComponent<Animation>();
    }

    public static void Play(string name) {
        ScoreBonus bonus = SessionInfo.current.rule.scoreBonus.FirstOrDefault(x => x.name == name);
        if (bonus == null) return;
        main.StartCoroutine(main.Playing(bonus));
    }

    IEnumerator Playing(ScoreBonus bonus) {

        SessionInfo.current.AddScorePoint(bonus.score);

        if (animation.isPlaying)
            yield break;

        text.text = "{0}\n<size=60%>+{1}</size>".FormatText(bonus.text, bonus.score);
        AudioAssistant.Shot(bonus.clip);

        animation.Play();

        while (animation.isPlaying)
            yield return 0;
    }

}

[Serializable]
public class ScoreBonus {
    public string name = "";
    public int score = 1;
    public string clip = null;
    public string text = "";
    [HideInInspector]
    public int id = 0;
}
