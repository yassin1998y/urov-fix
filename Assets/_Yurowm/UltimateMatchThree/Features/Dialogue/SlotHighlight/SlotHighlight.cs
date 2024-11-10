using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof (SlotRenderer))]
[RequireComponent (typeof (ContentAnimator))]
public class SlotHighlight : ILiveContent, IAnimated {
    new SlotRenderer renderer;
    ContentAnimator animator;
    internal bool autohide = true;
    bool hidden = true;

    public override void Initialize() {
        base.Initialize();
        renderer = GetComponent<SlotRenderer>();
        animator = GetComponent<ContentAnimator>();
    }

    public IEnumerator GetAnimationNames() {
        yield return "Show";
        yield return "Hide";
    }

    public IEnumerator Show(List<int2> slots) {
        renderer.Rebuild(slots);
        hidden = false;
        yield return animator.PlayAndWait("Show");
        if (autohide) StartCoroutine(Autohide());
    }

    public IEnumerator Hide(bool kill = false) {
        if (!hidden) {
            yield return animator.PlayAndWait("Hide");
            renderer.Rebuild(new List<int2>());
            hidden = true;
        }
        if (kill) Kill();
    }

    IEnumerator Autohide() {
        int movesCount = SessionInfo.current.GetMovesCount();
        while (movesCount == SessionInfo.current.GetMovesCount())
            yield return 0;
        yield return StartCoroutine(Hide());
    }

    public bool IsShown() {
        return !hidden;
    }

    public void HideAndKill() {
        StartCoroutine(Hide(true));
    }
}
