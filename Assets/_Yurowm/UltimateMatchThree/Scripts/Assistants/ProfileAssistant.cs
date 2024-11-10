using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Xml.Linq;
using Yurowm.GameCore;
using Object = System.Object;
#if ONLINE
using Facebook.Unity;
#endif

public class ProfileAssistant : MonoBehaviourAssistant<ProfileAssistant>, ILocalized {

#if UNITY_EDITOR
    public long debugTimeOffset = 0;
#endif

    public static System.Action onConnectionStarted = delegate { };
    public static System.Action onConnectionFinished = delegate { };
    public static System.Action onConnectionCanceled = delegate { };
    public static System.Action onDisconnectionStarted = delegate { };
    public static System.Action onDisconnectionFinished = delegate { };
    public static System.Action<string> onConnectionFailed = delegate { };
    public static System.Action<string> onConnectionStatusChanged = delegate { };
    
    public bool firstStartMenuSkiping = true;
    public List<InitialItem> firstStartInventory = new List<InitialItem>();
    
    public void UnlockAllLevels() {
        foreach (LevelDesign level in LevelAssistant.main.designs)
            CurrentUser.main.UpdateLevelStatistic(level.number, x => {
                if (!x.complete) x.bestScore = 1;
            });
        UserUtils.WriteProfileOnDevice(CurrentUser.main);
        LevelMapDisplayer.RefreshAll();
    }

    public void ClearData() {
        SessionInfo.RemoveSavedSession();
        UserUtils.RemoveProfileFromDevice();
        CurrentUser.main = new CurrentUser();
        LevelMapDisplayer.RefreshAll();
        if (Application.isPlaying)
            ItemCounter.RefreshAll();
    }

    void Awake() {
        CurrentUser.main = UserUtils.ReadProfileFromDevice();

        UIAssistant.onShowPage += TryToSaveProfile;
        
        DebugPanel.AddDelegate("Clear Data", ClearData);
        DebugPanel.AddDelegate("Unlock all levels", UnlockAllLevels);
        DebugPanel.AddDelegate("Add Lifes", () => CurrentUser.main.lifeSystem.Refuel());
        DebugPanel.AddDelegate("Add Coins", () => CurrentUser.main[ItemID.coin] += 100);
        DebugPanel.AddDelegate("Add Some Stuff", () => {
            foreach (ItemID id in Enum.GetValues(typeof(ItemID))) {
                if (id == ItemID.life || id == ItemID.lifeslot)
                    continue;
                CurrentUser.main[id] += 10;
                ItemCounter.refresh();
            }
        });
        #if ONLINE
        FB.Init(() => { }, (bool isGameShown) => { });
        #endif
    }

    void Start() {
        IMissYouNotification();
    }

    void IMissYouNotification() {
        string name = "IMissYou{0}";

        TimeSpan time = TimeSpan.FromDays(3);
        for (int i = 1; i <= 3; i++) {
            Notifications.ScheduleNotification(name.FormatText(i), time,
                LocalizationAssistant.main["notifications/i-miss-you-title"],
                LocalizationAssistant.main["notifications/i-miss-you-message{0}".FormatText(i)]);
            time = time.Add(TimeSpan.FromSeconds(12));
        }
    }

    public static DelayedAccess saveAccess = new DelayedAccess(3);
    bool isConnecting = false;

    void TryToSaveProfile(UIAssistant.Page page) {
        if (isConnecting) return;
        CurrentUser.main.Save();
        if (page.HasTag("SAVE") && TrueTime.IsKnown && saveAccess.GetAccess())
            Online.main.Save();
    }

#region Store Reward Actions 
    public void SetUnlimitedLivesHour() {
        CurrentUser.main.lifeSystem.SetUnlimitedLives(1);
    }

    public void SetUnlimitedLivesDay() {
        CurrentUser.main.lifeSystem.SetUnlimitedLives(24);
    }

    public void RefuelLives() {
        CurrentUser.main.lifeSystem.Refuel();
    }
#endregion

    public IEnumerator RequriedLocalizationKeys() {
        yield return "notifications/lives-title";
        yield return "notifications/lives-message";

        yield return "notifications/i-miss-you-title";
        for (int i = 1; i <= 3; i++)
            yield return "notifications/i-miss-you-message{0}".FormatText(i);
    }
}

public abstract class User {
    public Dictionary<int, LevelStatistic> sessions = new Dictionary<int, LevelStatistic>();

