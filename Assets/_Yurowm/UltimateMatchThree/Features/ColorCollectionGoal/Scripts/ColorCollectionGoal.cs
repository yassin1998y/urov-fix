using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.Events;
using Yurowm.GameCore;

public class ColorCollectionGoal : ILevelGoal {

    #region Level parameter keys
    public const string targetCount_parameter = "count";
    public const string collectionSize_parameter = "count_{0}";
    public const string collectionSize_XMLattribute = "count";
    #endregion

    [ContentSelector]
    public TargetCounterColor counter;

    [ContentSelector]
    public CollectionEffect collectionEffect;

    public Dictionary<ItemColor, int> colorTarget;

    public override UnityEvent GetControlEvent() {
        return null;
    }

    public override void SetInfo(LevelGoalInfo info, SessionInfo session) {
        base.SetInfo(info, session);
        Project.onSlotContentPrepareToDestroy.AddListener(OnSlotContentPrepareToDestroy);
        colorTarget = new Dictionary<ItemColor, int>();
        string key;
        for (int color = 0; color < info[string.Format(targetCount_parameter, color)].Int; color++) {
            key = string.Format(collectionSize_parameter, color);
            colorTarget.Set(session.colorMask[(ItemColor) color], info[key].Int);
        }
    }

    void OnSlotContentPrepareToDestroy(ISlotContent content) {
        if (content is IColored) {
            IColored colored = content as IColored;
            if (colorTarget.Get(colored.color) > 0) {
                if (collectionEffect) {
                    CollectionEffect effect = Content.Emit(collectionEffect);
                    effect.transform.SetParent(FieldAssistant.main.sceneFolder);
                    effect.transform.Reset();
                    effect.transform.position = content.transform.position;
                    effect.SetItem(content);
                    effect.SetTarget(colorCounters[colored.color].nodeTarget.transform);
                    effect.Repaint(colored.color);
                    effect.onReach += () => {
                        colorTarget[colored.color]--;
                        Control();
                    };
                    effect.Play();
                } else {
                    colorTarget[colored.color] --;
                    Control();
                }

            }
        } 
    }

    Dictionary<ItemColor, TargetCounterColor> colorCounters;
    public override List<TargetCounter> GetCurrentCounters(SessionInfo session) {
        List<TargetCounter> result = new List<TargetCounter>();
        foreach (ItemColor color in colorTarget.Keys) {
            TargetCounterColor instance = Content.GetItem<TargetCounterColor>(counter.name);
            instance.SetGoal(this);
            instance.color = color;
            result.Add(instance);
        }
        colorCounters = result.Cast<TargetCounterColor>().ToDictionary(x => x.color, x => x);
        return result;
    }

    public override List<TargetCounter> GetPreviewCounters(ILevelGoal goal, LevelDesign design, LevelGoalInfo info, PreviewType type) {
        List<TargetCounter> result = new List<TargetCounter>();
        if (type == PreviewType.Current) {
            if (goal != null) {
                foreach (var target in (goal as ColorCollectionGoal).colorTarget) {
                    TargetCounterColor instance = Content.GetItem<TargetCounterColor>(counter.name);
                    instance.color = target.Key;
                    (goal as ColorCollectionGoal).ShowValue(instance);
                    result.Add(instance);
                }
            }
        } else {
            string key;
            List<ItemColor> unsorted = ItemColorUtils.physiscalColors;
            if (design.randomizeColors)
                unsorted = unsorted.Unsort(new URandom(Project.randomSeed * design.number)).ToList();
            for (int colorID = 0; colorID < info[targetCount_parameter].Int; colorID++) {
                TargetCounterColor instance = Content.GetItem<TargetCounterColor>(counter.name);
                instance.color = unsorted[colorID];
                key = string.Format(collectionSize_parameter, colorID);
                switch (type) {
                    case PreviewType.Complete: instance.Complete(); break;
                    case PreviewType.Target: instance.ShowValue(info[key].Int.ToString()); break;
                }
                result.Add(instance);
            }
        }
        return result;
    }

    public override bool IsComplete() {
        return !colorTarget.Contains(x => x.Value > 0);
    }

    public override bool IsFailed() {
        return false;
    }

    public override void ShowValue(TargetCounter counter) {
        if (counter is TargetCounterColor) {
            if (colorTarget.Get(((TargetCounterColor) counter).color) <= 0)
                counter.Complete();
            else
                counter.ShowValue(colorTarget.Get(((TargetCounterColor) counter).color).ToString());
        }
    }

    public override int GetAssess(Slot slot) {
        if (slot.color.IsPhysicalColor() && colorTarget.Get(slot.color) > 0) {
            ISlotContent content = slot.GetCurrentContent();
            if (content.destroyable != null)
                return content.destroyable.destroyReward;
        }
        return 0;
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute(collectionSize_XMLattribute, string.Join(",",
            colorTarget.Select(x => x.Value.ToString()).ToArray())));
    }

    public override void Deserialize(XElement xml, LevelGoalInfo info) {
        int index = 0;
        foreach (int count in xml.Attribute(collectionSize_XMLattribute).Value.Split(',').Select(x => int.Parse(x))) {
            string key = string.Format(collectionSize_parameter, index);
            info[key].Int = count;
            index++;
        }
        info[targetCount_parameter].Int = index;
    }
}