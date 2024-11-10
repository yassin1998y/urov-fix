using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Yurowm.GameCore;
using System.Xml.Linq;

[SlotTagRenderer('I', ConsoleColor.Cyan, ConsoleColor.Black)]
public class IngredientChip : IChip, IDestroyable, IGeneratedWithCondition, IGoalExclusive {

    #region Tag Renderer Condition
    public static bool IsTagVisible(SlotSettings slot) {
        SlotContent generator = slot.GetSlotContent("Generator");
        if (generator == null) return false;
        return generator[string.Format(GeneratedWithCondition.keyFormat, "Ingredient")].Bool;
    }
    #endregion

    public static int lastDestroyDate = 0;

    public int destroyReward {
        get {
            return 1;
        }
    }

    public override int Hit(HitContext context) {
        if (slot.CheckModifier(x => x is IngredientHolder))
            return base.Hit(context);
        return 0;
    }

    public bool GenerationCondition() {
        if (SessionInfo.current.IsLevelGoal<IngredientsGoal>() && !SessionInfo.current.IsCompleteTarget<IngredientsGoal>() &&
            Count<IngredientChip>() < IngredientsGoal.activeCount && 
            lastDestroyDate < SessionInfo.current.rule.matchDate &&
            UnityEngine.Random.value > 0.4f)
            return true;
        return false;
    }

    public IEnumerator Destroying() {
        lastDestroyDate = SessionInfo.current.rule.matchDate;

        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");
    }

    public bool IsCompatibleWithGoal(ILevelGoal goal) {
        return goal is IngredientsGoal;
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}
}
