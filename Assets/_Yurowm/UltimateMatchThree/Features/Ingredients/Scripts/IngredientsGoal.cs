
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yurowm.GameCore;

public class IngredientsGoal : ILevelGoal {

    #region Level parameter keys
    public const string targetCount_parameter = "target";
    public const string activeCount_parameter = "active";
    #endregion

    public static int targetCount;
    public static int activeCount;

    [ContentSelector]
    public TargetCounter counter;

    [ContentSelector]
    public CollectionEffect collectionEffect;

    public override void SetInfo(LevelGoalInfo info, SessionInfo session) {
        base.SetInfo(info, session);
        Project.onSlotContentDestroyed.AddListener(OnContentDestroyed);
        targetCount = info[targetCount_parameter].Int;
        activeCount = info[activeCount_parameter].Int;
    }

    void OnContentDestroyed(ISlotContent content) {
        if (content is IngredientChip) {
            if (collectionEffect) {
                CollectionEffect effect = Content.Emit(collectionEffect);
                effect.transform.SetParent(FieldAssistant.main.sceneFolder);
                effect.transform.Reset();
                effect.transform.position = content.transform.position;
                effect.SetItem(content);
                effect.SetTarget(_counter.nodeTarget.transform);
                effect.onReach += () => {
                    targetCount--;
                    Control();
                };
                effect.Play();
            } else {
                targetCount--;
                Control();
            }
        }
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
            case PreviewType.Target: instance.ShowValue(info[targetCount_parameter].Int.ToString()); break;
            case PreviewType.Current: (goal as IngredientsGoal).ShowValue(instance); break;
        }
        return new List<TargetCounter>() { instance };
    }

    public override bool IsComplete() {
        return targetCount <= 0;
    }

    public override bool IsFailed() {
        return false;
    }

    public override void ShowValue(TargetCounter counter) {
        if (IsComplete())
            counter.Complete();
        else
            counter.ShowValue(targetCount.ToString());
    }

    public override int GetAssess(Slot slot) {
        int2 position = slot.position + Side.Top;
        while (position.y <= SessionInfo.current.design.fieldSize.y) {
            Slot _slot = Slot.allActive.Get(position);
            if (_slot) {
                if (_slot.chip && _slot.chip is IngredientChip)
                    return 10;
            }
            position.y++;
        }

        return 0;
    }
    
    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute(targetCount_parameter, targetCount));
        xml.Add(new XAttribute(activeCount_parameter, activeCount));
    }

    public override void Deserialize(XElement xml, LevelGoalInfo info) {
        info[targetCount_parameter].Int = int.Parse(xml.Attribute(targetCount_parameter).Value);
        info[activeCount_parameter].Int = int.Parse(xml.Attribute(activeCount_parameter).Value);
    }
}