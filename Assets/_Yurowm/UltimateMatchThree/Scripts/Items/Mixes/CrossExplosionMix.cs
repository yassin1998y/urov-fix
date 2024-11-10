using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Yurowm.GameCore;

public class CrossExplosionMix : IChipMix {
    [Range(0, 2)]
    public int offset = 0;
    public bool top = false;
    public bool topRight = false;
    public bool right = false;
    public bool bottomRight = false;
    public bool bottom = false;
    public bool bottomLeft = false;
    public bool left = false;
    public bool topLeft = false;

    public IEnumerator<Side> Sides() {
        if (top) yield return Side.Top;
        if (topRight) yield return Side.TopRight;
        if (right) yield return Side.Right;
        if (bottomRight) yield return Side.BottomRight;
        if (bottom) yield return Side.Bottom;
        if (bottomLeft) yield return Side.BottomLeft;
        if (left) yield return Side.Left;
        if (topLeft) yield return Side.TopLeft;
    }

    int2 position;
    ItemColor color = ItemColor.Unknown;

    public override void Prepare(IChip firstChip, IChip secondChip) {
        position = secondChip.slot.position;
        if (firstChip.colored != null && firstChip.colored.color.IsPhysicalColor())
            color = firstChip.colored.color;
        else if (secondChip.colored != null && secondChip.colored.color.IsPhysicalColor())
            color = firstChip.colored.color;
    }

    public override IEnumerator Destroying() {
        int step = 0;
        List<Side> sides = Sides().ToList();
        List<int2> hitted = new List<int2>();
        List<Slot> hitGroup = new List<Slot>();

        while (sides.Count > 0) {
            step++;
            foreach (Side side in Utils.allSides) {
                if (sides.Contains(side)) {
                    int2 coord = position + side.ToInt2() * step;
                    if (!SessionInfo.current.activeArea.IsItInclude(coord)) {
                        sides.Remove(side);
                        continue;
                    }
                    hitGroup.Add(Slot.all.Get(coord));
                    for (int o = 1; o <= offset; o++) {
                        hitGroup.Add(Slot.all.Get(coord + side.RotateSide(-2).ToInt2() * o));
                        hitGroup.Add(Slot.all.Get(coord + side.RotateSide(2).ToInt2() * o));
                    }
                }
            }
        }
        hitGroup = hitGroup.Distinct().Where(s => s != null).ToList();
        HitContext context = new HitContext(hitGroup, HitReason.BombExplosion);

        sides = Utils.ToList(Sides);
        step = 0;
        while (sides.Count > 0) {
            foreach (Side side in Utils.allSides) {
                if (sides.Contains(side)) {
                    int2 coord = position + side.ToInt2() * step;
                    if (!SessionInfo.current.activeArea.IsItInclude(coord)) {
                        sides.Remove(side);
                        continue;
                    }
                    if (Slot.allActive.ContainsKey(coord))
                        IChip.Explode(Slot.allActive[coord].transform.position, 4, 20);
                    CrossBomb.Hit(coord, 0, Side.Null, hitted, context);
                    for (int o = 1; o <= offset; o++) {
                        CrossBomb.Hit(coord, o, side.RotateSide(-2), hitted, context);
                        CrossBomb.Hit(coord, o, side.RotateSide(2), hitted, context);
                    }
                }
            }
            step++;
            yield return new WaitForSeconds(0.06f);
        }
    }

    public override void OnCreateDestroyingEffect(IEffect effect) {
        if (effect is LineExplosionEffect) {
            (effect as LineExplosionEffect).SetSides(Sides().ToList());
            effect.Repaint(color);
        }
    }
}
