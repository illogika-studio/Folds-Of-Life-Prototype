using UnityEngine;

/// <summary>Defines the region of the paper that folds.</summary>
public enum FoldRegionMode
{
    Right,
    Left,
    Top,
    Bottom,
    AboveDiagonal,
    BelowDiagonal
}

/// <summary>Settings for the dotted fold-line guide rendered on the paper.</summary>
[System.Serializable]
public class GuideLineSettings
{
    public bool visible;
    public float width;
}

/// <summary>Settings for the directional arrow guide rendered near the fold line.</summary>
[System.Serializable]
public class ArrowSettings
{
    public bool visible;
    public Vector2 arrowOrigin;
    public Vector2 arrowDirection;
}

/// <summary>All data describing a single origami fold step.</summary>
[System.Serializable]
public class OrigamiStep
{
    public string instructionText;

    /// <summary>Start of the fold line in paper-local XZ space.</summary>
    public Vector2 foldLineStart;

    /// <summary>End of the fold line in paper-local XZ space.</summary>
    public Vector2 foldLineEnd;

    /// <summary>Degrees to fold; positive = fold toward the viewer (upward in world Y).</summary>
    public float foldAngle;

    /// <summary>Duration of the fold animation in seconds.</summary>
    public float duration;

    public FoldRegionMode regionMode;
    public GuideLineSettings guideLine;
    public ArrowSettings arrow;

    public bool hasCameraOverride;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
}
