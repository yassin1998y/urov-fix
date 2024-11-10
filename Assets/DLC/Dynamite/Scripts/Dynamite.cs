using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class Dynamite : IChip, IColored, ILayered, IDestroyable, INeedToBeSetup, IGeneratedWithProbability {
    [ContentSelector]
    public IEffect explosionEffect;
    public TMPro.TextMeshPro counter;
    public IntRange randomLayerRange = new IntRange(5, 10);
    public GameObject warningObject;

    public ItemColor color {
        get;
        set;
    }
    public int layer {
        get;
        set;
    }

    public int destroyReward {
        get {
            return 1;
        }
    }

    public int destroyLayerReward {
        get {
            return 0;
        }
    }

    public override int Hit(HitContext context) {
        layer = 0;
        return base.Hit(context);
    }

    public IEnumerator Destroying() {
        SessionInfo.current.AddScorePoint();

        sound.Play("Destroying");
        animator.Play("Destroying");

        while (animator.IsPlaying("Destroying"))
            yield return 0;
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        Repaint(this, info["color"].ItemColor); 
        layer = info["layer"].Int;
        OnChangeLayer(layer);
    }

    public void OnSetup(Slot slot) {
        Repaint(this, SessionInfo.current.colorMask.Values.GetRandom());
        layer = URandom.main.Range(randomLayerRange);
        OnChangeLayer(layer);
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("color", (int) color));
        xContent.Add(new XAttribute("layer", layer));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["color"].ItemColor = (ItemColor) int.Parse(xContent.Attribute("color").Value);
        slotContent["layer"].Int = int.Parse(xContent.Attribute("layer").Value);
    }

    public int GetLayerCount() {
        return 99;
    }

    public void OnChangeLayer(int layer) {
        counter.text = layer.ToString();
        if (warningObject) warningObject.SetActive(layer <= 3);
    }

    public override IEnumerator GetAnimationNames() {
        yield return base.GetAnimationNames();
        yield return "Explosion";
    }

    public void Explosion() {
        animator.Play("Explosion");
        Explode(transform.position, 10, 60);
        if (explosionEffect) {
            IEffect effect = Content.Emit(explosionEffect);
            effect.transform.position = transform.position;
            if (color.IsPhysicalColor())
                effect.Repaint(color);
            effect.Play();
        }
    }
}

public class DynamiteReaction : Reaction {
   
    public override int GetPriority() {
        return 200;
    }

    public override ReactionType GetReactionType() {
        return ReactionType.Move;
    }

    public override IEnumerator React() {
        List<Dynamite> all = ILiveContent.GetAll<Dynamite>(x => x.isActiveContent);

        if (all.Count == 0) yield break;
        List<Dynamite> killers = new List<Dynamite>();
        foreach (Dynamite dynamite in all) {
            dynamite.layer--;
            dynamite.OnChangeLayer(dynamite.layer);
            if (dynamite.layer <= 0)
                killers.Add(dynamite);
        }

        if (killers.Count == 0) yield break;

        while (SessionInfo.current.GetMovesCount() > 0)
            SessionInfo.current.BurnMove();

        foreach (Dynamite dynamite in killers)
            dynamite.Explosion();

        yield return ReactionResult.Gravity;
    }
}