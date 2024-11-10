using System.Collections.Generic;
using Yurowm.GameCore;
using UnityEngine.Events;
using System.Xml.Linq;

public class StarsGoal : ILevelGoal, IDefault {

    [ContentSelector]
    public TargetCounter counter;

    public override bool IsComplete() {
        if (SessionInfo.current != null)
            return SessionInfo.current.design.thirdStarScore <= SessionInfo.current.GetScore();
        return false;
    }

    public override bool IsFailed() {
        return false;
    }

    public override List<TargetCounter> GetCurrentCounters(SessionInfo session) {
        TargetCounter instance = Content.GetItem<TargetCounter>(counter.name);
        instance.SetGoal(this);

        return new List<TargetCounter>() { instance };
    }

    public override List<TargetCounter> GetPreviewCounters(ILevelGoal goal, LevelDesign design, LevelGoalInfo info, PreviewType type) {
        TargetCounter instance = Content.GetItem<TargetCounter>(counter.name);
        switch (type) {
            case PreviewType.Complete: instance.Complete(); break;
            case PreviewType.Target: instance.ShowValue("3"); break;
            case PreviewType.Current: (goal as StarsGoal).ShowValue(instance); break;
        }
        return new List<TargetCounter>() { instance };
    }

    public override UnityEvent GetControlEvent() {
        return Project.onReachedTheStar;
    }

    public override void ShowValue(TargetCounter counter) {
        if (IsComplete())
            counter.Complete();
        else if (SessionInfo.current != null && SessionInfo.current.isPlaying)
            counter.ShowValue((3 - SessionInfo.current.GetStarCount()).ToString());
        else
            counter.ShowValue("3");
    }

    public override int GetAssess(Slot slot) {
        int assess = 0;
        foreach (ISlotContent content in slot.Content())
            if (content is IDestroyable)
                assess += (content as IDestroyable).destroyReward;

        return assess;
    }

    public override void Serialize(XElement xml) {}

    public override void Deserialize(XElement xml, LevelGoalInfo info) {}
}