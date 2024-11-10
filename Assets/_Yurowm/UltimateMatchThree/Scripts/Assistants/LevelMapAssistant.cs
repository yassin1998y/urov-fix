using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Yurowm.GameCore;

public class LevelMapAssistant : MonoBehaviourAssistant<LevelMapAssistant> {
    public float mapSize = 1200f;
    public MapLocation[] locationPrefabs;

    List<MapLocationInfo> locationsInfo = new List<MapLocationInfo>();


    #region Initialize
    void Awake () {
        InitianlizeLocationsInfo();
	}

    void InitianlizeLocationsInfo() {
        int order = 0;
        int levelCounter = 0;
        foreach (MapLocation prefab in locationPrefabs) {
            MapLocationInfo info = new MapLocationInfo();
            info.prefab = prefab;
            info.order = order++;
            info.firstLevel = levelCounter + 1;
            info.levelCount = prefab.GetLevelCount();
            levelCounter += info.levelCount;
            info.lastLevel = levelCounter;
            locationsInfo.Add(info);
        }
    }
    #endregion

    internal int GetLocationCount() {
        return locationsInfo.Count;
    }

    internal MapLocationInfo GetLocationInfo(int order) {
        return locationsInfo.Find(x => x.order == order);
    }

    internal MapLocation CreateNewLocation(int order) {
        MapLocationInfo info = GetLocationInfo(order);
        if (info == null)
            return null;
        MapLocation result = Instantiate(info.prefab).GetComponent<MapLocation>();
        result.order = order;
        result.name = info.prefab.name;
        return result;
    }

    internal MapLocation CreateNewLocationByLevelNumber(int level) {
        MapLocationInfo info = locationsInfo.Find(x => x.firstLevel <= level && x.lastLevel >= level);
        if (info == null) {
            if (level <= 0) info = locationsInfo.GetMin(x => x.firstLevel);
            else info = locationsInfo.GetMax(x => x.lastLevel);
        }
        if (info == null)
            return null;
        MapLocation result = Instantiate(info.prefab).GetComponent<MapLocation>();
        result.order = info.order;
        result.name = info.prefab.name;
        return result;
    }
}

public class MapLocationInfo {
    public MapLocation prefab;
    public int levelCount;
    public int firstLevel;
    public int lastLevel;
    public int order;
}
