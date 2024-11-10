using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Yurowm.GameCore;
using System.Linq;

public class MatchThreeSwapRule : LevelRule, IDefault, ISounded {

    [HideInInspector]
    public List<Combination> combinations = new List<Combination>();
    [HideInInspector]
    public List<BombMix> bombMixes = new List<BombMix>();
    [HideInInspector]
    public bool squares = true;
    [HideInInspector]
    public bool swapOnlyInWaitMode = false;
    [HideInInspector]
    public float swap_duration = 0.2f;
    [HideInInspector]
    public float hint_delay = 10;

    int matchTrick = 0;
    bool mixTrick = false;

    internal override IEnumerator SessionModes (PlayingMode playingMode) {
        switch (playingMode) {
            case PlayingMode.Wait: {
                    if (matchTrick >= 10) Feedback.Play("Megamatch");
                    else if (matchTrick >= 6) Feedback.Play("Multimatch");
                    matchTrick = 0;
                    if (hint_delay > 0) // if the delay is zero, we will not show any hints at all
                        StartCoroutine(ShowHint());

                } break;
            case PlayingMode.Matching: {
                    List<Solution> solutions = FindSolutions();
                    if (solutions.Count > 0)
                        yield return StartCoroutine(MatchSolutions(solutions));
                } break;
            case PlayingMode.Gravity: {
                    yield return 0;
                    if (mixTrick) Feedback.Play("Mix");
                    mixTrick = false;
                } break;
        }
    }

    IEnumerator ShowHint() {
        if (Slot.interactive != null || !SessionInfo.current.settings.showHints)
            yield break;

        float showTime = Time.time + hint_delay;

        while (showTime > Time.time) {
            if (GetMode() != PlayingMode.Wait)
                yield break;
            yield return 0;
        }

        Move hint = FindMoves().GetRandom();
        hint.solution.contents.Where(x => x != null).ForEach(x => (x as IChip).Flashing());
    }

    internal override bool IsThereAnySolutions() {
        return FindSolutions().Count > 0;
    }

    internal override bool IsThereAnyMoves() {
        return FindMoves().Count > 0;
    }

    RaycastHit2D hit;
    Vector2 startPoint = new Vector2();
    Slot pressedSlot = null;
    internal override void Control(bool isBegan, bool isPress, bool IsOverUI, Vector2 point) {
        if (isBegan) {
            if (IsOverUI) return;
            startPoint = point;
            hit = Physics2D.Raycast(startPoint, Vector2.zero);
            if (!hit.transform) return;
            pressedSlot = hit.transform.GetComponent<Slot>();
        }
        if (isPress && pressedSlot) {
            if (IsOverUI) return;
            Vector2 move = point - startPoint;
            if (move.magnitude > Project.main.slot_offset / 2) {              
                foreach (Side side in Utils.straightSides)
                    if (Vector2.Angle(move, side.X() * Vector2.right + side.Y() * Vector2.up) <= 45)
                        if (pressedSlot.chip)
                            SwapByPlayer(pressedSlot, side);
                pressedSlot = null;
            }
        }
    }

    #region Swapping
    void SwapByPlayer(Slot slot, Side side) {
        if (!slot || !slot[side]) return;
        switch (GetMode()) {
            case PlayingMode.Gravity:
            case PlayingMode.Matching:
                if (swapOnlyInWaitMode) return;
                goto case PlayingMode.Wait;
            case PlayingMode.Wait:
                StartCoroutine(SwapByPlayerRoutine(slot, slot[side])); return;
        }
    }

    // Temporary Variables
    bool swaping = false; // TRUE when the animation plays swapping 2 chips
    public bool iteraction = false;

