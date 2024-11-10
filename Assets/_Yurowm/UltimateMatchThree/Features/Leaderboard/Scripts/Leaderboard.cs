using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class Leaderboard : MonoBehaviour {

	void OnEnable () {
        StopAllCoroutines();
        StartCoroutine(Loading());		
	}

    IEnumerator Loading() {
        transform.localPosition = Vector3.right * 500;
        transform.DestroyChilds();
        int level = LevelDesign.selected.number;
        List<User> players = Online.main.players.Where(x => x.GetScore(level) > 0).ToList();

        if (players.Count == 0) yield break;

        players.Sort((x, y) => y.GetScore(level).CompareTo(x.GetScore(level)));

        LeaderboardInvite invite = Content.GetItem<LeaderboardInvite>();
        invite.transform.SetParent(transform);
        invite.transform.Reset();

        for (int place = 0; place < players.Count; place ++) {
            LeaderboardItem item = Content.GetItem<LeaderboardItem>();
            item.transform.SetParent(transform);
            item.transform.Reset();
            item.Set(players[place], place);
            yield return new WaitForSeconds(0.1f);
        }

    }
}
