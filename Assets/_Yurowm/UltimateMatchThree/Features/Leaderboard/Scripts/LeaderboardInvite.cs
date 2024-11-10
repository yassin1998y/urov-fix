using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if ONLINE
using Facebook.Unity;
#endif

[RequireComponent (typeof (Button))]
public class LeaderboardInvite : MonoBehaviour {
	void Awake () {
        GetComponent<Button>().onClick.AddListener(OnClick);
	}
	
	void OnClick () {
        StartCoroutine(Invite());		
	}

    IEnumerator Invite() {
        BerryAnalytics.Event("Invite Button Press");
        #if ONLINE
        yield return StartCoroutine(Online.main.OfflineQuestion());

        if (Online.main.IsOnline())
            FB.Mobile.AppInvite(new Uri(Project.main.fbAppLink), new Uri(Project.main.invitePictureURL), r => {
                Debug.Log(r.RawResult);
                BerryAnalytics.Event("Share Result", r.ResultDictionary.ToDictionary(x => x.Key, x => x.Value.ToString()));
            });
        #endif
        yield break;
    }

}
