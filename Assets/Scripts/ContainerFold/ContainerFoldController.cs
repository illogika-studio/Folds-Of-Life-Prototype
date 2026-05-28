using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a container-based origami fold sequence.
/// Each step rotates (and optionally translates) real child <see cref="Transform"/>s,
/// so the hierarchy correctly carries nested containers along for free.
/// <para>
/// Steps are authored directly on this component — not in the ScriptableObject — because
/// they reference scene <see cref="Transform"/>s. The <see cref="ContainerFoldModel"/> asset
/// only supplies materials and paper thickness.
/// </para>
/// </summary>
public class ContainerFoldController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("Asset that holds the front/back materials and paper thickness.")]
    public ContainerFoldModel model;

    [Tooltip("Root whose MeshRenderers receive the materials on Start. Defaults to this Transform.")]
    public Transform paperRoot;

    [Tooltip("Ordered list of fold steps. Drag scene containers into the movement slots.")]
    public List<ContainerFoldStep> steps = new List<ContainerFoldStep>();

    // -------------------------------------------------------------------------
    // Public state
    // -------------------------------------------------------------------------

    /// <summary>Zero-based index of the last fully-applied step. 0 = initial state.</summary>
    public int CurrentStep { get; private set; }

    /// <summary>Total number of fold steps on this controller.</summary>
    public int TotalSteps => steps.Count;

    /// <summary>True while a step animation is running.</summary>
    public bool IsAnimating => _isAnimating;

    /// <summary>Fires whenever <see cref="CurrentStep"/> changes. Passes the new index.</summary>
    public event Action<int> OnStepChanged;

    /// <summary>Fires when an animation starts (true) or ends (false).</summary>
    public event Action<bool> OnAnimatingChanged;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private bool _isAnimating;

    /// <summary>
    /// Initial transforms captured on Start for every container that appears in any step.
    /// Used by <see cref="Reset"/> to restore the original state exactly.
    /// Key: container instance ID.
    /// </summary>
    private readonly Dictionary<int, TransformSnapshot> _initialSnapshots
        = new Dictionary<int, TransformSnapshot>();

    /// <summary>
    /// Snapshot taken just before the current animation starts.
    /// Used as the interpolation baseline so each step cleanly builds on the last.
    /// Key: container instance ID.
    /// </summary>
    private Dictionary<int, TransformSnapshot> _stepStartSnapshot;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        CaptureInitialSnapshots();
        ApplyMaterials();
        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Animates the next fold step.</summary>
    public void Next()
    {
        if (_isAnimating)
        {
            Debug.Log("[ContainerFoldController] Next blocked: already animating.");
            return;
        }

        if (CurrentStep >= TotalSteps)
        {
            Debug.Log($"[ContainerFoldController] Already at last step ({CurrentStep}/{TotalSteps}).");
            return;
        }

        ContainerFoldStep step = steps[CurrentStep];
        StartCoroutine(AnimateStep(step, forward: true, onComplete: () =>
        {
            CurrentStep++;
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Reverses the most recent fold step.</summary>
    public void Previous()
    {
        if (_isAnimating)
        {
            Debug.Log("[ContainerFoldController] Previous blocked: already animating.");
            return;
        }

        if (CurrentStep <= 0)
        {
            Debug.Log("[ContainerFoldController] Already at first step.");
            return;
        }

        ContainerFoldStep step = steps[CurrentStep - 1];
        StartCoroutine(AnimateStep(step, forward: false, onComplete: () =>
        {
            CurrentStep--;
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Immediately snaps every container back to its Scene-start transform.</summary>
    public void Reset()
    {
        StopAllCoroutines();

        if (_isAnimating)
        {
            _isAnimating = false;
            OnAnimatingChanged?.Invoke(false);
        }

        foreach (var pair in _initialSnapshots)
        {
            if (pair.Value.source != null)
                pair.Value.source.SetLocalPositionAndRotation(
                    pair.Value.localPosition,
                    pair.Value.localRotation);
        }

        CurrentStep = 0;
        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records the local position and rotation of every container referenced across all steps.
    /// Called once in Start so Reset() always returns to the true origin.
    /// </summary>
    private void CaptureInitialSnapshots()
    {
        foreach (ContainerFoldStep step in steps)
        {
            if (step.movements == null) continue;

            foreach (ContainerMovement m in step.movements)
            {
                if (m.container == null) continue;

                int id = m.container.GetInstanceID();
                if (!_initialSnapshots.ContainsKey(id))
                {
                    _initialSnapshots[id] = new TransformSnapshot
                    {
                        source        = m.container,
                        localPosition = m.container.localPosition,
                        localRotation = m.container.localRotation,
                    };
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Material application
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns front and back materials to every MeshRenderer under <see cref="paperRoot"/>.
    /// GameObjects whose name contains "back" (case-insensitive) receive the back material;
    /// all others receive the front material.
    /// Both materials are forced to render both faces (_Cull = 0) so folded faces
    /// remain visible after a 180° rotation.
    /// </summary>
    private void ApplyMaterials()
    {
        if (model == null) return;

        // Force double-sided on both materials so rotated faces are never culled.
        SetDoubleSided(model.frontMaterial);
        SetDoubleSided(model.backMaterial);

        Transform root = paperRoot != null ? paperRoot : transform;
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

        foreach (MeshRenderer r in renderers)
        {
            bool isBack = r.gameObject.name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isBack && model.backMaterial != null)
                r.sharedMaterial = model.backMaterial;
            else if (!isBack && model.frontMaterial != null)
                r.sharedMaterial = model.frontMaterial;
        }
    }

    /// <summary>
    /// Sets the <c>_Cull</c> property on <paramref name="mat"/> to 0 (Off = render both faces).
    /// Works with URP Lit, URP Unlit, and any shader that exposes the standard <c>_Cull</c> property.
    /// </summary>
    private static void SetDoubleSided(Material mat)
    {
        if (mat == null) return;

        const int cullOff = 0;
        if (mat.HasProperty("_Cull"))
            mat.SetInt("_Cull", cullOff);

        // URP 14+ uses _CullMode instead.
        if (mat.HasProperty("_CullMode"))
            mat.SetInt("_CullMode", cullOff);
    }

    // -------------------------------------------------------------------------
    // Animation
    // -------------------------------------------------------------------------

    private IEnumerator AnimateStep(ContainerFoldStep step, bool forward, Action onComplete)
    {
        _isAnimating = true;
        OnAnimatingChanged?.Invoke(true);

        if (step.movements == null || step.movements.Length == 0)
        {
            _isAnimating = false;
            OnAnimatingChanged?.Invoke(false);
            onComplete?.Invoke();
            yield break;
        }

        // Snapshot the current transform of every container in this step
        // so we interpolate cleanly from wherever they are right now.
        _stepStartSnapshot = Snapshot(step.movements);

        float duration = Mathf.Max(step.duration, 0.01f);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyMovements(step.movements, _stepStartSnapshot, smoothT, forward);
            yield return null;
        }

        // Snap to exact final state to eliminate float error.
        ApplyMovements(step.movements, _stepStartSnapshot, 1f, forward);

        _isAnimating = false;
        OnAnimatingChanged?.Invoke(false);
        onComplete?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Movement helpers
    // -------------------------------------------------------------------------

    private static Dictionary<int, TransformSnapshot> Snapshot(ContainerMovement[] movements)
    {
        var result = new Dictionary<int, TransformSnapshot>();

        foreach (ContainerMovement m in movements)
        {
            if (m.container == null) continue;

            int id = m.container.GetInstanceID();
            if (!result.ContainsKey(id))
            {
                result[id] = new TransformSnapshot
                {
                    source        = m.container,
                    localPosition = m.container.localPosition,
                    localRotation = m.container.localRotation,
                };
            }
        }

        return result;
    }

    private static void ApplyMovements(
        ContainerMovement[]                movements,
        Dictionary<int, TransformSnapshot> snapshots,
        float                              t,
        bool                               forward)
    {
        foreach (ContainerMovement m in movements)
        {
            if (m.container == null) continue;

            int id = m.container.GetInstanceID();
            if (!snapshots.TryGetValue(id, out TransformSnapshot snap)) continue;

            Quaternion targetRot = forward
                ? Quaternion.Euler(m.targetEulerAngles)
                : snap.localRotation;

            m.container.localRotation = Quaternion.Slerp(snap.localRotation, targetRot, t);
        }
    }

    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    private struct TransformSnapshot
    {
        public Transform  source;
        public Vector3    localPosition;
        public Quaternion localRotation;
    }
}
