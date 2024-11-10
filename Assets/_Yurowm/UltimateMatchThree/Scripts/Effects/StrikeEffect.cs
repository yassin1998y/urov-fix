using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StrikeEffect : IEffect, ISounded, IAnimated {
    bool isComplete = false;

    public Action onReach = delegate {};
    public IEnumerator onReachCoroutine = null;

    public bool gravityLocker = true;


    public override bool IsComplete() {
        return isComplete;
    }

    protected Transform target = null;
    Vector3 targetPosition = Vector3.zero;
    protected Vector3 startPosition;

    public void SetTarget(Transform target) {
        this.target = target;
    }

    public void SetTarget(Vector3 target) {
        this.targetPosition = target;
    }

    public override void Launch() {
        StartCoroutine(Logic());
    }

    IEnumerator Logic() {
        if (gravityLocker) ISlotContent.gravityLockers.Add(this);
        if (sound) sound.Play("OnAwake");
        if (animator) animator.Play("OnAwake");

        Transform subEffect = transform.Find("OnAwake");
        if (subEffect) {
            subEffect.SetParent(transform.parent);
            subEffect.gameObject.SetActive(true);
        }

        startPosition = transform.position;

        while (true) {
            if (target)
                targetPosition = target.transform.position;


            transform.position = GetNewPosition(targetPosition);

            if (transform.position == targetPosition) break;

            transform.position += Vector3.up * Time.deltaTime * 1f;

            yield return 0;
        }

        subEffect = transform.Find("OnDeath");
        if (subEffect) {
            subEffect.SetParent(transform.parent);
            subEffect.gameObject.SetActive(true);
        }

        if (sound) sound.Play("OnDeath");
        if (animator) animator.Play("OnDeath");

        onReach();
        if (onReachCoroutine != null)
            yield return StartCoroutine(onReachCoroutine);

        ISlotContent.gravityLockers.Remove(this);

        yield return StartCoroutine(Death());

        while (animator && animator.IsPlaying())
            yield return 0;

        isComplete = true;
    }

    public IEnumerator GetSoundNames() {
        yield return "OnAwake";
        yield return "OnDeath";
    }

    public IEnumerator GetAnimationNames() {
        yield return "OnAwake";
        yield return "OnDeath";
    }

    public virtual IEnumerator Death() {
        yield break;
    }

    public abstract Vector3 GetNewPosition(Vector3 targetPosition);

}
