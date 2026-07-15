using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Utils
{
    /// <summary>
    /// Procedurally generates the 3D meshes the game needs (no model assets). Everything is built
    /// from primitives + a rounded-box generator so the whole scene renders as real lit geometry.
    /// Meshes are cached by key so every candy tile / bin / belt shares one mesh.
    /// </summary>
    public static class MeshFactory
    {
        private static readonly Dictionary<string, Mesh> _cache = new Dictionary<string, Mesh>();

        private static Mesh Cached(string key, System.Func<Mesh> make)
        {
            Mesh m;
            if (_cache.TryGetValue(key, out m) && m != null) return m;
            m = make();
            _cache[key] = m;
            return m;
        }

        /// <summary>
        /// A rounded box (candy block / bin / belt slab). <paramref name="size"/> is full extent,
        /// <paramref name="radius"/> the corner/edge rounding, <paramref name="res"/> the subdivision
        /// per face (higher = smoother corners). Each of the 6 faces is a subdivided quad whose
        /// vertices are projected onto the rounded surface; because the projection depends only on
        /// position, shared edges match exactly with no seams.
        /// </summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int res = 6)
        {
            string key = "rbox" + size.x.ToString("0.00") + "_" + size.y.ToString("0.00") + "_" +
                         size.z.ToString("0.00") + "_" + radius.ToString("0.00") + "_" + res;
            return Cached(key, () =>
            {
                Vector3 half = size * 0.5f;
                float r = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.999f);

                var verts = new List<Vector3>();
                var norms = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();

                // 6 faces: axis (0=x,1=y,2=z) and sign
                int[] axes = { 0, 0, 1, 1, 2, 2 };
                int[] signs = { 1, -1, 1, -1, 1, -1 };
                for (int f = 0; f < 6; f++)
                {
                    int axis = axes[f];
                    int sgn = signs[f];
                    // two in-plane axes
                    int u = (axis + 1) % 3;
                    int v = (axis + 2) % 3;
                    int baseIdx = verts.Count;
                    for (int iy = 0; iy <= res; iy++)
                        for (int ix = 0; ix <= res; ix++)
                        {
                            Vector3 p = Vector3.zero;
                            p[axis] = sgn * half[axis];
                            p[u] = Mathf.Lerp(-half[u], half[u], ix / (float)res);
                            p[v] = Mathf.Lerp(-half[v], half[v], iy / (float)res);

                            Vector3 rounded, n;
                            RoundPoint(p, half, r, out rounded, out n);
                            verts.Add(rounded);
                            norms.Add(n);
                            uvs.Add(new Vector2(ix / (float)res, iy / (float)res));
                        }
                    for (int iy = 0; iy < res; iy++)
                        for (int ix = 0; ix < res; ix++)
                        {
                            int a = baseIdx + iy * (res + 1) + ix;
                            int b = a + 1;
                            int c = a + (res + 1);
                            int d = c + 1;
                            // wind so the face points outward along sgn
                            if (sgn > 0) { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                            else { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c); }
                        }
                }

                var mesh = new Mesh { name = "RoundedBox" };
                if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetNormals(norms);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            });
        }

        private static void RoundPoint(Vector3 p, Vector3 half, float r, out Vector3 rounded, out Vector3 normal)
        {
            Vector3 inner = new Vector3(
                Mathf.Clamp(p.x, -(half.x - r), half.x - r),
                Mathf.Clamp(p.y, -(half.y - r), half.y - r),
                Mathf.Clamp(p.z, -(half.z - r), half.z - r));
            Vector3 delta = p - inner;
            float m = delta.magnitude;
            if (m > 1e-5f) { normal = delta / m; rounded = inner + normal * r; }
            else { normal = Vector3.up; rounded = p; }
        }

        /// <summary>
        /// A "pop-it" candy tile: a rounded box body with a grid of real raised bump-domes on its
        /// front (−Z) face, combined into one mesh. This is the signature bubble-wrap candy surface —
        /// actual geometry (not a normal map) so the bumps catch specular highlights and cast micro
        /// self-shading. <paramref name="bumps"/> is the count per axis.
        /// </summary>
        public static Mesh CandyBubbleTile(float size, float depth, int bumps = 3)
        {
            string key = "candytile" + size.ToString("0.00") + "_" + depth.ToString("0.00") + "_" + bumps;
            return Cached(key, () =>
            {
                var body = RoundedBox(new Vector3(size, size, depth), size * 0.22f, 7);
                var sphere = Sphere();

                var combine = new List<CombineInstance>();
                combine.Add(new CombineInstance { mesh = body, transform = Matrix4x4.identity });

                float half = size * 0.5f;
                float innerHalf = half - size * 0.15f;
                float span = innerHalf * 2f;
                float d = (span / bumps) * 1.12f;         // slightly overlapping domes
                float frontZ = -depth * 0.5f;
                float protr = d * 0.24f;
                for (int iy = 0; iy < bumps; iy++)
                    for (int ix = 0; ix < bumps; ix++)
                    {
                        float x = bumps > 1 ? Mathf.Lerp(-innerHalf, innerHalf, ix / (float)(bumps - 1)) : 0f;
                        float y = bumps > 1 ? Mathf.Lerp(-innerHalf, innerHalf, iy / (float)(bumps - 1)) : 0f;
                        var m = Matrix4x4.TRS(
                            new Vector3(x, y, frontZ - protr),
                            Quaternion.identity,
                            new Vector3(d, d, d * 0.62f));   // flattened dome
                        combine.Add(new CombineInstance { mesh = sphere, transform = m });
                    }

                var mesh = new Mesh { name = "CandyBubbleTile" };
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(combine.ToArray(), true, true);
                mesh.RecalculateBounds();
                return mesh;
            });
        }

        /// <summary>
        /// A trapezoid prism (funnel body): full <paramref name="topWidth"/> at +Y/2, narrowing to
        /// <paramref name="bottomWidth"/> at -Y/2, extruded <paramref name="depth"/> along Z. Flat
        /// per-face normals so the slanted sides shade like real chute walls.
        /// </summary>
        public static Mesh TaperedBox(float topWidth, float bottomWidth, float height, float depth)
        {
            string key = "taper" + topWidth.ToString("0.00") + "_" + bottomWidth.ToString("0.00") + "_" +
                         height.ToString("0.00") + "_" + depth.ToString("0.00");
            return Cached(key, () =>
            {
                float ht = topWidth * 0.5f, hb = bottomWidth * 0.5f, hy = height * 0.5f, hz = depth * 0.5f;
                // trapezoid ring (CCW seen from -Z): bottom-left, bottom-right, top-right, top-left
                Vector3[] ring =
                {
                    new Vector3(-hb, -hy, 0f), new Vector3(hb, -hy, 0f),
                    new Vector3(ht, hy, 0f), new Vector3(-ht, hy, 0f)
                };
                var verts = new List<Vector3>();
                var norms = new List<Vector3>();
                var tris = new List<int>();

                // front (-Z) and back (+Z) caps
                for (int face = 0; face < 2; face++)
                {
                    float z = face == 0 ? -hz : hz;
                    Vector3 nrm = face == 0 ? Vector3.back : Vector3.forward;
                    int start = verts.Count;
                    for (int i = 0; i < 4; i++) { verts.Add(new Vector3(ring[i].x, ring[i].y, z)); norms.Add(nrm); }
                    if (face == 0) { tris.Add(start); tris.Add(start + 1); tris.Add(start + 2); tris.Add(start); tris.Add(start + 2); tris.Add(start + 3); }
                    else { tris.Add(start); tris.Add(start + 2); tris.Add(start + 1); tris.Add(start); tris.Add(start + 3); tris.Add(start + 2); }
                }
                // side walls (same winding/normal convention as StarPrism)
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p0 = ring[i], p1 = ring[(i + 1) % 4];
                    Vector3 nrm = Vector3.Cross(p1 - p0, Vector3.forward).normalized;
                    int bi = verts.Count;
                    verts.Add(new Vector3(p0.x, p0.y, -hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p1.x, p1.y, -hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p1.x, p1.y, hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p0.x, p0.y, hz)); norms.Add(nrm);
                    tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 1);
                    tris.Add(bi); tris.Add(bi + 3); tris.Add(bi + 2);
                }

                var mesh = new Mesh { name = "TaperedBox" };
                mesh.SetVertices(verts);
                mesh.SetNormals(norms);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            });
        }

        /// <summary>An extruded star prism (belt token). Points spikes, in the XY plane, depth along Z.</summary>
        public static Mesh StarPrism(int points = 5, float outer = 0.5f, float inner = 0.24f, float depth = 0.18f)
        {
            string key = "star" + points + "_" + outer.ToString("0.00") + "_" + inner.ToString("0.00") + "_" + depth.ToString("0.00");
            return Cached(key, () =>
            {
                int n = points * 2;
                var ring = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    float ang = Mathf.PI / 2f + i * Mathf.PI / points; // first spike points up
                    float rad = (i % 2 == 0) ? outer : inner;
                    ring[i] = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad, 0f);
                }
                var verts = new List<Vector3>();
                var norms = new List<Vector3>();
                var tris = new List<int>();
                float hz = depth * 0.5f;

                // front + back caps (fan from centre)
                for (int face = 0; face < 2; face++)
                {
                    float z = face == 0 ? -hz : hz;
                    Vector3 nrm = face == 0 ? Vector3.back : Vector3.forward;
                    int center = verts.Count;
                    verts.Add(new Vector3(0, 0, z)); norms.Add(nrm);
                    int start = verts.Count;
                    for (int i = 0; i < n; i++) { verts.Add(new Vector3(ring[i].x, ring[i].y, z)); norms.Add(nrm); }
                    for (int i = 0; i < n; i++)
                    {
                        int a = start + i, b = start + (i + 1) % n;
                        if (face == 0) { tris.Add(center); tris.Add(a); tris.Add(b); }
                        else { tris.Add(center); tris.Add(b); tris.Add(a); }
                    }
                }
                // side walls
                for (int i = 0; i < n; i++)
                {
                    Vector3 p0 = ring[i], p1 = ring[(i + 1) % n];
                    Vector3 nrm = Vector3.Cross(p1 - p0, Vector3.forward).normalized;
                    int bi = verts.Count;
                    verts.Add(new Vector3(p0.x, p0.y, -hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p1.x, p1.y, -hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p1.x, p1.y, hz)); norms.Add(nrm);
                    verts.Add(new Vector3(p0.x, p0.y, hz)); norms.Add(nrm);
                    tris.Add(bi); tris.Add(bi + 2); tris.Add(bi + 1);
                    tris.Add(bi); tris.Add(bi + 3); tris.Add(bi + 2);
                }

                var mesh = new Mesh { name = "StarPrism" };
                mesh.SetVertices(verts);
                mesh.SetNormals(norms);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            });
        }

        // ---- shared primitive meshes (grabbed once from throwaway primitives) ----
        private static Mesh _sphere, _cylinder, _quad;

        public static Mesh Sphere()
        {
            if (_sphere == null) _sphere = GrabPrimitive(PrimitiveType.Sphere);
            return _sphere;
        }
        public static Mesh Cylinder()
        {
            if (_cylinder == null) _cylinder = GrabPrimitive(PrimitiveType.Cylinder);
            return _cylinder;
        }
        public static Mesh Quad()
        {
            if (_quad == null) _quad = GrabPrimitive(PrimitiveType.Quad);
            return _quad;
        }

        private static Mesh GrabPrimitive(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
            return mesh;
        }
    }
}
