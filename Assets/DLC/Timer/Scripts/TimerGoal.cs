using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using Yurowm.GameCore;

public class TimerGoal : ILevelGoal, ISounded {

    public const string timer_parameter = "timer";

    [ContentSelector]
    public TargetCounter counter;
    int time = 0;
    bool complete = false;

    public override void SetInfo(LevelGoalInfo info, SessionInfo session) {
        base.SetInfo(info, session);
        time = info[timer_parameter].Int;
        StartCoroutine(Logic());
    }

    IEnumerator Logic() {
        yield return new WaitForSeconds(1);

        while (!IsFailed() && !IsComplete()) {
            if (SessionInfo.current.isPlaying) {
                time --;
                if (time <= 10) sound.Play("TickTock");
                ForceUpdateCounters();            
            }
            yield return new WaitForSeconds(1);
        }

        if (IsComplete()) {
            ForceUpdateCounters();
            yield break;
        }

        time = 0;
        LevelRule rule = SessionInfo.current.rule;
        while (rule.GetMode() != PlayingMode.Wait)
            yield return 0;
        rule.SetMode(PlayingMode.TargetChecking);
    }

    public override int GetAssess(Slot slot) {
        return 0;
    }

    public override UnityEvent GetControlEvent() {
        return null;
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
            case PreviewType.Target: instance.ShowValue(TimerFormat(info[timer_parameter].Int)); break;
            case PreviewType.Current: (goal as TimerGoal).ShowValue(instance); break;
        }
        return new List<TargetCounter>() { instance };
    }

    public override bool IsComplete() {
        if (complete) return true;
        if (SessionInfo.current == null)
            complete = false;
        else 
            complete = !SessionInfo.current.GetGoals().Where(g => g != this).
                Contains(g => !g.IsComplete());
        return complete;
    }

    public override bool IsFailed() {
        if (complete) return false;
        return time <= 0;
    }


    public override void ShowValue(TargetCounter counter) {
        if (complete) counter.Complete();
        else counter.ShowValue(TimerFormat(time));
    }

    public static string TimerFormat(int seconds) {
        if (seconds < 0) seconds = 0;
        int minutes = Mathf.FloorToInt(1f * seconds / 60);
        seconds -= minutes * 60;
        return minutes + ":" + seconds.ToString("00");
    }

    public IEnumerator GetSoundNames() {
        yield return "TickTock";
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute(timer_parameter, time));
    }

    public override void Deserialize(XElement xml, LevelGoalInfo info) {
        info[timer_parameter].Int = int.Parse(xml.Attribute(timer_parameter).Value);
    }
}