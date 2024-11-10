using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent (typeof (Image))]
public class ColoredBackground : MonoBehaviour {
    static ColoredBackground main;

    Image image;

    public Color emptyColor;
    public float delay = 0.3f;

	void Awake () {
        image = GetComponent<Image>();
        main = this;
	}

    Color currentColor, targetColor;
    float actionTime;

	void Update () {
	    image.color = actionTime + delay >= Time.unscaledTime ?
            Color.Lerp(currentColor, targetColor, (Time.unscaledTime - actionTime) / delay) : targetColor;
	}

    public static void SetTargetColor(Color color, float power) {
        main.currentColor = main.image.color;
        main.targetColor = Color.Lerp(main.emptyColor, color, power);
        main.actionTime = Time.unscaledTime;
    }

    public static void SetEmpty() {
        SetTargetColor(main.emptyColor, 1);
    }
}
