using UnityEngine;
using UnityEngine.UI;
#if ONLINE
using Facebook.Unity;
#endif

[RequireComponent (typeof (Image))]
public class UserAvatar : MonoBehaviour {

    Image image;
    public bool currentUser = false;
    public string facebookUserID = "";
    public Sprite defaultPicture = null;

    void OnEnable() {
        Refresh();
    }

    public void Refresh() {
        if (!gameObject) return;
        if (!image) {
            image = GetComponent<Image>();
            ItemCounter.refresh += Refresh;
        }
        image.sprite = Online.main.GetOrOrderAvatar(id, Callback) ?? defaultPicture;
    }

    void OnDestroy() {
        ItemCounter.refresh -= Refresh;
    }

    string id {
        get {
#if ONLINE
            return currentUser ? (Online.main.IsOnline() ? AccessToken.CurrentAccessToken.UserId : "ME") : facebookUserID;
#else
            return currentUser ? "ME" : facebookUserID;
#endif
        }
    }

    void Callback(string id, Sprite avatar) {
        if (id == this.id) {
            Online.main.onAvatarDownloaded -= Callback;
            image.sprite = avatar;
        }
    }
}
