using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LinearAnimatior : MonoBehaviour {

    public bool localTime = false;
    public bool realTime = true;
    float time = 0;

    public bool rotZ = false;
    public float rotZampl = 0;
    public float rotZfreq = 0;
    float rotZoffset = 0;
    public float rotZphase = 0;
    public float rotZvelocity = 0;

    public bool sizeX = false;
    public float sizeXampl = 0;
    public float sizeXfreq = 0;
    float sizeXoffset = 1;

    public bool sizeY = false;
    public float sizeYampl = 0;
    public float sizeYfreq = 0;
    float sizeYoffset = 1;

    public bool posX = false;
    public float posXampl = 0;
    public float posXfreq = 0;
    public float posXphase = 0;
    float posXoffset = 1;
    public float posXvelocity = 0;

    public bool posY = false;
    public float posYampl = 0;
    public float posYfreq = 0;
    public float posYphase = 0;
    float posYoffset = 1;
    public float posYvelocity = 0;

    public bool alpha = false;
    public float alphaAmpl = 0;
    public float alphaFreq = 0;
    float alphaOffset = 0;
    public float alphaPhase = 0;

    Vector3 z;
    SpriteRenderer sprite;
    Graphic graphic;
    Color color;

    void Start() {
        if (alpha) {
            sprite = GetComponent<SpriteRenderer>();
            graphic = GetComponent<Graphic>();
        }
        Recalculate();
    }

    void Recalculate() {
        sizeXoffset = transform.localScale.x;
        sizeYoffset = transform.localScale.y;
        rotZoffset = transform.localEulerAngles.z;
        posXoffset = transform.localPosition.x;
        posYoffset = transform.localPosition.y;
        if (alpha) {
            if (sprite) alphaOffset = sprite.color.a;
            else if (graphic) alphaOffset = graphic.color.a;
        }
    }

    void Update() {
        if (localTime) time += realTime ? Time.unscaledDeltaTime : Time.deltaTime;
        else time = realTime ? Time.unscaledTime : Time.time;

        if (rotZ)
            transform.localEulerAngles = Vector3.forward * (rotZoffset + Mathf.Sin(rotZfreq * (rotZphase + time)) * rotZampl + rotZvelocity * time);

        if (sizeX || sizeY) {
            z = transform.localScale;

            if (sizeX)
                z.x = sizeXoffset + Mathf.Sin(sizeXfreq * time) * sizeXampl;
            if (sizeY)
                z.y = sizeYoffset + Mathf.Sin(sizeYfreq * time) * sizeYampl;

            transform.localScale = z;
        }

        if (posX || posY) {
            z = transform.localPosition;

            if (posX)
                z.x = posXoffset + Mathf.Sin(posXphase + posXfreq * time) * posXampl;
            if (posY)
                z.y = posYoffset + Mathf.Sin(posYphase + posYfreq * time) * posYampl;

            transform.localPosition = z;
        }

        if (alpha) {
            float a = (alphaOffset + Mathf.Sin(alphaFreq * (alphaPhase + time)) * alphaAmpl);
            if (sprite) {
                color = sprite.color;
                color.a = a;
                sprite.color = color;
            }
            if (graphic) {
                color = graphic.color;
                color.a = a;
                graphic.color = color;
            }
        }

    }
}
