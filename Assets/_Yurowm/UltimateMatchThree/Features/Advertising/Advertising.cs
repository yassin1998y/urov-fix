using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Flags]
public enum AdType {
    Regular = 1 << 0,
    Rewarded = 1 << 1
}

public class Advertising : MonoBehaviourAssistant<Advertising> {

    public int adDelay = 3;
    public int adFreeLevels = 5;

    static List<IAdIntegration> integrations = null;
    [HideInInspector]
    public List<IAdIntegration.Parameters> parameters = new List<IAdIntegration.Parameters>();

    DelayedAccess timeAccess;
    Action reward = null;

    void Start() {
        timeAccess = new DelayedAccess(adDelay * 60);
        integrations = IAdIntegration.allTypes
            .Select(type => {
                IAdIntegration.Parameters parameters = this.parameters.FirstOrDefault(x => x.typeName == type.FullName);
                if (parameters != null) return IAdIntegration.Deserialize(parameters);
                return (IAdIntegration) Activator.CreateInstance(type);
            }).Where(x => x != null).ToList();

        if (!Application.isEditor)
            integrations.ForEach(x => x.Initialize());

        UIAssistant.onShowPage += OnShowPage;
        DebugPanel.AddDelegate("Show Video Ads", () => ShowAds(null, true));
    }

    void OnShowPage(UIAssistant.Page page) {
        if (page.HasTag("ADS")) main.ShowAds();
    }

    void GiveReward() {
        if (reward != null) {
            reward.Invoke();
            reward = null;
        }
    }
    
    void Update() {
        foreach (IAdIntegration integration in integrations)
            integration.OnUpdate();
        if (Debug.isDebugBuild && DebugPanel.main) {
            foreach (IAdIntegration network in integrations) {
                if (!network.active) continue;
                if ((network.typeMask & AdType.Regular) != 0)
                    DebugPanel.Log(network.GetName() + " Regular", "Ads", network.IsReady(AdType.Regular));
                if ((network.typeMask & AdType.Rewarded) != 0)
                    DebugPanel.Log(network.GetName() + " Rewarded", "Ads", network.IsReady(AdType.Rewarded));
            }
        }
    }

    public void ShowAds(Action reward = null, bool force = false) {
        this.reward = reward;

        if (Application.isEditor) {
            if (reward != null) {
                reward.Invoke();
                reward = null;
            }
            return;
        }

        AdType type = reward == null ? AdType.Regular : AdType.Rewarded;

        List<IAdIntegration> target = integrations.Where(x => x.active && (x.typeMask & type) != 0 && x.IsReady(type)).ToList();

        if (target.Count > 0) ShowAds(target.GetRandom(), type, force);
    }

    void ShowAds(IAdIntegration integration, AdType type, bool force = false) {
        DebugPanel.Log("Ad Request", "Ads", "{0}: {1}".FormatText(integration.GetName(), type));
        StartCoroutine(ShowingAds(integration, type, force));
    }

    IEnumerator ShowingAds(IAdIntegration integration, AdType type, bool force = false) {
        if (CPanel.uiAnimation > 0)
            yield return 0;

        if (!integration.IsReady(type)) yield break;

        if (reward == null && !force) {
            if (CurrentUser.main.GetScore(adFreeLevels) == 0) yield break;
            if (!timeAccess.GetAccess()) yield break;
        }

        timeAccess.ResetTimer();
        integration.Show(type, GiveReward);
    }

    public int CountOfReadyAds(AdType type) {
        return integrations.Count(x => x.active && (x.typeMask & type) != 0 && x.IsReady(type));
    }
}

[Serializable]
public abstract class IAdIntegration {

    public AdType typeMask;
    public bool active = true;
    public static Type[] allTypes;

    static IAdIntegration() {
        allTypes = (typeof(Advertising)).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && (typeof(IAdIntegration)).IsAssignableFrom(x)).ToArray();
    }

    public IAdIntegration() {}

    public virtual void Initialize() {}
    public virtual void OnUpdate() {}

    public abstract bool IsReady(AdType type);

    public abstract void Show(AdType type, Action onComplete);

    public abstract string GetZoneID(AdType type);
    public abstract string GetAppID();

    public abstract string GetName();

    public Parameters Serialize() {
        return new Parameters(GetType().FullName, JsonUtility.ToJson(this));
    }

    public static IAdIntegration Deserialize(Parameters parameters) {
        Type type = allTypes.FirstOrDefault(x => x.FullName == parameters.typeName);
        if (type == null) return null;

        try {
            IAdIntegration result = (IAdIntegration) JsonUtility.FromJson(parameters.json, type);
            return result;
        } catch (Exception e) {
            Debug.LogException(e);
        }
        return null;
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
public abstract class AdIntegrationEditor {
    public abstract void OnGUI(IAdIntegration integration);
    public abstract bool IsSuitable(IAdIntegration integration);
}

public abstract class AdIntegrationEditor<T> : AdIntegrationEditor where T : IAdIntegration {
    public override void OnGUI(IAdIntegration integration) {
        DrawSettings((T) integration);
    }

    public abstract void DrawSettings(T integration);

    public override bool IsSuitable(IAdIntegration integration) {
        return integration is T;
    }
}
#endif