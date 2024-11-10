using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

using System.Linq;
using System;
using Yurowm.GameCore;

public class LevelMapDisplayer : UIBehaviour, IDragHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler {

    public float spawnOffset = 0.1f;
    public float friction = 10;

    public bool control = true;
    public bool buttons = true;
    
    internal Camera mapCamera;
    Transform content;

    Dictionary<int, MapLocation> locationsList = new Dictionary<int, MapLocation>();

    //public float moveSpeed = 200f;
    //void Update() {
    //    Move(Vector2.down * moveSpeed * Time.deltaTime);
    //}

    protected override void Awake() {
        if (line)
            line.thickness = 0.25f;

        if (content == null) {
            content = new GameObject("Map_" + name).transform;
            content.gameObject.layer = LayerMask.NameToLayer("Map");
        }
        if (mapCamera == null) {
            mapCamera = new GameObject("MapCamera_" + name).AddComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.clearFlags = CameraClearFlags.Depth;
            mapCamera.backgroundColor = Color.black;
            mapCamera.cullingMask = 1 << LayerMask.NameToLayer("Map");
            mapCamera.transform.position = new Vector3(0, 0, -10);
            mapCamera.nearClipPlane = 0;
            mapCamera.farClipPlane = 20;
            mapCamera.useOcclusionCulling = false;
            mapCamera.allowHDR = false;
            mapCamera.allowMSAA = false;

            /////
            //mapCamera.rect = new Rect(.3f, 0, 0.4f, 1f);
            //mapCamera.rect = new Rect(.3f, 0, 0.4f, 1f);
            /////
        }

        UIAssistant.onScreenResize += UpdateMapParameters;
        UpdateMapParameters();
        mapCamera.orthographicSize = camSizeMax;
        spawnOffset *= Mathf.Max(Screen.width, Screen.height);

        UIAssistant.onShowPage += page => { if (page.name == "LevelList") UpdateMap();};
    }

    public float camSizeMin = 0;
    public float camSizeMax = 0;

    

    void UpdateMapParameters() {
        camSizeMax = 0.5f * (LevelMapAssistant.main.mapSize * Screen.height) / (Screen.width * 100f);
        camSizeMin = Mathf.Min(3.5f, camSizeMax);
        mapCamera.orthographicSize = camSizeMax;
        //camSizeMax = 8.79f;
        //camSizeMin = camSizeMax;
        //mapCamera.orthographicSize = camSizeMax;
    }

    void UpdateMap() {
        locationsList.Values.ForEach(l => l.CreateButtons());
    }

    #region Navigation
    Vector2 inertion = new Vector2();

    bool drag = false;
    public void OnBeginDrag(PointerEventData eventData) {
        if (!control) return;
        inertion = Vector2.zero;
        drag = true;
    }

