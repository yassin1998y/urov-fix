using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class MatchClickRule : LevelRule {

    [HideInInspector]
    public List<Combination> combinations = new List<Combination>();
    [HideInInspector]
    public List<BombMix> bombMixes = new List<BombMix>();
    [HideInInspector]
    public float hint_delay = 10;
    bool mixTrick = false;

    public override ItemColor[] ColorGeneration(ItemColor[] colors, Dictionary<Side, ItemColor> nears, GenerationType type) {
        switch (type) {
            case GenerationType.AllUnkonwn:
                return colors.Where(c => !nears.Contains(x => x.Key.IsStraight() && x.Value.IsMatchWith(c))).ToArray();
            case GenerationType.EvenFinal:
                return null;
            case GenerationType.OddSlots:
            case GenerationType.EvenSlots:
                return new ItemColor[] { colors.Length > 0 ? colors.GetRandom() : nears.Values.Distinct().ToList().GetRandom() };
        }
        return null;
    }
    
    internal override IEnumerator SessionModes(PlayingMode playingMode) {
        switch (playingMode) {
            case PlayingMode.Wait: {
                    ShowHelpers();
                } break;
            case PlayingMode.Matching: {
                    HideHelpers();
                    if (!matching && ISlotContent.gravityLockers.Count == 0)
                        yield return PlayingMode.Reaction;

                    yield return new WaitWithDelay(() => 
                        !matching && ISlotContent.gravityLockers.Count == 0, .1f);

                    yield return PlayingMode.Gravity;
                } break;
            case PlayingMode.Gravity: {
                    yield return 0;
                    if (mixTrick)
                        Feedback.Play("Mix");
                    mixTrick = false;
                }
                break;
        }
    }

    RaycastHit2D hit;
    internal override void Control(bool isBegan, bool isPress, bool IsOverUI, Vector2 point) {
        if (isBegan) {
            if (IsOverUI)
                return;
            hit = Physics2D.Raycast(point, Vector2.zero);
            if (!hit.transform)
                return;
            Slot slot = hit.transform.GetComponent<Slot>();
            if (slot)
                StartCoroutine(Match(slot));
        }
    }

    bool matching = false;
    IEnumerator Match(Slot slot) {
        if (GetMode() != PlayingMode.Wait)
            yield break;

        if (!Slot.IsInteractive(slot))
            yield break;

        if (slot.GetCurrentContent() is IBomb) {
            IChip bomb = slot.GetCurrentContent() as IChip;
            Dictionary<Pair<IChip>, IChip> pairs = slot.nearSlot.Where(x => x.Value && x.Key.IsStraight() && x.Value.GetCurrentContent() is IChip)
                .Select(x => x.Value.chip).GroupBy(x => new Pair<IChip>(bomb.original.GetComponent<IChip>(),
                    x.original.GetComponent<IChip>()))
                .ToDictionary(x => x.Key, x => x.First());

            BombMix mix = bombMixes.FirstOrDefault(a => pairs.ContainsKey(a.refPair));

            if (mix != null) {
                matching = true;
                SetMode(PlayingMode.Matching);
                SessionInfo.current.BurnMove();

                IChip second = pairs[mix.refPair];
                IChip first = bomb;

                float progress = 0;
                float time = 0;
                float duration = .2f;
                mixTrick = true;

                while (progress < duration) {
                    time = EasingFunctions.easeInOutQuad(progress / duration);
                    second.transform.position = Vector3.Lerp(second.slot.transform.position,
                            first.slot.transform.position, time);
                    progress += Time.deltaTime;
                    yield return 0;
                }

                IChipMix chipMix = Content.Emit(mix.mix) as IChipMix;
                chipMix.transform.position = second.transform.position;
                chipMix.Prepare(second, first);
                HitContext mixContext = new HitContext(new Slot[] { first.slot, second.slot }, HitReason.Matching);
                chipMix.Activate();
                matchDate++;
                second.slot.DetachContent(second); first.slot.DetachContent(first);
                second.slot.Hit(mixContext); first.slot.Hit(mixContext);
                second.Hide(); first.Hide();
                do yield return 0; 
                while (chipMix != null);
                matching = false;
            } else {
                SessionInfo.current.BurnMove();
                slot.HitAndScore(new HitContext(HitReason.Matching));
                SetMode(PlayingMode.Matching);
            }
            yield break;
        }

        List<Slot> area = Matcher(new List<Slot>(), slot);

        if (area.Count < 2)
            yield break;

        matching = true;
        SetMode(PlayingMode.Matching);

        HitContext context = new HitContext(area, HitReason.Matching);
        matchDate++;
        ScoreEffect.ShowScore(slot.transform.position, area.Sum(x => x.Hit(context)), slot.color);

        List<ISlotContent> content = area.Select(x => x.GetCurrentContent()).Where(x => x is IChip && !(x is IBomb)).ToList();
        if (content.Count > 0) {
            var currentContent = slot.GetCurrentContent();
            if (!content.Contains(currentContent))
                slot = content.GetRandom().slot;
        }

        IChip.Explode(slot.transform.position, 3, -20 * content.Count);

        float t = 0;
        while (t <= 1) {
            foreach (ISlotContent chip in content)
                if (chip) chip.transform.position = Vector3.Lerp(chip.slot.transform.position, slot.transform.position, t);
            t += Time.deltaTime * 7;
            yield return 0;
        }

        if (content.Count > 0) {
            List<ISlotContent> allContent = area.Select(x => x.GetCurrentContent()).ToList();
            foreach (Combination combination in combinations) {
                if (combination.IsSuitable(allContent)) {
                    FieldAssistant.main.Add(combination.bomb, slot, content[0].colored.color);
                    break;
                }
            }
        }

        foreach (ISlotContent chip in content)
            if (chip) chip.transform.position = slot.transform.position;

        SessionInfo.current.BurnMove();
        matching = false;
    }

    List<List<Slot>> FindMoves() {
        List<List<Slot>> result = new List<List<Slot>>();
        List<Slot> pool = Slot.allActive.Select(x => x.Value.GetCurrentContent())
            .Where(x => x && x.colored != null)
            .Select(x => x.slot).ToList();

        while (pool.Count > 0) {
            Slot first = pool.First();
            List<Slot> match = Matcher(new List<Slot>(), first);
            pool.RemoveAll(x => match.Contains(x));
            if (match.Count > 1)
                result.Add(match);
        }

        return result;
    }

    List<Slot> Matcher(List<Slot> all, Slot first) {
        IColored colored = first.GetCurrentContent() as IColored;
        ItemColor color;
        if (colored != null) color = colored.color;
        else return all; 

        all.Add(first);

        foreach (var s in first.nearSlot) {
            if (!s.Key.IsStraight())
                continue;
            if (s.Value == null || !s.Value.isActiveSlot || all.Contains(s.Value))
                continue;
            colored = s.Value.GetCurrentContent() as IColored;
            if (colored != null && colored.color.IsMatchWith(color))
                all = Matcher(all, s.Value);
        }
        return all;
    }

    void ShowHelpers() {
        List<List<ISlotContent>> moves = FindMoves()
            .Select(x => x.Select(y => y.GetCurrentContent()).ToList()).ToList();
        Texture2D helper;
        foreach (var move in moves) {
            helper = null;
            foreach (var combination in combinations)
                if (combination.IsSuitable(move)) {
                    helper = combination.helper;
                    break;
                }
            if (helper != null)
                move.ForEach(x => ShowHelper(x, helper));
        }
    }

    void HideHelpers() {
        GetAll<ISlotContent>().ForEach(x => ShowHelper(x, null));
    }

    void ShowHelper(ISlotContent chip, Texture2D texture) {
        chip.GetComponentsInChildren<SpriteHelper>().ForEach(x => x.SetTeture(texture));
    }

    internal override bool IsThereAnyMoves() {
        foreach (Slot slot in Slot.allActive.Values)
            if (slot.color.IsMatchableColor())
                foreach (Side side in Utils.straightSides)
                    if (slot[side] && slot.color.IsMatchWith(slot[side].color))
                        return true;
        return Contains<ISlotContent>(x => x is IBomb);
    }

    internal override bool IsThereAnySolutions() {
        return false;
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
        public int minCount = 4;
        public int minVCount = 1;
        public int minHCount = 1;
        public Texture2D helper;
        public bool vert = false;

        public bool IsSuitable(List<ISlotContent> content) {
            int xmin = 100;
            int ymin = 100;
            int xmax = -100;
            int ymax = -100;
            content.ForEach(x => {
                xmin = Mathf.Min(xmin, x.slot.x);
                ymin = Mathf.Min(ymin, x.slot.y);
                xmax = Mathf.Max(xmax, x.slot.x);
                ymax = Mathf.Max(ymax, x.slot.y);
            });

            int horiz = xmax - xmin;
            int vert = ymax - ymin;

            return minCount <= content.Count &&
                minVCount <= vert && minHCount <= horiz &&
                !(minVCount != minHCount && this.vert != vert >= horiz);
        }
    }
}
