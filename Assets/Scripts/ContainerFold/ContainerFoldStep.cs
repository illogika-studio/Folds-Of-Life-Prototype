using System;
using UnityEngine;

/// <summary>
/// Describes a single step in a container-based fold sequence.
/// Each step can move / rotate several containers simultaneously.
/// </summary>
[Serializable]
public class ContainerFoldStep
{
    /// <summary>Human-readable label shown in the UI and debug output.</summary>
    public string label;

    /// <summary>Duration of the animated step in seconds.</summary>
    [Min(0.01f)]
    public float duration = 0.4f;

    /// <summary>Ordered list of individual container movements that happen in parallel during this step.</summary>
    public ContainerMovement[] movements;
}

/// <summary>
/// Rotates a container Transform to a target local euler rotation over the step's duration.
/// </summary>
[Serializable]
public class ContainerMovement
{
    /// <summary>The container Transform to rotate.</summary>
    public Transform container;

    /// <summary>
    /// Target local euler angles (X, Y, Z) in degrees.
    /// The container will be smoothly rotated to this orientation when stepping forward,
    /// and back to its original orientation when stepping backward.
    /// </summary>
    public Vector3 targetEulerAngles;
}
