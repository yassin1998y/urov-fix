using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class Dirt : ISlotModifier, ILayered, IDestroyable, INeedToBeSetup {
    [ContentSelector]
    public SlotRenderer slotRenderer;
    static Dictionary<int, SlotRenderer> renderers = null;

    public override void Initialize() {
        base.Initialize();
        Project.onHitSolution.AddListener(OnHitSolution);
    }

    public override void OnKill() {
        Project.onHitSolution.RemoveListener(OnHitSolution);
    }

    int clean = 0;
    void OnHitSolution(HitContext context) {
        if (!context.group.Contains(slot)) return;
        if (context.group.Contains(s => !s.Content().Contains(c => c is Dirt)) ||
            context.group.Contains(s => s.Content().Contains(c => c is Dirt && (c as Dirt).layer < layer))) {
            clean ++;
            Hit(null);
        }
    }

    public override int Hit(HitContext context) {
        int result = 0;
        while (clean > 0) {
            clean--;
            result += base.Hit(context);
        }
        return result;
    }

    public int layer {
        get; set;
    }

    public int destroyLayerReward {
        get {
            return 0;
        }
    }

    public int destroyReward {
        get {
            return 3;
        }
    }

    public int GetLayerCount() {
        return 3;
    }

    public void OnChangeLayer(int layer) {
        animator.Play("LayerDown");
        sound.Play("LayerDown");
        if (destroyingEffect) {
            IEffect effect = Content.Emit(destroyingEffect);
            if (effect) {
                effect.transform.position = transform.position;
                effect.Play();
            }
        }
        Rebuild();
    }

    public IEnumerator Destroying() {
        sound.Play("Destroying");
        Rebuild();
        yield return animator.PlayAndWait("Destroying");
    }

    void EmitRenderer() {
        if (renderers == null || renderers.Contains(r => r.Value == null)) {
            if (renderers != null)
                renderers.Where(r => r.Value != null).ForEach(r => r.Value.Kill());
            renderers = new Dictionary<int, SlotRenderer>();
            List<Dirt> all = GetAll<Dirt>();
            for (int layer = 1; layer <= GetLayerCount(); layer++) {
                if (all.Contains(d => d.layer >= layer)) {
                    SlotRenderer sr = Content.Emit(slotRenderer);
                    sr.transform.SetParent(FieldAssistant.main.sceneFolder);
                    sr.transform.Reset();
                    sr.transform.position = Vector3.up * layer * .02f;
                    sr.sorting.order += layer;
                    sr.Rebuild(GetAll<Dirt>().Where(x => x && !x.destroying && x.layer >= layer).Select(x => x.slot.position).ToList());
                    renderers.Add(layer, sr);
                }
            }
        }
    }

    void Rebuild() {
        EmitRenderer();
        foreach (var renderer in renderers) 
            renderer.Value.Rebuild(GetAll<Dirt>().Where(x => x && !x.destroying && x.layer >= renderer.Key).Select(x => x.slot.position).ToList());
    }

    public void OnSetupByContentInfo(Slot slot, SlotContent info) {
        layer = info["layer"].Int;
        StartCoroutine(Creating(false));
    }

    public override void Serialize(XElement xContent) {
        xContent.Add(new XAttribute("layer", layer));
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {
        slotContent["layer"].Int = int.Parse(xContent.Attribute("layer").Value);
    }

    public void OnSetup(Slot slot) {
        StartCoroutine(Creating(true));
    }

    private IEnumerator Creating(bool rebuild) {
        yield return 0;
        while (animator.IsPlaying())
            yield return 0;
        if (rebuild) Rebuild();
        else EmitRenderer();
    }
}