    static List<Slot> lastSwaped = new List<Slot>(); 
    internal IEnumerator SwapByPlayerRoutine(Slot a, Slot b) {
        if (!SessionInfo.current.isPlaying) yield break;

        if (SessionInfo.current.GetMovesCount() <= 0) yield break;

        // cancellation terms
        if (swaping) yield break; // If the process is already running
        if (!a || !b) yield break; // If one of the chips is missing
        if (a == b) yield break;
        if (!Slot.allActive.ContainsValue(a) || !Slot.allActive.ContainsValue(b)) yield break;
        if (!(a.GetCurrentContent() is IChip) || !(b.GetCurrentContent() is IChip)) yield break;

        IChip chipA = a.chip;
        IChip chipB = b.chip;

        if (!Slot.IsInteractive(chipA.slot) ||
            !Slot.IsInteractive(chipB.slot))
            yield break;

        if (chipA.destroying || chipB.destroying) yield break;
        if (chipA.falling || chipB.falling) yield break;

        Pair<string> chipPair = new Pair<string>(chipA.name, chipB.name);


        swaping = true;

        bool success = false;

        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        float progress = 0;
        float time = 0;

        BombMix mix = bombMixes.FirstOrDefault(x => x.pair == chipPair);

        sound.Play("SwapBegin");

        List<Solution> solutions = new List<Solution>();
        if (mix != null) {
            while (progress < swap_duration) {
                time = EasingFunctions.easeInOutQuad(progress / swap_duration);

                chipA.transform.position = Vector3.Lerp(posA, posB, time);

                progress += Time.deltaTime;

                yield return 0;
            }

            IChipMix chipMix = Content.Emit(mix.mix) as IChipMix;
            chipMix.transform.position = chipB.transform.position;
            lastSwaped.Clear();
            lastSwaped.Add(a);
            lastSwaped.Add(b);
            chipMix.Prepare(chipA, chipB);
            HitContext context = new HitContext(new Slot[] { a, b }, HitReason.Matching);
            chipMix.Activate();
            matchDate++;
            a.DetachContent(chipA); b.DetachContent(chipB);
            a.Hit(context); b.Hit(context);
            chipA.Hide(); chipB.Hide();
            matchTrick++;
            mixTrick = true;

            while (chipMix != null) yield return 0;


            success = true;
        } else {

            Vector3 normal = a.x == b.x ? Vector3.right : Vector3.up;

            while (progress < swap_duration) {
                time = EasingFunctions.easeInOutQuad(progress / swap_duration);

                chipA.transform.position = Vector3.Lerp(posA, posB, time) + normal * Mathf.Sin(3.14f * time) * 0.2f;
                chipB.transform.position = Vector3.Lerp(posB, posA, time) - normal * Mathf.Sin(3.14f * time) * 0.2f;

                progress += Time.deltaTime;

                yield return 0;
            }

            chipA.transform.position = posB;
            chipB.transform.position = posA;

            a.chip = chipB;
            b.chip = chipA;

            // searching for solutions of matching
            int count = 0;
            Solution solution;

            solution = MatchAnaliz(a);
            if (solution != null) {
                count += solution.count;
                solutions.Add(solution);
            }

            solution = MatchAnaliz(b);
            if (solution != null) {
                count += solution.count;
                solutions.Add(solution);
            }

            // Scenario canceling of changing places of chips

            if (count == 0) {
                sound.Play("SwapFailed");
                while (progress > 0) {
                    time = EasingFunctions.easeInOutQuad(progress / swap_duration);
                    chipA.transform.position = Vector3.Lerp(posA, posB, time) - normal * Mathf.Sin(3.14f * time) * 0.2f;
                    chipB.transform.position = Vector3.Lerp(posB, posA, time) + normal * Mathf.Sin(3.14f * time) * 0.2f;

                    progress -= Time.deltaTime;

                    yield return 0;
                }

                a.transform.position = posA;
                b.transform.position = posB;

                b.chip = chipB;
                a.chip = chipA;

            } else
                success = true;

        }

        swaping = false;

        if (success) {
            Project.onSwapSuccess.Invoke();
            SessionInfo.current.BurnMove();
            SessionInfo.current.swapEvent++;
            lastSwaped.Clear();
            lastSwaped.Add(a);
            lastSwaped.Add(b);
            if (GetMode() == PlayingMode.Wait)
                SetMode(PlayingMode.Matching);
            else
                yield return StartCoroutine(MatchSolutions(solutions));
        }

    }

