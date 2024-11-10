using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class AntiGravitation : ILevelExtension {
    public override void Setup(LevelExtensionInfo info) {
        foreach (Slot slot in Slot.all.Values) {
            var extension = info.GetSlot(slot.position);
            slot.falling.Clear();
            if (extension == null) continue;
            foreach (Side side in Utils.straightSides)
                if (extension[side.ToString()].Bool)
                    slot.falling.Add(side, null);
            slot.CulculateFallingSlot();
        }
    }
}
