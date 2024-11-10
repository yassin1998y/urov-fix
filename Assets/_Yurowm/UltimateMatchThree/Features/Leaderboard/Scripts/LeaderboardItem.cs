using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardItem : MonoBehaviour {

    public List<GameObject> crowns;
    public TextMeshProUGUI placeLabel;
    public UserAvatar avatar;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI nameLabel;

	public void Set (User user, int place) {
        if (place < crowns.Count) {
            for (int i = 0; i < crowns.Count; i++) {
                if (i == place) crowns[i].SetActive(true);
                else Destroy(crowns[i]);
                Destroy(placeLabel.gameObject);
            }
        } else {
            crowns.ForEach(Destroy);
            placeLabel.gameObject.SetActive(true);
            placeLabel.text = (place + 1).ToString();
        }
        avatar.facebookUserID = user.facebookID;
        avatar.Refresh();
        nameLabel.text = user.name;
        scoreLabel.text = user.GetScore(LevelDesign.selected.number).ToString();
	}
}
