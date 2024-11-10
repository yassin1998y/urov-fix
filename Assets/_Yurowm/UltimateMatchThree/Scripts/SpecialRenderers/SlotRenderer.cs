using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

[RequireComponent (typeof (MeshRenderer))]
[RequireComponent (typeof (MeshFilter))]
[DisallowMultipleComponent]
public class SlotRenderer : ILiveContent {

    Mesh mesh = null;
    public Material material;
    public bool rewriteMaterial = false;

    public SortingLayerAndOrder sorting;

    List<int2> slots = new List<int2>();
    public float offsetLeft = 0;
    public float offsetRight = 0;
    public float offsetTop = 0;
    public float offsetBottom = 0;

    public float sizeLeft = 0;
    public float sizeRight = 0;
    public float sizeTop = 0;
    public float sizeBottom = 0;

    public Rect oTL;
    public Rect oTR;
    public Rect oBL;
    public Rect oBR;

    public Rect iTL;
    public Rect iTR;
    public Rect iBL;
    public Rect iBR;

    public Rect BL;
    public Rect BR;
    public Rect BB;
    public Rect BT;

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


    [ContextMenu("Load Last Level")]
    void LoadLevel() {
        if (LevelAssistant.main) {
            LevelDesign design = LevelAssistant.main.designs.Last();
            slots = design.slots.Select(x => x.position).ToList();
            Rebuild();
        }
    }

    public void Rebuild(List<int2> slots) {
        this.slots = slots;
        Rebuild();
    }

