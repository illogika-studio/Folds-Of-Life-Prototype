using UnityEngine;

/// <summary>
/// Lightweight container returned by <see cref="FaceMeshImporter.Build"/>.
/// Holds the per-vertex base state and the per-face descriptor array
/// built from an imported mesh.
/// </summary>
public class FaceMeshData
{
    /// <summary>Original vertex positions in local space (one entry per vertex after splitting).</summary>
    public Vector3[] BaseVertices;

    /// <summary>Original vertex normals in local space.</summary>
    public Vector3[] BaseNormals;

    /// <summary>Triangle list referencing <see cref="BaseVertices"/> by index.</summary>
    public int[] Triangles;

    /// <summary>One <see cref="FaceData"/> entry per triangle.</summary>
    public FaceData[] Faces;
}
