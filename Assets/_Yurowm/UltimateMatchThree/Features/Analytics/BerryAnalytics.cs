using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class BerryAnalytics : MonoBehaviourAssistant<BerryAnalytics> {
    public bool log = true;

    public List<IAnalyticsIntegration> integrations = new List<IAnalyticsIntegration>();
    [HideInInspector]
    public List<IAnalyticsIntegration.Parameters> parameters = new List<IAnalyticsIntegration.Parameters>();

    void Awake() {
        if (!Application.isEditor) {
            Application.logMessageReceived += Log;
            integrations = IAnalyticsIntegration.allTypes.Select(type => {
                    IAnalyticsIntegration.Parameters parameters = this.parameters.FirstOrDefault(x => x.typeName == type.FullName);
                    if (parameters != null)
                        return IAnalyticsIntegration.Deserialize(parameters);
                    return (IAnalyticsIntegration) Activator.CreateInstance(type);
                }).ToList();
            integrations.ForEach(x => x.Initialize());
        }
    }

    static void Log(string condition, string stackTrace, LogType type) {
        if (type == LogType.Error || type == LogType.Exception)
            Event(type.ToString(), 
                "Condition:" + condition,
                "Stack Trace:" + stackTrace,
                "Version:" + Application.version);
    }

    public static void Event(string eventName) {
        if (!main.log) return;
        main.integrations.ForEach(x => x.Event(eventName));
    }

    public static void Event(string eventName, float sum) {
        if (!main.log) return;
        main.integrations.ForEach(x => x.Event(eventName, sum));
    }

    public static void Event(string eventName, Dictionary<string, string> segmentation) {
        if (!main.log) return;
        main.integrations.ForEach(x => x.Event(eventName, segmentation));
    }

    public static void Event(string eventName, params string[] keys) {
        if (!main.log) return;
        main.integrations.ForEach(x => x.Event(eventName, keys));
    }

    public static void Event(string eventName, float sum, params string[] keys) {
        if (!main.log) return;
        main.integrations.ForEach(x => x.Event(eventName, sum, keys));
    }

}

[Serializable]
public abstract class IAnalyticsIntegration {
    public static List<Type> allTypes;

    static IAnalyticsIntegration() {
        allTypes = Utils.FindInheritorTypes<IAnalyticsIntegration>().Where(x => !x.IsAbstract).ToList();
    }

    public IAnalyticsIntegration() {}

    public virtual void Initialize() { }

    public abstract string GetName();

    public abstract void Event(string eventName);

    public virtual void Event(string eventName, float sum) {
        Event(eventName);
    }

    public virtual void Event(string eventName, Dictionary<string, string> segmentation) {
        Event(eventName);
    }

    public virtual void Event(string eventName, params string[] keys) {
        Event(eventName, KeysToSementation(keys));
    }

    public virtual void Event(string eventName, float sum, params string[] keys) {
        Event(eventName, KeysToSementation(keys));
    }

    public Dictionary<string, string> KeysToSementation(string[] keys) {
        try {
            Dictionary<string, string> result = keys.Select(k => k.Split(':')).ToDictionary(x => x[0], x => x[1]);
            return result;
        } catch (Exception) { }
        return null;
    }

    public static IAnalyticsIntegration Deserialize(Parameters parameters) {
        Type type = allTypes.FirstOrDefault(x => x.FullName == parameters.typeName);
        if (type == null) return null;
        try {
            IAnalyticsIntegration result = (IAnalyticsIntegration) JsonUtility.FromJson(parameters.json, type);
            return result;
        } catch (Exception e) {
            Debug.LogException(e);
        }
        return null;
    }

    public Parameters Serialize() {
        return new Parameters(GetType().FullName, JsonUtility.ToJson(this));
    }

    [Serializable]
    public class Parameters {
        public string typeName;
        public string json;
        public Parameters(string typeName, string json) {
            this.typeName = typeName;
            this.json = json;
        }
    }
}

#if UNITY_EDITOR
public abstract class AnalyticsIntegrationEditor {
    public abstract void OnGUI(IAnalyticsIntegration integration);
    public abstract bool IsSuitable(IAnalyticsIntegration integration);
}

public abstract class AnalyticsIntegrationEditor<T> : AnalyticsIntegrationEditor where T : IAnalyticsIntegration {
    public override void OnGUI(IAnalyticsIntegration integration) {
        DrawSettings((T) integration);
    }

    public abstract void DrawSettings(T integration);

    public override bool IsSuitable(IAnalyticsIntegration integration) {
        return integration is T;
    }
}
#endif