    public string facebookID = "";
    public string name = "";

    public int level {
        get {
            int key = 0;
            if (sessions.Count > 0)
                key = sessions.Where(s => s.Value.complete).GetMax(x => x.Key).Key;
            return key + 1;
        }
    }

    public int GetScore(int level_number) {
        LevelStatistic levelStatistic = sessions.Get(level_number);
        return (levelStatistic != null ? levelStatistic.bestScore : 0);
    }

    public class LevelStatistic {
        public int bestScore = 0;
        public int totalCount = 0;
        public int successedCount = 0;
        public int failedCount = 0;
        public int escapedCount = 0;
        public bool complete {
            get {
                return bestScore > 0;
            }
        }

        public void Serialize(XElement xml) {
            xml.Add(new XAttribute("score", bestScore));
            xml.Add(new XAttribute("total", totalCount));
            xml.Add(new XAttribute("successed", successedCount));
            xml.Add(new XAttribute("failed", failedCount));
            xml.Add(new XAttribute("escaped", escapedCount));
        }

        public static LevelStatistic Deserialize(XElement xml) {
            LevelStatistic result = new LevelStatistic();
            result.bestScore = int.Parse(xml.Attribute("score").Value);
            result.totalCount = int.Parse(xml.Attribute("total").Value);
            result.successedCount = int.Parse(xml.Attribute("successed").Value);
            result.failedCount = int.Parse(xml.Attribute("failed").Value);
            result.escapedCount = int.Parse(xml.Attribute("escaped").Value);
            return result;
        }
    }
}

public class CurrentUser : User {

    static CurrentUser _main = null;
    public static CurrentUser main {
        get {
            return _main;
        }
        set {
            _main = value;
            _main.Initialize();
        }
    }

    public LifeSystemTask lifeSystem;
    public DailySpinTask dailySpin;
    public RewardedSpinTask rewardedSpin;

    void Initialize() {
        TrueTime.Schedule(lifeSystem);
        TrueTime.Schedule(dailySpin);
        TrueTime.Schedule(rewardedSpin);
    }

    public CurrentUser() {
        lifeSystem = new LifeSystemTask(this);
        dailySpin = new DailySpinTask(this);
        rewardedSpin = new RewardedSpinTask(this);
    }

    public string userID = "";
    public string hardwareID = SystemInfo.deviceUniqueIdentifier;

    public bool CompareTo(CurrentUser another) {
        return userID == another.userID
            && hardwareID == another.hardwareID;
    }

    public Dictionary<ItemID, int> inventory = new Dictionary<ItemID, int>();

    public int sessionCount = 0;
    public DateTime? lastSave = null;

    public int this[ItemID index] {
        get {
            if (!inventory.ContainsKey(index))
                inventory.Set(index, ProfileAssistant.main.firstStartInventory.Find(x => x.type == index).initialCount);
            return inventory[index];
        }
        set {
            int difference = value - this[index];
            if (difference != 0)
                BerryAnalytics.Event("Item Count Changed", difference,
                    "ItemID:" + index.ToString(),
                    "Balance:" + difference,
                    "Total:" + value);
            inventory.Set(index, value);
        }
    }

    public void Save() {
        UserUtils.WriteProfileOnDevice(this);
    }

    public XElement Serialize() {
        XElement result = new XElement("user");

        if (lastSave.HasValue)
            result.Add(new XAttribute("lastSave", lastSave.Value.Ticks.ToString()));

        result.Add(new XAttribute("id", userID));
        result.Add(new XAttribute("device", hardwareID));
        result.Add(new XAttribute("name", name));

        XElement life = new XElement("life");
        result.Add(life);
        lifeSystem.Serialize(life);

        XElement dailyspin = new XElement("dailyspin");
        result.Add(dailyspin);
        dailySpin.Serialize(dailyspin);

        XElement rewardedspin = new XElement("rewardedspin");
        result.Add(rewardedspin);
        rewardedSpin.Serialize(rewardedspin);

        XElement inventory = new XElement("inventory");
        result.Add(inventory);

        foreach (var item in this.inventory) {
            XElement itemElement = new XElement("item");
            itemElement.Add(new XAttribute("item", (int) item.Key));
            itemElement.Add(new XAttribute("count", item.Value));
            inventory.Add(itemElement);
        }

        XElement sessions = new XElement("sessions");
        result.Add(sessions);

        foreach (var session in this.sessions) {
            XElement sessionElement = new XElement("session");
            sessionElement.Add(new XAttribute("level", session.Key));
            session.Value.Serialize(sessionElement);
            sessions.Add(sessionElement);
        }

        return result;
    }

