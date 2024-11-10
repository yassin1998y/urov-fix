using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;
#if UNITY_IOS
using NotificationServices = UnityEngine.iOS.NotificationServices;
using LocalNotification = UnityEngine.iOS.LocalNotification;
#endif

public class Notifications : MonoBehaviour {

    void Awake() {
        #if ANDROID_NOTIFICATIONS
        if (Debug.isDebugBuild)
            DebugPanel.AddDelegate("Test Notification (10 sec.)", () => {
                ScheduleNotification("Test Notification", new TimeSpan(0, 0, 10),
                    "Test Notification", "Hello World!");
            });
        #endif
    }

    
    public enum NotificationExecuteMode {
        Inexact = 0,
        Exact = 1,
        ExactAndAllowWhileIdle = 2
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    const string fullClassName = "net.agasper.unitynotification.UnityNotificationManager";
    const string mainActivityClassName = "com.unity3d.player.UnityPlayerActivity";
    #endif

    public static void ScheduleNotificationUTC(string name, DateTime when, string title, string message) {
        if (!TrueTime.IsKnown || when < TrueTime.Now) return;
        DebugPanel.Log("N@" + name, "Notifications", "({0})\n\t{1}\n\t{2}".FormatText(when, title, message));
        Notification(name, when - TrueTime.Now, title, message);
    }

    public static void ScheduleNotification(string name, TimeSpan span, string title, string message) {
        Notification(name, span, title, message);
    }

    public static void ScheduleNotification(string name, DateTime when, string title, string message) {
        if (!TrueTime.IsKnown) return;
        ScheduleNotificationUTC(name, when - TrueTime.Zone, title, message);
    }

    public static void CancelNotification(string name) {
        DebugPanel.Log("N@" + name, "Notifications", "REMOVED");
        int id = (int) name.CheckSum();
        
        #if UNITY_ANDROID && !UNITY_EDITOR && ANDROID_NOTIFICATIONS
        AndroidJavaClass pluginClass = new AndroidJavaClass(fullClassName);
        if (pluginClass != null)
            pluginClass.CallStatic("CancelNotification", id);
        #endif

        #if UNITY_IOS
        for (int i = 0; i < NotificationServices.localNotificationCount; ++i) {
            LocalNotification notif = NotificationServices.GetLocalNotification(i);
            if (notif.userInfo["id"] == (object) id.ToString())
                NotificationServices.CancelLocalNotification(notif);
        }
        #endif
    }

    static void Notification(string name, TimeSpan span, string title, string message) {
        int id = (int) name.CheckSum();
        #if ANDROID_NOTIFICATIONS
        CancelNotification(name);

        title = title ?? Application.productName;

        DebugPanel.Log("N@" + name, "Notifications", "+({0})\n\t{1}\n\t{2}".FormatText(span, title, message));

        #if UNITY_ANDROID && !UNITY_EDITOR
        long delay = (long) span.TotalMilliseconds;
        Color32 bgColor = Color.white;
        bool sound = true;
        bool vibrate = true;
        bool lights = true;
        NotificationExecuteMode executeMode = NotificationExecuteMode.Inexact;
        AndroidJavaClass pluginClass = new AndroidJavaClass(fullClassName);
        if (pluginClass != null) {
            Debug.Log("notofication: " + delay);
            pluginClass.CallStatic("SetNotification", id, delay, title, message, message,
                sound ? 1 : 0, vibrate ? 1 : 0, lights ? 1 : 0,
                "app_icon", "app_icon",
                bgColor.r * 65536 + bgColor.g * 256 + bgColor.b, (int) executeMode, mainActivityClassName);
        }
        #endif
        #endif

        #if UNITY_IOS
        LocalNotification notification = new LocalNotification();
        notification.fireDate = DateTime.Now + span;
        notification.userInfo = new Dictionary<string, string>() { { "id", id.ToString() } };
        notification.alertBody = message;
        notification.alertAction = title;
        notification.hasAction = false;
        notification.soundName = LocalNotification.defaultSoundName;    
        NotificationServices.ScheduleLocalNotification(notification);
        #endif
    }
    
    static void AndroidRepeatingNotification(int id, double delay, long timeout, string title, string message,
        Color32 bgColor, bool sound = true, bool vibrate = true, bool lights = true, string bigIcon = "") {
        
        #if UNITY_ANDROID && !UNITY_EDITOR && ANDROID_NOTIFICATIONS
        AndroidJavaClass pluginClass = new AndroidJavaClass(fullClassName);
        if (pluginClass != null)
            pluginClass.CallStatic("SetRepeatingNotification", id, (long) delay, title, message, message, timeout,
            sound ? 1 : 0, vibrate ? 1 : 0, lights ? 1 : 0, bigIcon, "app_icon",
            bgColor.r * 65536 + bgColor.g * 256 + bgColor.b, mainActivityClassName);
        #endif
    }
}

//public class ANDROID_NOTIFICATIONS_sdsymbol : IScriptingDefineSymbol {
//    public override string GetBerryLink() {
//        return null;
//    }

//    public override string GetDescription() {
//        return "Android Notifications. Requires to install Agaspers Android Notification Plugin.";
//    }

//    public override string GetSybmol() {
//        return "ANDROID_NOTIFICATIONS";
//    }
//}
