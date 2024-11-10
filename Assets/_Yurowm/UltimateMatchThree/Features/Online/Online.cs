using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System.Collections.Generic;
using System.Linq;
using System;
#if ONLINE
using GameSparks.Api.Responses;
using GameSparks.Api.Requests;
using GameSparks.Core;
using Facebook.Unity;
#endif

public enum FacebookMode { NA, Online, Offline, Disabled, Processing }

public class Online : MonoBehaviourAssistant<Online>, ILocalized {
    internal FacebookMode mode = FacebookMode.NA;
    public List<User> players = new List<User>();

    void Awake() {
        StartCoroutine(Connect(false));
        StartCoroutine(AvatarDownloader());
    }

    void Update() {
        if (Debug.isDebugBuild)
            DebugPanel.Log("FB Mode", "Online", mode);
    }

    public IEnumerator Connect(bool force) {
        mode = FacebookMode.Processing;
        ItemCounter.RefreshAll();

        while (LevelAssistant.main.designs.Count == 0)
            yield return 0;

        players.Clear();
        players.Add(CurrentUser.main);
        players.AddRange(GetFakeUsers());

        #if ONLINE
        while (!TrueTime.IsKnown) yield return 0;
        bool wait = false;

        #region Facebook
        if (!FB.IsInitialized) {
            wait = true;
            FB.Init(() => {
                if (FB.IsInitialized) {
                    FB.ActivateApp();
                    mode = FB.IsLoggedIn ? FacebookMode.Online : FacebookMode.Offline;
                    ItemCounter.RefreshAll();
                } else
                    OnConnectionFailed("Failed to Initialize the Facebook SDK");
                wait = false;
            });

            while (wait) yield return 0;
        } else
            FB.ActivateApp();

        if (!FB.IsInitialized) yield break;

        if (!FB.IsLoggedIn && !force) {
            OnConnectionFailed();
            yield break;
        }

        string fbAccessToken = null;
        if (!FB.IsLoggedIn) {
            wait = true;
            FB.LogInWithReadPermissions(permissions, x => {
                if (string.IsNullOrEmpty(x.Error)) {
                    AccessToken token = x.AccessToken;
                    fbAccessToken = token.TokenString;
                    Debug.LogFormat("Facebook Logged In\nUserID: {0}\nPermissions: {1}\nToken: {2}",
                        token.UserId,
                        string.Join(", ", token.Permissions.ToArray()),
                        token.TokenString);
                } else
                    OnConnectionFailed(x.Error);
                wait = false;
            });
        } else
            fbAccessToken = AccessToken.CurrentAccessToken.TokenString;

        while (wait) yield return 0;

        if (!FB.IsLoggedIn) yield break;

        string fbResult = null;
        FB.API("/me?fields=id,first_name,friends", HttpMethod.GET,
            r => fbResult = r.Error.IsNullOrEmpty() ? r.RawResult : "Error");

        while (fbResult == null) yield return 0;

        if (fbResult == "Error") {
            OnConnectionFailed("Getting FB info error");
            yield break;
        }

        Dictionary<string, object> facebookUser = JSONUtility.Deserialize(fbResult) as Dictionary<string, object>;
        #endregion

        #region GameSparks

        while (!GS.Available)
            yield return 0;

        string gsUserID = null;

        if (GS.Authenticated) GS.Reset();

        wait = true;
        new FacebookConnectRequest()
            .SetAccessToken(fbAccessToken)
            .Send((response) => {
                if (response.HasErrors)
                    OnConnectionFailed("GameSpark Connection failed:\n" + response.Errors.JSON);
                else
                    gsUserID = response.UserId;
                wait = false;
            });

        while (wait) yield return 0;

        if (gsUserID.IsNullOrEmpty()) {
            OnConnectionFailed("GameSpark Connection failed");
            yield break;
        }
        #endregion

        Result<CurrentUser> result = new Result<CurrentUser>();
        yield return StartCoroutine(LoadUser(result));

        CurrentUser onlineUser = result.result;

        result = new Result<CurrentUser>();
        yield return StartCoroutine(GetActualUser(CurrentUser.main, onlineUser, result));

        if (!result.IsSuccess) {
            OnConnectionFailed(result.error);
            yield break;
        }

        if (result.result != CurrentUser.main) {
            foreach (int level in result.result.sessions.Keys.ToArray()) {
                User.LevelStatistic statistic = CurrentUser.main.sessions.Get(level);
                if (statistic != null) {
                    statistic.bestScore = result.result.sessions[level].bestScore;
                    result.result.sessions[level] = statistic;
                }
            }
            CurrentUser.main = result.result;
            CurrentUser.main.sessionCount = CurrentUser.main.sessions.Sum(x => x.Value.totalCount);
        } 
        CurrentUser.main.userID = gsUserID;
        CurrentUser.main.facebookID = AccessToken.CurrentAccessToken.UserId;
        CurrentUser.main.hardwareID = SystemInfo.deviceUniqueIdentifier;
        CurrentUser.main.name = (string) facebookUser.Get("first_name");

        result = new Result<CurrentUser>();
        CurrentUser.main.Save();
        ItemCounter.RefreshAll();
        yield return StartCoroutine(SaveUser(result));

        Debug.Log("User Loaded: " + result.IsSuccess);

        mode = FacebookMode.Online;
        #else
        mode = FacebookMode.Offline;
        #endif

        StartCoroutine(LoadFriends());
        ItemCounter.RefreshAll();

        yield break;
    }

