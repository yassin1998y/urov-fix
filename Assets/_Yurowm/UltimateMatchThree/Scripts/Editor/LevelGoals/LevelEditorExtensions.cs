using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.EditorCore;
using Yurowm.GameCore;
using UnityEditor;
using UnityEngine;
using UnityEditor.AnimatedValues;

public class GeneratorEditor : SlotEditorExtension {
    public override bool IsCompatibleWith(ILiveContent content) {
        return content is SlotGenerator;
    }

    public override void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> content) {
        if (SlotGenerator.generatedChips.Count > 0) {
            #region Chips with attributed probability
            foreach (var pair in SlotGenerator.probabilityPrefabs) {
                if (!LevelEditor.ValidateContent(pair.Key, design)) continue;
                string variable = string.Format(GeneratedWithProbability.keyFormat, pair.Key.name);
                string title = pair.Key.name + " Generating";
                EUtils.DrawMixedProperty(content.Keys,
                    mask: coord => true,
                    getValue: coord => content[coord][variable].Float,
                    setValue: (coord, value) => content[coord][variable].Float = value,
                    drawSingle: (position, value) => EditorGUILayout.Slider(title, value, 0, 1),
                    drawMixed: setDefault => {
                        float value = EditorGUILayout.Slider(title, -1, -1, 1);
                        if (value == -1) return false;
                        setDefault(value);
                        return true;
                    });
            }
            #endregion

            #region Chips with attributed condition
            foreach (var pair in SlotGenerator.conditionPrefabs) {
                if (!LevelEditor.ValidateContent(pair.Key, design)) continue;
                string key = string.Format(GeneratedWithCondition.keyFormat, pair.Key.name);
                string title = pair.Key.name + " Generating";
                EUtils.DrawMixedProperty(content.Keys,
                    mask: coord => true,
                    getValue: coord => content[coord][key].Bool,
                    setValue: (coord, value) => content[coord][key].Bool = value,
                    drawSingle: (position, value) => EditorGUILayout.Toggle(title, value),
                    drawMixed: setDefault => {
                        if (!EditorGUILayout.Toggle(title, false)) return false;
                        setDefault(true);
                        return true;
                    });
            }
            #endregion
        }
    }
}

public class ColoredEditor : SlotEditorExtension {
    protected Dictionary<string, IColored> content;

    public ColoredEditor() : base() {
        content = Content.GetPrefabList<ISlotContent>(x => x is IColored)
            .ToDictionary(x => x.name, x => x as IColored);
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is IColored;
    }

    public override void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> slots) {
        EUtils.DrawMixedProperty(slots.Keys,
            mask: coord => true,
            getValue: coord => slots[coord]["color"].ItemColor,
            setValue: (coord, value) => slots[coord]["color"].ItemColor = value,
            drawSingle: (position, value) => DrawSingle(design, value),
            drawMixed: setDefault => {
                ItemColor color = DrawSingle(design, ItemColor.Uncolored);
                if (!color.IsColored()) return false;
                setDefault(color);
                return true;
            });
        
    }

    ItemColor DrawSingle(LevelDesign design, ItemColor selected) {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (Event.current.type == EventType.Layout) return selected;

        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent("Color group"));
        rect.xMin = rect2.x;

        Rect buttonRect = new Rect(rect);
        buttonRect.width /= design.colorCount + 1;

        GUIStyle style;

        if ((GUI.Toggle(buttonRect, selected == ItemColor.Unknown, "X", EditorStyles.miniButtonLeft)) != (selected == ItemColor.Unknown))
            selected = ItemColor.Unknown;

        buttonRect.x += buttonRect.width;

        for (int i = 0; i < design.colorCount; i++) {
            if (i == design.colorCount - 1) style = EditorStyles.miniButtonRight;
            else style = EditorStyles.miniButtonMid;

            using (new GUIHelper.BackgroundColor(RealColors.Get((ItemColor) i))) {
                if ((GUI.Toggle(buttonRect, selected == (ItemColor) i, "", style)) != (selected == (ItemColor) i))
                    selected = (ItemColor) i;
                buttonRect.x += buttonRect.width;
            }
        }

        return selected;
    }
}

public class LayeredEditor : SlotEditorExtension {
    protected Dictionary<string, ILayered> content;

