using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility that processes a <see cref="Mesh"/> asset and builds the per-face
/// data structures required by <see cref="FaceFoldController"/>.
/// <para>
/// Critical assumption: each face must own its own 3 vertices with no sharing across face
/// boundaries. If the source mesh shares vertices between triangles, this importer
/// automatically splits them so every face gets its own copy — preventing a fold on one
/// face from dragging its geometric neighbours.
/// </para>
/// </summary>
public static class FaceMeshImporter
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts per-face vertex data from <paramref name="sourceMesh"/> and returns a
    /// fully populated <see cref="FaceMeshData"/> ready for use by <see cref="FaceFoldController"/>.
    /// Shared vertices are split so each face owns its own set.
    /// </summary>
    /// <param name="sourceMesh">The mesh asset to process. Must not be null.</param>
    public static FaceMeshData Build(Mesh sourceMesh)
    {
        if (sourceMesh == null)
        {
            Debug.LogError("[FaceMeshImporter] sourceMesh is null.");
            return null;
        }

        Vector3[] srcVertices = sourceMesh.vertices;
        Vector3[] srcNormals  = sourceMesh.normals;
        int[]     srcTris     = sourceMesh.triangles;

        // If normals array is missing or wrong length, fall back to Vector3.up per vertex.
        bool hasNormals = srcNormals != null && srcNormals.Length == srcVertices.Length;

        int faceCount = srcTris.Length / 3;

        // Split all vertices so each face owns exactly 3 unique indices.
        // Even if the mesh already has unshared vertices this is a safe no-op in terms
        // of behaviour: the resulting mesh is functionally identical.
        Vector3[] splitVerts   = new Vector3[srcTris.Length];
        Vector3[] splitNormals = new Vector3[srcTris.Length];
        int[]     splitTris    = new int[srcTris.Length];
        FaceData[] faces       = new FaceData[faceCount];

        for (int f = 0; f < faceCount; f++)
        {
            int triBase = f * 3;

            int srcI0 = srcTris[triBase];
            int srcI1 = srcTris[triBase + 1];
            int srcI2 = srcTris[triBase + 2];

            // New contiguous vertex indices for this face.
            int newI0 = triBase;
            int newI1 = triBase + 1;
            int newI2 = triBase + 2;

            // Copy vertex positions.
            splitVerts[newI0] = srcVertices[srcI0];
            splitVerts[newI1] = srcVertices[srcI1];
            splitVerts[newI2] = srcVertices[srcI2];

            // Copy or derive normals.
            if (hasNormals)
            {
                splitNormals[newI0] = srcNormals[srcI0];
                splitNormals[newI1] = srcNormals[srcI1];
                splitNormals[newI2] = srcNormals[srcI2];
            }
            else
            {
                splitNormals[newI0] =
                splitNormals[newI1] =
                splitNormals[newI2] = Vector3.up;
            }

            // Triangle indices now map 1:1 into the split vertex array.
            splitTris[triBase]     = newI0;
            splitTris[triBase + 1] = newI1;
            splitTris[triBase + 2] = newI2;

            // Compute centroid and face normal from the split positions.
            Vector3 v0 = splitVerts[newI0];
            Vector3 v1 = splitVerts[newI1];
            Vector3 v2 = splitVerts[newI2];

            Vector3 centroid   = (v0 + v1 + v2) / 3f;
            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            faces[f] = new FaceData
            {
                faceIndex     = f,
                vertexIndices = new int[] { newI0, newI1, newI2 },
                centroid      = centroid,
                faceNormal    = faceNormal
            };
        }

        return new FaceMeshData
        {
            BaseVertices = splitVerts,
            BaseNormals  = splitNormals,
            Triangles    = splitTris,
            Faces        = faces
        };
    }
}