    #endregion

    // Search function possible moves
    internal List<Move> FindMoves() {
        List<Move> moves = new List<Move>();

        Solution solution;
        int potential;

        Side[] asixes = new Side[] { Side.Right, Side.Top };

        Type chipType = typeof(IChip);
        BombMix mix;

        foreach (Side asix in asixes) {
            foreach (Slot slot in Slot.allActive.Values) {
                if (slot[asix] == null) continue;
                if (!chipType.IsAssignableFrom(slot.GetCurrentType()) || !chipType.IsAssignableFrom(slot[asix].GetCurrentType())) continue;

                if (!slot.chip || !slot[asix].chip || !slot[asix].isActiveSlot) continue;
                Move move = new Move();
                move.from = slot.position;
                move.to = slot[asix].position;

                Pair<string> pair = new Pair<string>(slot.chip.name, slot[asix].chip.name);

                mix = bombMixes.FirstOrDefault(x => x.pair == pair);
                if (mix != null) {
                    move.potencial = 1000;
                    move.solution = new Solution();
                    move.solution.contents.Add(slot.chip);
                    move.solution.contents.Add(slot[asix].chip);
                    moves.Add(move);
                    continue;
                }


                if (slot.chip is IColored && slot[asix].chip is IColored && (slot.chip as IColored).color == (slot[asix].chip as IColored).color)
                    continue;

                AnalizSwap(move);

                Dictionary<Slot, Solution> solutions = new Dictionary<Slot, Solution>();

                Slot[] cslots = new Slot[2] { slot, slot[asix] };
                foreach (Slot cslot in cslots) {
                    solutions.Add(cslot, null);

                    potential = 0;
                    solution = MatchAnaliz(cslot);
                    if (solution != null) {
                        solutions[cslot] = solution;
                        potential = solution.potential;
                    }

                    move.potencial += potential;
                }

                if (solutions[cslots[0]] != null && solutions[cslots[1]] != null)
                    move.solution = solutions[cslots[0]].potential > solutions[cslots[1]].potential ? solutions[cslots[0]] : solutions[cslots[1]];
                else
                    move.solution = solutions[cslots[0]] ?? solutions[cslots[1]];

                AnalizSwap(move);

                if (move.potencial > 0) moves.Add(move);
            }
        }

        return moves;
    }