    IEnumerator LoadFriends() {
        players.Clear();
        #if ONLINE
        Result<List<FriendUser>> result = new Result<List<FriendUser>>();
        yield return StartCoroutine(GetFriends(result));
        players = result.result.Cast<User>().ToList();
        #endif

        players.Insert(0, CurrentUser.main);
        players.AddRange(GetFakeUsers());

        ItemCounter.RefreshAll();
        yield break;
    }
    #if ONLINE
    IEnumerator GetFriends(Result<List<FriendUser>> result) {
        bool wait = true;
        new LogEventRequest().SetEventKey("GetFriends")
         .Send((response) => {
             if (response.HasErrors) {
                 Debug.LogError(response.Errors.JSON.ToString());
                 result.error = response.Errors.JSON.ToString();
             } else
                 result.result = response.ScriptData.GetGSDataList("friends")
                    .Select(x => FriendUser.DeserializeJSON(x.JSON)).ToList();
             wait = false;
         });

        while (wait) yield return 0;
    }
    #endif

    IEnumerator GetActualUser(CurrentUser offlineUser, CurrentUser onlineUser, Result<CurrentUser> result) {
        if (onlineUser == null)
            result.result = offlineUser;
        else if (offlineUser.userID.IsNullOrEmpty()) {
            if (!onlineUser.IsEmpty() && !offlineUser.IsEmpty())
                yield return StartCoroutine(Conflict(onlineUser, offlineUser, result));
            else if (!onlineUser.IsEmpty())
                result.result = onlineUser;
            else
                result.result = offlineUser;
        } else if (!offlineUser.CompareTo(onlineUser))
            yield return StartCoroutine(Conflict(onlineUser, offlineUser, result));
         else
            result.result = offlineUser.lastSave > onlineUser.lastSave ? offlineUser : onlineUser;
    }

    IEnumerator Conflict(CurrentUser cloudUser, CurrentUser localUser, Result<CurrentUser> result) {
        string userFormat = "{0} ({1})\n{2}\n<sprite name=\"IL_Gem\">{3} <sprite name=\"IL_Spin\">{4} <sprite name=\"IL_Life\">{5}";
        string description = LocalizationAssistant.main["conflict/description"].FormatText(
            userFormat.FormatText(LocalizationAssistant.main["conflict/online"].ToUpper(),
                cloudUser.lastSave.HasValue ? (cloudUser.lastSave.Value + TrueTime.Zone).ToString("dd.MM.yyyy HH:mm") : "-",
                LocalizationAssistant.main["level"].Replace("{level}", cloudUser.level.ToString()),
                cloudUser[ItemID.coin], cloudUser[ItemID.spin], cloudUser[ItemID.life]),
            userFormat.FormatText(LocalizationAssistant.main["conflict/offline"].ToUpper(),
                localUser.lastSave.HasValue ? (localUser.lastSave.Value + TrueTime.Zone).ToString("dd.MM.yyyy HH:mm") : "-",
                LocalizationAssistant.main["level"].Replace("{level}", localUser.level.ToString()),
                localUser[ItemID.coin], localUser[ItemID.spin], localUser[ItemID.life])
            );

        Question.main.Ask(
            LocalizationAssistant.main["conflict/title"],
            description,
            LocalizationAssistant.main["conflict/online"],
            LocalizationAssistant.main["conflict/offline"],
            LocalizationAssistant.main["cancel"]
            );

        while (Question.main.Wait())
            yield return 0;

        switch (Question.main.GetResult()) {
            case 0: result.result = cloudUser; break; // First
            case 1: result.result = localUser; break; // Second
            case 2: result.error = "Canceled"; break; // Cancel
        }
    }

