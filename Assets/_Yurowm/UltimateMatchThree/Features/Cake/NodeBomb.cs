using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class NodeBomb : IChip, IBomb, IMixable {
    public int mixingPriority;

    [ContentSelector]
    public StrikeEffect rocketEffect;

    public int destroyReward {
        get {
            return 5;
        }
    }

    public IEnumerator Destroying() {
        var mixing = Mixing(null);
        while (mixing.MoveNext())
            yield return mixing.Current;

        while (animator.IsPlaying())
            yield return 0;
    }

    public void Explode() {
        slot.HitAndScore();
    }

    protected ItemColor targetColor = ItemColor.Unknown;
    public IEnumerator Mixing(IChip secondChip) {   
        sound.Play("Destroying");
        animator.Play("Destroying");

        targetColor = colored == null ? ItemColor.Unknown : colored.color;

        if (!targetColor.IsPhysicalColor() && secondChip && secondChip.colored != null)
            targetColor = secondChip.colored.color;
        
        if (!targetColor.IsPhysicalColor())
            targetColor = SessionInfo.current.colorMask.Values.GetRandom();

        if (secondChip && destroyingEffect) {
            IEffect effect = Content.Emit(destroyingEffect);
            effect.transform.position = transform.position;

            if (targetColor.IsPhysicalColor())
                effect.Repaint(targetColor);

            effect.Play();
        }

        IChip bombReference = null;
        if (secondChip && (secondChip is IBomb) && secondChip.original)
            bombReference = secondChip.original.GetComponent<IChip>();

        List<Slot> targets = GetTargets(secondChip);

        if (secondChip) targets.Remove(secondChip.slot);
        targets.Remove(slot);

        HitContext context = new HitContext(targets, HitReason.Matching);
        foreach (Slot target in targets) {
            StrikeEffect effect = Content.Emit(rocketEffect);
            effect.transform.position = transform.position;
            if (targetColor.IsPhysicalColor())
                effect.Repaint(targetColor);
            effect.SetTarget(target.transform);
            effect.Play();

            Slot slot = target;
            if (bombReference)
                effect.onReachCoroutine = EmitBomb(slot, bombReference, targetColor, context);
            else
                effect.onReach += () => slot.HitAndScore(context);
        }

        yield break;
    }

    public virtual List<Slot> GetTargets(IChip secondChip) {
        if (secondChip is NodeBomb)
            return Slot.allActive.Values.Where(x => x.GetCurrentContent() is IDestroyable).ToList();
        else
            return Slot.allActive.Values.Where(x => x.color == targetColor).ToList();
    }
         
    IEnumerator EmitBomb(Slot slot, IChip bombReference, ItemColor color, HitContext context) {
        ISlotContent content = slot.GetCurrentContent();
        if (content && content is IChip && content is IDefaultSlotContent) {
            ISlotContent bomb = FieldAssistant.main.Add(bombReference, slot, color);
            bomb.birthDate--;
            yield return new WaitForSeconds(0.3f);
            if (bomb.destroyable != null && !bomb.destroying)
                bomb.slot.HitAndScore(context);
        } else
            slot.HitAndScore(context);
    }

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public int GetMixingLogicPriority() {
        return mixingPriority;
    }
}
