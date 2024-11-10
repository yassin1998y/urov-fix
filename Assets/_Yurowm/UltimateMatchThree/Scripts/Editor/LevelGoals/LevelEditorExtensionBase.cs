using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Yurowm.GameCore;
using System.Reflection;
using Yurowm.EditorCore;

public abstract class LevelEditorExtension {
    public abstract bool IsCompatibleWith(ILiveContent content);

    public static List<LevelEditorExtension> GenerateEditors() {
        Type refType = typeof(LevelEditorExtension);
        return refType.Assembly.GetTypes()
            .Where(x => !x.IsAbstract && refType.IsAssignableFrom(x))
            .Select(x => (LevelEditorExtension) Activator.CreateInstance(x))
            .ToList();
    }
}

public abstract class GoalEditorExtension : LevelEditorExtension {

    public void Draw(LevelDesign design, LevelGoalInfo info) {
        Context context = new Context();
        context.design = design;
        context.info = info;
        OnModeEditorGUI(context);
    }

    protected abstract void OnModeEditorGUI(Context context);

    protected struct Context {
        public LevelDesign design;
        public LevelGoalInfo info;
    }
}

public abstract class LevelExtensionEditorExtension : LevelEditorExtension {

    public readonly bool slotEditor = false;
    public readonly bool levelParameterEditor = false;

    public LevelExtensionEditorExtension() {
        Type type = GetType();
        var eFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        slotEditor = type.GetMethod("DrawSlotParameterGUI", eFlags).DeclaringType == type;
        levelParameterEditor = type.GetMethod("DrawLevelParameterGUI", eFlags).DeclaringType == type;
    }

    public void DrawLevelParameter(LevelDesign design, ILevelExtension.LevelExtensionInfo info) {
        if (!levelParameterEditor) return;

        Context context = new Context();
        context.design = design;
        context.info = info;
        DrawLevelParameterGUI(context);
    }

    public void DrawSlotParameter(LevelDesign design, Dictionary<int2, SlotSettings> slots, List<int2> selection, ILevelExtension.LevelExtensionInfo info) {
        if (!slotEditor) return;
        Context context = new Context();
        context.design = design;
        context.info = info;
        context.selection = selection;
        context.slots = slots;
        DrawSlotParameterGUI(context);
    }

    public void DrawSlotIcon(Rect rect, int2 coord, LevelDesign design, Dictionary<int2, SlotSettings> slots, ILevelExtension.LevelExtensionInfo info) {
        if (!slotEditor) return;

        Context context = new Context();
        context.design = design;
        context.info = info;
        context.slots = slots;

        DrawSlotIconGUI(rect, coord, context);
    }

    protected virtual void DrawLevelParameterGUI(Context context) {}

    protected virtual void DrawSlotParameterGUI(Context context) {}

    protected virtual void DrawSlotIconGUI(Rect rect, int2 coord, Context context) {}

    protected struct Context {
        public LevelDesign design;
        public ILevelExtension.LevelExtensionInfo info;
        public Dictionary<int2, SlotSettings> slots;
        public List<int2> selection;
    }
}

public abstract class SlotEditorExtension : LevelEditorExtension {

    public void Draw(LevelDesign design, Dictionary<int2, SlotContent> content) {
        OnSlotEditorGUI(design, content);
    }

    public virtual void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> content) { }

    public virtual void DrawSlotIcon(Rect rect, SlotContent content) {}
}