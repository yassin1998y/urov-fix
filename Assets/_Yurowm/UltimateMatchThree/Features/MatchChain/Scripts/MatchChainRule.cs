using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using System.Linq;
using System;
using System.Xml.Linq;

public class MatchChainRule : LevelRule {
    
    public override void Initialize() {
        base.Initialize();
        line_renderer = line.GetComponent<MeshRenderer>();

        Project.onStackCountChanged.AddListener(OnStackSizeChanged);
        Project.onAllTargetsIsReached.AddListener(OnAllTargetsIsReached);
    }

    void OnAllTargetsIsReached() {
        longLineTrick = 0;
    }

    RaycastHit2D hit;
    internal override void Control(bool isBegan, bool isPress, bool IsOverUI, Vector2 point) {
        if (isBegan) {
            if (IsOverUI) return;
            hit = Physics2D.Raycast(point, Vector2.zero);
            if (!hit.transform) return;
            StartFillingStack();
        }
        if (IsFillingStack()) {
            if (isPress) {
                if (IsOverUI)
                    return;
                if (slot_stack.Count > 0) {
                    Vector2 delta = point - slot_stack.Last().transform.position.To2D();
                    if (delta.magnitude > Project.main.slot_offset)
                        point = slot_stack.Last().transform.position.To2D() + delta.normalized * Project.main.slot_offset;
                }

                hit = Physics2D.Raycast(point, Vector2.zero);
                if (!hit.transform) return;
                AddSlotToStack(hit.transform.GetComponent<Slot>());
            }
            if (!isPress) ReleaseStack();
        }
    }

    int longLineTrick = 0;

    internal override IEnumerator SessionModes(PlayingMode playingMode) {
        switch (playingMode) {
            case PlayingMode.Wait: {
                    if (longLineTrick > 0) {
                        if (longLineTrick > 3) longLineTrick = 3; 
                        Feedback.Play("LongLine" + longLineTrick);
                    }
                } break;
        }
        yield break;
    }

    internal override bool IsThereAnyMoves() {
        foreach (Slot slot in Slot.allActive.Values)
            if (slot.color.IsMatchableColor())
                foreach (Side side in Utils.straightSides)
                    if (slot[side] && slot.color.IsMatchWith(slot[side].color))
                        return true;
        return false;
    }

    internal override bool IsThereAnySolutions() {
        return false;
    }

    bool CanIWait() {
        return GetMode() == PlayingMode.Wait && SessionInfo.current != null && SessionInfo.current.isPlaying && IChip.busyList.Count == 0;
    }

    public override void Serialize(XElement xml) {
        xml.Add(new XAttribute("longLineTrick", longLineTrick));
    }

    public override void Deserialize(XElement xml) {
        longLineTrick = int.Parse(xml.Attribute("longLineTrick").Value);
    }

    #region Slot Stack
    public Line line;

    int maxStackSize = 7;
    bool filling = false;
    ItemColor stackColor;
    public List<Slot> slot_stack = new List<Slot>();

    void AddSlotToStack(Slot slot) {
        if (!filling)
            return;
        if (!slot.color.IsMatchableColor())
            return;

        if (!Slot.IsInteractive(slot))
            return;

        if (slot_stack.Count == 0) {
            slot_stack.Add(slot);
            stackColor = slot.color;
            slot.OnPress();
            ColoredBackground.SetTargetColor(RealColors.Get(stackColor), 1f / maxStackSize);
            Project.onStackCountChanged.Invoke();
            UpdateStackLine();
            return;
        }
        if ((stackColor == slot.color || slot.color.IsUniversalColor() || stackColor.IsUniversalColor()) && !slot_stack.Contains(slot)) {
            Slot lastSlot = slot_stack.Last();
            foreach (Side side in Utils.straightSides)
                if (slot[side] && slot[side] == lastSlot) {
                    slot_stack.Add(slot);
                    if (stackColor == ItemColor.Unknown || stackColor == ItemColor.Universal)
                        if (stackColor != slot.color) {
                            stackColor = slot.color;
                            foreach (Slot _slot in Slot.allActive.Values)
                                if (_slot.isPressed && !slot_stack.Contains(_slot))
                                    _slot.OnUnpress();
                        }
                    slot.OnPress();
                    Project.onStackCountChanged.Invoke();
                    ColoredBackground.SetTargetColor(RealColors.Get(stackColor), 1f * slot_stack.Count / maxStackSize);
                    break;
                }
        }

        if (slot_stack.Count >= 2 && slot_stack[slot_stack.Count - 2] == slot) {
            slot_stack.RemoveAt(slot_stack.Count - 1);

            Slot s = slot_stack.Find(x => !x.color.IsUniversalColor());

            if (s)
                stackColor = s.color;
            else
                stackColor = ItemColor.Universal;

            Project.onStackCountChanged.Invoke();
            ColoredBackground.SetTargetColor(RealColors.Get(stackColor), 1f * slot_stack.Count / maxStackSize);
        }

        line_renderer.material.SetColor("_TintColor", RealColors.Get(stackColor));
        UpdateStackLine();
    }

