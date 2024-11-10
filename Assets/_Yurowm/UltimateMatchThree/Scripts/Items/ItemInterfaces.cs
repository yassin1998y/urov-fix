using UnityEngine;
using Yurowm.GameCore;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Xml.Linq;

public abstract class IBlock : ISlotContent {
    abstract public bool CanItContainChip();
}

public abstract class ISlotModifier : ISlotContent {

}

public abstract class ILevelExtension : ILiveContent {

    public LevelExtensionInfo info;
    public abstract void Setup(LevelExtensionInfo info);

    [Serializable]
    public class LevelExtensionInfo : IEnumerable<SlotExtension> {
        public ILevelExtension prefab;

        public List<LevelParameter> levelParameters = new List<LevelParameter>();
        [SerializeField]
        List<SlotExtension> slotParameters = new List<SlotExtension>();

        public LevelExtensionInfo(ILevelExtension prefab) {
            this.prefab = prefab;
        }

        public SlotExtension GetSlot(int2 coord) {
            return slotParameters.FirstOrDefault(x => x.coord == coord);
        }

        Dictionary<int2, SlotExtension> _slotDictionary = null;

        public SlotExtension this[int2 coord, bool createIfNeeded = false] {
            get {
                if (_slotDictionary == null) {
                    slotParameters = slotParameters.GroupBy(x => x.coord).Select(x => x.First()).ToList();
                    _slotDictionary = slotParameters.ToDictionary(x => x.coord, x => x);
                }
                if (coord == null)
                    return null;
                if (_slotDictionary.ContainsKey(coord)) return _slotDictionary[coord];
                if (!createIfNeeded) return null;
                SlotExtension newSlot = new SlotExtension(coord.GetClone());
                slotParameters.Add(newSlot);
                _slotDictionary.Add(newSlot.coord, newSlot);
                return newSlot;
            }

            set {
                if (value == null && _slotDictionary.ContainsKey(coord)) {
                    slotParameters.Remove(_slotDictionary[coord]);
                    _slotDictionary.Remove(coord);
                    return;
                }
                if (value.coord == coord) {
                    if (_slotDictionary.ContainsKey(coord))
                        this[coord] = null;
                    slotParameters.Add(value);
                    _slotDictionary.Add(value.coord, value);
                }
            }
        }

        public bool ContainsSlot(Func<SlotExtension, bool> match) {
            return _slotDictionary.Values.Contains(x => match(x));
        }

        public SlotExtension FindSlot(Func<SlotExtension, bool> match) {
            return _slotDictionary.Values.FirstOrDefault(x => match(x));
        }

        public void ClearSlots() {
            slotParameters.Clear();
            if (_slotDictionary != null) _slotDictionary.Clear();
        }

        public IEnumerator<SlotExtension> GetEnumerator() {
            foreach (SlotExtension slot in slotParameters)
                yield return slot;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public XElement Serialize(string name) {
            XElement xml = new XElement(name);
            if (!prefab) return xml;
            xml.Add(new XAttribute("type", prefab.name));
            foreach (SlotExtension slot in slotParameters) {
                XElement element = new XElement("slot");
                slot.Serialize(element);
                xml.Add(element);
            }
            foreach (LevelParameter level in levelParameters) {
                XElement element = new XElement("level");
                level.Serialize(element);
                xml.Add(element);
            }
            return xml;
        }

        public static LevelExtensionInfo CteateAndDeserialize(XElement xml) {
            var a = xml.Attribute("type");
            if (a == null) return null;
            LevelExtensionInfo result = new LevelExtensionInfo(Content.GetPrefab<ILevelExtension>(a.Value));
            foreach (XElement e in xml.Elements("slot"))
                result.slotParameters.Add(SlotExtension.Deserialize(e));
            foreach (XElement e in xml.Elements("level"))
                result.levelParameters.Add(LevelParameter.Deserialize(e));
            return result;
        }

        public LevelExtensionInfo Clone() {
            LevelExtensionInfo clone = (LevelExtensionInfo) MemberwiseClone();
            clone.slotParameters = slotParameters.Select(x => x.Clone()).ToList();
            clone.levelParameters = levelParameters.Select(x => x.Clone()).ToList();
            return clone;
        }

        internal void Deserialize(XElement xml) {
            XElement info = xml.Element("info");
            foreach (XElement element in info.Elements("slot"))
                slotParameters.Add(SlotExtension.Deserialize(element));
            foreach (XElement element in info.Elements("level"))
                levelParameters.Add(LevelParameter.Deserialize(element));
        }

        internal void Serialize(XElement xml) {
            XElement info = new XElement("info");
            xml.Add(info);
            foreach (SlotExtension slot in slotParameters) {
                XElement element = new XElement("slot");
                slot.Serialize(element);
                info.Add(element);
            }
            foreach (LevelParameter level in levelParameters) {
                XElement element = new XElement("level");
                level.Serialize(element);
                info.Add(element);
            }
        }

        public void Refresh() {
            _slotDictionary = null;
        }
    }