    public LayeredEditor() : base() {
        content = Content.GetPrefabList<ISlotContent>(x => x is ILayered)
            .ToDictionary(x => x.name, x => x as ILayered);
    }

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is ILayered;
    }

    public override void OnSlotEditorGUI(LevelDesign design, Dictionary<int2, SlotContent> slots) {
        List<int2> editable = slots.Where(x => content.ContainsKey(x.Value.name)).Select(x => x.Key).ToList();

        if (editable.Count > 0) {
            int max = slots.Where(x => content.ContainsKey(x.Value.name)).
                Min(x => (content[slots[x.Key].name] as ILayered).GetLayerCount());
            EUtils.DrawMixedProperty(editable,
                mask: coord => true,
                getValue: coord => slots[coord]["layer"].Int,
                setValue: (coord, value) => slots[coord]["layer"].Int = value,
                drawSingle: (position, value) => Mathf.RoundToInt(EditorGUILayout.Slider("Layer", value, 1, max)),
                drawMixed: setDefault => {
                    EditorGUI.BeginChangeCheck();
                    float layer = EditorGUILayout.Slider("Layer", 0, 1, max);
                    if (EditorGUI.EndChangeCheck()) {
                        setDefault(Mathf.RoundToInt(layer));
                        return true;
                    }
                    return false;
                });
        
        }
    }
}

public class WallEditor : LevelExtensionEditorExtension {

    static readonly Color wallIconColor = new Color(.7f, .5f, .2f, .7f);
    static readonly Color wallShadowColor = new Color(0, 0, 0, .3f);
    static readonly Side[] wallSides = new Side[] {
        Side.Top,
        Side.Left
    };

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is Wall;
    }

    protected override void DrawSlotParameterGUI(Context context) {
        DrawOutline(context);
        DrawSides(context);
    }

    void DrawSides(Context context) {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(100));

        if (Event.current.type == EventType.Layout) return;

        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent("Sides"));
        rect.xMin = rect2.x;

        Rect areaRect = new Rect(rect2);
        areaRect.width = Mathf.Min(rect2.width, rect.height);

        Dictionary<Side, Rect> sideButtons = new Dictionary<Side, Rect>();
        float buttonSize = EditorGUIUtility.singleLineHeight;
        sideButtons.Add(Side.Left, new Rect(areaRect.xMin, areaRect.yMin + buttonSize, buttonSize, areaRect.height - buttonSize * 2));
        sideButtons.Add(Side.Right, new Rect(areaRect.xMax - buttonSize, areaRect.yMin + buttonSize, buttonSize, areaRect.height - buttonSize * 2));
        sideButtons.Add(Side.Bottom, new Rect(areaRect.xMin + buttonSize, areaRect.yMax - buttonSize, areaRect.width - buttonSize * 2, buttonSize));
        sideButtons.Add(Side.Top, new Rect(areaRect.xMin + buttonSize, areaRect.yMin, areaRect.width - buttonSize * 2, buttonSize));

        foreach (var side in sideButtons) {
            EUtils.DrawMixedProperty(context.selection,
                mask: coord => context.slots.ContainsKey(coord) && context.slots.ContainsKey(coord + side.Key),
                getValue: coord => Wall.IsWall(coord, side.Key, context.slots, context.info),
                setValue: (coord, value) => {
                    if (value != Wall.IsWall(coord, side.Key, context.slots, context.info))
                        SetWall(value, coord, side.Key, context);
                },
                drawSingle: (position, value) => GUI.Toggle(side.Value, value, "", EditorStyles.miniButton),
                drawMixed: setDefault => {
                    if (!GUI.Toggle(side.Value, false, "*", EditorStyles.miniButton)) return false;
                    setDefault.Invoke(true);
                    return true;
                },
                drawEmpty: () => GUI.Label(side.Value, "-", Styles.centeredMiniLabel)
                );
        }
    }

    void DrawOutline(Context context) {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));

        if (Event.current.type == EventType.Layout) return;

        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent("Outline"));
        rect.xMin = rect2.x;

        int exist = 0;
        int notexist = 0;

        foreach (int2 coord in context.selection) {
            if (!context.slots.ContainsKey(coord)) continue;
            foreach (Side side in Utils.straightSides)
                if (!context.selection.Contains(coord + side) && context.slots.ContainsKey(coord + side)) {
                    if (Wall.IsWall(coord, side, context.slots, context.info)) exist++; 
                    else notexist++;
                }
        }

        if (exist > 0 && notexist > 0) {
            if (GUI.Button(rect, "[Set]", EditorStyles.miniButton)) SetOutline(true, context);
        } else if (exist > 0) {
            if (GUI.Button(rect, "Remove", EditorStyles.miniButton)) SetOutline(false, context);
        } else if (notexist > 0) {
            if (GUI.Button(rect, "Set", EditorStyles.miniButton)) SetOutline(true, context);
        } else {
            GUI.Button(rect, "-", EditorStyles.miniButton);
        }
    }

    void SetOutline(bool value, Context context) {
        foreach (int2 coord in context.selection) {
            if (!context.slots.ContainsKey(coord)) continue;
            foreach (Side side in Utils.straightSides)
                if (!context.selection.Contains(coord + side))
                    SetWall(value, coord, side, context);
        }
    }

    protected override void DrawSlotIconGUI(Rect rect, int2 coord, Context context) {
        Handles.color = Color.white;
        Rect wRect;
        foreach (Side side in wallSides) {
            if (Wall.IsWall(coord, side, context.slots, context.info)) {
                if (side.X() == 0)
                    wRect = new Rect(rect.x, rect.y - LevelEditor.cellOffset, rect.width, LevelEditor.cellOffset);
                else
                    wRect = new Rect(rect.x - LevelEditor.cellOffset, rect.y, LevelEditor.cellOffset, rect.height);
                Handles.DrawSolidRectangleWithOutline(wRect, wallIconColor, wallShadowColor);
            }
        }
    }

    void SetWall(bool value, int2 coord, Side side, Context context) {
        Wall.Coord wall = Wall.GetCoord(coord, side, context.slots);

        if (!wall.exist) return;

        SlotExtension slot = context.info[wall.coord, true];

        string key = wall.vert ? Wall.vertKey : Wall.horizKey;

        if (slot[key].Bool == value) return;

        if (value && slot == null)
            slot = context.info[wall.coord, true];

        if (slot != null) {
            slot[key].Bool = value;

            if (!slot[Wall.vertKey].Bool && !slot[Wall.horizKey].Bool)
                context.info[wall.coord] = null;
        }
    }
}

