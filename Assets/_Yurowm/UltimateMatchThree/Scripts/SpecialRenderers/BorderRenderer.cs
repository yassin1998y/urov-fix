using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof (MeshRenderer))]
[RequireComponent (typeof (MeshFilter))]
[DisallowMultipleComponent]
public class BorderRenderer : MonoBehaviour {

    Mesh mesh = null;
    public Material material;

    public SortingLayerAndOrder sorting;

    List<int2> verticalBorders = new List<int2>();
    List<int2> horizontalBorders = new List<int2>();
    
    public Vector2 vBorderOffset = new Vector2(0, 0);
    public Vector2 vBorderScale = new Vector2(0.2f, 1f);

    public Vector2 hBorderOffset = new Vector2(0, 0);
    public Vector2 hBorderScale = new Vector2(1f, 0.2f);

    public bool corners = true;
    public Vector2 cornerOffset = new Vector2(0, 0);
    public Vector2 cornerScale = new Vector2(0.2f, 0.2f);

    public Rect vB;
    public Rect hB;
    public Rect cR;

    MeshFilter _meshFilter = null;
    public MeshFilter meshFilter {
        get {
            if (!_meshFilter)
                _meshFilter = GetComponent<MeshFilter>();
            return _meshFilter;
        }
    }

    MeshRenderer _meshRenderer = null;
    public MeshRenderer meshRenderer {
        get {
            if (!_meshRenderer)
                _meshRenderer = GetComponent<MeshRenderer>();
            return _meshRenderer;
        }
    }

    [ContextMenu("LoadLevel")]
    void LoadLevel() {
        if (LevelAssistant.main) {
            LevelDesign design = LevelAssistant.main.designs.Last();
            verticalBorders.Clear();
            horizontalBorders.Clear();
            var info = design.extensions.FirstOrDefault(x => x.prefab is Wall);
            if (info != null) {
                var slots = design.slots.ToDictionary(x => x.position, x => x);
                Side[] sides = new Side[] { Side.Top, Side.Left };
                foreach (SlotSettings slot in design.slots) {
                    foreach (Side side in sides) {
                        if (Wall.IsWall(slot.position, side, slots, info)) {
                            if (side == Side.Left) verticalBorders.Add(slot.position);
                            else horizontalBorders.Add(slot.position + int2.up);
                        }
                    }
                }

                Rebuild();
            }
        }
    }

    public void Rebuild(List<int2> verticalBorders, List<int2> horizontalBorders) {
        this.verticalBorders = verticalBorders;
        this.horizontalBorders = horizontalBorders;
        Rebuild();
    }

    void OnValidate() {
        Rebuild();
    }