    void StartFillingStack() {
        if (!CanIWait())
            return;
        slot_stack.Clear();
        filling = true;
        stackColor = ItemColor.Unknown;
        line.Clear();
        Project.onStartFillingStack.Invoke();
    }

    void OnStackSizeChanged() {
        if (slot_stack.Count >= maxStackSize) {
            foreach (Slot slot in Slot.allActive.Values)
                if (!slot_stack.Contains(slot) && (slot.color == stackColor || (stackColor.IsUniversalColor() && slot.color.IsColored())))
                    slot.OnPress();
        } else {
            foreach (Slot slot in Slot.allActive.Values)
                if (!slot_stack.Contains(slot))
                    slot.OnUnpress();
        }
    }

    void ReleaseStack() {
        Project.onReleaseStack.Invoke(new List<Slot>(slot_stack));
        StartCoroutine(ReleaseStackRoutine());
    }

    internal int lastStackSize = 0;
    IEnumerator ReleaseStackRoutine() {
        filling = false;
        lastStackSize = slot_stack.Count;
        
        if (slot_stack.Count < 2) {
            foreach (Slot slot in Slot.allActive.Values)
                slot.OnUnpress();
            slot_stack.Clear();
            UpdateStackLine();
            ColoredBackground.SetEmpty();
            yield break;
        }

        if (SessionInfo.current.OutOfLimit())
            yield break;
        SessionInfo.current.BurnMove();

        SetMode(PlayingMode.Matching);
        if (Slot.allActive.Values.Count(x => x.isPressed) >= maxStackSize) {
            Project.onSuperMatch.Invoke();
            HitContext context = new HitContext(Slot.allActive.Values.Where(x => x.isPressed), HitReason.Matching);
            foreach (Slot _slot in Slot.allActive.Values)
                if (_slot.isPressed)
                    _slot.HitAndScore(context);
            longLineTrick ++;
        } else {
            HitContext context = new HitContext(slot_stack.ToArray(), HitReason.Matching);
            for (int i = slot_stack.Count - 1; i >= 0; i--) {
                ColoredBackground.SetTargetColor(RealColors.Get(stackColor), 1f * slot_stack.Count / maxStackSize);
                slot_stack[i].HitAndScore(context);
                slot_stack.RemoveAt(i);
                UpdateStackLine();

                yield return new WaitForSeconds(0.05f);
            }
            longLineTrick = 0;
        }

        matchDate++;
        slot_stack.Clear();
        UpdateStackLine();
        stackColor = ItemColor.Unknown;
        ColoredBackground.SetEmpty();
    }

    public ItemColor GetStackColor() {
        return stackColor;
    }

    bool IsFillingStack() {
        return filling;
    }

    MeshRenderer line_renderer;

    void UpdateStackLine() {
        line.Clear();
        foreach (Slot slot in slot_stack)
            line.AddPoint(new Vector2(slot.transform.position.x, slot.transform.position.y));
        line_renderer.material.SetTextureScale("_MainTex", new Vector2(1, 1));
    }

    public override ItemColor[] ColorGeneration(ItemColor[] colors, Dictionary<Side, ItemColor> nears, GenerationType type) {
        switch (type) {
            case GenerationType.AllUnkonwn:
                return colors.Where(c => !nears.Contains(x => x.Key.IsStraight() && x.Value.IsMatchWith(c))).ToArray();
            case GenerationType.EvenFinal:
                return null;
            case GenerationType.OddSlots:
            case GenerationType.EvenSlots:
                return new ItemColor[] { colors.Length > 0 ? colors.GetRandom() : nears.Values.Distinct().ToList().GetRandom() };
        }
        return null;
    }
    #endregion
}
