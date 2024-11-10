using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class IMixableChipMix : IChipMix {

    IEnumerator logic;

    public override IEnumerator Destroying() {
        while (logic.MoveNext())
            yield return logic.Current;
    }

    public override void Prepare(IChip firstChip, IChip secondChip) {
        IMixable mixableChip = null;
        IChip second = null;

        IMixable mixable1 = firstChip as IMixable;
        IMixable mixable2 = secondChip as IMixable;

        if (mixable1 != null && mixable2 != null) {
            mixableChip = mixable1.GetMixingLogicPriority() > mixable2.GetMixingLogicPriority() ?
                mixable1 : mixable2;
            second = mixableChip == mixable1 ? secondChip : firstChip;
        } else if (mixable1 != null) {
            mixableChip = mixable1;
            second = secondChip;
        } else if (mixable2 != null) {
            mixableChip = mixable2;
            second = firstChip;
        } else
            throw new System.Exception("IMixableChipMix: one of mixed chips must implement IMixable interface");

        logic = mixableChip.Mixing(second);
    }
}
