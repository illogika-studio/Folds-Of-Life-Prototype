using UnityEngine;

/// <summary>Describes a single triangular face: its vertex indices, centroid, and face normal.</summary>
[System.Serializable]
public struct FaceData
{
    /// <summary>Zero-based face index (triangle index / 3).</summary>
    public int faceIndex;

    /// <summary>The three vertex indices that make up this face. Always length 3.</summary>
    public int[] vertexIndices;

    /// <summary>Average of the three base vertex positions in local space.</summary>
    public Vector3 centroid;

    /// <summary>Face normal computed from the cross product of two triangle edges.</summary>
    public Vector3 faceNormal;
}
