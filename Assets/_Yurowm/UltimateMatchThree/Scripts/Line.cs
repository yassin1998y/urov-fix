using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshRenderer))]
public class Line : MonoBehaviour {
    public enum ConnectionType {Thread, Chain, Brick}

    public ConnectionType type;
    public int smooth = 0;

    [Range(0.1f, 1f)]
    public float smoothPower = 0.33f;

    //public Vector2[] startPoints;

    List<Vector2> points = new List<Vector2>();

    public float Thickness = 0;

    float _thickness = 1f;
    public float thickness {
        set {
            if (value != _thickness) {
                _thickness = value;
                changed = true;
            }
        }
        get {
            return _thickness;
        }
    }

    bool _loop = false;
    public bool loop {
        set {
            if (value != _loop) {
                _loop = value;
                changed = true;
            }
        }
        get {
            return _loop;
        }
    }

    MeshFilter _filter;
    MeshFilter filter {
        get {
            if (_filter == null)
                _filter = GetComponent<MeshFilter>();
            return _filter;
        }
    }

    bool changed = true;
    void Update() {
        if (changed) {
            Refresh();
            changed = false;
        }
    }

    Mesh _mesh;
    Mesh mesh {
        set {
            _mesh = value;
        }
        get {
            if (_mesh == null) {
                _mesh = new Mesh();
                _mesh.name = "Line_" + gameObject.GetHashCode();
            }
            return _mesh;
        }
    }

    void Awake() {
        thickness = Thickness;
        //if (points == null || points.Count == 0)
        //    points = new List<Vector2>(startPoints);
    }

    public void AddPoint(Vector2 point) {
        points.Add(point);
        changed = true;
    }

    public void ChangePoint(int id, Vector2 point) {
        if (id >= 0 && id < points.Count && points[id] != point) {
            points[id] = point;
            changed = true;
        }

    }

    public void Clear() {
        points.Clear();
        changed = true;
    }

    public void Refresh() {
        switch (type) {
            case ConnectionType.Thread: RefreshThread(); return;
            case ConnectionType.Chain:
            case ConnectionType.Brick: RefreshBrick(); return;
        }
    }

    void RefreshThread() {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Triangle> triangles = new List<Triangle>();

        int p = 0;
        float length = 0;
        List<Vector2> _points;
        if (smooth >= 2 && points.Count >= 3) {
            Vector2 a, b, c, d;
            float t;

            _points = new List<Vector2>();
            List<Vector2> guides = new List<Vector2>();
            for (int i = 0; i < points.Count; i++) {
                if (i == 0 || i == points.Count - 1)
                    guides.Add(Vector2.zero);
                else {
                    Vector2 guide = (points[i - 1] - points[i]).normalized + (points[i + 1] - points[i]).normalized;
                    guide = guide.normalized;
                    guide.x += guide.y;
                    guide.y -= guide.x;
                    guide.x += guide.y;

                    if (Vector2.Angle(guide, points[i + 1] - points[i]) > 90)
                        guide *= -1;
                    guides.Add(guide);
                }
            }
            for (int i = 0; i < points.Count - 1; i++) {
                float guide_magnitude = Vector2.Distance(points[i], points[i + 1]) * smoothPower;
                a = points[i];
                b = a + guides[i] * guide_magnitude;
                d = points[i + 1];
                c = d - guides[i + 1] * guide_magnitude;
                //Debug.DrawLine(points[i], points[i] + guideStart, Color.green, 1);
                //Debug.DrawLine(points[i + 1], points[i + 1] + guideEnd, Color.red, 1);
                //Debug.DrawLine(points[i], points[i + 1], Color.yellow, 1);
                _points.Add(points[i]);
                for (int s = 1; s < smooth; s++) {
                    t = 1f * s / smooth;

                    _points.Add(Vector2.Lerp(
                        Vector2.Lerp(
                            Vector2.Lerp(a, b, t),
                            Vector2.Lerp(b, c, t),
                            t),
                        Vector2.Lerp(
                            Vector2.Lerp(b, c, t),
                            Vector2.Lerp(c, d, t),
                            t),
                        t
                        ));
                }
            }
            _points.Add(points[points.Count - 1]);

        } else
            _points = points;

        for (int i = 0; i < _points.Count; i++) {
            Vector2 a = GetPoint(i - 1, _points) - GetPoint(i, _points);
            Vector2 b = GetPoint(i + 1, _points) - GetPoint(i, _points);

            a = a.normalized;
            b = b.normalized;

            Vector2 offset = Vector2.Lerp(a, b, 0.5f);
            if (offset == Vector2.zero)
                offset = Quaternion.Euler(0, 0, 90) * a;
            
            offset = offset.normalized;

            float _thickness = this._thickness / Mathf.Cos(Mathf.Deg2Rad * (90 - Vector2.Angle(a, b) / 2));
            Vector3 left = Vector3.Project(Quaternion.Euler(0, 0, 90) * a, offset).normalized * _thickness / 2;


            vertices.Add(new Vector3(_points[i].x, _points[i].y, 0) - left);
            vertices.Add(new Vector3(_points[i].x, _points[i].y, 0) + left);

            if (i > 0)
                length += (_points[i - 1] - _points[i]).magnitude;
            uv.Add(new Vector2(1, length));
            uv.Add(new Vector2(0, length));

            if (vertices.Count < 4)
                continue;

            triangles.Add(new Triangle(p, p + 1, p + 2));
            triangles.Add(new Triangle(p + 3, p + 2, p + 1));

            p += 2;
        }

        if (_loop) {
            vertices.Add(vertices[0]);
            vertices.Add(vertices[1]);

            triangles.Add(new Triangle(p, p + 1, p + 2));
            triangles.Add(new Triangle(p + 3, p + 2, p + 1));

            length += (_points[_points.Count - 1] - _points[0]).magnitude;
            uv.Add(new Vector2(1, length));
            uv.Add(new Vector2(0, length));
        }

        mesh.triangles = null;
        mesh.uv = null;
        mesh.vertices = vertices.ToArray();

        List<int> _triangles = new List<int>();
        foreach (Triangle triangle in triangles) {
            _triangles.Add(triangle.a);
            _triangles.Add(triangle.b);
            _triangles.Add(triangle.c);
        }
        mesh.triangles = _triangles.ToArray();

        for (int i = 0; i < uv.Count; i++)
            uv[i] = new Vector2(uv[i].x, uv[i].y / length);

        mesh.uv = uv.ToArray();

        filter.mesh = mesh;
    }

