using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;

[RequireComponent (typeof (ContentAnimator))]
public class SpeechBubble : ILiveContent, IAnimated {

    ContentAnimator animator;

    public RectTransform arrow;
    public Text label;
    public Button clickHandler;
    [NonSerialized]
    public bool clicked = false;

    Transform arrowConnector = null;

    public override void Initialize() {
        animator = GetComponent<ContentAnimator>();
        clickHandler.gameObject.SetActive(false);
        clickHandler.onClick.AddListener(OnClick);
    }

    public void Say(string text, Character character) {
        label.text = text;
        if (character && character.currentPose) {
            arrow.gameObject.SetActive(true);
            arrowConnector = character.currentPose.bubbleConnector;
            arrow.localScale = new Vector3(arrow.position.x > arrowConnector.position.x ? 1 : -1, arrow.localScale.y, 1);
        } else
            arrow.gameObject.SetActive(false);

        clicked = false;
        clickHandler.gameObject.SetActive(true);
    }

    void OnClick() {
        clicked = true;
        clickHandler.gameObject.SetActive(false);
    }

    void Update() {
        if (arrowConnector && arrow.gameObject.activeSelf) {
            arrow.position = arrowConnector.position;
            arrow.localRotation = Quaternion.Euler(0, 0, arrow.localPosition.To2D().Angle() + 90);
            if (arrow.parent.lossyScale.y != 0)
                arrow.localScale = new Vector3(arrow.localScale.x, Vector3.Distance(arrowConnector.position, arrow.parent.position) / arrow.parent.lossyScale.y, 1);
        }
    }

    public IEnumerator Show() {
        gameObject.SetActive(true);
        yield return animator.PlayAndWait("Show");
    }

    public IEnumerator Hide() {
        yield return animator.PlayAndWait("Hide");
        gameObject.SetActive(false);
    }

    public IEnumerator GetAnimationNames() {
        yield return "Show";
        yield return "Hide";
    }
}
