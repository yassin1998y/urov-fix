using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class RocketChip : IChip, IBomb, IColored, INeedToBeSetup, IMixable {
    public ItemColor color { get; set; }

    public int mixingPriority;

    [ContentSelector]
    public IEffect reachedTheTargetEffect;

    [ContentSelector]
    public NodeEffect mixingNodeEffect;

    public int destroyReward {
        get {
            return 3;
        }
    }

    static List<Slot> stack = new List<Slot>();
    public IEnumerator Destroying() {
        Explode(transform.position, 2, -30);

        Slot target = FindTargets(true).Unsort().GetMax(x => x.Value).Key;

        stack.Add(target);

        foreach (SetSortingLayer sprite in GetComponentsInChildren<SetSortingLayer>()) {
            if (SortingLayer.IDToName(sprite.sorting.layerID) == "Chips") {
                sprite.sorting.layerID = SortingLayer.NameToID("UI");
                sprite.Refresh();
            }
        }

        Vector3 startPosition = transform.position;
        float rotationSpeed = Random.Range(-270f, 270f);
        float distance = (startPosition - target.transform.position).magnitude;
        float speed = Random.Range(5f, 8f) / distance;
        speed = Mathf.Clamp(speed, .5f, 2f);

        for (float t = 0; t < 1f; t = Mathf.Min(1f, t + Time.deltaTime * speed)) {
            transform.position = Vector3.Lerp(startPosition, target.transform.position + Vector3.up * distance * (1f - t), t);
            transform.localScale = Vector3.one * (1f + 2f * Mathf.Sin(3.14f * t));
            transform.Rotate(0, 0, Time.deltaTime * rotationSpeed);
            yield return 0;
        }

        target.HitAndScore(new HitContext(HitReason.Matching));
        Explode(transform.position, 2, 20);

        stack.Remove(target);

        sound.Play("Destroying");
        animator.Play("Destroying");

        if (reachedTheTargetEffect) {
            IEffect effect = Content.Emit(reachedTheTargetEffect);
            effect.transform.position = transform.position;

            if (color.IsPhysicalColor())
                effect.Repaint(color);

            effect.Play();
        }

        while (animator.IsPlaying())
            yield return 0;
    }

    Dictionary<Slot, int> FindTargets(bool any) {

        Dictionary <Slot, int> result = new Dictionary<Slot, int>();
        List<ILevelGoal> goals = SessionInfo.current.GetGoals().Where(x => !x.IsComplete()).ToList();

        foreach (Slot slot in Slot.allActive.Values) {
            if (slot == this.slot) continue;
            if (stack.Contains(slot)) continue;
            ISlotContent content = slot.GetCurrentContent();
            if (content == null || content is RocketChip) continue;
            if (!any && (!(content is IChip) || content is IBomb)) continue;
            if (content.destroyable == null || content.destroying) continue;
            if (content.birthDate == SessionInfo.current.rule.matchDate) continue;

            result.Add(slot, goals.Sum(x => x.GetAssess(slot)));
        }

        return result;
    }

    public void Explode() {
        slot.HitAndScore();
    }

    public void OnSetup(Slot slot) {
        Repaint(this, color.IsPhysicalColor() ? color : SessionInfo.current.colorMask.Values.GetRandom());
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        color = info["color"].ItemColor;
        Repaint(this, color);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) color));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
    }

    public class RocketChipStackCleaner : Reaction {
        public override int GetPriority() {
            return 0;
        }

        public override ReactionType GetReactionType() {
            return ReactionType.Match;
        }

        public override IEnumerator React() {
            stack.Clear();
            yield break;
        }
    }

    public int GetMixingLogicPriority() {
        return mixingPriority;
    }

    public IEnumerator Mixing(IChip secondChip) {
        IChip bombReference = null;
        ItemColor color = this.color;

        List<Slot> targets = new List<Slot>();
        var targetList = FindTargets(false);

        if (secondChip) targetList.Remove(secondChip.slot);
        targetList.Remove(slot);
        targetList = targetList.Unsort();

        if (secondChip is RocketChip) {
            int countOfInstances = 4;

            while (targets.Count < countOfInstances) {
                Slot target = targetList.GetMax(x => x.Value).Key;
                targetList.Remove(target);
                targets.Add(target);
                if (targetList.Count == 0) break;
            }
        } else {
            targets.Add(targetList.GetMax(x => x.Value).Key);
            bombReference = secondChip.original.GetComponent<IChip>();
        }

        stack.AddRange(targets);

        Explode(transform.position, 2, -30);

        foreach (Slot target in targets) {
            StrikeEffect effect = Content.Emit(mixingNodeEffect);
            effect.transform.position = transform.position;
            if (color.IsPhysicalColor())
                effect.Repaint(color);
            effect.SetTarget(target.transform);
            effect.Play();

            Slot slot = target;
            HitContext context = new HitContext(new Slot[] { slot, this.slot },  HitReason.Matching);
            if (bombReference)
                effect.onReachCoroutine = EmitBomb(slot, bombReference, color, context);
            else
                effect.onReach += () => {
                    slot.HitAndScore(context);
                    Explode(slot.transform.position, 2, 20);
                    stack.Remove(slot);
                };
        }

        yield break;
    }

    IEnumerator EmitBomb(Slot slot, IChip bombReference, ItemColor color, HitContext context) {
        ISlotContent content = slot.GetCurrentContent();
        if (content && content is IChip && content is IDefaultSlotContent) {
            ISlotContent bomb = FieldAssistant.main.Add(bombReference, slot, color);
            bomb.birthDate--;
            yield return 0;
            if (bomb.destroyable != null && !bomb.destroying)
                bomb.slot.HitAndScore(context);
        } else
            slot.HitAndScore(context);
        Explode(slot.transform.position, 2, 20);
        stack.Remove(slot);
    }
}