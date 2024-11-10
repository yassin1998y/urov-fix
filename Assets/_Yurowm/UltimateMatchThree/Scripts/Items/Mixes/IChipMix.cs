using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public abstract class IChipMix : ILiveContent {

    internal ContentAnimator animator;
    internal ContentSound sound;

    [ContentSelector]
    public IEffect mixEffect;

    public override void Initialize() {
        base.Initialize();

        animator = GetComponent<ContentAnimator>();
        sound = GetComponent<ContentSound>();
    }

    public void Activate() {
        StartCoroutine(Process());
    }

    IEnumerator Process() {
        IEnumerator destroying = Destroying();
        ISlotContent.gravityLockers.Add(this);

        if (mixEffect) {
            IEffect destroyingEffect = Content.Emit<IEffect>(mixEffect);
            if (destroyingEffect) {
                destroyingEffect.transform.position = transform.position;
                OnCreateDestroyingEffect(destroyingEffect);
                destroyingEffect.Play();
            }
        }

        while (destroying.MoveNext())
            yield return destroying.Current;
        ISlotContent.gravityLockers.Remove(this);

        Kill();
    }

    public virtual void OnCreateDestroyingEffect(IEffect effect) {}

    public abstract void Prepare(IChip firstChip, IChip secondChip);

    public abstract IEnumerator Destroying();

}
