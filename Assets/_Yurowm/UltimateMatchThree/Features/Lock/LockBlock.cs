using System;
using System.Collections;
using Yurowm.GameCore;
using UnityEngine;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine.UI;

public class LockBlock : IBlock, ILayered, ISounded, IColored, IDestroyable, INeedToBeSetup {

    public Text counter;

    int counter_value {
        set {
            counter.text = value.ToString();
        }
    }

    int stack;

    public override IEnumerator GetSoundNames() {
        yield return base.GetSoundNames();
        yield return "StackChanged";
    }

    public void OnChangeLayer(int layer) {
        SessionInfo.current.AddScorePoint();

        if (pressed) OnUnpress();

        sound.Play("LayerDown");
        animator.Play("LayerDown");

        counter_value = layer;
    }

    public override bool CanItContainChip() {
        return true;
    }

    public override void OnKill() {
        Project.onStackCountChanged.RemoveListener(OnStackCountChanged);
        Project.onReleaseStack.RemoveListener(OnReleaseStack);
    }

    bool pressed = false;

    public int layer { get; set; }

    public int destroyLayerReward {
        get {
            return 1;
        }
    }

    public int destroyReward {
        get {
            return 1;
        }
    }

    ItemColor _color;
    public ItemColor color {
        get {
            return _color;
        }

        set {
            _color = value;
        }
    }

    public override void OnPress() {
        base.OnPress();
        pressed = true;
        Project.onStackCountChanged.AddListener(OnStackCountChanged);
        Project.onReleaseStack.AddListener(OnReleaseStack);
    }

    public override void OnUnpress() {
        base.OnUnpress();
        pressed = false;
        Project.onStackCountChanged.RemoveListener(OnStackCountChanged);
        Project.onReleaseStack.RemoveListener(OnReleaseStack);

        counter.gameObject.SetActive(true);
        counter_value = layer;
    }

    public override int Hit(HitContext context) {
        if (damage == 0)
            return base.Hit(context);

        int score = 0;
        while (damage > 0) {
            score += base.Hit(context);
            damage--;
        }

        return score;
    }

    int damage = 0;
    void OnReleaseStack(List<Slot> stack) {
        damage = pressed ? stack.Count(x => x.GetCurrentType() == typeof(SimpleChip)) : 0;
    }

    void OnStackCountChanged() {
        if (pressed) {
            stack = (SessionInfo.current.rule as MatchChainRule).slot_stack.Count(x => x.GetCurrentType() == typeof(SimpleChip));
            int count = Mathf.Max(0, layer - stack);
            counter_value = count;

            counter.gameObject.SetActive(count > 0);

            sound.Play("StackChanged");
        }
    }

    public IEnumerator Destroying() {
        counter.gameObject.SetActive(false);
        slot.block = null;
        slot.gravity = true;
        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");
    }

    public int GetLayerCount() {
        return 99;
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        layer = info["layer"].Int;
        color = info["color"].ItemColor;

        counter_value = layer;
        
        slot.gravity = false;

        Repaint(this, color);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) _color));
        xContent.Add(new XAttribute("layer", layer));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
        slotContent["layer"].Int = int.Parse(xContent.Attribute("layer").Value);
    }

    public void OnSetup(Slot slot) {}
}
