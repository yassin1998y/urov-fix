using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;
using Yurowm.GameCore;
using System.Xml.Linq;

public class SpinWheelAssistant : MonoBehaviourAssistant<SpinWheelAssistant>, ILocalized {

    const string localizationItemFormat = "id/{0}";
    public const int itemCount = 12;

    [HideInInspector]
    public List<Reward> rewards = new List<Reward>();

    public Reward EmitReward() {
        Reward result = null;

        float rnd = UnityEngine.Random.Range(0, rewards.Sum(x => x.weight));
        foreach (Reward reward in rewards) {
            rnd -= reward.weight;
            if (rnd <= 0) {
                result = reward;
                break;
            }
        }

        if (result != null) {
            CurrentUser.main[result.item] += result.count;
            UserUtils.WriteProfileOnDevice(CurrentUser.main);
        }

        return result;
    }

    public IEnumerator RequriedLocalizationKeys() {
        foreach (string itemID in Enum.GetNames(typeof(ItemID)))
            yield return string.Format(localizationItemFormat, itemID);
        yield return "notifications/daily-title";
        yield return "notifications/daily-message";
    }

    [Serializable]
    public class Reward {
        public enum Type {Regular, Rare, Unique}

        public int index = 0;
        public ItemID item;
        public Type type;
        public int count;
        public float weight;
        public Sprite icon;

        public Reward(ItemID item, int count, float weight) {
            this.count = count;
            this.item = item;
            this.weight = weight;
        }
    }
}

public class DailySpinTask : ScheduledTask {
    const int eventHour = 8;
    const ItemID spinID = ItemID.spin;

    CurrentUser user;

    DateTime? nextFreeSpin = null;
    readonly TimeSpan notificationSpan = TimeSpan.FromHours(4);
    TimeSpan? zone = null;

    public DailySpinTask(CurrentUser user) {
        uniqueID = "DailySpin";
        this.user = user;
    }

    public override NextCall OnStart(DateTime time) {
        if (!zone.HasValue) {
            zone = TrueTime.Zone;
            nextFreeSpin = null;
        } else if (nextFreeSpin.HasValue && zone.Value != TrueTime.Zone) {
            nextFreeSpin -= zone.Value;
            zone = TrueTime.Zone;
            nextFreeSpin += zone.Value;
            user.Save();
            UpdateNotification();
        }
        if (!nextFreeSpin.HasValue) {
            nextFreeSpin = GetNextFreeSpin(TrueTime.NowLocal);
            user.Save();
            UpdateNotification();
        }
        return new NextCall(Update);
    }

    void Update() {
        if (nextFreeSpin.Value <= TrueTime.NowLocal) {
            user[spinID]++;
            zone = TrueTime.Zone;
            DateTime next = GetNextFreeSpin(TrueTime.NowLocal);
            if ((next - nextFreeSpin.Value).TotalDays < .5d)
                next = next.AddDays(1);
            nextFreeSpin = next;
            user.Save();
            ItemCounter.RefreshAll();
        }
        SetNextCall(Update, nextFreeSpin.Value - zone.Value);
        UpdateNotification();
    }

    DateTime GetNextFreeSpin(DateTime time) {
        DateTime result = new DateTime(time.Year, time.Month, time.Day, eventHour, 0, 0);
        while (result < time)
            result = result.AddDays(1);
        return result;
    }

    public string GetTimer() {
        if (!TrueTime.IsKnown || !nextFreeSpin.HasValue)
            return "...";
        TimeSpan span = nextFreeSpin.Value - TrueTime.NowLocal;
        return string.Format("{0:00}:{1:00}:{2:00}",
               Mathf.FloorToInt((float) span.TotalHours), span.Minutes, span.Seconds);
    }

