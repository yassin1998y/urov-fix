using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class DeepLevelEdges : ILiveContent {

    public Transform top;
    public Transform bottom;

    public override void Initialize() {
        base.Initialize();
        Project.onLevelStart.AddListener(() => StartCoroutine(Logic()));
        Project.onLevelEnd.AddListener(OnLevelEnd);
    }

    private void OnLevelEnd() {
        StopCoroutine(Logic());
        top.gameObject.SetActive(false);
        bottom.gameObject.SetActive(false);
    }

    private IEnumerator Logic() {
        area area;

        MoveEdge(top, SessionInfo.current.activeArea.up + 1, true);
        MoveEdge(bottom, SessionInfo.current.activeArea.down, true);

        while (true) {
            area = SessionInfo.current.activeArea;
            while (area == SessionInfo.current.activeArea)
                yield return 0;
            while (!MoveEdge(top, SessionInfo.current.activeArea.up + 1) |
                !MoveEdge(bottom, SessionInfo.current.activeArea.down))
                yield return 0;
        } 
    }

    bool MoveEdge(Transform edge, float target, bool immediate = false) {
        Vector3 t = Vector3.up * target * Project.main.slot_offset;
        if (immediate)
            edge.position = t;
        else
            edge.position = Vector3.MoveTowards(edge.position, Vector3.up * target * Project.main.slot_offset,
                Time.unscaledDeltaTime * 9 * Project.main.slot_offset);
        return t == edge.position;
    }
}
