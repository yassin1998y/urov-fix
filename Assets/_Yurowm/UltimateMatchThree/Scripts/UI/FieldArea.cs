using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Yurowm.GameCore;

[RequireComponent (typeof (RectTransform))]
public class FieldArea : MonoBehaviour {

    static FieldArea _main;
    public static FieldArea main {
        get {
            if (!_main)
                _main = FindObjectOfType<FieldArea>();
            return _main;
        }
        set {
            _main = value;
        }
    }

    RectTransform rect;
    public static Vector2 position = new Vector2();
    public static Vector2 size = new Vector2();
    public static Vector2 screen_size = new Vector2();

    // Use this for initialization
    void Awake () {
        rect = transform.rect();
        Image image = GetComponent<Image>();
        if (image) image.enabled = false;
	}

    void OnEnable() {
        main = this;
        UpdateParameters();
        GameCamera.main.OnScreenResize();
    }

    public void UpdateParameters() {
        position = rect.anchoredPosition;
        screen_size = rect.parent.rect().rect.size;
        size = rect.rect.size;
    }
}
