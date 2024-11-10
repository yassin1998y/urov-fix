using System.Collections;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof  (ContentAnimator))]
public class AnimationEffect : IEffect, IAnimated {
    static readonly string clipName = "Effect";

    public IEnumerator GetAnimationNames() {
        yield return clipName;
    }

    public override void Initialize() {
        animator = GetComponent<ContentAnimator>();

        base.Initialize();
    }

    public override bool IsComplete() {
        return !animator.IsPlaying();
    }

    public override void Launch() {
        if (!animator) Initialize();
        animator.Play(clipName);
    }
}
