using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using TMPro;

public class LevelButton : IMapButton {
    
    public int level = 0;
    public ScoreStarFromMemory stars;
    public TextMeshPro levelNumber;

    public void Initialize() {
        if (stars) {
            stars.level = level;
            stars.Resresh();
        }
        
        if (stars)
            stars.gameObject.SetActive(true);

        if (levelNumber) levelNumber.text = level.ToString();
    }

    internal static bool IsLocked(int level) {
        return level > 1 && CurrentUser.main.GetScore(level - 1) == 0;
    }

    internal static bool IsItCurrentLevel(int level) {
        return CurrentUser.main.GetScore(level) == 0 && (level == 1 || CurrentUser.main.GetScore(level - 1) > 0);
    }

    public override void OnClick() {
        LevelAssistant.SelectDesign(level);
    }
}

[RequireComponent (typeof (Collider))]
public abstract class IMapButton : MonoBehaviour {
    public abstract void OnClick();
}
