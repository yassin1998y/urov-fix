using UnityEngine;
using System.Collections.Generic;
using System;
using Yurowm.GameCore;
using UnityEngine.Events;
using System.Linq;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public class Project : MonoBehaviourAssistant<Project> {
    
    public static Action update = delegate { };

    void Update() {
        update.Invoke();
    }

    public float chip_acceleration = 20f;
    public float chip_max_velocity = 17f;
    public float chip_start_velosity = 4f;
    public float explosion_multiplier = 1f;
    public int refilling_time = 30;
    public int dailyreward_hour = 10;
    public float slot_offset = 0.7f;
    public float music_volume_max = 0.4f;
    public string iosAppID = "";
    #if ONLINE
    public string fbAppLink = "";
    public string permamentFacebookToken = "";
    public string invitePictureURL = "";
    #endif

    public string storeAppLink {
        get {
            switch (Application.platform) {
                case RuntimePlatform.Android: return "https://play.google.com/store/apps/details?id={0}".FormatText(Application.identifier);
                default: return "http://google.com/";
            }
        }
    }

    public string internalAppLink {
        get {
            switch (Application.platform) {
                case RuntimePlatform.Android: return "market://details?id={0}".FormatText(Application.identifier);
                case RuntimePlatform.IPhonePlayer: return "itms-apps://itunes.apple.com/app/id{0}".FormatText(iosAppID);
                default: return "http://google.com/";
            }
        }
    } 

    public static int randomSeed = 0;

    void OnDestroy() {
        ItemCounter.refresh = delegate {};
    }

    void Awake() {
        randomSeed = UnityEngine.Random.Range(9, 999);
        StartCoroutine(MD5Validation());
    }

    #region MD5 Protetion
    public string md5KeysProviderUrl = "";
    public string md5EncryptionKey = "";
    [NonSerialized]
    public bool md5Valid = true;
    public MD5ProtectionType md5ProtectionType = MD5ProtectionType.None;
    public enum MD5ProtectionType {
        None,
        DisableIAP,
        Kill
    }

    IEnumerator MD5Validation() {
        if (Application.isEditor ||
            Debug.isDebugBuild ||
            Application.platform != RuntimePlatform.Android ||
            md5ProtectionType == MD5ProtectionType.None ||
            md5KeysProviderUrl.IsNullOrEmpty())
            yield break;

        md5Valid = false;

        string currentMD5 = null;
        try {
            FileInfo apkFile = new FileInfo(Application.dataPath);
            if (apkFile.Exists)
                using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(apkFile.FullName))
                        currentMD5 = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        } catch { }

        if (currentMD5.IsNullOrEmpty())
            yield break;

        currentMD5 = CryptMD5(currentMD5);
        if (!md5EncryptionKey.IsNullOrEmpty())

        DebugPanel.Log("MD5 Crypted", "System", currentMD5);

        while (!TrueTime.IsKnown) yield return null;
        
        WWW www = new WWW(md5KeysProviderUrl);

        while (!www.isDone && www.error.IsNullOrEmpty())
            yield return null;

        string wwwResult;
        if (!www.error.IsNullOrEmpty()) {
            Debug.LogError(www.error);
            wwwResult = PlayerPrefs.GetString("MD5ValidCache");
        } else {
            wwwResult = www.text;
            PlayerPrefs.SetString("MD5ValidCache", wwwResult);
        }

        foreach (string validMD5 in Regex.Split(wwwResult, @"\s")) {
            if (validMD5.IsNullOrEmpty()) continue;
            if (validMD5 == currentMD5) {
                md5Valid = true;
                break;
            }
        }

        if (!md5Valid) 
            switch (md5ProtectionType) {
                case MD5ProtectionType.Kill: Application.Quit(); break;
                case MD5ProtectionType.DisableIAP: BerryStore.main.iaps.Clear(); break;
            }
    }

    public string CryptMD5(string md5) {
        string result = md5;

        if (!md5.IsNullOrEmpty()) {
            if (!md5EncryptionKey.IsNullOrEmpty())
                result += md5EncryptionKey;
            result = result.CheckSum().ToString();
        }

        return result;
    }
    #endregion

    #region Delegates 
    public static Event<List<Slot>> onReleaseStack = new Event<List<Slot>>();
    public static UnityEvent onStartFillingStack = new UnityEvent();
    public static Event<ISlotContent> onSlotContentDestroyed = new Event<ISlotContent>();
    public static UnityEvent onSomeContentDestroyed = new UnityEvent();
    public static Event<ISlotContent> onSlotContentPrepareToDestroy = new Event<ISlotContent>();
    public static UnityEvent onChipCrush = new UnityEvent();
    public static UnityEvent onStartWaitingNextMove = new UnityEvent();
    public static UnityEvent onLevelCreate = new UnityEvent();
    public static UnityEvent onLevelStart = new UnityEvent();
    public static UnityEvent onLevelComplete = new UnityEvent();
    public static UnityEvent onLevelEnd = new UnityEvent();
    public static UnityEvent onLevelFailed = new UnityEvent();
    public static UnityEvent onLevelClose = new UnityEvent();
    public static UnityEvent onAllTargetsIsReached = new UnityEvent();
    public static UnityEvent onScoreChanged = new UnityEvent();
    public static UnityEvent onReachedTheStar = new UnityEvent();
    public static UnityEvent onSuperMatch = new UnityEvent();
    public static UnityEvent onStackCountChanged = new UnityEvent();
    public static UnityEvent onSwapSuccess = new UnityEvent();
    public static Event<HitContext> onHitSolution = new Event<HitContext>();
    public static Event<PlayingMode> onPlayingModeChanged = new Event<PlayingMode>();

    public static void ClearDelegates() {
        Type eventType = typeof(UnityEventBase);
        typeof(Project).GetFields()
            .Where(x => x.IsStatic && eventType.IsAssignableFrom(x.FieldType))
            .ForEach(x => (x.GetValue(null) as UnityEventBase).RemoveAllListeners());
    }
    #endregion

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!Application.isPlaying && main) {
            foreach (ILiveContent lc in FindObjectsOfType<ISlotContent>()) {
                Gizmos.color = Color.cyan;
                if (lc is IBigObject) {
                    var shape = (lc as IBigObject).Shape();
                    while (shape.MoveNext())
                        Gizmos.DrawWireCube(lc.transform.position + 
                            new Vector3(main.slot_offset * shape.Current.x, main.slot_offset * shape.Current.y, 0),
                            new Vector3(main.slot_offset, main.slot_offset, 0));
                } else
                    Gizmos.DrawWireCube(lc.transform.position, new Vector3(main.slot_offset, main.slot_offset, 0));
            }
        }
    }
    #endif
}

public class Event<T> : UnityEvent<T> {}
public class Event<T1, T2> : UnityEvent<T1, T2> {}
public class Event<T1, T2, T3> : UnityEvent<T1, T2, T3> {}
public class Event<T1, T2, T3, T4> : UnityEvent<T1, T2, T3, T4> {}