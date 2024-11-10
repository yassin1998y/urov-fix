using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.Events;
using Yurowm.GameCore;

public class CollectionGoal : ILevelGoal, IDeepLevelGoal {

    #region Level parameter keys
    public const string targetCount_parameter = "count";
    #endregion

    [ContentSelector]
    public TargetCounter counter;
    [ContentSelector]
    public CollectionEffect collectionEffect;
    [ContentSelector(typeof(IDestroyable))]
    public ISlotContent target;
    public bool toDestroyAllTargets = false;
    int count = 0;


    public override void SetInfo(LevelGoalInfo info, SessionInfo session) {
        base.SetInfo(info, session);
        Project.onSlotContentPrepareToDestroy.AddListener(OnSlotContentPrepareToDestroy);
        if (!toDestroyAllTargets)
            count = info[targetCount_parameter].Int;
    }

    void OnSlotContentPrepareToDestroy(ISlotContent content) {
        if (!toDestroyAllTargets && count <= 0)
            return;
        if (target.EqualContent(content)) {
            if (collectionEffect) {
                CollectionEffect effect = Content.Emit(collectionEffect);
                effect.transform.SetParent(FieldAssistant.main.sceneFolder);
                effect.transform.Reset();
                effect.transform.position = content.transform.position;
                effect.SetItem(content);
                effect.SetTarget(_counter.nodeTarget.transform);
                effect.onReach += () => {
                    count--;
                    Control();
                };
                effect.Play();
            } else {
                count--;
                Control();
            }
        }
    }

    public override int GetAssess(Slot slot) {
        int assess =  slot.Content().Count(c => target.EqualContent(c)) * 10;
        ISlotContent content = slot.GetCurrentContent();
        if (content.destroyable != null)
            assess += content.destroyable.destroyReward;
        return assess;
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
            case PreviewType.Current: (goal as CollectionGoal).ShowValue(instance); break;
            case PreviewType.Target: {
                    if (toDestroyAllTargets)
                        instance.ShowValue(design.slots.Count(
                            x => x.content.FirstOrDefault(y => y.name == target.name) != null).ToString());
                    else
                        instance.ShowValue(info[targetCount_parameter].Int.ToString());
                } break;
        }
        return new List<TargetCounter>() { instance };
    }

    public override bool IsComplete() {
        if (toDestroyAllTargets) return !Contains(CheckTargets, target);
        else return count <= 0;
    }

    public override bool IsFailed() {
        return false;
    }

    public override void ShowValue(TargetCounter counter) {
        int count = toDestroyAllTargets ? Count(CheckTargets, target) : this.count;
        if (count > 0) counter.ShowValue(count.ToString());
        else counter.Complete();
    }

    bool CheckTargets(ISlotContent content) {
        return !content.destroying;
    }

    public override void Serialize(XElement xml) {
        if (!toDestroyAllTargets)
            xml.Add(new XAttribute(targetCount_parameter, count));
    }

    public override void Deserialize(XElement xml, LevelGoalInfo info) {
        if (!toDestroyAllTargets) {
            XAttribute attribute = xml.Attribute(targetCount_parameter);
            if (attribute != null) info[targetCount_parameter].Int = int.Parse(attribute.Value);
        }
    }

    public DeepLevelDirection GetDirection() {
        return DeepLevelDirection.Down;
    }

    public int ChangeDeepIndex() {
        int activeTargetCount = Count(x => x.isActiveContent, target);
        if (activeTargetCount != 0)
            return 0;
        var hieghest = GetAll(null, target).GetMax(x => (x as ISlotContent).slot.y);
        
        return hieghest.slot.y - SessionInfo.current.deepIndex - 2;
    }
}
