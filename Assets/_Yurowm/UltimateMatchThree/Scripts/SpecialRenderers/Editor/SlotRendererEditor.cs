using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.EditorCore;

[CustomEditor(typeof(SlotRenderer))]
public class SREditor : Editor {

    SlotRenderer provider;

    GUIStyle _rectLabel = null;
    GUIStyle rectLabel {
        get {
            if (_rectLabel == null) {
                _rectLabel = new GUIStyle(Styles.centeredMiniLabel);
                _rectLabel.alignment = TextAnchor.MiddleCenter;
                _rectLabel.normal.textColor = Color.white;
                _rectLabel.fontSize = 8;
            }
            return _rectLabel;
        }
    }

    //MaterialEditor materialEditor = null;

    static readonly Dictionary<string, string> rects = new Dictionary<string, string>() {
        { "Border Right", "BR" },
        { "Border Left", "BL" },
        { "Border Top", "BT" },
        { "Border Bottom", "BB" },
        { "Outer Corner Top Right", "oTR" },
        { "Outer Corner Top Left", "oTL" },
        { "Outer Corner Bottom Right", "oBR" },
        { "Outer Corner Bottom Left", "oBL" },
        { "Inner Corner Top Right", "iTR" },
        { "Inner Corner Top Left", "iTL" },
        { "Inner Corner Bottom Right", "iBR" },
        { "Inner Corner Bottom Left", "iBL" }
    };
    List<string> rectNames;

    Rect editedRect = new Rect();
    string editedRectName = "";

    void OnEnable() {
        provider = (SlotRenderer) target;
        editedRectName = rects.First().Key;
        editedRect = GetRect(editedRectName);
        LoadTextures(provider.material);
        rectNames = rects.Keys.ToList();
        if (magnetCellSize.IsEmpty())
            magnetCellSize.Float = .02f;

        //if (provider.material)
        //    materialEditor = (MaterialEditor) CreateEditor(provider.material);
    }

    void OnDisable() {
        //if (materialEditor != null)
        //    DestroyImmediate(materialEditor);
    }

    public override void OnInspectorGUI() {
        Undo.RecordObject(provider, "SlotRenderer is changed");
        using (new GUIHelper.Change(provider.Rebuild)) {
            provider.rewriteMaterial = EditorGUILayout.Toggle("Rewrite Material", provider.rewriteMaterial);
            if (provider.rewriteMaterial)
                using (new GUIHelper.Change(OnMaterialChanged)) 
                    provider.material = (Material) EditorGUILayout.ObjectField("Material", provider.material, typeof(Material), true, GUILayout.ExpandWidth(true));

            SortingLayerProperty.DrawSortingLayerAndOrder("Sorting", provider.sorting); 

            using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                GUILayout.Label("Border Offset", Styles.centeredMiniLabel);
                provider.offsetRight = EditorGUILayout.Slider("Right", provider.offsetRight, 0, 0.5f);
                provider.offsetLeft = EditorGUILayout.Slider("Left", provider.offsetLeft, 0, 0.5f);
                provider.offsetTop = EditorGUILayout.Slider("Top", provider.offsetTop, 0, 0.5f);
                provider.offsetBottom = EditorGUILayout.Slider("Bottom", provider.offsetBottom, 0, 0.5f);
            }

            using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                GUILayout.Label("Border Size", Styles.centeredMiniLabel);
                provider.sizeRight = EditorGUILayout.Slider("Right", provider.sizeRight, 0, 0.5f);
                provider.sizeLeft = EditorGUILayout.Slider("Left", provider.sizeLeft, 0, 0.5f);
                provider.sizeTop = EditorGUILayout.Slider("Top", provider.sizeTop, 0, 0.5f);
                provider.sizeBottom = EditorGUILayout.Slider("Bottom", provider.sizeBottom, 0, 0.5f);
            }

