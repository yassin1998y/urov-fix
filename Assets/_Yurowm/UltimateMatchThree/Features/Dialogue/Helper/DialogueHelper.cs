using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class DialogueHelper : ILiveContent {
    List<int2> slots;

    public bool autohide = true;
    public LoopingMode loopingMode;
    public enum LoopingMode {
        Loop,
        PingPong,
        Circle
    }

    new SpriteRenderer renderer;

    public override void Initialize() {
        base.Initialize();
        renderer = GetComponent<SpriteRenderer>();
    }

    public IEnumerator Show(List<int2> slots) {
        this.slots = slots;
        StartCoroutine(Animation());
        if (autohide) StartCoroutine(Autohide());
        yield break;
    }

    IEnumerator Autohide() {
        int movesCount = SessionInfo.current.GetMovesCount();
        while (movesCount == SessionInfo.current.GetMovesCount())
            yield return 0;
        yield return StartCoroutine(Hide());
    }

    IEnumerator Animation() {
        renderer.enabled = false;
        yield return new WaitForSeconds(0.1f);
        renderer.enabled = true;
        Vector3 target;
        while (slots.Count > 0) {
            transform.position = Slot.all[slots[0]].transform.position;
            for (int i = 1; i < slots.Count; i++) {
                target = Slot.all[slots[i]].transform.position;
                while (transform.position != target) {
                    transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2);
                    yield return 0;
                }
            }
            switch (loopingMode) {
                case LoopingMode.Circle: {
                        if (slots.Count > 0) {
                            target = Slot.all[slots[0]].transform.position;
                            while (transform.position != target) {
                                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2);
                                yield return 0;
                            }
                        }
                    } break;

                case LoopingMode.Loop: break;

                case LoopingMode.PingPong: {
                        if (slots.Count > 1) {
                            for (int i = slots.Count - 2; i >= 0; i--) {
                                target = Slot.all[slots[i]].transform.position;
                                while (transform.position != target) {
                                    transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 2);
                                    yield return 0;
                                }
                            }
                        }
                    }
                    break;

            }
            yield return 0;
        }
        yield return StartCoroutine(Hide());
    }

    public IEnumerator Hide() {
        renderer.enabled = false;
        StopCoroutine(Animation());
        slots.Clear();
        yield break;
    }
}
