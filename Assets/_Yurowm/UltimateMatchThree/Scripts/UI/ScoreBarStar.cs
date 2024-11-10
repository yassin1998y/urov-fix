using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreBarStar : MonoBehaviour {

    bool filled = false;

    public StarType starType; // displaying star type - first, second, third

    Animation anim;
    Image image;
    string state;

    void Awake() {
        anim = GetComponent<Animation>();
        image = GetComponent<Image>();
        state = anim.clip.name;
        ScoreBar.onStarGet += OnStarGet;
    }

    void OnEnable() {
        transform.localScale = Vector3.zero;
        filled = (int) SessionInfo.current.GetStar() >= (int) starType;

        Refresh();
    }

    void Refresh() {
        image.enabled = filled;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;
    }

    void OnStarGet(StarType star) {
        if (star != starType)
            return;
        if (!gameObject.activeInHierarchy)
            return;

        image.enabled = true;
        anim.enabled = true;
        anim.Play(state);

    }
}
