using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yurowm.GameCore;

public class FloodGoal : ILevelGoal, IDeepLevelGoal, ISounded {

    #region Level parameter keys
    public const string targetCount_parameter = "count";
    #endregion

    [ContentSelector]
    public TargetCounter counter;
    [ContentSelector]
    public LifeBuoyChip sodaChip;
    [ContentSelector]
    public CollectionEffect collectionEffect;

    public Transform sodaLevel;

    int count = 0;

    public override void SetInfo(LevelGoalInfo info, SessionInfo session) {
        base.SetInfo(info, session);
        Project.onSlotContentPrepareToDestroy.AddListener(OnSlotContentPrepareToDestroy);
        count = info[targetCount_parameter].Int;
        UpdateSodaLevel();
        sodaLevel.gameObject.SetActive(false);
        Project.onLevelStart.AddListener(() => StartCoroutine(Logic()));
    }

    private IEnumerator Logic() {
        int count = -1;

        while (SessionInfo.current == null || !SessionInfo.current.isPlaying)
            yield return 0;

        sodaLevel.gameObject.SetActive(true);
        sodaLevel.transform.position = new Vector3(GameCamera.camera.transform.position.x,
            Project.main.slot_offset * this.count, 0) - Vector3.up * 5;

        while (true) {
            while (count == this.count)
                yield return 0;
            sound.Play("FloodLevel");
            Debug.Log("FloodLevel");
            while (!MoveWater(this.count))
                yield return 0;
            count = this.count;
        }
    }

    bool MoveWater(float target) {
        Vector3 t = new Vector3(GameCamera.camera.transform.position.x, Project.main.slot_offset * target, 0);

        sodaLevel.position = Vector3.MoveTowards(sodaLevel.position, t,
            Project.main.slot_offset * 5 * Time.deltaTime);

        return t == sodaLevel.position;
    }

    void OnSlotContentPrepareToDestroy(ISlotContent content) {
        if (sodaChip.EqualContent(content)) {
            if (IsComplete())
                return;
            if (collectionEffect) {
                CollectionEffect effect = Content.Emit(collectionEffect);
                effect.transform.SetParent(FieldAssistant.main.sceneFolder);
                effect.transform.Reset();
                effect.transform.position = content.transform.position;
                effect.Repaint(content.colored.color);
                effect.SetItem(content);
                effect.SetTarget(_counter.nodeTarget.transform);
                effect.onReach += () => {
                    count++;
                    Control();
                    UpdateSodaLevel();
                };
                effect.Play();
            } else {
                count++;
                Control();
                UpdateSodaLevel();
            }
        }
    }

    void UpdateSodaLevel() {
        foreach (Slot slot in Slot.all.Values) {
            if (slot.y == count - 1 || slot.y == count)
                slot.falling.Clear();
            else
                slot.falling = new Dictionary<Side, Slot>() 
                { { slot.y < count ? Side.Top : Side.Bottom, null} };
        }
        
        Slot.all.ForEach(x => x.Value.CulculateFallingSlot());
    }

    public override int GetAssess(Slot slot) {
        ISlotContent content = slot.GetCurrentContent();
        if (sodaChip.EqualContent(content))
            return 1000;
        if (content.destroyable != null)
            return content.destroyable.destroyReward;
        return 0;
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
            case PreviewType.Target: instance.ShowValue((design.deep ? design.deepHeight : design.height).ToString()); break;
            case PreviewType.Current: (goal as FloodGoal).ShowValue(instance); break;
        }
        return new List<TargetCounter>() { instance };
    }

    public override bool IsComplete() {
        return count >= GetTargetCount();
    }

    public override bool IsFailed() {
        return false;
    }

    public override void ShowValue(TargetCounter counter) {
        int count = GetTargetCount() - this.count;
        if (count > 0) counter.ShowValue(count.ToString());
        else {
            counter.Complete();
            count += 3;
        }
    }

    int GetTargetCount() {
        return SessionInfo.current.design.deep?
            SessionInfo.current.design.deepHeight :
            SessionInfo.current.design.height;
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute(targetCount_parameter, count));
    }

    public override void Deserialize(XElement xml, LevelGoalInfo info) {
        XAttribute attribute = xml.Attribute(targetCount_parameter);
        if (attribute != null)
            info[targetCount_parameter].Int = int.Parse(attribute.Value);
    }

    public DeepLevelDirection GetDirection() {
        return DeepLevelDirection.Up;
    }

    public int ChangeDeepIndex() {
        return count - SessionInfo.current.deepIndex - SessionInfo.current.design.height / 2;
    }

    public IEnumerator GetSoundNames() {
        yield return "FloodLevel";
    }
}

public class SodaGoalReaction : Reaction {
   
    public override int GetPriority() {
        return 0;
    }

    public override bool IsSuitable() {
        return SessionInfo.current.IsLevelGoal<FloodGoal>();
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Move;
    }

    public override IEnumerator React() {
        if (ILiveContent.Contains<LifeBuoyChip>(x => x.isActiveContent))
            yield break;

        for (int i = 0; i < 2; i++) {
            FieldAssistant.main.Add<LifeBuoyChip>();
            yield return new WaitForSeconds(0.2f);
        }
    }
}