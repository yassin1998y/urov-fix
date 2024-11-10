using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

[SlotTagRenderer('I', ConsoleColor.Yellow, ConsoleColor.Black)]
public class IngredientHolder : ISlotModifier, IGoalExclusive {
    
    public static bool IsTagVisible(SlotSettings slot) {
        return slot.content.Contains(x => x.name == "IngredientHolder");
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public bool IsCompatibleWithGoal(ILevelGoal goal) {
        return goal is IngredientsGoal;
    }
}

public class IngredientReaction : Reaction {
    public override int GetPriority() {
        return -100;
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Match;
    }

    public override IEnumerator React() {
        IngredientChip.lastDestroyDate = 0;

        List<IngredientHolder> holders = ILiveContent.GetAll<IngredientHolder>(x => x.slot.chip != null && x.slot.chip is IngredientChip);

        if (holders.Count == 0)
            yield break;

        HitContext context = new HitContext(HitReason.Reaction);
        foreach (IngredientHolder holder in holders)
            holder.slot.chip.Hit(context);

        yield return ReactionResult.GravityAndRepeate;

        yield return new WaitForSeconds(1f);
    }
}