    void UpdateNotification() {
        string name = "DailyBonus";
        if (nextFreeSpin.HasValue)
            Notifications.ScheduleNotification(name, nextFreeSpin.Value + notificationSpan,
                LocalizationAssistant.main["notifications/daily-title"],
                LocalizationAssistant.main["notifications/daily-message"]);
        else
            Notifications.CancelNotification(name);
    }

    public override void Serialize(XElement xml) {
        if (nextFreeSpin.HasValue) xml.Add(new XAttribute("time", nextFreeSpin.Value.Ticks));
        if (zone.HasValue) xml.Add(new XAttribute("zone", zone.Value.Ticks));
    }

    public override Dictionary<string, object> Serialize() {
        Dictionary<string, object> json = base.Serialize();

        if (nextFreeSpin.HasValue) json.Set("time", nextFreeSpin.Value.Ticks);
        else json.Set("time", null);

        if (zone.HasValue) json.Set("zone", zone.Value.Ticks);
        else json.Set("zone", null);

        return json;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("time");
        if (attribute != null) nextFreeSpin = new DateTime(long.Parse(attribute.Value));
        else nextFreeSpin = null;
        attribute = xml.Attribute("zone");
        if (attribute != null) zone = new TimeSpan(long.Parse(attribute.Value));
        else zone = null;
    }

    public override void Deserialize(Dictionary<string, object> json) {
        object value = json.Get("time");
        if (value != null) nextFreeSpin = new DateTime((long) (double) value); 
        else nextFreeSpin = null;
        value = json.Get("zone");
        if (value != null) zone = new TimeSpan((long) value);
        else zone = null;
    }
}

public class RewardedSpinTask : ScheduledTask {
    readonly TimeSpan delay = new TimeSpan(4, 0, 0);
    const ItemID spinID = ItemID.spin;

    CurrentUser user;

    DateTime? nextReward = null;

    public RewardedSpinTask(CurrentUser user) {
        uniqueID = "RewardedSpin";
        this.user = user;
    }

    public override NextCall OnStart(DateTime time) {
        if (!nextReward.HasValue) 
            return null;
        return new NextCall(Update, nextReward.Value);
    }

    void Update() {
        if (nextReward.Value <= TrueTime.Now) {
            nextReward = null;
            user.Save();
            ItemCounter.RefreshAll();
        }
        TrueTime.Unschedule(this);
    }

    public bool IsAvailable() {
        return TrueTime.IsKnown && !nextReward.HasValue &&
            Advertising.main.CountOfReadyAds(AdType.Rewarded) > 0;
    }

    public void Get() {
        if (IsAvailable()) {
            BerryAnalytics.Event("Rewarded Ads Spin");
            Advertising.main.ShowAds(() => {
                user[spinID]++;
                nextReward = TrueTime.Now + delay;
                user.Save();
                TrueTime.Schedule(this);
                ItemCounter.RefreshAll();
            });
        }
    }

    public string GetTimer() {
        if (!TrueTime.IsKnown || !nextReward.HasValue)
            return "...";
        TimeSpan span = nextReward.Value - TrueTime.Now;
        return string.Format("{0:00}:{1:00}:{2:00}",
               Mathf.FloorToInt((float) span.TotalHours), span.Minutes, span.Seconds);
    }

    public override void Serialize(XElement xml) {
        if (nextReward.HasValue)
            xml.Add(new XAttribute("time", nextReward.Value.Ticks));
    }

    public override Dictionary<string, object> Serialize() {
        Dictionary<string, object> json = base.Serialize();

        if (nextReward.HasValue) json.Set("time", nextReward.Value.Ticks);
        else json.Set("time", null);

        return json;
    }

    public override void Deserialize(XElement xml) {
        XAttribute attribute = xml.Attribute("time");
        if (attribute != null) nextReward = new DateTime(long.Parse(attribute.Value));
        else nextReward = null;
    }

    public override void Deserialize(Dictionary<string, object> json) {
        object value = json.Get("time");
        if (value != null) nextReward = new DateTime((long) (double) value);
        else nextReward = null;
    }
}