    public void Rebuild() {
        if (mesh == null) {
            mesh = new Mesh();
            mesh.name = name + "_SRMesh";
        }
        mesh.Clear();

        List<Point> points = new List<Point>();
        List<Face> faces = new List<Face>();
        List<Border> borders = new List<Border>();
        Dictionary<int2, Cell> cells = new Dictionary<int2, Cell>();

        foreach (int2 slot in slots) {
            Cell cell = new Cell();
            cell.sortID = - slot.y * 100 + slot.x;
            cells.Add(slot, cell);
            cell.coord = slot;
            cell.near = Utils.allSides.Where(x => slots.Contains(slot + x)).ToList();

            Face face = new Face();
            face.sortID = cell.sortID;
            cell.face = face;
            faces.Add(face);

            Point point;

            //Bottom Left
            point = new Point(Project.main.slot_offset * slot.x, Project.main.slot_offset * slot.y);
            points.Add(point);
            face.bl = point;

            //Bottom Right
            point = new Point(Project.main.slot_offset * (1 + slot.x), Project.main.slot_offset * slot.y);
            points.Add(point);
            face.br = point;

            //Top Left
            point = new Point(Project.main.slot_offset * slot.x, Project.main.slot_offset * (1 + slot.y));
            points.Add(point);
            face.tl = point; 

            //Top Right
            point = new Point(Project.main.slot_offset * (1 + slot.x), Project.main.slot_offset * (1 + slot.y));
            points.Add(point);
            face.tr = point; 
        }

        Dictionary<Vector3, Point> vertexes = points.Select(x => x.position).Distinct().ToDictionary(x => x, p => new Point(p.x, p.y));
        points.Clear();
        points.AddRange(vertexes.Values);
        foreach (Face face in faces) {
            face.bl = vertexes[face.bl.position];
            face.br = vertexes[face.br.position];
            face.tl = vertexes[face.tl.position];
            face.tr = vertexes[face.tr.position];
        }

        if (offsetLeft > 0 || offsetRight > 0 || offsetTop > 0 || offsetBottom > 0) {
            foreach (Cell cell in cells.Values) {
                if (cell.near.Contains(Side.TopRight) && !cell.near.Contains(Side.Right) && !cell.near.Contains(Side.Top)) {
                    Face face = cell.face;
                    Point point = face.tr.Clone();
                    points.Add(point);
                    face.tr = point;
                    cells[cell.coord + Side.TopRight].near.Remove(Side.BottomLeft);
                    cell.near.Remove(Side.TopRight);
                }
                if (cell.near.Contains(Side.TopLeft) && !cell.near.Contains(Side.Left) && !cell.near.Contains(Side.Top)) {
                    Face face = cell.face;
                    Point point = face.tl.Clone();
                    points.Add(point);
                    face.tl = point;
                    cells[cell.coord + Side.TopLeft].near.Remove(Side.BottomRight);
                    cell.near.Remove(Side.TopLeft);
                }
            }
        }

        foreach (Cell cell in cells.Values) {
            foreach (Side side in Utils.allSides)
                if (!cell.near.Contains(side)) {
                    if (!side.IsStraight())
                        if (cell.near.Contains(side.RotateSide(1)) != cell.near.Contains(side.RotateSide(-1)))
                            continue;
                    Border border = new Border();
                    border.outer = !cell.near.Contains(side.RotateSide(1));
                    border.cell = cell;
                    border.side = side;
                    borders.Add(border);
                }
        }

        if (offsetLeft == 0 && offsetRight == 0 && offsetTop == 0 && offsetBottom == 0) {
            foreach (Cell cell in cells.Values) {
                if (cell.near.Contains(Side.TopRight) && !cell.near.Contains(Side.Right) && !cell.near.Contains(Side.Top)) {
                    Border border = new Border();
                    border.cell = new Cell();
                    border.cell.coord = cell.coord + Side.Right;
                    border.cell.face = new Face();
                    border.cell.face.sortID = cell.sortID;

                    border.cell.face.tl = cell.face.tr;
                    border.side = Side.TopLeft;
                    border.outer = false;
                    borders.Add(border);

                    border = new Border();
                    border.cell = new Cell();
                    border.cell.coord = cell.coord + Side.Top;
                    border.cell.face.sortID = cell.sortID;
                    border.cell.face = new Face();
                    border.cell.face.br = cell.face.tr;
                    border.side = Side.BottomRight;
                    border.outer = false;
                    borders.Add(border);
                }
                if (cell.near.Contains(Side.TopLeft) && !cell.near.Contains(Side.Left) && !cell.near.Contains(Side.Top)) {
                    Border border = new Border();
                    border.cell = new Cell();
                    border.cell.coord = cell.coord + Side.Left;
                    border.cell.face.sortID = cell.sortID;
                    border.cell.face = new Face();
                    border.cell.face.tr = cell.face.tl;
                    border.side = Side.TopRight;
                    border.outer = false;
                    borders.Add(border);

                    border = new Border();
                    border.cell = new Cell();
                    border.cell.coord = cell.coord + Side.Top;
                    border.cell.face.sortID = cell.sortID;
                    border.cell.face = new Face();
                    border.cell.face.bl = cell.face.tl;
                    border.side = Side.BottomLeft;
                    border.outer = false;
                    borders.Add(border);
                }
            }
        }

        foreach (Side side in Utils.straightSides) {
            List<Point> p = borders.Where(x => x.side == side).SelectMany(x => x.cell.face.GetBorder(side)).Distinct().ToList();
            foreach (Point point in p) {
                switch (side) {
                    case Side.Left: point.position.x += Project.main.slot_offset * offsetLeft; break;
                    case Side.Right: point.position.x -= Project.main.slot_offset * offsetRight; break;
                    case Side.Top: point.position.y -= Project.main.slot_offset * offsetTop; break;
                    case Side.Bottom: point.position.y += Project.main.slot_offset * offsetBottom; break;
                }
            }
        }

        List<Face> borderFaces = new List<Face>();
        List<Point> borderPoints = new List<Point>();

        foreach (Side side in Utils.slantedSides) {
            if (BorderSize(side.Horizontal()) == 0 || BorderSize(side.Vertical()) == 0) continue;
            
            foreach (Border b in borders.Where(x => x.side == side)) {
                Point p = b.cell.face.GetPoint(side);
                if (p == null) continue;
                Face face = new Face();
                face.sortID = b.cell.sortID;
                borderFaces.Add(face);
                Vector2 size = new Vector2();
                size.x = side.X() > 0 ? sizeRight : sizeLeft;
                size.y = side.Y() > 0 ? sizeTop : sizeBottom;
                size *= Project.main.slot_offset;
                Vector2 position = p.position;
                position.x += 0.5f * size.x * side.X();
                position.y += 0.5f * size.y * side.Y();

                Rect uv;

                if (b.outer)
                    uv = OuterCornerRect(side);
                else
                    uv = InnerCornerRect(side);


                Point point;
                //Bottom Left
                point = new Point(position.x - size.x / 2, position.y - size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMin;
                point.uv2.y = uv.yMin;
                face.bl = point;

                //Bottom Right
                point = new Point(position.x + size.x / 2, position.y - size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMax;
                point.uv2.y = uv.yMin;
                face.br = point;

                //Top Left
                point = new Point(position.x - size.x / 2, position.y + size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMin;
                point.uv2.y = uv.yMax;
                face.tl = point;

                //Top Right
                point = new Point(position.x + size.x / 2, position.y + size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMax;
                point.uv2.y = uv.yMax;
                face.tr = point;
            }
        }

        foreach (Side side in Utils.straightSides) {
            if (BorderSize(side) == 0) continue;
            
            List<Border> b = borders.Where(x => x.side == side).ToList();
            foreach (Border _b in b) {
                Face face = new Face();
                face.sortID = _b.cell.sortID;
                borderFaces.Add(face);


                Rect uv = BorderRect(side.MirrorSide());

                Point[] _p = _b.cell.face.GetBorder(_b.side);

                Vector2 size = new Vector2();
                size.x = side.Y() != 0 ? Mathf.Abs(_p[0].position.x - _p[1].position.x) : (side.X() > 0 ? sizeRight : sizeLeft) * Project.main.slot_offset;
                size.y = side.X() != 0 ? Mathf.Abs(_p[0].position.y - _p[1].position.y) : (side.Y() > 0 ? sizeTop : sizeBottom) * Project.main.slot_offset;

                Vector2 position = new Vector2();
                position.x = (_p[0].position.x + _p[1].position.x) / 2;
                position.y = (_p[0].position.y + _p[1].position.y) / 2;
                if (side.Y() != 0) position.y += (side.Y() > 0 ? sizeTop : -sizeBottom) * Project.main.slot_offset / 2;
                else position.x += (side.X() > 0 ? sizeRight : -sizeLeft) * Project.main.slot_offset / 2;

                Point point;
                //Bottom Left
                point = new Point(position.x - size.x / 2, position.y - size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMin;
                point.uv2.y = uv.yMin;
                face.bl = point;

                //Bottom Right
                point = new Point(position.x + size.x / 2, position.y - size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMax;
                point.uv2.y = uv.yMin;
                face.br = point;

                //Top Left
                point = new Point(position.x - size.x / 2, position.y + size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMin;
                point.uv2.y = uv.yMax;
                face.tl = point;

                //Top Right
                point = new Point(position.x + size.x / 2, position.y + size.y / 2);
                borderPoints.Add(point);
                point.uv2.x = uv.xMax;
                point.uv2.y = uv.yMax;
                face.tr = point;

                for (int r = -1; r <= 1; r += 2) {
                    if (_b.cell.near.Contains(side.RotateSide(r))) {
                        _p = face.GetBorder(side.RotateSide(r * 2));
                        foreach (Point k in _p) {
                            switch (side.RotateSide(r * 2)) {
                                case Side.Left: k.position.x += Project.main.slot_offset * sizeRight; break;
                                case Side.Right: k.position.x -= Project.main.slot_offset * sizeLeft; break;
                                case Side.Top: k.position.y -= Project.main.slot_offset * sizeBottom; break;
                                case Side.Bottom: k.position.y += Project.main.slot_offset * sizeTop; break;
                            }
                        }
                    }
                }
            }
        }
       
        faces.AddRange(borderFaces);

        foreach (Point point in points)
            point.uv2 = Vector2.left;
        points.AddRange(borderPoints);

        foreach (Point point in points) {
            point.uv1.x = point.position.x / Project.main.slot_offset;
            point.uv1.y = point.position.y / Project.main.slot_offset;
        }

        int id = 0;
        points.ForEach(x => x.id = id++);
        mesh.SetVertices(points.Select(x => x.position).ToList());

        faces.Sort((x, y) => x.sortID.CompareTo(y.sortID));

        mesh.SetTriangles(faces.SelectMany(x => x.ids).ToList(), 0);
        mesh.SetUVs(0, points.Select(x => x.uv1).ToList());
        mesh.SetUVs(1, points.Select(x => x.uv2).ToList());

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        if (rewriteMaterial)
            meshRenderer.material = material;
        meshRenderer.sortingLayerID = sorting.layerID;
        meshRenderer.sortingOrder = sorting.order;
    }

    public void Clear() {
        slots.Clear();
        if (mesh == null) {
            mesh = new Mesh();
            mesh.name = name + "_SRMesh";
        }
        mesh.Clear();
        meshFilter.mesh = mesh;
    }

    float BorderSize(Side side) {
        switch (side) {
            case Side.Right: return sizeRight;
            case Side.Left: return sizeLeft;
            case Side.Top: return sizeTop;
            case Side.Bottom: return sizeBottom;
        }
        return 0;
    }

    Rect BorderRect(Side side) {
       switch (side) {
            case Side.Right: return BR;
            case Side.Left: return BL;
            case Side.Top: return BT;
            case Side.Bottom: return BB;
        }
        throw new Exception("The side is not straight");
    }

    Rect InnerCornerRect(Side side) {
       switch (side) {
            case Side.TopRight: return iTL;
            case Side.TopLeft: return iTR;
            case Side.BottomRight: return iBL;
            case Side.BottomLeft: return iBR;
        }
        throw new Exception("The side is straight");
    }

    Rect OuterCornerRect(Side side) {
       switch (side) {
            case Side.TopRight: return oBR;
            case Side.TopLeft: return oBL;
            case Side.BottomRight: return oTR;
            case Side.BottomLeft: return oTL;
        }
        throw new Exception("The side is straight");
    }

    class Cell {
        public int2 coord;
        public Face face;
        public List<Side> near;
        public int sortID = 0;
    }

    class Face {
        public int sortID = 0;

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

    class Border {
        public Cell cell;
        public Side side;
        public bool outer;
    }
}