public class AntiGravitationExtension : LevelExtensionEditorExtension {
    static readonly Color directionIconColor = new Color(.5f, .9f, .2f, 1f);
    static readonly Color directionShadowColor = new Color(.4f, .5f, .1f, .3f);
    static readonly Side[] sides = {
        Side.Top,
        Side.Right,
        Side.Bottom,
        Side.Left
    };
    static readonly Dictionary<Side, string> symbols = new Dictionary<Side, string>() {
        { Side.Top, "^"},
        { Side.Right, ">"},
        { Side.Bottom, "v"},
        { Side.Left, "<"}
    };

    public override bool IsCompatibleWith(ILiveContent content) {
        return content is AntiGravitation;
    }

    protected override void DrawSlotParameterGUI(Context context) {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));

        if (Event.current.type == EventType.Layout) return;

        Rect rect2 = EditorGUI.PrefixLabel(rect, new GUIContent("Direction"));
        rect.xMin = rect2.x;

        Rect buttonRect = new Rect(rect);
        buttonRect.width /= sides.Length;

        GUIStyle style;        

        for (int i = 0; i < sides.Length; i++) {
            Side side = sides[i];

            if (i == 0) style = EditorStyles.miniButtonLeft;
            else if (i == sides.Length - 1) style = EditorStyles.miniButtonRight;
            else style = EditorStyles.miniButtonMid;

            EUtils.DrawMixedProperty(context.selection,
                mask: coord => context.slots.ContainsKey(coord),
                getValue: coord => GetDirection(coord, side, context),
                setValue: (coord, value) => SetDirection(coord, side, value, context),
                drawSingle: (coord, value) => GUI.Toggle(buttonRect, value, symbols[side], style),
                drawMixed: setDefault => {
                    if (!GUI.Toggle(buttonRect, false, "[" + symbols[side] + "]", style)) return false;
                    setDefault.Invoke(false);
                    return true;
                },
                drawEmpty: () => GUI.Toggle(buttonRect, false, "-", style)
                );
            buttonRect.x += buttonRect.width;
        }
    }

    protected override void DrawSlotIconGUI(Rect rect, int2 coord, Context context) {
        foreach (Side side in Utils.straightSides) {
            if (GetDirection(coord, side, context)) {
                Handles.color = Color.white;
                Rect wRect = new Rect();

                if (side.X() == 0) {
                    wRect.width = rect.width * 2 / 3;
                    wRect.height = 2;
                    wRect.x = rect.center.x - wRect.width / 2;
                    wRect.y = side.Y() < 0 ? rect.yMax - wRect.height : rect.yMin;
                } else {
                    wRect.width = 2;
                    wRect.height = rect.height * 2 / 3;
                    wRect.x = side.X() > 0 ? rect.xMax - wRect.width : rect.xMin;
                    wRect.y = rect.center.y - wRect.height / 2;
                }

                Handles.DrawSolidRectangleWithOutline(wRect, directionIconColor, directionShadowColor);
            }
        }
    }

    void SetDirection(int2 coord, Side side, bool value, Context context) {
        var slot = context.info[coord];
        if (slot == null && value)
            slot = context.info[coord, true];
        if (slot != null)
            slot[side.ToString()].Bool = value;
    }

    bool GetDirection(int2 coord, Side side, Context context) {
        var slot = context.info.GetSlot(coord);
        if (slot == null) return false;
        return slot[side.ToString()].Bool;
    }
}