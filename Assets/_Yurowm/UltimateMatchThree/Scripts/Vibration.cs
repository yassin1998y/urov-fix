using System.Threading.Tasks;
using UnityEngine;
using Yurowm.GameCore;

public class Vibration : MonoBehaviourAssistant<Vibration> {

    AndroidJavaClass unityPlayer;
    AndroidJavaObject currentActivity;
    AndroidJavaObject vibrator;    
    #if UNITY_ANDROID && !UNITY_EDITOR
    long? vibrationTime = null;
    #endif

    void Awake() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        #endif
    }

    public void Vibrate(long milliseconds) {
        return;
        if (milliseconds <= 0) return;
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!vibrationTime.HasValue || vibrationTime.Value < milliseconds)
            vibrationTime = milliseconds;
        #endif
    }

    void LateUpdate() {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrationTime.HasValue) {
            long span = vibrationTime.Value;
            vibrationTime = null;
            vibrator.Call("vibrate", span);
        }
        #endif
    }

    //public static void Vibrate() {
    //    if (vibrationTime == Time.unscaledTime) return;
    //    vibrationTime = Time.unscaledTime;
    //    if (isAndroid())
    //        vibrator.Call("vibrate");
    //    else
    //        Handheld.Vibrate();
    //}


    //public static void Vibrate(long[] pattern, int repeat) {
    //    if (vibrationTime == Time.unscaledTime) return;
    //    vibrationTime = Time.unscaledTime;
    //    if (isAndroid())
    //        vibrator.Call("vibrate", pattern, repeat);
    //    else
    //        Handheld.Vibrate();
    //}

    //public static bool HasVibrator() {
    //    return isAndroid();
    //}

    //public static void Cancel() {
    //    if (isAndroid())
    //        vibrator.Call("cancel");
    //}

    //static bool isAndroid() {
    //    #if UNITY_ANDROID && !UNITY_EDITOR
	   // return true;
    //    #else
    //    return false;
    //    #endif
    //}
}