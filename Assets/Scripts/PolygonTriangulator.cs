using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
/// Contains methods for the triangular decomposition of planar surfaces / polygons
/// </summary>
public static class PolygonTriangulator
{
    /// <summary>
    /// Triangular decomposition of the building surface, which is spanned by the vertices in the form of a polygon. All vertices must be in one plane. 
    /// The returned array contains indexes for the assignment of the vertices of the initial polygon involved in the formation of a triangle. Each three
    /// steps in the returned array vertices are assigned to a new triangle. 
    /// </summary>
    /// <param name="unitySurfaceCoordinates">Vertices that span a planar surface in 3D space.</param>
    /// <param name="surface"><c>Surface</c> whos geometries are to be triangulated</param>
    /// <returns>Array with indexes referencing the vertices of the area passed as parameter.</returns>
    /// <exception cref="ArgumentException">Thown when less than 3 coordinate values are provided by the <c>unitySurfaceCoordinates</c></exception>
    public static int[] GetTriangles(Vector3[] unitySurfaceCoordinates, Surface surface)
    {
        /// Check for sufficient vertices for triagulation
        if (unitySurfaceCoordinates.Length < 3)
        {
            throw new ArgumentException("A surface must consist of at least three vertices. Surface CityGML ID: " + surface.CityGMLID);
        }

        /// As the triangulation algorithm in <seealso cref="Triangulator"/> requires a planar surface described by a polygon with 2D points, a dimensional reduction is necessary. As the given polygons are planar by definition and already converted to Unity3D coordinates, the dimensional reduction can be achieved by rotating the polygon until its surface normal vector points downwards and by neglecting the y-component (Keep in mind that Unity3D uses a left handed coordinate system!).The x- and the z-component are stored in a new temporarly list of <seealso cref="Vector2"/> items to be triangulated afterwards. 
        
        Vector3 surfaceNormal = surface.GetSurfaceNormal();

        Quaternion rotation = Quaternion.FromToRotation(surfaceNormal, Vector3.down);
        
        List<Vector2> surface2DCoordinates = new List<Vector2>();
        foreach (Vector3 vertex in unitySurfaceCoordinates)
        {
            Vector3 currentVertex = vertex;

            currentVertex = rotation * currentVertex;

            surface2DCoordinates.Add(new Vector2(currentVertex.x, currentVertex.z)); ;
        }

        return Triangulator.Triangulate(surface2DCoordinates);
    }


    /// <summary>
    /// Class for triangular decomposition of a polygon.
    /// Modified Unity Trinagulator, available in the <see href="http://wiki.unity3d.com/index.php/Triangulator">Unity Wiki</see>.
    /// </summary>
    private static class Triangulator
    {
        /// <summary>
        /// Performs a triangle decomposition of the given polygon and returns the indexes for referencing the initial points spanning the triangles in an interger array. 
        /// <remarks>This must be a planar polygon in 2D space. A new triangle starts every three steps in the array.</remarks>
        /// </summary>
        /// <param name="vertices">Vertices spanning the considered polygon.</param>
        /// <returns>Indexes to describe the triangles.</returns>
        public static int[] Triangulate(List<Vector2> vertices)
        {
            List<int> indices = new List<int>();

            int n = vertices.Count;
            if (n < 3)
            {
                return indices.ToArray();
            }

            int[] V = new int[n];
            if (Area(vertices) > 0)
            {
                for (int v = 0; v < n; v++)
                {
                    V[v] = v;
                }
            }
            else
            {
                for (int v = 0; v < n; v++)
                {
                    V[v] = (n - 1) - v;
                }
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                {
                    return indices.ToArray();
                }

                int u = v;
                if (nv <= u)
                {
                    u = 0;
                }

                v = u + 1;
                if (nv <= v)
                {
                    v = 0;
                }

                int w = v + 1;
                if (nv <= w)
                {
                    w = 0;
                }

                if (Snip(vertices, u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                    {
                        V[s] = V[t];
                    }

                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private static float Area(List<Vector2> vertices)
        {
            int n = vertices.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = vertices[p];
                Vector2 qval = vertices[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private static bool Snip(List<Vector2> vertices, int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = vertices[V[u]];
            Vector2 B = vertices[V[v]];
            Vector2 C = vertices[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            {
                return false;
            }

            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                {
                    continue;
                }

                Vector2 P = vertices[V[p]];
                if (InsideTriangle(A, B, C, P))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}
