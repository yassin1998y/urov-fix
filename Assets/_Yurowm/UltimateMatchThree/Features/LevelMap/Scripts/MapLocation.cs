using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Yurowm.GameCore;
using System.Collections.Generic;
using System.Linq;
using System;

public class MapLocation : MonoBehaviour {
    public Transform nextLocationConnector;
    public Transform previousLocationConnector;
    public bool buttons = true;
    internal int order = 0;
    MapLocation nextLocation = null;
    MapLocation previousLocation = null;

    public List<Transform> waypoints = new List<Transform>();

    internal LevelMapDisplayer displayer;
    
    internal System.Action<int> onDestroy = delegate { };
    
    void Start() {
        OnPositionChanged();
        BroadcastMessage("SetCamera", displayer.mapCamera, SendMessageOptions.DontRequireReceiver);
    }

    public void CreateButtons() {
        if (!buttons) return;
        Transform locators_folder = transform.Find("Buttons");
        if (!locators_folder) {
            Debug.LogError("I can't find Buttons folder");
            return;
        }

        Transform connector;
        LevelButton level_button;
        int level;
        MapLocationInfo locationInfo = LevelMapAssistant.main.GetLocationInfo(order);

        waypoints.Clear();

        int firstLevel = locationInfo.firstLevel;
        int lastCount = locationInfo.lastLevel;
        for (int l = 0; l < lastCount; l++) {
            level = firstLevel + l;
            if (locators_folder.childCount <= l) return;
            connector = locators_folder.GetChild(l);

            if (connector) {
                connector.DestroyChilds();
                if (LevelAssistant.main.GetDesign(level) != null) {
                    level_button = Content.GetItem<LevelButton>(LevelButton.IsLocked(level) ? "LevelButtonLocked" :  LevelButton.IsItCurrentLevel(level) ? "LevelButtonCurrent" : "LevelButton");
                    level_button.transform.parent = connector;
                    level_button.transform.localPosition = Vector3.zero;
                    level_button.level = level;
                    level_button.Initialize();
                    waypoints.Add(connector);
                    List<User> list = Online.main.players.Where(p => p.level == level)
                        .Cast<User>().ToList();
                    if (list.Count > 0) {
                        LevelMapAvatars levelMapAvatar = Content.GetItem<LevelMapAvatars>();
                        levelMapAvatar.transform.SetParent(level_button.transform);
                        levelMapAvatar.transform.Reset();
                        levelMapAvatar.Set(list.Select(x => x.facebookID).ToArray());
                    }
                } else if (LevelAssistant.main.GetDesign(level - 1) != null) {
                    Transform soon = Content.GetItem<Transform>("LevelButtonWIP");
                    if (soon) {
                        soon.parent = connector;
                        soon.localPosition = Vector3.zero;
                        waypoints.Add(connector);
                    }
                }
            }
        }

        displayer.UpdateLine();
    }

    public void OnPositionChanged() {
        int p = displayer.IsVisible(previousLocationConnector);
        int n = displayer.IsVisible(nextLocationConnector);
        if (n != 0 && p != 0 && p == n) {
            Destroy(gameObject);
            return;
        }
        if (nextLocation == null) {
            if (n == 0) {
                nextLocation = displayer.ShowNextLocation(this);
            }
        }
        if (previousLocation == null) {
            if (p == 0) {
                previousLocation = displayer.ShowPreviuosLocation(this);
            }
        }
    }

    public int GetLevelCount() {
        return transform.Find("Buttons").childCount;    
    }

    void OnDestroy() {
        onDestroy.Invoke(order);
    }

    public Transform GetLevelButton(int targetLevel) {
        MapLocationInfo locationInfo = LevelMapAssistant.main.GetLocationInfo(order);

        int firstLevel = locationInfo.firstLevel;
        int count = locationInfo.lastLevel;
        if (targetLevel < firstLevel) return null;
        if (targetLevel >= firstLevel + count) return null;

        Transform locators_folder = transform.Find("Buttons");
        return locators_folder.GetChild(targetLevel - firstLevel);
    }
}