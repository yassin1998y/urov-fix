using UnityEngine;
using System.Collections;

public class ParallaxTweenAnimator : MonoBehaviour {

    [ContextMenu ("Inverse")]
    public void Inverse() {
        Vector3 z = endPosition;
        endPosition = startPosition;
        startPosition = z;

        z = endScale;
        endScale = startScale;
        startScale = z;

        z.x = endRotation;
        endRotation = startRotation;
        startRotation = z.x;
    }

    [ContextMenu("SetStart")]
    public void SetStart() {
        if (position)
            transform.localPosition = startPosition;
        if (rotation)
            transform.rotation = Quaternion.Euler(0, 0, startRotation);
        if (scale)
            transform.localScale = startScale;
    }

    [ContextMenu("SetEnd")]
    public void SetEnd() {
        if (position)
            transform.localPosition = endPosition;
        if (rotation)
            transform.rotation = Quaternion.Euler(0, 0, endRotation);
        if (scale)
            transform.localScale = endScale;
    }

    [ContextMenu("GetStartFromCurrent")]
    public void GetStartFromCurrent() {
        if (position)
            startPosition = transform.localPosition;
        if (rotation)
            startRotation = transform.localEulerAngles.z;
        if (scale)
            startScale = transform.localScale;
    }

    [ContextMenu("GetEndFromCurrent")]
    public void GetEndFromCurrent() {
        if (position)
            endPosition = transform.localPosition;
        if (rotation)
            endRotation = transform.localEulerAngles.z;
        if (scale)
            endScale = transform.localScale;
    }

    Camera parallaxCamera;
    float lastTime = -1000;
    SpriteRenderer spriteRenderer;

    public Transform pivot;

    Vector3 lastPosition = new Vector3();

    #if UNITY_EDITOR
    public float value;
    #endif

    public float start = 0f;
    public float end = 1f;

    public bool linear = true;
    public AnimationCurve curve;

    public bool position = false;
    public Vector3 startPosition;
    public Vector3 endPosition;

    public bool rotation = false;
    public float startRotation;
    public float endRotation;

    public bool scale = false;
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.one;

    public bool color = false;
    public Color startColor;
    public Color endColor;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void SetCamera(Camera camera) {
        parallaxCamera = camera;
    }

	void LateUpdate () {
        if (!parallaxCamera)
            return;

        Transform pivot = this.pivot ? this.pivot.transform : transform;

        if (pivot.transform.position == lastPosition)
            return;

        float time = parallaxCamera.WorldToScreenPoint(pivot.position).y / Screen.height;

        #if UNITY_EDITOR
        value = time;
        #endif

        time = (time - start) / (end - start);


        if (!linear)
            time = curve.Evaluate(time);

        if (lastTime != time) {
            lastTime = time;
            if (position) transform.localPosition = Vector3.Lerp(startPosition, endPosition, time);
            if (rotation) transform.localEulerAngles = Vector3.forward * Mathf.LerpAngle(startRotation, endRotation, time);
            if (scale) transform.localScale = Vector3.Lerp(startScale, endScale, time);
            if (color && spriteRenderer) spriteRenderer.color = Color.Lerp(startColor, endColor, time);
        }
        
	}
}
