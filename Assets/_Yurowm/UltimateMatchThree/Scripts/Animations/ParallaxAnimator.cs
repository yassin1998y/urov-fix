using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Animation))]
public class ParallaxAnimator : MonoBehaviour {

    Camera parallaxCamera;

    public Transform pivot;

    public float start = 0f;
    public float end = 1f;

    public bool linear = true;
    public AnimationCurve curve;

    Animation anim;
    string clipName = "";


    public float _time;

    void Start() {
        anim = GetComponent<Animation>();
        clipName = anim.clip.name;
        anim.Play(clipName);
        anim.enabled = false;
    }

    void SetCamera(Camera camera) {
        parallaxCamera = camera;
    }

    void Update () {
        if (!parallaxCamera)
            return;
        
        float time = parallaxCamera.WorldToScreenPoint((pivot ? pivot.transform : transform).position).y / Screen.height;

        time = (time - start) / (end - start);


        if (!linear)
            time = curve.Evaluate(time);

        anim.enabled = true;
        anim[clipName].time = time * anim[clipName].length;
        anim.Sample();
        anim.enabled = false;

        _time = time;
	}
}
