using UnityEngine;

/// <summary>Generates subdivided flat quad-grid meshes for use as origami paper.</summary>
public static class PaperMeshGenerator
{
    /// <summary>
    /// Generates a flat subdivided paper mesh centered at the world origin.
    /// Vertices lie in the XZ plane (Y = 0).
    /// </summary>
    /// <param name="width">Total width along the X axis.</param>
    /// <param name="height">Total height along the Z axis.</param>
    /// <param name="subdX">Number of column subdivisions.</param>
    /// <param name="subdY">Number of row subdivisions.</param>
    public static Mesh Generate(float width, float height, int subdX, int subdY)
    {
        int cols = subdX + 1;
        int rows = subdY + 1;
        int vertexCount = cols * rows;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            float t = (float)row / subdY;
            float z = Mathf.Lerp(-halfH, halfH, t);

            for (int col = 0; col < cols; col++)
            {
                float s = (float)col / subdX;
                float x = Mathf.Lerp(-halfW, halfW, s);

                int idx = row * cols + col;
                vertices[idx] = new Vector3(x, 0f, z);
                uvs[idx] = new Vector2(s, t);
            }
        }

        // 2 triangles per quad cell, CCW winding from above (positive Y looking down).
        int quadCount = subdX * subdY;
        int[] triangles = new int[quadCount * 6];
        int ti = 0;

        for (int row = 0; row < subdY; row++)
        {
            for (int col = 0; col < subdX; col++)
            {
                int bl = row * cols + col;
                int br = bl + 1;
                int tl = bl + cols;
                int tr = tl + 1;

                // Triangle 1 (bottom-left, top-left, top-right)
                triangles[ti++] = bl;
                triangles[ti++] = tl;
                triangles[ti++] = tr;

                // Triangle 2 (bottom-left, top-right, bottom-right)
                triangles[ti++] = bl;
                triangles[ti++] = tr;
                triangles[ti++] = br;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "PaperMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Creates a copy of the given mesh with all normals negated, suitable for a back-face paper layer.
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
            int tmp = tris[i];
            tris[i] = tris[i + 2];
            tris[i + 2] = tmp;
        }
        flipped.triangles = tris;

        return flipped;
    }
}
