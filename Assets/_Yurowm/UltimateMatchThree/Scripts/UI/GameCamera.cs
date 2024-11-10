using UnityEngine;
using System.Collections;
using Yurowm.GameCore;
using System;

// Management of the main camera
public class GameCamera : MonoBehaviour {

	public static GameCamera main;

    public new static Camera camera;
    public bool animate = false;

    Vector3 targetPosition;
    float targetSize;

    void Awake() {
        main = this;
        camera = GetComponent<Camera>();
        UIAssistant.onScreenResize += OnScreenResize;
        OnScreenResize();
    }

    public void OnScreenResize() {
        if (!FieldArea.main)
            return;

        FieldArea.main.UpdateParameters();

        float targetSize = GetTargetSize();

        Vector3 targetPosition = new Vector3(-2f * FieldArea.position.x / FieldArea.screen_size.x, -2f * FieldArea.position.y / FieldArea.screen_size.y, -10);
        targetPosition.x *= targetSize * Screen.width / Screen.height;
        targetPosition.y *= targetSize;
        area activeArea = SessionInfo.current.activeArea;
        targetPosition += new Vector3(0.5f * activeArea.size.x + activeArea.position.x,
            0.5f * activeArea.size.y + activeArea.position.y, 0) * Project.main.slot_offset;

        camera.orthographicSize = targetSize;
        transform.position = targetPosition;
    }

    float GetTargetSize() {
        area activeArea = SessionInfo.current.activeArea;
        float width = activeArea.width * Project.main.slot_offset * (FieldArea.screen_size.x / FieldArea.size.x) * 0.5f * Screen.height / Screen.width;
        float height = activeArea.height * Project.main.slot_offset * (FieldArea.screen_size.y / FieldArea.size.y) * 0.5f;

        return Mathf.Max(width, height);
    }

	// Coroutine of displaying of field
	IEnumerator ShowFieldRoutine (float speed) {
        while (animate) yield return 0;
        animate = true;
        while (FieldArea.main == null)
            yield return 0;

        FieldArea.main.UpdateParameters();

        camera.orthographicSize = targetSize;

        Vector3 position = transform.position;
        position.x = targetPosition.x;
        position.z = -10;

        while (transform.position != targetPosition) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition,
                Time.unscaledDeltaTime * speed * Project.main.slot_offset);
            yield return 0;
        }
        animate = false;
        //while (t < 1) {
        //    t += (-Mathf.Abs(0.5f - t) + 0.5f + 0.05f) * Time.unscaledDeltaTime * 6;
        //    transform.position = Vector3.Lerp(position, targetPosition, t);
        //    yield return 0;
        //}
    }

	// Coroutine of displaying of game menu
	public IEnumerator HideFieldRoutine () {
        while (animate)
            yield return 0;

        animate = true;

        float t = 0;

        Vector3 position = transform.position;
        Vector3 target = transform.position - Vector3.right * 5;

        while (t < 1) {
            t += (-Mathf.Abs(0.5f - t) + 0.5f + 0.05f) * Time.unscaledDeltaTime * 6;
            transform.position = Vector3.Lerp(position, target, t);
            yield return 0;
        }

        animate = false;

        yield break;
	}

    public void SetPosition(float y, float size) {
        area activeArea = SessionInfo.current.activeArea;
        transform.position = new Vector3(activeArea.width / 2, y, -10);
        camera.orthographicSize = size;
    }

    public void ShowField(float speed = 20) {
        area activeArea = SessionInfo.current.activeArea;
        targetSize = GetTargetSize();
        targetPosition = new Vector3(-2f * FieldArea.position.x / FieldArea.screen_size.x, -2f * FieldArea.position.y / FieldArea.screen_size.y, -10);
        targetPosition.x *= targetSize * Screen.width / Screen.height;
        targetPosition.y *= targetSize;
        targetPosition += new Vector3(0.5f * activeArea.size.x + activeArea.position.x,
            0.5f * activeArea.size.y + activeArea.position.y, 0) * Project.main.slot_offset;
        StartCoroutine(main.ShowFieldRoutine(speed));
    }
}