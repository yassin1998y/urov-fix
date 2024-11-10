using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// The script is responsible for the logic of the progress bar of game score
[RequireComponent (typeof (Image))]
public class ScoreBar : MonoBehaviour {

    public static System.Action<StarType> onStarGet = delegate{};
    
	float current = 0;

    Image image;

    void Awake() {
        image = GetComponent<Image>();
    }

	void OnEnable () {
		if (SessionAssistant.main == null) return;
		current = SessionInfo.current.GetStarCount();
	}

	void Update () {
        image.fillAmount = 1f * SessionInfo.current.GetScore() / SessionInfo.current.design.thirdStarScore;
        if (current < SessionInfo.current.GetStarCount()) {
            current = SessionInfo.current.GetStarCount();
            onStarGet.Invoke(SessionInfo.current.GetStar());
        }
	}
}