    public void Rebuild() {
        if (mesh == null) {
            mesh = new Mesh();
            mesh.name = name + "_BRMesh";
        }
        mesh.Clear();

        List<Point> points = new List<Point>();
        List<Face> faces = new List<Face>();

        if (vBorderScale.x != 0 && vBorderScale.y != 0)
            foreach (int2 coord in verticalBorders) {
                Face face = new Face();
                faces.Add(face);

                Point point;

                //Bottom Left
                point = new Point(Project.main.slot_offset * (1f * coord.x - vBorderScale.x / 2 + vBorderOffset.x),
                    Project.main.slot_offset * (1f + coord.y - vBorderScale.y / 2 - .5f + vBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(vB.xMin, 1f - vB.yMin);
                face.bl = point;

                //Bottom Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + vBorderScale.x / 2 + vBorderOffset.x),
                    Project.main.slot_offset * (1f + coord.y - vBorderScale.y / 2 - .5f + vBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(vB.xMax, 1f - vB.yMin);
                face.br = point;

                //Top Left
                point = new Point(Project.main.slot_offset * (1f * coord.x - vBorderScale.x / 2 + vBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y + vBorderScale.y / 2 + .5f + vBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(vB.xMin, 1f - vB.yMax);
                face.tl = point;

                //Top Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + vBorderScale.x / 2 + vBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y + vBorderScale.y / 2 + .5f + vBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(vB.xMax, 1f - vB.yMax);
                face.tr = point;
            }

        if (hBorderScale.x != 0 && hBorderScale.y != 0)
            foreach (int2 coord in horizontalBorders) {
                Face face = new Face();
                faces.Add(face);

                Point point;

                //Bottom Left
                point = new Point(Project.main.slot_offset * (1f + coord.x - hBorderScale.x / 2 - .5f + hBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y - hBorderScale.y / 2 + hBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(hB.xMin, 1f - hB.yMax);
                face.bl = point;

                //Bottom Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + hBorderScale.x / 2 + .5f + hBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y - hBorderScale.y / 2 + hBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(hB.xMax, 1f - hB.yMax);
                face.br = point;

                //Top Left
                point = new Point(Project.main.slot_offset * (1f + coord.x - hBorderScale.x / 2 - .5f + hBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y + hBorderScale.y / 2 + hBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(hB.xMin, 1f - hB.yMin);
                face.tl = point;

                //Top Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + hBorderScale.x / 2 + .5f + hBorderOffset.x),
                    Project.main.slot_offset * (1f * coord.y + hBorderScale.y / 2 + hBorderOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(hB.xMax, 1f - hB.yMin);
                face.tr = point;
            }

        if (corners && cornerScale.x != 0 && cornerScale.y != 0) {
            List<int2> _corners = new List<int2>();
            foreach (int2 coord in horizontalBorders)
                for (int i = -1; i <= 1; i += 2)
                    if (!horizontalBorders.Contains(coord + int2.right * i))
                        _corners.Add(i > 0 ? coord + int2.right : coord);
            foreach (int2 coord in verticalBorders)
                for (int i = -1; i <= 1; i += 2)
                    if (!verticalBorders.Contains(coord + int2.up * i))
                        _corners.Add(i > 0 ? coord + int2.up : coord);
            _corners.Distinct();
            foreach (int2 coord in _corners) {
                Face face = new Face();
                faces.Add(face);

                Point point;
                //Bottom Left
                point = new Point(Project.main.slot_offset * (1f * coord.x - cornerScale.x / 2 + cornerOffset.x),
                    Project.main.slot_offset * (1f * coord.y + cornerScale.y / 2 + cornerOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(cR.xMin, 1f - cR.yMin);
                face.bl = point;

                //Bottom Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + cornerScale.x / 2 + cornerOffset.x),
                    Project.main.slot_offset * (1f * coord.y + cornerScale.y / 2 + cornerOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(cR.xMax, 1f - cR.yMin);
                face.br = point;

                //Top Left
                point = new Point(Project.main.slot_offset * (1f * coord.x - cornerScale.x / 2 + cornerOffset.x),
                    Project.main.slot_offset * (1f * coord.y - cornerScale.y / 2 + cornerOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(cR.xMin, 1f - cR.yMax);
                face.tl = point;

                //Top Right
                point = new Point(Project.main.slot_offset * (1f * coord.x + cornerScale.x / 2 + cornerOffset.x),
                    Project.main.slot_offset * (1f * coord.y - cornerScale.y / 2 + cornerOffset.y));
                points.Add(point);
                point.uv1 = new Vector2(cR.xMax, 1f - cR.yMax);
                face.tr = point;
            }
        }

        int id = 0;
        points.ForEach(x => x.id = id++);
        mesh.SetVertices(points.Select(x => x.position).ToList());
        mesh.SetTriangles(faces.SelectMany(x => x.ids).ToList(), 0);
        mesh.SetUVs(0, points.Select(x => x.uv1).ToList());

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        meshRenderer.sortingLayerID = sorting.layerID;
        meshRenderer.sortingOrder = sorting.order;
    }

    public void Clear() {
        verticalBorders.Clear();
        horizontalBorders.Clear();
        if (mesh == null) {
            mesh = new Mesh();
            mesh.name = name + "_BRMesh";
        }
        mesh.Clear();
        meshFilter.mesh = mesh;
    }

    class Face {

        public Point bl;
        public Point br;
        public Point tl;
        public Point tr;

        public Triangle triangleA {
            get {
                Triangle result = new Triangle();
                result.a = br;
                result.b = bl;
                result.c = tl;
                return result;
            }
        }

        public Triangle triangleB {
            get {
                Triangle result = new Triangle();
                result.a = tl;
                result.b = tr;
                result.c = br;
                return result;
            }
        }

        public int[] ids {
            get {
                int[] a = triangleA.ids;
                int[] b = triangleB.ids;
                return new int[] { a[0], a[1], a[2], b[0], b[1], b[2] };
            }
        }

        public Point[] GetBorder(Side side) {
            switch (side) {
                case Side.Left: return new Point[] {bl, tl };
                case Side.Right: return new Point[] {br, tr };
                case Side.Top: return new Point[] {tr, tl };
                case Side.Bottom: return new Point[] {bl, br };
            }
            return null;
        }

        public Point GetPoint(Side side) {
            switch (side) {
                case Side.TopLeft: return tl;
                case Side.TopRight: return tr;
                case Side.BottomLeft: return bl;
                case Side.BottomRight: return br;
            }
            return null;
        }

    }

    class Triangle {
        public Point a;
        public Point b;
        public Point c;

        public int[] ids {
            get {
                return new int[] { a.id, b.id, c.id };
            }
        }
    }

    class Point {
        public Point(float x, float y) {
            position = new Vector3(x, y, 0);
        }

        public Vector3 position;
        public int id;
        public Vector2 uv1 = new Vector2();
        public Vector2 uv2 = new Vector2();

        internal Point Clone() {
            return (Point) MemberwiseClone();
        }
    }
}
