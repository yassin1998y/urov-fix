using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent(typeof(ContentAnimator))]
public class Character : ILiveContent, IAnimated {

    internal ContentAnimator animator;
    [NonSerialized]
    public List<CharacterPose> poses = null;
    [NonSerialized]
    public CharacterPose currentPose = null;

    void Awake() {
        FindPoses();
    }

    public void FindPoses() {
        animator = GetComponent<ContentAnimator>();
        poses = GetComponentsInChildren<CharacterPose>(true).ToList();
        if (Application.isPlaying)
            poses.ForEach(x => x.gameObject.SetActive(false));
    }

    public IEnumerator GetAnimationNames() {
        yield return "Show";
        yield return "Hide";
    }

    public IEnumerator Hide() {
        yield return animator.PlayAndWait("Hide");
        gameObject.SetActive(false);
    }

    public IEnumerator Show() {
        gameObject.SetActive(true);
        yield return animator.PlayAndWait("Show");
    }

    public void SetPose(string pose) {
        currentPose = poses.FirstOrDefault(x => x.name == pose);
        poses.ForEach(x => x.gameObject.SetActive(x == currentPose));
    }
}
