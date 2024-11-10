using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using System;
using System.Xml.Linq;

public class Glass : ISlotModifier, ILayered, IDestroyable, IGoalExclusive, INeedToBeSetup {

    #region Level parameter keys
    public const string glass_layer_parameter = "Glass_level";
    #endregion

    public Sprite[] sprites;
	public Sprite[] glasses;
    public new SpriteRenderer renderer;
    public SpriteRenderer glass;

    public int layer { get; set; }

    public int destroyLayerReward {
        get {
            return 0;
        }
    }

    public int destroyReward {
        get {
            return 3;
        }
    }
    
    public int GetLayerCount() {
        return 3;
    }

    public void OnChangeLayer(int layer) {
        animator.Play("LayerDown");
        sound.Play("LayerDown");
        renderer.sprite = sprites[layer - 1];
        glass.sprite = glasses[layer - 1];
    }

    public IEnumerator Destroying() {
        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");
    }

    public bool IsCompatibleWithGoal(ILevelGoal goal) {          
        return goal is CollectionGoal && (goal as CollectionGoal).target.EqualContent(this);
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        layer = info["layer"].Int;
        renderer.sprite = sprites[layer - 1];
        glass.sprite = glasses[layer - 1];
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("layer", layer));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["layer"].Int = int.Parse(xContent.Attribute("layer").Value);
    }

    public void OnSetup(Slot slot) {}
}