            using (new GUIHelper.Vertical(Styles.area, GUILayout.ExpandWidth(true))) {
                GUILayout.Label("Border UV", Styles.centeredMiniLabel);

                using (new GUIHelper.Change(() => editedRect = GetRect(editedRectName)))
                    editedRectName = rectNames[EditorGUILayout.Popup("Area", Mathf.Max(0, rectNames.IndexOf(editedRectName)), rectNames.ToArray())];

                using (new GUIHelper.Change(() => SaveRect(editedRectName))) {
                    editedRect.xMin = EditorGUILayout.Slider("X Min", editedRect.xMin, 0, editedRect.xMax);
                    editedRect.xMax = EditorGUILayout.Slider("X Max", editedRect.xMax, editedRect.xMin, 1);
                    editedRect.yMin = EditorGUILayout.Slider("Y Min", editedRect.yMin, 0, editedRect.yMax);
                    editedRect.yMax = EditorGUILayout.Slider("Y Max", editedRect.yMax, editedRect.yMin, 1);

                    if (GUILayout.Button("Reset Rect")) {
                        editedRect.size = Vector2.one * .3f;
                        editedRect.center = Vector2.one * .5f;
                    }
                }

                if (textures.Count > 0) {
                    GUILayout.Label("Preview", Styles.centeredMiniLabel);
                    currentTexture = textureNames[EditorGUILayout.Popup("Preview Texture", Mathf.Max(0, textureNames.IndexOf(currentTexture)), textureNames.ToArray())];
                    DrawPreview();
                }
            }
        }
    }

    void OnMaterialChanged() {
        LoadTextures(provider.material);
    }

    Color editedRectFaceColor = new Color(0, 1, 0, 0.1f);
    Color editedRectBorderColor = new Color(0, 1, 0, 0.8f);

    Color rectFaceColor = new Color(0, 0, 0, 0.1f);
    Color rectBorderColor = new Color(1, 1, 1, 0.2f);

    Color previewBackgroundColor = new Color(0.5f, 0.25f, 0.6f, 1);
    PrefVariable magnetCellSize = new PrefVariable("SREditor_Magnet");
    void DrawPreview() {
        if (textures[currentTexture] == null)
            return;

        magnetCellSize.Float = EditorGUILayout.Slider("Magnet Cell Size", magnetCellSize.Float, 0, .5f);

        Vector2 previewSize = new Vector2(300, 300);
        Rect previewRect;
        using (new GUIHelper.Horizontal()) {
            GUILayout.FlexibleSpace();
            previewRect = EditorGUILayout.GetControlRect(GUILayout.Width(previewSize.x), GUILayout.Height(previewSize.y));
            GUILayout.FlexibleSpace();
        }

        Handles.DrawSolidRectangleWithOutline(previewRect, previewBackgroundColor, Color.clear);

        EditorGUI.DrawTextureTransparent(previewRect, textures[currentTexture]);

        foreach (string name in rects.Keys) {
            if (name == editedRectName)
                continue;
            Rect currentRect = GetRect(name);
            Rect r = new Rect(previewRect.x + previewRect.width * currentRect.xMin,
                previewRect.y + previewRect.height * currentRect.yMin,
                previewRect.width * currentRect.width,
                previewRect.height * currentRect.height);

            Handles.DrawSolidRectangleWithOutline(r, rectFaceColor, rectBorderColor);
            using (new GUIHelper.Color(rectBorderColor))
                GUI.Box(r, name, rectLabel);
        }

        if (editedRectName != "") {
            Rect r = new Rect(previewRect.x + previewRect.width * editedRect.xMin,
                previewRect.y + previewRect.height * editedRect.yMin,
                previewRect.width * editedRect.width,
                previewRect.height * editedRect.height);
            Handles.DrawSolidRectangleWithOutline(r, editedRectFaceColor, editedRectBorderColor);
            using (new GUIHelper.Color(editedRectBorderColor))
                GUI.Box(new Rect(r.x, r.y, r.width, 16), editedRectName, rectLabel);

            Vector2 delta;
            using (new GUIHelper.Change(() => {
                        ClampRect(ref editedRect);
                        SaveRect(editedRectName);
                    })) {
                if (DragCursor(new Vector2(r.xMin, r.center.y), Vector2.one * 4, 1, editedRectBorderColor, out delta))
                    if (Event.current.type == EventType.Repaint)
                        editedRect.xMin -= delta.x / previewRect.width;
                if (DragCursor(new Vector2(r.xMax, r.center.y), Vector2.one * 4, 2, editedRectBorderColor, out delta))
                    if (Event.current.type == EventType.Repaint)
                        editedRect.xMax -= delta.x / previewRect.width;
                if (DragCursor(new Vector2(r.center.x, r.yMin), Vector2.one * 4, 3, editedRectBorderColor, out delta))
                    if (Event.current.type == EventType.Repaint)
                        editedRect.yMin -= delta.y / previewRect.height;
                if (DragCursor(new Vector2(r.center.x, r.yMax), Vector2.one * 4, 4, editedRectBorderColor, out delta))
                    if (Event.current.type == EventType.Repaint)
                        editedRect.yMax -= delta.y / previewRect.height;

                if (DragCursor(r.center, Vector2.one * 6, 5, editedRectBorderColor, out delta))
                    if (Event.current.type == EventType.Repaint) {
                        editedRect.x = Mathf.Clamp(editedRect.x - delta.x / previewRect.width, 0, 1f - editedRect.width);
                        editedRect.y = Mathf.Clamp(editedRect.y - delta.y / previewRect.height, 0, 1f - editedRect.height);
                    }
            }
        }


    }

    void ClampRect(ref Rect rect) {
        rect.xMin = Mathf.Clamp(rect.xMin, 0, rect.xMax);
        rect.yMin = Mathf.Clamp(rect.yMin, 0, rect.yMax);
        rect.xMax = Mathf.Clamp(rect.xMax, rect.xMin, 1);
        rect.yMax = Mathf.Clamp(rect.yMax, rect.yMin, 1);
        float magnet = magnetCellSize.Float;
        if (magnet > 0) {
            rect.xMin = Mathf.Round(rect.xMin / magnet) * magnet;
            rect.yMin = Mathf.Round(rect.yMin / magnet) * magnet;
            rect.xMax = Mathf.Round(rect.xMax / magnet) * magnet;
            rect.yMax = Mathf.Round(rect.yMax / magnet) * magnet;
        }
    }

    int currentControlID = -1;
    bool DragCursor(Vector2 position, Vector2 size, int controlID, Color color, out Vector2 delta) {
        Rect rect = new Rect(position - size / 2, size);

        Handles.DrawSolidRectangleWithOutline(rect, color, Color.clear);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ScaleArrow);

        if (currentControlID < 0 && Event.current.type == EventType.mouseDown && rect.Contains(Event.current.mousePosition))
            currentControlID = controlID;

        if (controlID == currentControlID) {
            delta = position - Event.current.mousePosition;
            if (delta != Vector2.zero)
                GUI.changed = true;

            if (EditorWindow.mouseOverWindow)
                EditorWindow.mouseOverWindow.Repaint();


            if (Event.current.type == EventType.MouseUp)
                currentControlID = -1;
            return true;
        }

        delta = Vector2.zero;
        return false;
    }

    Rect GetRect(string name) {
        return (Rect) typeof(SlotRenderer).GetField(rects[name]).GetValue(provider);
    }

    void SaveRect(string name) {
        typeof(SlotRenderer).GetField(rects[name]).SetValue(provider, editedRect);
    }

    Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    List<string> textureNames = new List<string>();
    string currentTexture = "";
    void LoadTextures(Material material) {
        textures.Clear();
        if (material) {
            for (int p = 0; p < ShaderUtil.GetPropertyCount(material.shader); p++) {
                if (ShaderUtil.GetPropertyType(material.shader, p) == ShaderUtil.ShaderPropertyType.TexEnv) {
                    string name = ShaderUtil.GetPropertyName(material.shader, p);
                    Texture2D texture = material.GetTexture(name) as Texture2D;
                    if (texture != null)
                        textures.Add(name, texture);
                }
            }

            textureNames = textures.Keys.ToList();
        }
    }
}
