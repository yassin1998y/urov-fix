using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;

public class ScoreEffect : AnimationEffect {

    public Text label;

    public void SetScore(string score) {
        label.text = score;
    }


    static ScoreEffect mainReference = null;
    public static void Emit(Vector3 position, string score, ItemColor color = ItemColor.Unknown, float scale = 1f, ScoreEffect reference = null) {

        if (!reference) {
            if (!mainReference) mainReference = Content.GetPrefabList<ScoreEffect>().FirstOrDefault();
            reference = mainReference;
        }

        if (reference) {
            ScoreEffect effect = Content.Emit(reference);
            effect.transform.position = position;
            effect.transform.SetParent(FieldAssistant.main.sceneFolder);
            effect.transform.localScale = Vector3.one * scale;
            if (color.IsPhysicalColor()) effect.Repaint(color);
            effect.SetScore(score);
            effect.Play();
        }
    }

    public static void ShowScore(Vector3 position, int score, ItemColor color = ItemColor.Unknown, float scale = 1f, ScoreEffect reference = null) {
        if (score > 0)
            Emit(position, score.ToString(), color, scale, reference);
    }
}
