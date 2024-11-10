using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Reference {

    static Dictionary<string, Func<int>> references = null;
    static List<string> keys = null;

    public static int Get(string key) {
        if (references == null) Initialize();

        if (references.ContainsKey(key)) return references[key]();

        return 0;
    }

    public static List<string> Keys() {
        if (references == null) Initialize();

        return keys;
    } 

    static void Initialize() {
        references = new Dictionary<string, Func<int>>();
        CurrentUser user = CurrentUser.main;

        references.Add("ExtraLifes", () => Mathf.Max(0, user[ItemID.life] - user[ItemID.lifeslot]));
        references.Add("HasLife", () => user.lifeSystem.HasLife() ? 1 : 0);
        references.Add("TimeIsKnown", () => TrueTime.IsKnown ? 1 : 0);
        references.Add("UnlimitedLifesSpan", () => Mathf.CeilToInt(user.lifeSystem.GetUnlimitedHours()));
        references.Add("Ready Rewarded Ad", () => Advertising.main.CountOfReadyAds(AdType.Rewarded));

        references = references.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        keys = references.Keys.ToList();
    }
}