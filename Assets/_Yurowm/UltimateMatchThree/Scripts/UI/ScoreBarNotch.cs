using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Script is responsible for displaying notches (goals for stars) on the progress bar of game score.
[RequireComponent (typeof (RectTransform))]
public class ScoreBarNotch : MonoBehaviour {

	RectTransform rect;
	public StarType star;

	void Awake() {
		rect = GetComponent<RectTransform> ();
    }

	void OnEnable () {
		float value = 0;
		float max = SessionInfo.current.design.thirdStarScore;
		switch (star) {
			case StarType.First: value = SessionInfo.current.design.firstStarScore; break;
			case StarType.Second: value = SessionInfo.current.design.secondStarScore; break;
			case StarType.Third: value = SessionInfo.current.design.thirdStarScore; break;
		}
		value = value / max;

        Vector2 pos = Vector2.up * (rect.parent as RectTransform).rect.width / 2;
        pos = Quaternion.Euler(0, 0, -360f * value) * pos;
        pos *= 0.95f;

        rect.anchoredPosition = pos;



		//Vector2 pos = rect.anchoredPosition;
		//pos.x = value * ((RectTransform)rect.parent).rect.width - rect.rect.width;
		//rect.anchoredPosition = pos;
	}
}

public enum StarType {
    None = 0,
    First = 1,
    Second = 2,
    Third = 3};