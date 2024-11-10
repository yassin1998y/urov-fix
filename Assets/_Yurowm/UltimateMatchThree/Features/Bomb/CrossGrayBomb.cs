using System.Collections;
using System.Collections.Generic;
using Yurowm.GameCore;
using UnityEngine;
using System.Linq;
using System.Xml.Linq;

public class CrossGrayBomb : IChip, IBomb, ILevelRuleExclusive {
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

    public int destroyReward {
        get {
            return 3;
        }
    }

    public IEnumerator Destroying() {
        int2 position = slot.position;
        
        sound.Play("Destroying");
        animator.Play("Destroying");

        while (animating) yield return 0;

        int step = 0;
        List<Side> sides = Utils.ToList(Sides);
        List<int2> hitted = new List<int2>();
        List<Slot> hitGroup = new List<Slot>() { slot };

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
            step++;
            foreach (Side side in Utils.allSides) {
                if (sides.Contains(side)) {
                    int2 coord = position + side.ToInt2() * step;
                    if (!SessionInfo.current.activeArea.IsItInclude(coord)) {
                        sides.Remove(side);
                        continue;
                    }
                    if (Slot.allActive.ContainsKey(coord))
                        Explode(Slot.allActive[coord].transform.position, 3, 13);
                    Hit(coord, 0, Side.Null, hitted, context);
                    for (int o = 1; o <= offset; o++) {
                        Hit(coord, o, side.RotateSide(-2), hitted, context);
                        Hit(coord, o, side.RotateSide(2), hitted, context);
                    }
                }
            }
            yield return new WaitForSeconds(0.06f);
        }

        while (animator.IsPlaying("Destroying"))
            yield return 0;
    }

    public override void OnCreateDestroyingEffect(IEffect effect) {
        if (effect is LineExplosionEffect)
            (effect as LineExplosionEffect).SetSides(Sides().ToList());
    }

    internal static void Hit(int2 coord, int offset, Side offsetSide, List<int2> hitted, HitContext context) {
        int2 _coord = coord;
        if (offset > 0)
            _coord += offset * offsetSide.ToInt2();
        if (!hitted.Contains(_coord)) {

            hitted.Add(_coord);
            if (Slot.allActive.ContainsKey(_coord))
                Slot.allActive[_coord].HitAndScore(context);
        }
    }

    bool animating = false;

    public void Explode() {
        slot.HitAndScore();
    }

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

    public override void Serialize(XElement xContent) {}

    public override void Deserialize(XElement xContent, SlotContent slotContent) {}

    public bool IsCompatibleWith(LevelRule rule) {
        return rule is MatchClickRule;
    }
}
