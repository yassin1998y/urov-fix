using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public abstract class IEffect : ILiveContent {
	public bool killAfterLifetime = true;

    protected ContentSound sound;
    protected ContentAnimator animator;

    public override void Initialize() {
        sound = GetComponent<ContentSound>();
        animator = GetComponent<ContentAnimator>();
        base.Initialize();
    }

    void Start() {
        Play();
	}

    bool isStarted = false;
    public void Play() {
        if (isStarted)
            return;

        gameObject.SetActive(true);

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail)
            trail.Clear();

        isStarted = true;
        Launch();

        if (killAfterLifetime) StartCoroutine(KillTimer());

        if (transform.parent == null)
            transform.SetParent(FieldAssistant.main.sceneFolder);
    }

    public abstract bool IsComplete();
    public abstract void Launch();

    IEnumerator KillTimer () {
        while (!IsComplete())
            yield return 0;
        Kill();
	}

    public void Repaint(ItemColor color) {
        foreach (var coloredObject in GetComponentsInChildren<SetSpriteColor>(true))
            coloredObject.SetColor(color, false);
    }
}