    public void Save() {
        #if ONLINE
        StartCoroutine(SaveUser(new Result(), r => {
            if (r.IsSuccess) {
                CurrentUser.main.lastSave = TrueTime.Now;
                CurrentUser.main.Save();
            }
        }));
        #else
        CurrentUser.main.lastSave = TrueTime.Now;
        CurrentUser.main.Save();
        #endif
    }

    public IEnumerator OfflineQuestion() {
        if (!IsOnline()) {
            Question.main.Ask(
                LocalizationAssistant.main["offline/title"],
                LocalizationAssistant.main["offline/description"],
                LocalizationAssistant.main["offline/signin"],
                LocalizationAssistant.main["offline/cancel"]
                );

            while (Question.main.Wait())
                yield return 0;

            switch (Question.main.GetResult()) {
                case 0: // Sign in
                    yield return StartCoroutine(Connect(true));
                    break;
                case 1: // Cancel
                    yield break;
            }
        }
    }

    public FakeUser me;
    public List<FakeUser> fakeUsers;
    List<User> GetFakeUsers() {
        List<User> users = new List<User>();
        IntRange intRanges = new IntRange(Math.Min(4, LevelAssistant.main.designs.Count + 1), LevelAssistant.main.designs.Count + 1);
        Dictionary<int, LevelDesign> dictionary = LevelAssistant.main.designs.ToDictionary(x => x.number, x => x);
        for (int i = 0; i < fakeUsers.Count; i++) {
            URandom uRandom = new URandom(2703 + i);
            FakeUser fake = fakeUsers[i];
            FriendUser friendUser = new FriendUser() {
                fake = true,
                facebookID = i.ToString(),
                name = fake.name
            };
            int random = intRanges.GetRandom(uRandom, "Level");
            for (int j = 1; j < random; j++) {
                User.LevelStatistic levelStatistic = new User.LevelStatistic();
                IntRange intRanges1 = new IntRange(dictionary[j].firstStarScore, dictionary[j].thirdStarScore);
                IntRange intRanges2 = intRanges1;
                intRanges2.max = intRanges2.max + (intRanges1.max - intRanges1.min) / 2;
                levelStatistic.bestScore = uRandom.Range(intRanges1, j.ToString());
                friendUser.sessions.Add(j, levelStatistic);
            }
            users.Add(friendUser);
        }
        return users;
    }

    #if ONLINE
    IEnumerator SaveUser(Result result = null, Action<Result> onComplete = null) {
        bool wait = true;
        new LogEventRequest().SetEventKey("SaveUser")
         .SetEventAttribute("Data", CurrentUser.main.SerializeJSON())
         .Send((response) => {
             if (response.HasErrors)
                 Debug.LogError(response.Errors.JSON.ToString());
             if (result != null && response.HasErrors)
                 result.error = response.Errors.JSON.ToString();
             wait = false;
         });

        while (wait) yield return 0;

        if (onComplete != null) onComplete(result);
    }

    IEnumerator LoadUser(Result<CurrentUser> result, Action<Result<CurrentUser>> onComplete = null) {
        FacebookMode currentMode = mode;
        if (currentMode != FacebookMode.Processing) {
            mode = FacebookMode.Processing;
            ItemCounter.RefreshAll();
        }

        bool wait = true;
        new LogEventRequest().SetEventKey("LoadUser")
         .Send((response) => {
             if (response.HasErrors) {
                 Debug.LogError(response.Errors.JSON.ToString());
                 result.error = response.Errors.JSON.ToString();
             } else {
                 GSData profile = response.ScriptData.GetGSData("profile");
                 CurrentUser user = profile != null ? CurrentUser.DeserializeJSON(profile.JSON) : new CurrentUser();
                 user.lastSave = ServerTimeParse(response.ScriptData.GetLong("lastSave").Value);
                 user.userID = response.ScriptData.GetString("userID");
                 result.result = user;
             }
             wait = false;
         });

        while (wait) yield return 0;
        if (currentMode != FacebookMode.Processing) {
            mode = currentMode;
            ItemCounter.RefreshAll();
        }

        if (onComplete != null) onComplete(result);
    }

