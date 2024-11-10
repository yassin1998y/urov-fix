using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Yurowm.GameCore;
using System.Linq;
using System.Xml.Linq;

// Base class for slots
public class Slot : ILiveContent, IAnimated {

    public static Dictionary<int2, Slot> all = new Dictionary<int2, Slot>();
    public static Dictionary<int2, Slot> allActive = new Dictionary<int2, Slot>();
    public static List<int2> interactive = new List<int2>();

    public SetSpriteColor highlight;

    // Position of this slot
    public int2 position = new int2();
    public int x { get { return position.x;} }
	public int y { get { return position.y;} }

	public Slot this[Side index] { // access to neighby slots on the index
		get {
			return nearSlot[index];
		}
	}

    public Dictionary<Side, Slot> nearSlot = new Dictionary<Side, Slot> (); // Nearby slots dictionary

    ContentAnimator animator;

    public override void Initialize() {
        base.Initialize();
        animator = GetComponent<ContentAnimator>();
        GetComponent<BoxCollider2D>().size = Vector2.one * Project.main.slot_offset;
    }

	public static void  InitializeAll (){
        foreach (Slot slot in all.Values) {
            foreach (Side side in Utils.allSides) // Filling of the nearby slots dictionary 
                slot.nearSlot.Add(side, all.ContainsKey(slot.position + side) ? all[slot.position + side] : null);
            slot.nearSlot.Add(Side.Null, null);
        }

        foreach (Slot slot in all.Values)
            slot.CulculateFallingSlot();

        CalculateActiveSlots();
        SessionInfo.current.chipPhysic.Initialize();

        interactive = null;
    }

    public static void CalculateActiveSlots() {
        area activeArea = SessionInfo.current.activeArea;
        allActive = all.Where(x => activeArea.IsItInclude(x.Key)).ToDictionary();
        all.ForEach(x => x.Value.isActiveSlot = false);
        allActive.ForEach(x => x.Value.isActiveSlot = true);
    }

    public void CulculateFallingSlot() {
        area fieldArea = SessionInfo.current.design.area;
        foreach (Side side in falling.Keys.ToArray()) {
            falling[side] = null;
            if (!all.ContainsKey(position + side)) {
                int2 coord = new int2(position);
                while (fieldArea.IsItInclude(coord)) {
                    coord += side;
                    if (all.ContainsKey(coord)) {
                        falling[side] = all[coord];
                        break;
                    }
                }
            } else if (this[side])
                falling[side] = all[position + side];
        }
    }

    public IBlock block { get; set; }

    IChip _chip = null;
    public IChip chip {
        get {
            return _chip;
        }
        set {
            if (value == null) {
                if (_chip)
                    _chip.slot = null;
                _chip = null;
                return;
            }
            if (_chip)
                _chip.slot = null;
            _chip = value;
            _chip.transform.parent = transform;
            if (_chip.slot)
                _chip.slot.chip = null;
            _chip.slot = this;
        }
    }

    public static bool IsInteractive(Slot slot) {
        if (interactive == null) return true;
        if (interactive.Count == 0) return false;
        return interactive.Contains(slot.position);
    }

    List<ISlotModifier> modifiers = new List<ISlotModifier>();

    public IEnumerable<ISlotContent> Content() {
        if (block) yield return block;
        if (chip) yield return chip;
        foreach (ISlotModifier modifier in modifiers)
            yield return modifier;
    }

    public ISlotContent GetCurrentContent() {
        if (block) return block;
        if (chip) return chip;
        return null;
    }

    public int Hit(HitContext context = null) {
        if (!isActiveSlot)
            return 0;
        OnUnpress();
        if (block)
            return block.Hit(context);

        int score = modifiers.Sum(x => x.Hit(context));

        if (chip)
            score += chip.Hit(context);

        return score;
    }

    public void HitAndScore() {
        HitAndScore(null);
    }

    public void HitAndScore(HitContext context) {
        ScoreEffect.ShowScore(transform.position, Hit(context), color);
    }

    public ItemColor color {
        get {
            if (block) {
                if (block is IColored)
                    return (block as IColored).color;
            } else if (chip) {
                if (chip is IColored)
                    return (chip as IColored).color;
            }
            return ItemColor.Unknown;
        }
    }

    public void Highlight() {
        if (color.IsPhysicalColor())
            highlight.SetColor(color, false);
        animator.Play("Highlight");
    }

    public void Unlight() {
        animator.Play("Unlight");
    }

    public Type GetCurrentType() {
        if (block) return block.GetType();
        else if (chip) return chip.GetType();
        return null;
    }

    internal void Repaint(ItemColor color) {
        if (color.IsColored()) {
            if (block) {
                if (block is IColored) 
                    ISlotContent.Repaint(block, color);
            } else if (chip && chip is IColored)
                ISlotContent.Repaint(chip, color);
        }
    }

    internal bool isPressed = false;
    public void OnPress() {
        if (isPressed)
            return;
        isPressed = true;
        if (block)
            block.OnPress();
        else if (chip)
            chip.OnPress();
    }

    public void OnUnpress() {
        if (!isPressed)
            return;
        isPressed = false;
        if (block)
            block.OnUnpress();
        else if (chip)
            chip.OnUnpress();
    }

    public static Slot HasSlot(int2 position) {
        if (all.ContainsKey(position))
            return all[position];
        return null;
    }

    public static Slot HasSlot(int x, int y) {
        return HasSlot(new int2(x, y));
    }
    
    public void DetachContent(ISlotContent item) {
        if (item is IChip) {
            if (item == _chip)
                _chip = null;
            return;
        }
        if (item is IBlock) {
            if (item == block)
                block = null;
            return;
        }
        if (item is ISlotModifier) {
            if (modifiers.Contains(item as ISlotModifier))
                modifiers.Remove(item as ISlotModifier);
            return;
        }
    }