    public virtual void Deserialize(XElement xml, LevelExtensionInfo info) {
        info.Deserialize(xml);
    }

    public virtual void Serialize(XElement xml) {
        info.Serialize(xml);
    }
}

public class HitContext {
    internal Slot[] group = null;
    internal HitReason reason = HitReason.Unknown;

    public HitContext(HitReason reason = HitReason.Unknown) {
        this.reason = reason;
    }
    public HitContext(Slot[] group, HitReason reason = HitReason.Unknown) : this(reason) {
        this.group = group;
        Project.onHitSolution.Invoke(this);
    }
    public HitContext(IEnumerable<Slot> group, HitReason reason = HitReason.Unknown) : this(reason) {
        this.group = group.ToArray();
        Project.onHitSolution.Invoke(this);
    }
    public HitContext(Slot slot, HitReason reason = HitReason.Unknown) : this(reason) {
        group = new Slot[] { slot };
        Project.onHitSolution.Invoke(this);
    }
}

public abstract class ISlotContent : ILiveContent, ISounded, IAnimated {

    public string shortName;
    internal int birthDate = -1;
    internal Slot slot;
    internal ContentAnimator animator;
    internal ContentSound sound;

    internal bool isActiveContent {
        get {
            return slot.isActiveSlot;
        }
    }

    [ContentSelector]
    public IEffect destroyingEffect;

    internal HitContext context = null;

    public ILayered layered {
        get {
            return this as ILayered;
        }
    }
    public IDestroyable destroyable {
        get {
            return this as IDestroyable;
        }
    }
    public IColored colored {
        get {
            return this as IColored;
        }
    }

    internal static List<ILiveContent> gravityLockers = new List<ILiveContent>();

    public override void Initialize() {
        base.Initialize();

        animator = GetComponent<ContentAnimator>();
        sound = GetComponent<ContentSound>();

        if (animator) animator.Play("Awake");
        if (sound) sound.Play("Awake");
    }

    virtual public void OnPress() {
        if (animator) animator.Play("Press");
        if (sound) sound.Play("Press");
    }
    virtual public void OnUnpress() {
        if (animator) animator.Play("Unpress");
        if (sound) sound.Play("Unpress");
    }
        
    public virtual void OnStartDestroying() {}
    public virtual void OnEndDestroying() {}

    public virtual int Hit(HitContext context) {
        if (!isActiveContent)
            return 0;

        if (birthDate >= SessionInfo.current.rule.matchDate)
            return 0;

        if (destroyable != null && destroying)
            return 0;

        this.context = context;

        if (layered != null) {
            layered.layer--;
            if (layered.layer > 0) {
                layered.OnChangeLayer(layered.layer);
                return layered.destroyLayerReward;
            }
        }

        if (destroyable != null) {
            StartCoroutine(_Destroying());
            return destroyable.destroyReward;
        }

        return 0;
    }

