using System;
using Yurowm.GameCore;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent (typeof (ContentAnimator))]
public class TargetCounter : MonoBehaviour, IAnimated {

    public Text counter;
    public Transform nodeTarget;

    ContentAnimator animator;

    protected ILevelGoal mode = null;
    internal bool complete;

    void Awake() {
        animator = GetComponent<ContentAnimator>();
    }

    public void Complete() {
        complete = true;
        counter.gameObject.SetActive(false);
        animator.Play("Complete");
    }

    public IEnumerator GetAnimationNames() {
        yield return "Complete";
    }

    public void SetGoal(ILevelGoal mode) {
        this.mode = mode;
    }

    public void ShowValue(string value) {
        counter.text = value;
    }
}