    public void OnDrag(PointerEventData eventData) {
        if (!control) return;
        
        inertion = eventData.delta;

        Move(inertion);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!control) return;
        drag = false;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (!control) return;
        if (drag) return;
        RaycastHit2D hit = Physics2D.Raycast(mapCamera.ScreenPointToRay(eventData.position).origin, Vector2.zero);
        if (!hit.transform) return;
        IMapButton button = hit.transform.GetComponent<IMapButton>();
        button.OnClick();
    }

    void LateUpdate() {
        if (!control) return;
        if (drag) return;
        Move(inertion);
        float speed = inertion.magnitude;
        speed = Mathf.MoveTowards(speed, 0, Time.unscaledDeltaTime * friction);
        if (speed == 0) inertion = Vector2.zero;
        else inertion = inertion.normalized * speed;
    }
    
    void Move(Vector2 delta, bool absolute = false) {
        if (absolute) delta = WorldToScreenDelta(delta);
        if (delta == Vector2.zero) return;

        if (locationsList.ContainsKey(0)) {
            float y = mapCamera.WorldToScreenPoint(locationsList[0].previousLocationConnector.transform.position).y;
            if (y + delta.y >= 0) {
                inertion = Vector2.zero;
                delta.y = -y;
            }
        }
        if (locationsList.ContainsKey(LevelMapAssistant.main.GetLocationCount() - 1)) {
            float y = mapCamera.WorldToScreenPoint(locationsList[LevelMapAssistant.main.GetLocationCount() - 1].nextLocationConnector.transform.position).y;
            if (y + delta.y <= Screen.height) {
                inertion = Vector2.zero;
                delta.y = Screen.height - y;
            }
        }
        Vector3 position = mapCamera.transform.position;
        float crop = (camSizeMax - mapCamera.orthographicSize) * LevelMapAssistant.main.mapSize / (100 * 2 * camSizeMax);

        position -= ScreenToWorldDelta(delta);
        position.x = Mathf.Clamp(position.x, -crop, crop);
        mapCamera.transform.position = position;

        foreach (MapLocation location in new List<MapLocation>(locationsList.Values))
            location.OnPositionChanged();
    }
    #endregion

    Vector2 WorldToScreenDelta(Vector2 delta) {
        return mapCamera.WorldToScreenPoint(delta) - mapCamera.WorldToScreenPoint(Vector2.zero);
    }

    Vector3 ScreenToWorldDelta(Vector2 delta) {
        return mapCamera.ScreenToWorldPoint(delta) - mapCamera.ScreenToWorldPoint(Vector2.zero);
    }

    #region Line
    public Line line;
    List<Transform> waypoints = new List<Transform>();

    internal void UpdateLine() {
        if (!line) return;
        
        Vector2[] points = waypoints.Select(x => new Vector2(x.position.x, x.position.y)).ToArray();
        line.Clear();
        foreach (Vector2 point in points) line.AddPoint(point);

        line.Refresh();
    }
    #endregion

    #region Location Creation
    public int IsVisible(Transform o) {
        float y = mapCamera.WorldToScreenPoint(o.position).y;
        if (y < -spawnOffset)
            return 1;
        if (y >= Screen.height + spawnOffset)
            return -1;
        return 0;
    }

    public MapLocation ShowNextLocation(MapLocation mapLocation) {
        int newOrder = mapLocation.order + 1;

        if (newOrder < 0 || newOrder >= LevelMapAssistant.main.GetLocationCount())
            return null;

        MapLocation location;
        if (locationsList.ContainsKey(newOrder))
            location = locationsList[newOrder];
        else {
            location = LevelMapAssistant.main.CreateNewLocation(newOrder); 
            Transform t = location.transform;

            t.parent = content;
            t.position = mapLocation.nextLocationConnector.position;
            t.position -= location.previousLocationConnector.position - t.position;
            t.localScale = Vector3.one;
            location.onDestroy += OnDestroyLocation;;
            location.displayer = this;
            location.buttons = buttons;
            locationsList.Add(newOrder, location);

            location.CreateButtons();
            UpdateWaypoints();
            UpdateLine();
        }

        return location;
    }

    public MapLocation ShowPreviuosLocation(MapLocation mapLocation) {
        int newOrder = mapLocation.order - 1;

        if (newOrder < 0 || newOrder >= LevelMapAssistant.main.GetLocationCount())
            return null;

        MapLocation location;
        if (locationsList.ContainsKey(newOrder))
            location = locationsList[newOrder];
        else {
            location = LevelMapAssistant.main.CreateNewLocation(newOrder);
            Transform t = location.transform;

            t.parent = content;
            t.localScale = Vector3.one;
            t.position = mapLocation.previousLocationConnector.position;
            t.position -= location.nextLocationConnector.position - t.position;
            location.onDestroy += OnDestroyLocation;
            location.displayer = this;
            location.buttons = buttons;
            locationsList.Add(newOrder, location);

            location.CreateButtons();
            UpdateWaypoints();
            UpdateLine();
        }

        return location;
    }

    void UpdateWaypoints() {
        locationsList = locationsList.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        waypoints = locationsList.Values.SelectMany(x => x.waypoints).ToList();
    }

    void OnDestroyLocation(int order) {
        locationsList.Remove(order);

        locationsList.OrderBy(x => x.Key);
        waypoints = locationsList.Values.SelectMany(x => x.waypoints).ToList();

        UpdateLine();
    }
    #endregion

    protected override void OnEnable() {
        ItemCounter.refresh += UpdateMap;

        content.gameObject.SetActive(true);
        mapCamera.gameObject.SetActive(true);
        UpdateMapParameters();

        if (content.childCount == 0) {
            int target_level = 1;
            if (LevelDesign.selected != null)
                target_level = LevelDesign.selected.number;
            else
                target_level = CurrentUser.main.level;

            target_level = Mathf.Min(target_level, LevelAssistant.main.designs.Max(d => d.number));

            MapLocation location = LevelMapAssistant.main.CreateNewLocationByLevelNumber(target_level);
            location.onDestroy += OnDestroyLocation;
            location.displayer = this;
            location.buttons = buttons;
            locationsList.Add(location.order, location);


            Transform t = location.transform;
            t.parent = content;
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one;

            Vector3 position = mapCamera.transform.position;
            position.y = (location.nextLocationConnector.position.y + location.previousLocationConnector.position.y) / 2;
            mapCamera.transform.position = position;

            location.CreateButtons();
            Move(Vector3.down * (location.GetLevelButton(target_level).transform.position.y - position.y), true);

            UpdateWaypoints();
            UpdateLine();
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        ItemCounter.refresh -= UpdateMap;
    }

    protected override void OnDisable() {
        ItemCounter.refresh -= UpdateMap;
        foreach (MapLocation loc in new List<MapLocation>(locationsList.Values))
            if (loc)
                DestroyImmediate(loc.gameObject);
        locationsList.Clear();
        if (content)
            content.gameObject.SetActive(false);
        if (mapCamera)
            mapCamera.gameObject.SetActive(false);
    }

    public void Refresh() {
        if (gameObject.activeInHierarchy) {
            enabled = false;
            enabled = true;
        }
    }

    public static void RefreshAll() {
        foreach (LevelMapDisplayer displayer in FindObjectsOfType<LevelMapDisplayer>())
            displayer.Refresh();
    }
}
