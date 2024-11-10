using System;
using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using UnityEngine;
using System.Xml.Linq;
using System.Linq;

[SlotTagRenderer('W', ConsoleColor.Black, ConsoleColor.Cyan)]
public class Water : ISlotModifier, IAnimated, ISounded, INeedToBeSetup {
    [ContentSelector]
    public SlotRenderer slotRenderer;

    [ContentSelector]
    public IEffect waveEffect;

    public static new SlotRenderer renderer = null;

    public static bool IsTagVisible(SlotSettings slot) {
        return slot.content.Contains(x => x.name == "Water");
    }

    public override IEnumerator GetAnimationNames() {
        yield return "Drop";
    }

    public override IEnumerator GetSoundNames() {
        yield return "Drop";
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        if (!renderer) {
            renderer = Content.Emit(slotRenderer);
            renderer.transform.SetParent(FieldAssistant.main.sceneFolder);

            renderer.Rebuild(SessionInfo.current.design.slots.Where(x => x.content.Contains(y => y.name == name)).Select(x => x.position).ToList());
        }
    }

    public void OnSetup(Slot slot) {}
}

public class WaterReaction : Reaction {
   
    public override int GetPriority() {
        return -50;
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Move;
    }

    public override IEnumerator React() {

        List<Water> ocean = ILiveContent.GetAll<Water>(x => x.isActiveContent && x.slot.GetCurrentContent() is IChip);

        if (ocean.Count == 0)
            yield break;

        IEffect _effect = null;
        foreach (Water water in ocean) {
            water.sound.Play("Drop");
            if (water.waveEffect) {
                IEffect effect = Content.Emit(water.waveEffect);
                if (effect) {
                    effect.transform.position = water.transform.position;
                    effect.Play();
                }
                if (!_effect) _effect = effect;
            }
        }

        float playTime = .6f;

        List<IChip> chips = ocean.Select(x => x.slot.chip).ToList();

        for (float t = 1f; t > 0; t -= Time.deltaTime / playTime) {
            chips.ForEach(x => x.transform.localScale = Vector3.one * t);
            yield return 0;
        }

        ocean.ForEach(x => x.slot.chip.Kill());

        while (_effect)
            yield return 0;

        yield return ReactionResult.Gravity;
    }
}