    public override void OnKill() {
        gravityLockers.Remove(this);
        base.OnKill();
    }

    public void Hide() {
        _destroying = true;
        StopAllCoroutines();
        Kill();
    }

    bool _destroying = false;

    public bool destroying {
        get {
            return _destroying;
        }
    }

    IEnumerator _Destroying() {
        if (!(this is IDestroyable)) yield break;
        IDestroyable destroyable = this as IDestroyable;

        if (_destroying) yield break;

        if (slot.isPressed)
            slot.OnUnpress();

        _destroying = true;

        Project.onChipCrush.Invoke();

        OnStartDestroying();

        yield return 0;

        Project.onSlotContentPrepareToDestroy.Invoke(this);

        gravityLockers.Add(this);

        if (destroyingEffect) {
            IEffect effect = Content.Emit(destroyingEffect);
            if (effect) {
                effect.transform.position = transform.position;
                if (this is IColored)
                    effect.Repaint((this as IColored).color);
                OnCreateDestroyingEffect(effect);
                effect.Play();
            }
        }

        yield return new WaitForSeconds(0.05f);

        yield return StartCoroutine(destroyable.Destroying());

        gravityLockers.Remove(this);

        OnEndDestroying();
        
        if (slot)
            slot.DetachContent(this);

        Project.onSlotContentDestroyed.Invoke(this);
        Project.onSomeContentDestroyed.Invoke();

        Kill();
    }

    public virtual IEnumerator GetAnimationNames() {
        yield return "Awake";
        if (this is IChip)
            yield return "Landing";
        yield return "Flashing";
        if (this is IDestroyable) {
            yield return "Destroying";
            if (this is ILayered)
                yield return "LayerDown";
        }
        if (colored != null && colored.color.IsColored()) {
            yield return "Unpress";
            yield return "Press";
            yield return "Pressed";
        }
    }

    public virtual IEnumerator GetSoundNames() {
        yield return "Awake";
        if (this is IChip)
            yield return "Landing";
        yield return "Flashing";
        if (this is IDestroyable) {
            yield return "Destroying";
            if (this is ILayered)
                yield return "LayerDown";
        }
        if (colored != null && colored.color.IsColored()) {
            yield return "Unpress";
            yield return "Press";
        }
    }

    public virtual void OnCreateDestroyingEffect(IEffect effect) {}

    public static void Repaint(ISlotContent coloredObject, ItemColor color) {
        if (coloredObject is IColored) {
            IColored colored = coloredObject as IColored;
            colored.color = color;
            foreach (var sprite in coloredObject.GetComponentsInChildren<SetSpriteColor>(true))
                sprite.SetColor(color, false);
        }
    }

    public void Setup(Slot slot) {
        transform.SetParent(slot.transform);
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;
        this.slot = slot;
        slot.AttachContent(this);
    }

    public abstract void Serialize(XElement xContent);

    public abstract void Deserialize(XElement xContent, SlotContent slotContent);
}


public abstract class IBigModifier : ISlotModifier, IBigObject {
    public Color editorColor = Color.white;
    public Color GetEditorColor() {
        return editorColor;
    }

    List<Slot> slots = new List<Slot>();
    public List<Slot> GetSlots() {
        return slots;
    }

    public void BigSetup(Slot slot) {
        Setup(slot);
        BigObjectHelper.Setup(slot, slots, this);
        
    }

    public virtual IEnumerator<int2> Shape() {
        return BigObjectHelper.DefaultShape();
    }
}

public abstract class IBigBlock : IBlock, IBigObject {
    public Color editorColor = Color.white;
    public Color GetEditorColor() {
        return editorColor;
    }

    List<Slot> slots = new List<Slot>();
    public List<Slot> GetSlots() {
        return slots;
    }

    public void BigSetup(Slot slot) {
        Setup(slot);
        BigObjectHelper.Setup(slot, slots, this);
        
    }