    public string SerializeJSON() {
        Dictionary<string, object> json = new Dictionary<string, object>();

        json.Set("name", name);
        json.Set("device", hardwareID);
        json.Set("life", lifeSystem.Serialize());
        json.Set("dailyspin", dailySpin.Serialize());
        json.Set("rewardedspin", rewardedSpin.Serialize());

        Dictionary<string, object> inventory = new Dictionary<string, object>();
        json.Set("inventory", inventory);
        foreach (var item in this.inventory)
            inventory.Set(((int) item.Key).ToString(), item.Value);

        Dictionary<string, object> scores = new Dictionary<string, object>();
        json.Set("score", scores);
        foreach (var session in sessions)
            scores.Set(session.Key.ToString(), session.Value.bestScore);
        
        return JSONUtility.Serialize(json);
    }

    public static CurrentUser DeserializeJSON(string jsonRaw) {
        try {
            CurrentUser result = new CurrentUser();

            Dictionary<string, object> json = (Dictionary<string, object>) JSONUtility.Deserialize(jsonRaw);

            result.name = (string) json.Get("name");
            result.hardwareID = (string) json.Get("device");
            result.lifeSystem.Deserialize((Dictionary<string, object>) json.Get("life"));
            result.dailySpin.Deserialize((Dictionary<string, object>) json.Get("dailyspin"));
            result.rewardedSpin.Deserialize((Dictionary<string, object>) json.Get("rewardedspin"));

            Dictionary<string, object> inventory = (Dictionary<string, object>) json.Get("inventory");
            foreach (var item in inventory)
                result.inventory.Set((ItemID) int.Parse(item.Key), (int) (long) item.Value);

            Dictionary<string, object> scores = (Dictionary<string, object>) json.Get("score");
            foreach (var score in scores) {
                int level = int.Parse(score.Key);
                var stats = result.sessions.Get(level);
                if (stats == null) stats = new LevelStatistic();
                stats.bestScore = (int) (long) score.Value;
                result.sessions.Set(level, stats);
            }

            result.sessionCount = result.sessions.Sum(x => x.Value.totalCount);

            return result;
        } catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

    public static CurrentUser Deserialize(XElement xml) {
        try {
            CurrentUser result = new CurrentUser();

            XAttribute attribute = xml.Attribute("lastSave");
            if (attribute != null) result.lastSave = new DateTime(long.Parse(attribute.Value));

            attribute = xml.Attribute("name");
            if (attribute != null) result.name = attribute.Value;

            attribute = xml.Attribute("id");
            if (attribute != null) result.userID = attribute.Value;

            attribute = xml.Attribute("device");
            if (attribute != null) result.hardwareID = attribute.Value;

            XElement inventory = xml.Element("inventory");
            foreach (XElement element in inventory.Elements()) {
                ItemID id = (ItemID) int.Parse(element.Attribute("item").Value);
                int count = int.Parse(element.Attribute("count").Value);
                result.inventory.Set(id, count);
            }

            XElement sessions = xml.Element("sessions");
            foreach (var element in sessions.Elements()) {
                int number = int.Parse(element.Attribute("level").Value);
                LevelStatistic session = LevelStatistic.Deserialize(element);
                result.sessions.Set(number, session);
            }

            result.sessionCount = result.sessions.Sum(x => x.Value.totalCount);
            
            result.lifeSystem = new LifeSystemTask(result);
            result.lifeSystem.Deserialize(xml.Element("life"));
            TrueTime.Schedule(result.lifeSystem);

            result.dailySpin = new DailySpinTask(result);
            result.dailySpin.Deserialize(xml.Element("dailyspin"));
            TrueTime.Schedule(result.dailySpin);

            result.rewardedSpin = new RewardedSpinTask(result);
            result.rewardedSpin.Deserialize(xml.Element("rewardedspin"));
            TrueTime.Schedule(result.rewardedSpin);

            return result;
        } catch (Exception) { }

        return new CurrentUser();
    }

    public void UpdateLevelStatistic(int levelNumber, Action<LevelStatistic> logic) {
        LevelStatistic statistic = sessions.GetAndAdd(levelNumber);
        logic(statistic);
        sessions.Set(levelNumber, statistic);
    }

    public bool IsEmpty() {
        return level <= 1 && !inventory.Contains(i =>
            ProfileAssistant.main.firstStartInventory.Find(x => x.type == i.Key).initialCount != i.Value);
    }

    public class LifeSystemTask : ScheduledTask {
        TimeSpan cooldownTime;
        DateTime? nextLifeTime;
        DateTime? unlimitedLivesEnd;
        
        CurrentUser user;
        const ItemID lifeID = ItemID.life;
        const ItemID slotID = ItemID.lifeslot;

        public LifeSystemTask(CurrentUser user) {
            uniqueID = "Life";
            this.user = user;
        }
        
        public override NextCall OnStart(DateTime time) {
            cooldownTime = new TimeSpan(0, Project.main.refilling_time, 0);
            if (user[lifeID] >= user[slotID])
                return new NextCall(Update, 1f);
            else {
                if (unlimitedLivesEnd.HasValue)
                    return new NextCall(Update, unlimitedLivesEnd.Value);
                if (!nextLifeTime.HasValue) {
                    nextLifeTime = time + cooldownTime;
                    user.Save();
                }
                UpdateNotification();
                return new NextCall(Update, nextLifeTime.Value);
            }
        }

        void Update() {
            if (user == null || user != main) {
                TrueTime.Unschedule(this);
                return;
            }

            if (unlimitedLivesEnd.HasValue) {
                if (unlimitedLivesEnd.Value <= TrueTime.Now) {
                    unlimitedLivesEnd = null;
                    SetNextCall(Update, 1f);
                } else {
                    SetNextCall(Update, unlimitedLivesEnd.Value);
                    UpdateNotification();
                }
                user.Save();
                return;
            }

            if (user[lifeID] >= user[slotID]) {
                nextLifeTime = null;
                SetNextCall(Update, 1f);
            } else {
                if (!nextLifeTime.HasValue) {
                    nextLifeTime = TrueTime.Now + cooldownTime;
                    UpdateNotification();
                } else {
                    int count = 0;
                    while (nextLifeTime < TrueTime.Now && user[lifeID] + count < user[slotID]) {
                        count++;
                        nextLifeTime += cooldownTime;
                    }
                    user[lifeID] += count;
                    if (user[lifeID] >= user[slotID]) nextLifeTime = null;
                    UpdateNotification();
                    ItemCounter.RefreshAll();
                }
                if (nextLifeTime.HasValue) {
                    SetNextCall(Update, nextLifeTime.Value);
                    user.Save();
                } else SetNextCall(Update, 1f);
            }
        }

        public override void Serialize(XElement xml) {
            if (nextLifeTime.HasValue) xml.Add(new XAttribute("time", nextLifeTime.Value.Ticks));
            if (unlimitedLivesEnd.HasValue) xml.Add(new XAttribute("unlimited", unlimitedLivesEnd.Value.Ticks));
        }

        public override Dictionary<string, object> Serialize() {
            Dictionary<string, object> json = base.Serialize();

            if (nextLifeTime.HasValue) json.Set("time", nextLifeTime.Value.Ticks);
            else json.Set("time", null);

            if (unlimitedLivesEnd.HasValue) json.Set("unlimited", unlimitedLivesEnd.Value.Ticks);
            else json.Set("unlimited", null);

            return json;
        }

        public override void Deserialize(XElement xml) {
            XAttribute attribute = xml.Attribute("time");
            if (attribute != null) nextLifeTime = new DateTime(long.Parse(attribute.Value));
            else nextLifeTime = null;

            attribute = xml.Attribute("unlimited");
            if (attribute != null) unlimitedLivesEnd = new DateTime(long.Parse(attribute.Value));
            else unlimitedLivesEnd = null;
        }

        public override void Deserialize(Dictionary<string, object> json) {
            object value = json.Get("time");
            if (value != null) nextLifeTime = new DateTime((long) (double) value);
            else nextLifeTime = null;
            value = json.Get("unlimited");
            if (value != null) unlimitedLivesEnd = new DateTime((long) (double) value);
            else unlimitedLivesEnd = null;
        }

        public string GetTimer() {
            TimeSpan span = cooldownTime;
            if (GetUnlimitedHours() > 0)
                span = unlimitedLivesEnd.Value - TrueTime.Now;
            else if (!TrueTime.IsKnown || !nextLifeTime.HasValue)
                return "...";
            else if (user[lifeID] < user[slotID] && nextLifeTime.Value > TrueTime.Now)
                span = nextLifeTime.Value - TrueTime.Now;

            return string.Format("{0:00}:{1:00}:{2:00}",
                Mathf.FloorToInt((float) span.TotalHours), span.Minutes, span.Seconds);
        }

        public float GetLifeProgression () {
            if (!TrueTime.IsKnown) return 0;
            if (nextLifeTime.HasValue && user[lifeID] < user[slotID] && nextLifeTime.Value > TrueTime.Now)
                return 1f - (float) ((nextLifeTime.Value - TrueTime.Now).TotalMinutes / cooldownTime.TotalMinutes);
            return 1f;
        }

        public bool IsFull() {
            return !nextLifeTime.HasValue;
        }

        public void SetUnlimitedLives(int hours) {
            unlimitedLivesEnd = TrueTime.Now + new TimeSpan(hours, 0, 0);
            Refuel();
        }

        public void Refuel() {
            if (user[lifeID] < user[slotID]) user[lifeID] = user[slotID];
            nextLifeTime = null;
            Update();
            user.Save();
            UpdateNotification();
        }

        public void BurnLife() {
            if (TrueTime.IsKnown && unlimitedLivesEnd.HasValue) return;
            if (user[lifeID] > 0) user[lifeID]--;
            user.Save();
            UpdateNotification();
        }

        public float GetUnlimitedHours() {
            if (TrueTime.IsKnown && unlimitedLivesEnd.HasValue)
                return (float) (unlimitedLivesEnd.Value - TrueTime.Now).TotalHours;
            return 0;
        }

        public bool HasLife() {
            if (GetUnlimitedHours() > 0)
                return true;
            return user[lifeID] > 0;
        }

        void UpdateNotification() {
            string name = "Lives";
            if (!unlimitedLivesEnd.HasValue && nextLifeTime.HasValue)
                Notifications.ScheduleNotificationUTC(name, nextLifeTime.Value + TimeSpan.FromTicks(cooldownTime.Ticks * (user[slotID] - 1 - user[lifeID])),
                    LocalizationAssistant.main["notifications/lives-title"],
                    LocalizationAssistant.main["notifications/lives-message"]);
            else
                Notifications.CancelNotification(name);
        }
    }
}

public class FriendUser : User {
    public string userID;