    void OnConnectionFailed(string error = null) {
        Logout();
        if (mode != FacebookMode.Offline) {
            mode = FacebookMode.Offline;
            ItemCounter.RefreshAll();
        }

        if (error != null) {
            DebugPanel.Log("Status", "Online", "Failed");
            DebugPanel.Log("Error", "Online", error);
            Debug.LogError(error);
        }
    }
    public void Login() {
        #if UNITY_EDITOR
        if (!string.IsNullOrEmpty(Project.main.permamentFacebookToken))
            UnityEditor.EditorGUIUtility.systemCopyBuffer = Project.main.permamentFacebookToken;
        #endif
        
        StartCoroutine(Connect(true));
    }

    public void Logout() {
        FB.LogOut();
        GS.Reset();
        if (IsOnline()) {
            CurrentUser.main = new CurrentUser();
            mode = FacebookMode.Offline;
            ItemCounter.RefreshAll();
        }
    }

    List<string> permissions = new List<string>() { "public_profile", "email", "user_friends"
        #if !UNITY_EDITOR
        //, "publish_actions"
        #endif    
    };
    #endif

    public bool IsOnline() {
        return mode == FacebookMode.Online;
    }

    public static DateTime ServerTimeParse(long value) {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(value);
    }

    #region Avatars
    Dictionary<string, Sprite> avatars = new Dictionary<string, Sprite>();
    List<string> avatarOrders = new List<string>();
    public Action<string, Sprite> onAvatarDownloaded = delegate { };

    public void OrderAvatar(string userID) {
        if (userID.IsNullOrEmpty()) return;
        if (!avatars.ContainsKey(userID) && !avatarOrders.Contains(userID))
            avatarOrders.Add(userID);
    }

    public Sprite GetOrOrderAvatar(string userID, Action<string, Sprite> callback) {
        if (userID.IsNullOrEmpty()) return null;
        if (avatars.ContainsKey(userID))
            return avatars[userID];
        else {
            OrderAvatar(userID);
            onAvatarDownloaded += callback;
            return null;
        }
    }

    IEnumerator AvatarDownloader() {
        Texture2D result;

        avatars.Set("ME", me.avatar);
        for (int i = 0; i < fakeUsers.Count; i++)
            avatars.Set(i.ToString(), fakeUsers[i].avatar);

        while (true) {
            while (avatarOrders.Count == 0) yield return 0;

            string fbID = avatarOrders[0];
            if (avatars.ContainsKey(fbID)) {
                avatarOrders.Remove(fbID);
                onAvatarDownloaded(fbID, avatars[fbID]);
                continue;
            }

            result = new Texture2D(128, 128, TextureFormat.RGB24, true);

            WWW data = new WWW("https://graph.facebook.com/{0}/picture?type=large".FormatText((object) fbID));
            yield return data;

            if (data.error.IsNullOrEmpty()) {
                data.LoadImageIntoTexture(result);

                Sprite sprite = Sprite.Create(result, new Rect(0, 0, result.width, result.height), Vector2.one * 0.5f);

                avatars.Set(fbID, sprite);
                avatarOrders.RemoveAt(0);
                onAvatarDownloaded(fbID, sprite);
            } else {
                Debug.LogError(data.error);
                avatarOrders.Add(fbID);
                avatarOrders.RemoveAt(0);
            }
        }
    }
    #endregion

    public IEnumerator RequriedLocalizationKeys() {
        yield return "conflict/description";
        yield return "conflict/online";
        yield return "conflict/offline";
        yield return "conflict/title";

        yield return "offline/title";
        yield return "offline/description";
        yield return "offline/signin";
        yield return "offline/cancel";
    }

    [Serializable]
    public class FakeUser {
        public string name;
        public Sprite avatar;
    }

    class Result<T> : Result where T : class {
        public T result = null;
    }

    class Result {
        public string error = null;
        public bool IsSuccess {
            get {
                return error.IsNullOrEmpty();
            }
        }
    }
}

public class ONLINE_sdsymbol : IScriptingDefineSymbol {
    public override string GetBerryLink() {
        return "online";
    }

    public override string GetDescription() {
        return "The implementation code for such features as Facebook connection, leaderboards, score sharing, friends invite, cloud storing of user data. This code requires to install Facebook SDK, GameSparks SDK and PlayServicesResolver. Read more in the manual.";
    }

    public override string GetSybmol() {
        return "ONLINE";
    }
}