    public virtual IEnumerator<int2> Shape() {
        return BigObjectHelper.DefaultShape();
    }
}

public static class BigObjectHelper {
    public static void Setup(Slot slot, List<Slot> slots, ISlotContent obj) {
        if (obj is IBigObject) {
            var shape = (obj as IBigObject).Shape();
            bool centered = false;
            while (shape.MoveNext()) {
                if (shape.Current == int2.zero)
                    centered = true;
                Slot s = Slot.all.Get(shape.Current + slot.position);
                if (!s)
                    throw new NullReferenceException("The big object is located in the place without slot");
                if (!slots.Contains(s)) {
                    s.AttachContent(obj);
                    slots.Add(s);
                }
            }
            if (!centered)
                throw new Exception("The shape is not centered. It must contain a zero coordinate");
        }
    }

    public static IEnumerator<int2> DefaultShape() {
        yield return new int2(0, 0);
        yield return new int2(1, 0);
        yield return new int2(0, 1);
        yield return new int2(1, 1);
    }
}

public enum HitReason { Unknown, Matching, BombExplosion, Reaction }

/// <summary>
/// For items which can be layered. When it is hit, it destroys only one layer, but not whole item. This kind of item can be destroyed only by destroying all layers.
/// </summary>
public interface ILayered {
    int GetLayerCount();
    int layer { get; set; }
    void OnChangeLayer(int layer);
    int destroyLayerReward { get; }
}

/// <summary>
/// Does it have a color? Colored item can be matched, using current level rule.
/// </summary>
public interface IColored {
    ItemColor color { get; set; }
}

/// <summary>
/// This kind of items can be destroyed by matching or by bomb.
/// </summary>
public interface IDestroyable {
    IEnumerator Destroying();
    int destroyReward { get; }
    bool destroying { get; }
}

/// <summary>
/// This kind of CHIPs participates in the shuffling
/// </summary>
public interface IShuffled {
}

/// <summary>
/// Special kind of chips which is destroyed by mixing 
/// </summary>
public interface IMixable {
    int GetMixingLogicPriority();
    IEnumerator Mixing(IChip secondChip);
}

public interface IBomb : IDestroyable {
    void Explode();
}

public interface IBigObject {
    List<Slot> GetSlots();
    void BigSetup(Slot slot);
    IEnumerator<int2> Shape();
    Color GetEditorColor();
}

public interface IGoalExclusive {
    bool IsCompatibleWithGoal(ILevelGoal mode);
}

public interface ILevelRuleExclusive {
    bool IsCompatibleWith(LevelRule rule);
}

public interface IDefault {
}

public interface IDefaultSlotContent {
    bool CanBeSetInNewSlot(LevelDesign design, SlotSettings slot);
}

public interface INeedToBeSetup {
    void OnSetupByContentInfo(Slot slot, SlotContent info);
    void OnSetup(Slot slot);
}

public interface IAnimated {
    IEnumerator GetAnimationNames();
}

public interface ISounded {
    IEnumerator GetSoundNames();
}

public interface IReactionProvider {
    Func<IEnumerator> GetReactorLogic();
    void Serialize(XElement xml);
    void Deserizalie(XElement xml);
}

public enum DeepLevelDirection {Down, Up};
public interface IDeepLevelGoal {
    DeepLevelDirection GetDirection();
    int ChangeDeepIndex();
}

public abstract class Reaction {

    public abstract int GetPriority();

    public abstract IEnumerator React();

    public abstract ReactionType GetReactionType();

    public virtual bool IsSuitable() {
        return true;
    }

    public static List<Reaction> GetReactions() {
        Type refType = typeof(Reaction);
        
        List<Reaction> result = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => !x.IsAbstract && refType.IsAssignableFrom(x))
            .Select(x => (Reaction) Activator.CreateInstance(x))
            .Where(x => x.IsSuitable())
            .ToList();

        result.Sort((x, y) => x.GetPriority().CompareTo(y.GetPriority()));