    public bool fake = false;

    public static FriendUser DeserializeJSON(string jsonRaw) {
        FriendUser friendUser;
        try {
            FriendUser friendUser1 = new FriendUser();
            Dictionary<string, object> strs = (Dictionary<string, object>) JSONUtility.Deserialize(jsonRaw);
            friendUser1.userID = (string) strs.Get("userID");
            friendUser1.facebookID = (string) strs.Get("fbID");
            friendUser1.name = (string) strs.Get("name");
            foreach (KeyValuePair<string, object> keyValuePair in (Dictionary<string, object>) strs.Get<string, object>("score")) {
                int num = int.Parse(keyValuePair.Key);
                LevelStatistic levelStatistic = new LevelStatistic() {
                    bestScore = (int) ((long) keyValuePair.Value)
                };
                friendUser1.sessions.Set(num, levelStatistic);
            }
            friendUser = friendUser1;
        } catch (Exception exception) {
            Debug.LogException(exception);
            friendUser = null;
        }
        return friendUser;
    }
}

public class UserUtils {
    public const string userSaveKey = "currentUser";
    public const string userCheckSumKey = "currentUser@";

    public static void WriteProfileOnDevice(CurrentUser user) {
        string xml = user.Serialize().ToString();
        PlayerPrefs.SetString(userSaveKey, xml);
        PlayerPrefs.SetString(userCheckSumKey, xml.CheckSum().ToString());
        PlayerPrefs.Save();
    }

    public static CurrentUser ReadProfileFromDevice() {
        CurrentUser profile;
        if (PlayerPrefs.HasKey(userSaveKey)) {
            string xml = PlayerPrefs.GetString(userSaveKey);
            string checkSum = PlayerPrefs.GetString(userCheckSumKey);
            if (checkSum == xml.CheckSum().ToString())
                profile = CurrentUser.Deserialize(XDocument.Parse(xml).Root);
            else
                profile = new CurrentUser();
        } else
            profile = new CurrentUser();

        return profile;
    }

    public static void RemoveProfileFromDevice() {
        PlayerPrefs.DeleteKey(userSaveKey);
        PlayerPrefs.DeleteKey(userCheckSumKey);
    }
}

[Serializable]
public class InitialItem {
    public ItemID type;
    public int initialCount;
}