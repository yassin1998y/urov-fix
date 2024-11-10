using UnityEngine;
using System.Collections;
using System;
using Yurowm.GameCore;
using System.Collections.Generic;
using System.Xml.Linq;

public class Pearl : IChip, IColored, IDestroyable, IGeneratedWithProbability, IShuffled {
    public ItemColor color {
        get {
            return ItemColor.Universal;
        }
        set {}
    }

    public int destroyReward {
        get {
            return 1;
        }
    }

    public IEnumerator Destroying() {
        OnUnpress();

        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");
    }
    
    public override void OnPress() {
        base.OnPress();
        Project.onStackCountChanged.AddListener(UpdateColor);
    }

    public override void OnUnpress() {
        base.OnUnpress();
        Project.onStackCountChanged.RemoveListener(UpdateColor);
        Repaint(this, ItemColor.Universal);
    }

    void UpdateColor() {
        if (SessionInfo.current.rule is MatchChainRule) {
            ItemColor itemColor = (SessionInfo.current.rule as MatchChainRule).GetStackColor();
            Repaint(this, itemColor);
        }
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}
}
