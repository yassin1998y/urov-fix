using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof (Image))]
public class SpinWheelDot : MonoBehaviour {

    public static SpinWheelDot controller;
    static List<SpinWheelDot> all = new List<SpinWheelDot>();
    static Func<SpinWheelDot, bool> func = null;

    [Range(2, 36)]
    public int count = 2;
    float angle = 0;
    int index = 0;

    public Sprite unlitSprite;
    public Sprite litSprite;

    Image image;

	void Awake () {
        image = GetComponent<Image>();
        if (controller == null) {
            controller = this;
            EmitInstances();
        }
	}

    void OnEnable() {
        if (controller == this)
            StartCoroutine(Program());
    }

    IEnumerator Program() {
        while (true) {
            func = x => Mathf.RoundToInt(Time.unscaledTime * 2) % 2 == 0;
            yield return new WaitForSeconds(2);

            func = x => Mathf.Abs(Mathf.DeltaAngle(Time.unscaledTime * 360, x.angle * 360)) < 40;
            yield return new WaitForSeconds(2);

            func = x => Mathf.RoundToInt(Time.unscaledTime) % 2 == x.index % 2;
            yield return new WaitForSeconds(2);

            func = x => Mathf.Abs(Mathf.DeltaAngle(Time.unscaledTime * 360, x.angle * 360)) < 30f ||
                        Mathf.Abs(Mathf.DeltaAngle(-Time.unscaledTime * 360, x.angle * 360)) < 15f;
            yield return new WaitForSeconds(2);
            while (Mathf.Abs(Time.unscaledTime - Mathf.Floor(Time.unscaledTime)) > 0.05f)
                yield return 0;

            func = x => (Time.unscaledTime - Mathf.Floor(Time.unscaledTime)) > x.angle;
            yield return new WaitForSeconds(0.2f);
            while (Mathf.Abs(Time.unscaledTime - Mathf.Floor(Time.unscaledTime)) > 0.05f)
                yield return 0;
        }
    }

    void Update() {
        if (func != null)
            SetLight(func(this));
    }

    bool state = false;
    private void SetLight(bool value) {
        if (state != value) {
            state = value;
            image.sprite = state ? litSprite : unlitSprite;
        }
    }

    void EmitInstances() {
        if (controller != this) return;
        all.Clear();
        all.Add(this);
        for (int i = 1; i < count; i++) {
            GameObject instance = Instantiate(gameObject);
            instance.transform.SetParent(transform.parent);
            instance.transform.localScale = transform.localScale;
            instance.transform.rotation = transform.rotation;
            SpinWheelDot light = instance.GetComponent<SpinWheelDot>();
            light.index = i;
            light.angle = 1f * i / count;
            instance.transform.localPosition = Quaternion.Euler(0, 0, light.angle * 360) * transform.localPosition;
            all.Add(light);
        }
    }
}