        return result;
    }
}

public enum ReactionResult { Gravity, GravityAndRepeate, Complete }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class SlotTagRendererAttribute : Attribute {
    int priority;
    char symbol;
    Color color;
    Color background;
    const string conditionMethodName = "IsTagVisible";
    MethodInfo conditionMethod;

    /// <summary>
    /// Attribure for showing slot tag-label in the Level Editor. It works only in the Unity Editor and have not any sense in the compiled game.
    /// It also requier the condition function. It must be STATIC function with "IsTagVisible" name, one SlotSettings parameter, and BOOLEAN return value.
    /// Example:
    /// public static bool IsTagVisible(SlotSettings slot) {
    ///    return slot.generator != null;
    /// }
    /// </summary>
    /// <param name="symbol">Symbol which will be show in the slots tag</param>
    /// <param name="color">Color of the symbol</param>
    /// <param name="background">Color of the tags background</param>
    /// <param name="priority">Tags with bigger priority shows earlier.</param>
    public SlotTagRendererAttribute(char symbol, ConsoleColor color = ConsoleColor.White, ConsoleColor background = ConsoleColor.Black, int priority = 0) {
        this.symbol = symbol;
        this.priority = priority;
        this.color = GetColor(color);
        this.background = GetColor(background);
    }

    void InitializeMethod(Type type) {
        conditionMethod = type.GetMethods()
            .Where(x => x.IsStatic && x.ReturnType == typeof(bool) && x.Name == conditionMethodName
                 && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(SlotSettings))
                            .FirstOrDefault();
    }

    public int GetPriority() {
        return priority;
    }

    static Color GetColor(ConsoleColor color) {
        switch (color) {
            case ConsoleColor.Black: return Color.black;
            case ConsoleColor.Blue: return Color.blue;
            case ConsoleColor.Cyan: return Color.cyan;
            case ConsoleColor.DarkBlue: return new Color(0, 0, .5f, 1);
            case ConsoleColor.DarkCyan: return new Color(0, .5f, .5f, 1);
            case ConsoleColor.DarkGray: return new Color(.25f, .25f, .25f, 1);
            case ConsoleColor.DarkGreen: return new Color(0, .5f, 0, 1);
            case ConsoleColor.DarkMagenta: return new Color(.5f, 0, .5f, 1);
            case ConsoleColor.DarkRed: return new Color(.5f, 0, 0, 1);
            case ConsoleColor.DarkYellow: return new Color(.5f, .46f, .008f, 1);
            case ConsoleColor.Gray: return Color.gray;
            case ConsoleColor.Green: return Color.green;
            case ConsoleColor.Magenta: return Color.magenta;
            case ConsoleColor.Red: return Color.red;
            case ConsoleColor.White: return Color.white;
            case ConsoleColor.Yellow: return Color.yellow;
        }
        return Color.clear;
    }

    public Color GetSymbolColor() {
        return color;
    }

    public Color GetBackgroundColor() {
        return background;
    }

    public char GetSymbol() {
        return symbol;
    }

    public bool Condition(SlotSettings slot) {
        if (conditionMethod == null)
            return false;
        return (bool) conditionMethod.Invoke(null, new object[] { slot });
    }

    public static List<SlotTagRendererAttribute> Extract() {
        List<SlotTagRendererAttribute> result = new List<SlotTagRendererAttribute>();
        List<Type> typesWithAttribute = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetCustomAttributes(false).Contains(y => y is SlotTagRendererAttribute))
            .ToList();
        foreach (Type type in typesWithAttribute) {
            SlotTagRendererAttribute attribute = (SlotTagRendererAttribute) type.GetCustomAttributes(false)
                .First(x => x is SlotTagRendererAttribute);
            attribute.InitializeMethod(type);
            result.Add(attribute);
        }

        result.Sort((x, y) => y.GetPriority().CompareTo(x.GetPriority()));
        return result;
    }
}