    void RefreshBrick() {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Triangle> triangles = new List<Triangle>();

        int p = 0;
        for (int i = 0; i < points.Count; i++) {
            if (type == ConnectionType.Chain && i == 0 && !loop)
                continue;

            if (type == ConnectionType.Brick && i % 2 == 0)
                continue;

            Vector2 a = GetPoint(i - 1, points);
            Vector2 b = GetPoint(i, points);

            Vector2 offset = Quaternion.Euler(0, 0, 90) * (b - a);
            offset = offset.normalized;
            
            Vector3 left = offset * thickness / 2;


            vertices.Add(new Vector3(a.x, a.y, 0) + left);
            vertices.Add(new Vector3(a.x, a.y, 0) - left);
            uv.Add(new Vector2(0, 0));
            uv.Add(new Vector2(1, 0));

            vertices.Add(new Vector3(b.x, b.y, 0) + left);
            vertices.Add(new Vector3(b.x, b.y, 0) - left);
            uv.Add(new Vector2(0, 1));
            uv.Add(new Vector2(1, 1));

            triangles.Add(new Triangle(p, p + 1, p + 2));
            triangles.Add(new Triangle(p + 3, p + 2, p + 1));
            p += 4;
        }

        mesh.triangles = null;
        mesh.uv = null;
        mesh.vertices = vertices.ToArray();

        List<int> _triangles = new List<int>();
        foreach (Triangle triangle in triangles) {
            _triangles.Add(triangle.a);
            _triangles.Add(triangle.b);
            _triangles.Add(triangle.c);
        }   
        mesh.triangles = _triangles.ToArray();

        mesh.uv = uv.ToArray();

        filter.mesh = mesh;
    }

    Vector2 GetPoint(int i, List<Vector2> points) {
        if (points.Count == 0)
            return Vector2.zero;
        if (points.Count == 1)
            return points[0];
        if (i < 0) {
            if (_loop) return points[points.Count - 1];
            else return Vector2.LerpUnclamped(points[0], points[1], -1);
        }

        if (i >= points.Count) {
            if (_loop) return points[0];
            else return Vector2.LerpUnclamped(points[points.Count - 1], points[points.Count - 2], -1);
        }
        return points[i];
    }
}

public class Triangle {
    public int a;
    public int b;
    public int c;

    public Triangle(int a, int b, int c) {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}