using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using UnityEngine;
using UnityEngine.Events;
using System.Xml.Linq;
using System.Text.RegularExpressions;

public abstract class ILevelGoal : ILiveContent  {

    protected List<TargetCounter> counters = new List<TargetCounter>();

    internal ContentSound sound;

    public abstract bool IsComplete();
    public abstract bool IsFailed();
    public abstract UnityEvent GetControlEvent();

    UnityEvent control = null;

    public abstract List<TargetCounter> GetCurrentCounters(SessionInfo session);
    public abstract List<TargetCounter> GetPreviewCounters(ILevelGoal goal, LevelDesign design, LevelGoalInfo info, PreviewType type);

    const string postfix = "Goal";

    public override void Initialize() {
        base.Initialize();
        sound = GetComponent<ContentSound>();
        control = GetControlEvent();
        if (control != null)
            control.AddListener(Control);
    }


    public void SubsribeToUpdate(TargetCounter counter) {
        counters.Add(counter);
    }

    public virtual void SetInfo(LevelGoalInfo info, SessionInfo session) {}

    protected void Control () {
        counters.RemoveAll(x => x == null);
        foreach (var counter in counters)
            if (!counter.complete)
                ShowValue(counter);
        if (IsComplete()) {
            if (control != null)
                control.RemoveListener(Control);
        }
    }

    public void ForceUpdateCounters() {
        Control();
    }

    public abstract void ShowValue(TargetCounter counter);
    
    public static string GetName(Type type) {
        if (!(typeof(ILevelGoal).IsAssignableFrom(type)))
            return "";
        return type.Name.NameFormat(null, "Goal", true);
    }

    public static Type GetModeByName(string name) {
        Type refType = typeof(ILevelGoal);
        return refType.Assembly.GetTypes().FirstOrDefault(x => refType.IsAssignableFrom(x) && refType.Name == name);
    }

    public abstract int GetAssess(Slot slot);

    public abstract void Serialize(XElement xml);

    public abstract void Deserialize(XElement xml, LevelGoalInfo info);
}
