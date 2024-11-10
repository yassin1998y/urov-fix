using UnityEngine;
using System.Collections;
using System;
using Yurowm.GameCore;
using System.Xml.Linq;
using System.Linq;

public class UndeadBlocker : IBlock, INeedToBeSetup {
    [ContentSelector]
    public SlotRenderer slotRenderer;

    public static new SlotRenderer renderer = null;

    public override bool CanItContainChip() {
        return false;
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        if (!renderer) {
            renderer = Content.Emit(slotRenderer);
            renderer.transform.SetParent(FieldAssistant.main.sceneFolder);
            renderer.transform.Reset();
            renderer.Rebuild(SessionInfo.current.design.slots.Where(x => x.block != null && x.block.name == name).Select(x => x.position).ToList());
        }
    }

    public void OnSetup(Slot slot) {}
}
