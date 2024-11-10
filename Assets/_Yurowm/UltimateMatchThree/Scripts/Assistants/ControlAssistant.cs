using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

using System.Linq;
using System;
using Yurowm.GameCore;

public class ControlAssistant : MonoBehaviour {

    public static ControlAssistant main;
    RaycastHit2D hit;
    public Camera controlCamera;
    
    bool isMobilePlatform;
    
    Func<bool> isBegan;
    Func<bool> isPress;
    Func<Vector2> getPoint;

    public enum ControlMode {
        Regular,
        Click,
        Autopilot
    }

    ControlMode currentMode = ControlMode.Regular;

    public void ChangeMode(ControlMode mode) {
        currentMode = mode;
    }

    public ControlMode GetMode() {
        return currentMode;
    }

    void Awake() {
        main = this;

        isMobilePlatform = Application.isMobilePlatform;

        if (isMobilePlatform) {
            isBegan = () => Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            isPress = () => Input.touchCount > 0;
            getPoint = () => controlCamera.ScreenPointToRay(Input.GetTouch(0).position).origin;
        } else {
            isBegan = () => Input.GetMouseButtonDown(0);
            isPress = () => Input.GetMouseButton(0);
            getPoint = () => controlCamera.ScreenPointToRay(Input.mousePosition).origin;
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    bool IsOverUI() {
        return EventSystem.current && (EventSystem.current.IsPointerOverGameObject(-1) || EventSystem.current.IsPointerOverGameObject(0));
    }

    bool IsControl() {
        return Time.timeScale != 0 && SessionInfo.current != null && SessionInfo.current.isPlaying;
    }

    public IEnumerator ControlRoutine(LevelRule rule) {
        while (SessionInfo.current != null) {
            yield return new WaitWithDelay(IsControl, 0.2f);
            while (IsControl()) {
                switch (currentMode) {
                    case ControlMode.Regular: rule.Control(isBegan(), isPress(), IsOverUI(), isPress() ? getPoint() : Vector2.zero); break;
                    case ControlMode.Click: ClickControl(); break;
                }
                yield return 0;
            }
        }

    }

    public Action<Slot> onClick = delegate { }; 

    void ClickControl() {
        if (isBegan()) {
            if (IsOverUI())
                return;
            Vector2 point = getPoint();
            hit = Physics2D.Raycast(point, Vector2.zero);
            if (!hit.transform)
                return;
            onClick(hit.transform.GetComponent<Slot>());
        }
    }

    public Slot GetSlotFromTouch() {
        Vector2 point;
        if (isMobilePlatform)
            if (Input.touchCount == 0)
                return null;

        point = getPoint();

        hit = Physics2D.Raycast(point, Vector2.zero);
        if (!hit.transform)
            return null;
        return hit.transform.GetComponent<Slot>();
    }
}