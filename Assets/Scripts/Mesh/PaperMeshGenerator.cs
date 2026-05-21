using System.Collections.Generic;
using UnityEngine;

/// <summary>Generates subdivided flat quad-grid meshes for use as origami paper.</summary>
public static class PaperMeshGenerator
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates a flat subdivided paper mesh centered at the world origin.
    /// Vertices lie in the XZ plane (Y = 0).
    /// After the base grid is built, each step's fold line is used to insert
    /// additional edge-aligned vertices exactly on diagonal boundaries so that
    /// the fold silhouette is perfectly sharp instead of stairstepped.
    /// </summary>
    public static Mesh Generate(
        float width, float height,
        int subdX, int subdY,
        IList<OrigamiStep> steps = null)
    {
        Mesh mesh = BuildGrid(width, height, subdX, subdY);

        if (steps != null)
        {
            foreach (var step in steps)
                mesh = SubdivideAlongFoldLine(mesh, step.foldLineStart, step.foldLineEnd);
        }

        return mesh;
    }

    /// <summary>
    /// Creates a copy of the given mesh with all normals negated and triangle
    /// winding reversed, suitable for the back-face paper layer.
    /// </summary>
    public static Mesh GenerateFlippedNormals(Mesh source)
    {
        Mesh flipped = Object.Instantiate(source);
        flipped.name = source.name + "_Back";

        Vector3[] normals = flipped.normals;
        for (int i = 0; i < normals.Length; i++)
            normals[i] = -normals[i];
        flipped.normals = normals;

        // Reverse triangle winding so the back mesh is visible from below.
        int[] tris = flipped.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int tmp  = tris[i];
            tris[i]  = tris[i + 2];
            tris[i + 2] = tmp;
        }
        flipped.triangles = tris;

        return flipped;
    }

    // -------------------------------------------------------------------------
    // Grid builder
    // -------------------------------------------------------------------------

    private static Mesh BuildGrid(float width, float height, int subdX, int subdY)
    {
        int cols = subdX + 1;
        int rows = subdY + 1;

        Vector3[] vertices = new Vector3[cols * rows];
        Vector2[] uvs      = new Vector2[cols * rows];

        float halfW = width  * 0.5f;
        float halfH = height * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            float t = (float)row / subdY;
            float z = Mathf.Lerp(-halfH, halfH, t);

            for (int col = 0; col < cols; col++)
            {
                float s   = (float)col / subdX;
                float x   = Mathf.Lerp(-halfW, halfW, s);
                int   idx = row * cols + col;

                vertices[idx] = new Vector3(x, 0f, z);
                uvs[idx]      = new Vector2(s, t);
            }
        }

        int[] triangles = BuildTriangles(subdX, subdY, cols);

        Mesh mesh = new Mesh
        {
            name      = "PaperMesh",
            vertices  = vertices,
            uv        = uvs,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static int[] BuildTriangles(int subdX, int subdY, int cols)
    {
        int[] triangles = new int[subdX * subdY * 6];
        int   ti        = 0;

        for (int row = 0; row < subdY; row++)
        {
            for (int col = 0; col < subdX; col++)
            {
                int bl = row * cols + col;
                int br = bl + 1;
                int tl = bl + cols;
                int tr = tl + 1;

                triangles[ti++] = bl;
                triangles[ti++] = tl;
                triangles[ti++] = tr;

                triangles[ti++] = bl;
                triangles[ti++] = tr;
                triangles[ti++] = br;
            }
        }

        return triangles;
    }

    // -------------------------------------------------------------------------
    // Fold-line subdivision
    // -------------------------------------------------------------------------

    /// <summary>
    /// Walks every triangle in <paramref name="mesh"/> and splits any triangle
    /// that straddles the infinite fold line defined by
    /// (<paramref name="lineStart"/>, <paramref name="lineEnd"/>) in XZ space.
    /// Triangles entirely on one side are kept as-is.
    /// Triangles that cross the line are replaced by 3 triangles whose shared
    /// edge sits exactly on the fold line.
    /// </summary>
    private static Mesh SubdivideAlongFoldLine(Mesh mesh, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector3[] srcVerts = mesh.vertices;
        Vector2[] srcUVs   = mesh.uv;
        int[]     srcTris  = mesh.triangles;

        List<Vector3> dstVerts = new List<Vector3>(srcVerts);
        List<Vector2> dstUVs   = new List<Vector2>(srcUVs);
        List<int>     dstTris  = new List<int>(srcTris.Length);

        Vector2 lineDir = (lineEnd - lineStart).normalized;

        // Pre-compute signed distances for all source vertices.
        float[] side = new float[srcVerts.Length];
        for (int i = 0; i < srcVerts.Length; i++)
            side[i] = SignedDist(new Vector2(srcVerts[i].x, srcVerts[i].z), lineStart, lineDir);

        for (int t = 0; t < srcTris.Length; t += 3)
        {
            int i0 = srcTris[t];
            int i1 = srcTris[t + 1];
            int i2 = srcTris[t + 2];

            float s0 = side[i0];
            float s1 = side[i1];
            float s2 = side[i2];

            bool b0 = s0 >= 0f;
            bool b1 = s1 >= 0f;
            bool b2 = s2 >= 0f;

            if (b0 == b1 && b1 == b2)
            {
                // All on the same side – keep unchanged.
                dstTris.Add(i0); dstTris.Add(i1); dstTris.Add(i2);
                continue;
            }

            // Find the one vertex that is alone on its side (the "odd" vertex).
            // Rotate indices so 'alone' is always first — this preserves the original
            // CCW winding in every generated triangle without special-casing.
            //
            //   Original CCW:  (i0, i1, i2)
            //   i0 alone  →  keep   (i0, i1, i2)
            //   i1 alone  →  rotate (i1, i2, i0)   still CCW
            //   i2 alone  →  rotate (i2, i0, i1)   still CCW
            int alone, sharedA, sharedB;
            float sAlone, sA, sB;

            if (b0 != b1 && b0 != b2)          // i0 is alone
            {
                alone = i0; sharedA = i1; sharedB = i2;
                sAlone = s0; sA = s1; sB = s2;
            }
            else if (b1 != b0 && b1 != b2)     // i1 is alone — rotate left once
            {
                alone = i1; sharedA = i2; sharedB = i0;
                sAlone = s1; sA = s2; sB = s0;
            }
            else                                // i2 is alone — rotate left twice
            {
                alone = i2; sharedA = i0; sharedB = i1;
                sAlone = s2; sA = s0; sB = s1;
            }

            // iA lies on edge alone→sharedA, iB on edge alone→sharedB.
            float tA   = sAlone / (sAlone - sA);
            float tB   = sAlone / (sAlone - sB);
            Vector3 pA = Vector3.Lerp(srcVerts[alone], srcVerts[sharedA], tA);
            Vector3 pB = Vector3.Lerp(srcVerts[alone], srcVerts[sharedB], tB);
            Vector2 uA = Vector2.Lerp(srcUVs[alone],   srcUVs[sharedA],   tA);
            Vector2 uB = Vector2.Lerp(srcUVs[alone],   srcUVs[sharedB],   tB);

            int iA = dstVerts.Count; dstVerts.Add(pA); dstUVs.Add(uA);
            int iB = dstVerts.Count; dstVerts.Add(pB); dstUVs.Add(uB);

            // Alone triangle — [alone, iA, iB] — same CCW rotation as [alone, sharedA, sharedB].
            dstTris.Add(alone);  dstTris.Add(iA);     dstTris.Add(iB);

            // Shared quad split along the diagonal iA→sharedB.
            // [iA, sharedA, sharedB] and [iA, sharedB, iB] — both CCW.
            dstTris.Add(iA);     dstTris.Add(sharedA); dstTris.Add(sharedB);
            dstTris.Add(iA);     dstTris.Add(sharedB); dstTris.Add(iB);
        }

        Vector3[] finalVerts = dstVerts.ToArray();
        Mesh result = new Mesh
        {
            name      = mesh.name,
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        result.vertices  = finalVerts;
        result.uv        = dstUVs.ToArray();
        result.triangles = dstTris.ToArray();
        result.RecalculateNormals();
        result.RecalculateBounds();
        return result;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Signed 2D distance of point <paramref name="p"/> from the line.</summary>
    private static float SignedDist(Vector2 p, Vector2 lineStart, Vector2 lineDir)
    {
        Vector2 toP = p - lineStart;
        // Perpendicular component (right-hand normal of lineDir).
        return lineDir.x * toP.y - lineDir.y * toP.x;
    }
}
