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

    /// <summary>
    /// The state mesh to activate for this step. The controller deactivates all other entries in
    /// stateRoots and activates this one before animating. Must be an entry in
    /// <see cref="ContainerFoldController.stateRoots"/>.
    /// </summary>
    public GameObject stateRoot;

    /// <summary>
    /// When true the controller immediately calls <c>Next()</c> (or <c>Previous()</c>) after this
    /// step's animation completes, without waiting for user input.
    /// Intended for mesh-organisation steps that carry no meaningful instructional beat.
    /// </summary>
    public bool autoAdvance = false;
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