    void AnalizSwap(Move move) {
        Slot slot;
        try {
            IChip fChip = Slot.allActive[move.from].chip;
            IChip tChip = Slot.allActive[move.to].chip;
            if (!fChip || !tChip)
                return;
            slot = tChip.slot;
            fChip.slot.chip = tChip;
            slot.chip = fChip;
        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    bool IsInteractableChip(ISlotContent content) {
        if (!content) return false;
        if (content is IChip && !content.destroying) return true;
        return false;
    }

    bool IsInteractableContent(ISlotContent content) {
        if (!content) return false;
        if (!content.destroying &&
            (content is IChip && !(content as IChip).falling))
            return true;
        return false;
    }

    List<Solution> FindSolutions() {
        List<Solution> solutions = new List<Solution>();
        Solution zsolution;
        foreach (Slot slot in Slot.allActive.Values) {
            zsolution = MatchAnaliz(slot);
            if (zsolution != null)
                solutions.Add(zsolution);
        }
        return solutions;
    }

    // Analysis of chip for combination
    Solution MatchAnaliz(Slot slot) {

        if (!slot) return null;

        ISlotContent content = slot.GetCurrentContent();
        if (!IsInteractableChip(content)) return null;

        IColored colored = content as IColored;
        if (colored == null) return null;

        ItemColor currentColor = colored.color;

        if (currentColor.IsUniversalColor()) { // multicolor
            List<Solution> solutions = new List<Solution>();
            Solution z;
            List<ItemColor> colors = Utils.straightSides.Select(x => slot[x])
                .Where(x => x != null).Select(x => x.GetCurrentType() as IColored)
                .Where(x => x != null && x.color.IsPhysicalColor()).Select(x => x.color).ToList();

            foreach (ItemColor color in colors) {
                colored.color = color;
                z = MatchAnaliz(slot);
                if (z != null) solutions.Add(z);
            }

            colored.color = ItemColor.Universal;
            return solutions.GetMax(x => x.potential);
        }

        Slot _slot;
        Dictionary<Side, List<ISlotContent>> sides = new Dictionary<Side, List<ISlotContent>>();
        List<ISlotContent> square = new List<ISlotContent>();
        int count;
        int2 key;
        foreach (Side side in Utils.straightSides) {
            count = 1;
            sides.Add(side, new List<ISlotContent>());
            while (true) {
                key = slot.position + side.ToInt2() * count;
                if (!Slot.allActive.ContainsKey(key)) break;
                _slot = Slot.allActive[key];

                ISlotContent currentContent = _slot.GetCurrentContent();
                if (!currentContent || !IsInteractableContent(currentContent)) break;

                IColored icolored = _slot.GetCurrentContent() as IColored;
                if (icolored == null || !icolored.color.IsMatchWith(colored.color)) break;

                sides[side].Add(currentContent);
                count++;
            }
        }

        if (squares) {
            ISlotContent[] zSqaure = new ISlotContent[3];
            foreach (Side side in Utils.straightSides) {
                for (int r = 0; r <= 2; r++) {
                    key = slot.position + side.RotateSide(r).ToInt2();
                    if (!Slot.allActive.ContainsKey(key))
                        break;
                    _slot = Slot.allActive[key];

                    ISlotContent currentContent = _slot.GetCurrentContent();
                    if (!currentContent || !IsInteractableContent(currentContent)) break;

                    IColored icolored = _slot.GetCurrentContent() as IColored;
                    if (icolored == null || !icolored.color.IsMatchWith(colored.color)) break;

                    zSqaure[r] = currentContent;
                    if (r == 2)
                        square.AddRange(zSqaure);
                }
            }

            square.Distinct();
        } else
            square.Clear();

        bool h = sides[Side.Right].Count + sides[Side.Left].Count >= 2;
        bool v = sides[Side.Top].Count + sides[Side.Bottom].Count >= 2;
        bool s = square.Count > 0;

        if (h || v || s) {
            Solution solution = new Solution();

            solution.horizontal = h;
            solution.vertical = v;
            solution.square = s;

            solution.contents = new List<ISlotContent>();
            solution.contents.Add(slot.GetCurrentContent());

            if (h) {
                solution.contents.AddRange(sides[Side.Right]);
                solution.contents.AddRange(sides[Side.Left]);
            }
            if (v) {
                solution.contents.AddRange(sides[Side.Top]);
                solution.contents.AddRange(sides[Side.Bottom]);
            }
            if (s) {
                solution.contents.AddRange(square);
                solution.contents.Distinct();
            }

            solution.count = solution.contents.Count;

            solution.x = slot.x;
            solution.y = slot.y;
            solution.color = currentColor;

            foreach (ISlotContent c in solution.contents)
                solution.potential += 1;
            
            return solution;
        }
        return null;
    }

    IEnumerator MatchSolutions(List<Solution> solutions) {
        if (!SessionInfo.current.isPlaying) yield break; ;
        
        solutions.Sort((x, y) => y.potential.CompareTo(x.potential));

        area fieldArea = SessionInfo.current.design.area;

        bool[,] mask = new bool[fieldArea.width, fieldArea.height];
        Slot slot;

        foreach (int2 coord in fieldArea.GetPoints()) {
            mask[coord.x, coord.y] = false;
            if (Slot.allActive.ContainsKey(coord)) {
                slot = Slot.allActive[coord];
                if (slot.GetCurrentContent() is IColored)
                    mask[coord.x, coord.y] = true;
            }
        }

        List<Solution> final_solutions = new List<Solution>();

        bool breaker;
        foreach (Solution s in solutions) {
            breaker = false;
            foreach (ISlotContent c in s.contents) {
                if (!mask[c.slot.x, c.slot.y]) {
                    breaker = true;
                    break;
                }
            }
            if (breaker) continue;

            final_solutions.Add(s);

            foreach (ISlotContent c in s.contents) mask[c.slot.x, c.slot.y] = false;
        }

        matchDate++;
        int birthDate = matchDate;
        foreach (Solution solution in final_solutions) {
            solution.bombSlot = solution.GetCenteredSlot(x => x.EqualContent(SlotGenerator.mainChip));
            if (!solution.bombSlot) {
                solution.bombSlot = solution.GetCenteredSlot();
                continue;
            }
            foreach (Combination combination in combinations) {
                if (combination.minCount > solution.count) continue;
                if (combination.type != Combination.Type.Any) {
                    if (!squares && combination.square)
                        continue;
                    if (!combination.square) {
                        if (combination.type != Combination.Type.Lined) {
                            if (combination.horizontal != solution.horizontal) continue;
                            if (combination.vertical != solution.vertical) continue;
                        }
                    } else if (!solution.square) continue;
                }

                lastSwaped.Remove(solution.bombSlot);
                solution.bomb = combination.bomb;
                break;
            }
        }

        foreach (Solution solution in final_solutions) {
            int score = 0;
            HitContext context = new HitContext(solution.contents.Select(c => c.slot), HitReason.Matching);
            foreach (ISlotContent c in solution.contents) {
                c.birthDate = -1;
                score += c.slot.Hit(context);
            }
            matchTrick++;
            ScoreEffect.ShowScore(solution.bombSlot.transform.position, score, solution.color);
        }

        if (final_solutions.Count == 0) yield break;

        foreach (Solution solution in final_solutions)
            if (solution.bomb)
                FieldAssistant.main.Add(solution.bomb, solution.bombSlot, solution.color).birthDate = birthDate;

        float t = 0;
        while (t <= 1) {
            foreach (Solution solution in final_solutions)
                foreach (ISlotContent ch in solution.contents.Where(x => x != null && x.name == SlotGenerator.mainChip.name))
                    ch.transform.position = Vector3.Lerp(ch.transform.parent.position, solution.bombSlot.transform.position, t);
            t += Time.deltaTime * 7;
            yield return 0;
        }

    }

    public override ItemColor[] ColorGeneration(ItemColor[] colors, Dictionary<Side, ItemColor> nears, GenerationType type) {
        switch (type) {
            case GenerationType.AllUnkonwn:
            case GenerationType.EvenFinal:
                return colors.Where(c => !nears.Contains(x => x.Key.IsStraight() && x.Value.IsMatchWith(c))).ToArray();
            case GenerationType.OddSlots:
            case GenerationType.EvenSlots: {
                    List<ItemColor> result = new List<ItemColor>();
                    foreach (ItemColor color in colors) {
                        if (nears.Count(x => x.Value.IsMatchWith(color)) == 0)
                            result.Add(color);
                        else {
                            foreach (var near in nears.Where(x => x.Value.IsMatchWith(color))) {
                                if (!near.Key.IsStraight())
                                    continue;
                                Side mirrorSide = near.Key.MirrorSide();
                                if (nears.ContainsKey(mirrorSide) && nears[mirrorSide].IsMatchWith(color))
                                    goto NEXT;
                            }
                            result.Add(color);
                        }
                        NEXT:
                        continue;
                    }
                    return new ItemColor[] { result.Count > 0 ? result.GetRandom() : colors.GetRandom()};
                }
        }
        return null;
    }

    public IEnumerator GetSoundNames() {
        yield return "SwapBegin";
        yield return "SwapFailed";
    }

    internal class Move {
        //
        // A -> B
        //

        // position of start chip (A)
        public int2 from;
        // position of target chip (B)
        public int2 to;

        public Solution solution; // solution of this move
        public int potencial; // potential of this move
    }

    internal class Solution {
        //   T
        //   T
        // LLXRR  X - center of solution
        //   B
        //   B

        public int count; // count of chip combination (count = T + L + R + B + X)
        public int potential; // potential of solution
        public ItemColor color; // ID of chip color
        public List<ISlotContent> contents = new List<ISlotContent>();
        public IChip bomb;
        public Slot bombSlot;

        // center of solution
        public int x;
        public int y;

        public bool vertical = false; // is this solution is vertical?  (v = L + R + X >= 3)
        public bool horizontal = false; // is this solution is horizontal? (h = T + B + X >= 3)
        public bool square = false;

        public Slot GetCenteredSlot(Func<ISlotContent, bool> condition = null) {
            ISlotContent result = contents.FirstOrDefault(c => (condition == null || condition(c)) && lastSwaped.Contains(c.slot));
            if (result) return result.slot;
            if (square) {
                Slot slot = Slot.allActive[new int2(x, y)];
                if (condition == null || condition(slot.GetCurrentContent()))
                    return slot;
                else {
                    result = contents.Where(c => (condition == null || condition(c))).GetRandom();
                    if (!result) result = contents.GetRandom();
                    return result ? result.slot : null;
                }
            }

            if (vertical && horizontal)
                foreach (ISlotContent content in contents) {
                    if (condition != null && !condition(content)) continue;
                    Slot slot = content.slot;
                    if (contents.All(c => (condition == null || condition(c)) && (slot.x == c.slot.x || slot.y == c.slot.y)))
                        return slot;
                }

            result = contents[Mathf.FloorToInt(0.5f * contents.Count)];

            if (condition != null && !condition(result))
                result = contents.Where(condition).GetRandom();

            return result ? result.slot : null;
        }
        //public int posV; // number on right chips (posV = R)
        //public int negV; // number on left chips (negV = L)
        //public int posH; // number on top chips (posH = T)
        //public int negH; // number on bottom chips (negH = B)
    }

    [Serializable]
    public class Combination {

        #if UNITY_EDITOR
        [NonSerialized]
        static System.Random random = new System.Random();
        public int uniqueID = 0;
        public Combination() {
            uniqueID = random.Next(int.MinValue, int.MaxValue);
        }
        #endif

        public IChip bomb;
        public Type type;
        public int minCount = 4;

        public bool horizontal {
            get {
                return type == Type.Horizontal || type == Type.Cross;
            }
        }
        public bool vertical {
            get {
                return type == Type.Vertical || type == Type.Cross;
            }
        }
        public bool square {
            get {
                return type == Type.Square;
            }
        }

        public enum Type {
            Horizontal = 0,
            Vertical = 1,
            Cross = 2,
            Square = 3,
            Lined = 4,
            Any = 5
        }
    }
}

[Serializable]
public class BombMix {
    public IChip firstBomb;
    public IChip secondBomb;
    public IChipMix mix;
    public Pair<string> pair {
        get {
            return new Pair<string>(firstBomb.name, secondBomb.name);
        }
    }

    public Pair<IChip> refPair {
        get {
            return new Pair<IChip>(firstBomb, secondBomb);
        }
    }

    public class Comparer : IEqualityComparer<BombMix> {
        public bool Equals(BombMix x, BombMix y) {
            return x.pair == y.pair;
        }

        public int GetHashCode(BombMix obj) {
            return obj.pair.GetHashCode();
        }
    }
}