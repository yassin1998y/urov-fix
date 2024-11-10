using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yurowm.GameCore;

public class GummyBearGoal : ILevelGoal {

    [ContentSelector]
    public TargetCounter counter;
    [ContentSelector]
    public CollectionEffect collectionEffect;

    public override int GetAssess(Slot slot) {
        return slot.Content().Contains(c => c is Dirt) ? 100 : 0;
    }

    public override UnityEvent GetControlEvent() {
        return null;
    }

    TargetCounter _counter;
    public override List<TargetCounter> GetCurrentCounters(SessionInfo session) {
        _counter = Content.GetItem<TargetCounter>(counter.name);
        _counter.SetGoal(this);
        return new List<TargetCounter>() { _counter };
    }

    public override List<TargetCounter> GetPreviewCounters(ILevelGoal goal, LevelDesign design, LevelGoalInfo info, PreviewType type) {
        TargetCounter instance = Content.GetItem<TargetCounter>(counter.name);
        switch (type) {
            case PreviewType.Complete: instance.Complete(); break;
            case PreviewType.Current: goal.ShowValue(instance); break;
            case PreviewType.Target: {
                    string[] names = Content.GetPrefabList<GummyBear>().Select(b => b.name).ToArray();
                    instance.ShowValue(design.bigObjects.Count(o => names.Contains(o.content.name)).ToString());
                } break;
        }
        return new List<TargetCounter>() { instance };
    }

    public void CollectionEffect(GummyBear bear, SpriteRenderer icon) {
        CollectionEffect effect = Content.Emit(collectionEffect);
        effect.transform.SetParent(FieldAssistant.main.sceneFolder);
        effect.transform.Reset();
        effect.transform.position = icon.transform.position;
        effect.SetTarget(_counter.nodeTarget.transform);
        effect.Play();
        effect.icon.sprite = icon.sprite;
        effect.icon.transform.rotation = icon.transform.rotation;
        effect.icon.transform.localScale = Vector3.one;
        effect.icon.transform.localScale = 
            new Vector3(icon.transform.lossyScale.x / effect.icon.transform.lossyScale.x,
            icon.transform.lossyScale.y / effect.icon.transform.lossyScale.y, 1);
        effect.onReach = () => ShowValue(_counter);
        ContentAnimator animation = effect.GetComponent<ContentAnimator>();
        if (animation) animation.Stop("OnAwake");
    }

    public override bool IsComplete() {
        return !Contains<GummyBear>();
    }

    public override bool IsFailed() {
        return false;
    }

    public override void ShowValue(TargetCounter counter) {
        int count = Count<GummyBear>();
        if (count > 0) counter.ShowValue(count.ToString());
        else counter.Complete();
    }

    public override void Serialize(XElement xml) {}
    public override void Deserialize(XElement xml, LevelGoalInfo info) {}
}
