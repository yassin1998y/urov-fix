using UnityEngine;
using System.Collections;
using System;
using Yurowm.GameCore;
using System.Xml.Linq;

// Destroyable blocks on playing field
public class Sandwich : IBlock, ILayered, IDestroyable, INeedToBeSetup {

    public GameObject[] layerObjects;

    public override void Initialize() {
        base.Initialize();
        Project.onSlotContentPrepareToDestroy.AddListener(OnContentDestroyed);
    }

    public override void OnKill() {
        Project.onSlotContentPrepareToDestroy.RemoveListener(OnContentDestroyed);
    }

    public int layer { get; set; }

    public int destroyReward {
        get {
            return 3;
        }
    }

    public int destroyLayerReward {
        get {
            return 1;
        }
    }

    void OnContentDestroyed(ISlotContent content) {
        if (content.context == null || content.context.reason != HitReason.Matching) return;
        if (content is IChip && slot.position.FourSideDistanceTo(content.slot.position) <= 1)
            slot.HitAndScore();
    }

    public override bool CanItContainChip() {
        return false;
    }

    public int GetLayerCount() {
        return layerObjects.Length;
    }

    public void OnChangeLayer(int layer) {
        sound.Play("LayerDown");
        animator.Play("LayerDown");
        for (int i = layerObjects.Length - 1; i >= layer; i--)
            if (layerObjects[i]) {
                IEffect effect = layerObjects[i].GetComponentInChildren<IEffect>(true);
                if (effect) {
                    effect.gameObject.SetActive(true);
                    effect.transform.SetParent(null);
                    effect.Play();
                }
                Destroy(layerObjects[i]);
            }
    }

    public IEnumerator Destroying() {
        OnChangeLayer(0);
        yield break;
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        layer = info["layer"].Int;

        for (int i = layerObjects.Length - 1; i >= layer; i--)
            if (layerObjects[i])
                Destroy(layerObjects[i]);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("layer", layer));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["layer"].Int = int.Parse(xContent.Attribute("layer").Value);
    }

    public void OnSetup(Slot slot) {}
}

