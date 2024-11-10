using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class GummyBear : IBigModifier, IGoalExclusive, IDestroyable {
    public SortingLayerAndOrder cleanSorting;
    public int2 size = new int2(2, 3);

    public int destroyReward {
        get {
            return 10;
        }
    }

    SpriteRenderer icon;
    public override void Initialize() {
        base.Initialize();
        var tIcon = transform.AllChild(true).FirstOrDefault(c => c.name == "Icon");
        if (tIcon) {
            icon = tIcon.GetComponent<SpriteRenderer>();
        }
    }

    public override IEnumerator<int2> Shape() {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return new int2(x, y);
    }

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public override void Serialize(XElement xContent) {}

    public bool IsCompatibleWithGoal(ILevelGoal mode) {
        return mode is GummyBearGoal;
    }

    public override int Hit(HitContext context) {
        if (!GetSlots().Contains(s => s.Content().Contains(c => c is Dirt && !(c as Dirt).destroying)))
            return base.Hit(context);
        return 0;
    }

    public IEnumerator Destroying() {
        GetSlots().ForEach(s => s.DetachContent(this));
        if (icon) {
            icon.sortingLayerID = cleanSorting.layerID;
            icon.sortingOrder = cleanSorting.order;
        }

        sound.Play("Destroying");
        yield return animator.PlayAndWait("Destroying");

        if (icon) {
            GummyBearGoal goal = SessionInfo.current.GetGoals().FirstOrDefault(g => g is GummyBearGoal) as GummyBearGoal;
            if (goal)
                goal.CollectionEffect(this, icon);
        }
    }
}