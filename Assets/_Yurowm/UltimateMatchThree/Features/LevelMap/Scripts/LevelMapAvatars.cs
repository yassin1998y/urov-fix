using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelMapAvatars : MonoBehaviour {

    public Transform[] avatars;

    public void Set(params string[] userIDs) {
        for (int i = 0; i < userIDs.Length; i++) {
            if (i < avatars.Length) {
                UserAvatar avatar = avatars[i].GetComponentInChildren<UserAvatar>(true);
                if (!avatar) continue;
                avatar.facebookUserID = userIDs[i];
                avatars[i].gameObject.SetActive(true);
            } else break;
        }
        for (int i = userIDs.Length; i < avatars.Length; i++)
            Destroy(avatars[i].gameObject);
    }
}