    public void AttachContent(ISlotContent item) {
        if (item is IChip) {
            _chip = item as IChip;
            return;
        }
        if (item is IBlock) {
            block = item as IBlock;
            return;
        }
        if (item is ISlotModifier) {
            if (!modifiers.Contains(item as ISlotModifier))
                modifiers.Add(item as ISlotModifier);
            return;
        }
    }

    public bool CheckModifier(Func<ISlotModifier, bool> condition) {
        return modifiers.Contains(x => condition(x));
    }

    public bool GetModifier(Func<ISlotModifier, bool> condition) {
        return modifiers.Find(x => condition(x));
    }

    public IEnumerator GetAnimationNames() {
        yield return "Highlight";
        yield return "Unlight";
    }

    public void Serialize(XElement xml) {
        xml.Add(new XAttribute("position", position.ToString()));
        foreach (ISlotContent content in Content()) {
            if (!(content is IBigObject)) {
                XElement xContent = new XElement(content.name);
                xml.Add(xContent);
                content.Serialize(xContent);
            }
        }
    }

    public Slot slot;

    public Dictionary<Side, Slot> falling = new Dictionary<Side, Slot>();

    void  Awake (){
		slot = GetComponent<Slot>();
        falling.Add(Side.Bottom, null);
	}

    internal bool gravity = true;
    internal bool isActiveSlot = false;

    public bool GravityReaction (){
        if (!gravity) return false;	

		if (!SessionInfo.current.isPlaying) return false; 

        if (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity) return false;
		
        if (!slot || !slot.chip || slot.block) return false;

        if (ISlotContent.gravityLockers.Count > 0) return false;

		if (Mathf.Abs(transform.position.x - slot.chip.transform.position.x) > Project.main.slot_offset ||
            Mathf.Abs(transform.position.y - slot.chip.transform.position.y) > Project.main.slot_offset) return false; // Work is possible only if the chip is physically clearly in the slot

        Slot targetSlot;

        if (falling.Count == 0) return false;
        targetSlot = SessionInfo.current.chipPhysic.GetFallingSlot(this);

        if (!targetSlot) return false; 

        slot.chip.toLand = true;
        targetSlot.chip = slot.chip;
		GravityReaction();

        return true;
	}
}

public abstract class IChipPhysic {
    public static readonly Dictionary<string, IChipPhysic> physics;
    public static readonly IChipPhysic defaultPhysic;
    static IChipPhysic() {
        Type refType = typeof(IChipPhysic);

        physics = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => !x.IsAbstract && refType.IsAssignableFrom(x))
            .Select(x => (IChipPhysic) Activator.CreateInstance(x))
            .ToDictionary(x => GetName(x), x => x);
        defaultPhysic = physics.Values.FirstOrDefault(p => p is IDefault);
        if (defaultPhysic == null)
            defaultPhysic = physics.Values.FirstOrDefault();
    }

    public abstract Slot GetFallingSlot(Slot currentSlot);

    public virtual void Initialize() {}

    public static bool IsAvailableForFalling(Slot slot) {
        return slot && !slot.chip && !slot.block;
    }
    public static string GetName(IChipPhysic physic) {
        return physic.GetType().Name.NameFormat("", "ChipPhysic", true);
    }
}

public class StraightChipPhysic : IChipPhysic, IDefault {
    public override Slot GetFallingSlot(Slot currentSlot) {
        return currentSlot.falling.Values.Where(s => IsAvailableForFalling(s)).GetRandom();
    }
}

public class RealisticChipPhysic : IChipPhysic {
    List<Slot> straightSlots = new List<Slot>();
    StraightChipPhysic straightPhysic = new StraightChipPhysic();

    public override Slot GetFallingSlot(Slot currentSlot) {
        Slot target = straightPhysic.GetFallingSlot(currentSlot);
        if (target) return target;

        target = currentSlot[Side.Top];
        bool topLocked = target && target.chip && !target.block;

        if (CheckTarget(currentSlot, topLocked, Side.Left, Side.BottomLeft, out target)) return target;

        if (CheckTarget(currentSlot, topLocked, Side.Right, Side.BottomRight, out target)) return target;

        return null;
    }

    public override void Initialize() {
        Project.onPlayingModeChanged.AddListener(OnStartFalling);
    }

    bool CheckTarget(Slot slot, bool topLocked, Side lockerSide, Side targetSide, out Slot target) {
        target = slot[targetSide];
        return target && ((slot[lockerSide] && slot[lockerSide].block) || (!topLocked && !straightSlots.Contains(target))) && IsAvailableForFalling(target);
    }

    List<Slot> closed = new List<Slot>();
    List<Slot> opened = new List<Slot>();
    void OnStartFalling(PlayingMode mode) {
        if (mode != PlayingMode.Gravity) return;

        closed.Clear();
        opened.Clear();
        straightSlots.Clear();
        opened.AddRange(Slot.all.Values.Where(s => !s.block && s.CheckModifier(GeneratorChecker)));
        straightSlots.AddRange(opened);

        while (opened.Count > 0) {
            Slot current = opened[0];
            opened.RemoveAt(0);
            closed.Add(current);
            foreach (Slot f in current.falling.Values) {
                if (f && !f.block && !straightSlots.Contains(f) && !opened.Contains(f) && !closed.Contains(f)) {
                    straightSlots.Add(f);
                    opened.Add(f);
                }
            }
        }
    }

    bool GeneratorChecker(ISlotModifier modifier) {
        return modifier.GetType().Name.Contains("SlotGenerator");
    }
}