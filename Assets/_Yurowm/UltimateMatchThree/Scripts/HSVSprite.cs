using UnityEngine;
using System.Collections;

[RequireComponent (typeof (SpriteRenderer))]
public class HSVSprite : MonoBehaviour {

    public float hue = 0;
    public float saturation = 1;
    public float value = 1;

    void Awake () {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.material.SetFloat("_Hue", hue);
        spriteRenderer.material.SetFloat("_Saturation", saturation);
        spriteRenderer.material.SetFloat("_Value", value);
        Destroy(this);
	}
}
