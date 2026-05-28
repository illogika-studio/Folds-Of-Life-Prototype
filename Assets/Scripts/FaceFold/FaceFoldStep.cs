using UnityEngine;

/// <summary>
/// Describes a single face-fold step: which faces to rotate, around which axis, and by how much.
/// </summary>
[System.Serializable]
public class FaceFoldStep
{
    /// <summary>Human-readable label shown in UI and debug output.</summary>
    public string label;

    /// <summary>Zero-based face indices to include in this fold.</summary>
    public int[] faceIndices;

    /// <summary>
    /// Start point of the fold axis in mesh-local XZ space.
    /// Typically the shared crease edge between the folding faces and their stationary neighbours.
    /// </summary>
    public Vector2 axisStart;

    /// <summary>End point of the fold axis in mesh-local XZ space.</summary>
    public Vector2 axisEnd;

    /// <summary>Degrees to rotate. Positive = fold toward +Y (upward).</summary>
    public float foldAngle;

    /// <summary>Duration of the animated fold in seconds.</summary>
    public float duration;
}
