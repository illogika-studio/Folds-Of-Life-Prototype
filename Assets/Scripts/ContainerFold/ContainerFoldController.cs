using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives a container-based origami fold sequence.
/// Each step rotates real child <see cref="Transform"/>s so the hierarchy carries
/// nested containers along for free.
/// <para>
/// Steps are authored directly on this component — not in the ScriptableObject — because
/// they reference scene <see cref="Transform"/>s. The <see cref="ContainerFoldModel"/> asset
/// only supplies materials and paper thickness.
/// </para>
/// <para>
/// Supports multi-state sequences where different steps target objects living in separate
/// State GameObjects. Assign all state roots to <see cref="stateRoots"/> and set
/// <see cref="ContainerFoldStep.stateRoot"/> per step to enable automatic activation,
/// deactivation, and world-space snapping on state transitions.
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

    [Tooltip("Exhaustive list of all State GameObjects for this sequence. Index 0 is activated on Start; " +
             "all others are deactivated. Steps reference individual entries via ContainerFoldStep.stateRoot.")]
    public List<GameObject> stateRoots = new List<GameObject>();

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

    /// <summary>
    /// Fires when the user advances past the last step in the forward direction.
    /// Wire this to <c>ButterflyAliveAnimation.Play()</c> via the Inspector.
    /// </summary>
    public UnityEvent onSequenceComplete;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private bool _isAnimating;

    /// <summary>Which State GameObject is currently active. Set to stateRoots[0] on Start.</summary>
    private GameObject _activeStateRoot;

    /// <summary>
    /// Records, for each step that performed a state switch, which state was active BEFORE the switch.
    /// Used by <see cref="RevertState"/> to restore the previous state when stepping backward.
    /// </summary>
    private readonly Dictionary<ContainerFoldStep, GameObject> _stepPreviousState
        = new Dictionary<ContainerFoldStep, GameObject>();

    /// <summary>
    /// Records the container rotations captured at the START of each forward animation.
    /// Used as the backward target so Previous reverses to the exact pre-step state.
    /// </summary>
    private readonly Dictionary<ContainerFoldStep, Dictionary<int, TransformSnapshot>>
        _forwardStartSnapshots = new Dictionary<ContainerFoldStep, Dictionary<int, TransformSnapshot>>();

    /// <summary>
    /// Initial transforms captured on Start for every container that appears in any step,
    /// plus every transform under each stateRoot.
    /// Used by <see cref="Reset"/> to restore the original state exactly.
    /// Key: transform instance ID.
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
        InitialiseStateRoots();
        ApplyMaterials();
        OnStepChanged?.Invoke(CurrentStep);
    }

    /// <summary>Temporary watchdog: logs if a container rotation changes unexpectedly after animation ends.</summary>
    private void LateUpdate()
    {
        if (_isAnimating || CurrentStep == 0 || steps == null || steps.Count == 0) return;

        ContainerFoldStep lastStep = steps[Mathf.Min(CurrentStep, steps.Count) - 1];
        if (lastStep.movements == null) return;

        foreach (ContainerMovement m in lastStep.movements)
        {
            if (m.container == null) continue;
            Vector3 euler = m.container.localEulerAngles;
            // Only log if the rotation is suspiciously close to identity after a completed forward step
            if (euler.sqrMagnitude < 0.01f && CurrentStep > 0)
            {
                Debug.LogWarning($"[DIAG WATCHDOG] {m.container.name} rotation is near zero " +
                                 $"(euler={euler}) at CurrentStep={CurrentStep}! Something reset it.",
                                 m.container);
                break; // log once per frame
            }
        }
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

    /// <summary>
    /// Immediately snaps every container back to its scene-start transform,
    /// restores State activation to stateRoots[0], and fires <see cref="OnStepChanged"/>.
    /// </summary>
    public void Reset()
    {
        StopAllCoroutines();

        if (_isAnimating)
        {
            _isAnimating = false;
            OnAnimatingChanged?.Invoke(false);
        }

        foreach (KeyValuePair<int, TransformSnapshot> pair in _initialSnapshots)
        {
            if (pair.Value.source != null)
                pair.Value.source.SetLocalPositionAndRotation(
                    pair.Value.localPosition,
                    pair.Value.localRotation);
        }

        // Restore State activation: only index 0 is active.
        if (stateRoots != null && stateRoots.Count > 0)
        {
            for (int i = 0; i < stateRoots.Count; i++)
            {
                if (stateRoots[i] != null)
                    stateRoots[i].SetActive(i == 0);
            }
            _activeStateRoot = stateRoots[0];
        }
        else
        {
            _activeStateRoot = null;
        }

        _stepPreviousState.Clear();
        _forwardStartSnapshots.Clear();
        CurrentStep = 0;
        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records the local position and rotation of every container referenced across all steps,
    /// and all transforms under each stateRoot.
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

        // Also snapshot all transforms under stateRoots so Reset() can fully restore them.
        if (stateRoots == null) return;

        foreach (GameObject stateRoot in stateRoots)
        {
            if (stateRoot == null) continue;

            Transform[] children = stateRoot.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (Transform t in children)
            {
                int id = t.GetInstanceID();
                if (!_initialSnapshots.ContainsKey(id))
                {
                    _initialSnapshots[id] = new TransformSnapshot
                    {
                        source        = t,
                        localPosition = t.localPosition,
                        localRotation = t.localRotation,
                    };
                }
            }
        }
    }

    /// <summary>
    /// Activates stateRoots[0] and deactivates all others on Start.
    /// </summary>
    private void InitialiseStateRoots()
    {
        if (stateRoots == null || stateRoots.Count == 0) return;

        for (int i = 0; i < stateRoots.Count; i++)
        {
            if (stateRoots[i] != null)
                stateRoots[i].SetActive(i == 0);
        }

        _activeStateRoot = stateRoots[0];
    }

    // -------------------------------------------------------------------------
    // State switching
    // -------------------------------------------------------------------------

    /// <summary>
    /// Activates <paramref name="step"/>'s <see cref="ContainerFoldStep.stateRoot"/> and deactivates
    /// all sibling State objects. Records the previous active state for reverse stepping.
    /// No-op when <c>stateRoot</c> is null.
    /// </summary>
    private void SwitchState(ContainerFoldStep step)
    {
        if (step.stateRoot == null) return;

        if (stateRoots != null && !stateRoots.Contains(step.stateRoot))
        {
            Debug.LogWarning($"[ContainerFoldController] Step '{step.label}' references stateRoot " +
                             $"'{step.stateRoot.name}' which is not in the stateRoots list. " +
                             $"This is likely an authoring mistake.");
        }

        // Record previous state so RevertState can restore it when stepping backward.
        _stepPreviousState[step] = _activeStateRoot;

        if (stateRoots != null)
        {
            foreach (GameObject sr in stateRoots)
            {
                if (sr != null)
                    sr.SetActive(sr == step.stateRoot);
            }
        }

        step.stateRoot.SetActive(true);
        _activeStateRoot = step.stateRoot;
    }

    /// <summary>
    /// Reverts the state switch performed by <paramref name="step"/> when it was animated forward.
    /// Re-activates the state that was active before the switch.
    /// No-op when the step has no stateRoot or no recorded previous state.
    /// </summary>
    private void RevertState(ContainerFoldStep step)
    {
        if (step.stateRoot == null) return;
        if (!_stepPreviousState.TryGetValue(step, out GameObject previousState)) return;
        if (previousState == _activeStateRoot) return;

        if (stateRoots != null)
        {
            foreach (GameObject sr in stateRoots)
            {
                if (sr != null)
                    sr.SetActive(sr == previousState);
            }
        }

        if (previousState != null)
            previousState.SetActive(true);

        _activeStateRoot = previousState;
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

            if (step.autoAdvance)
            {
                if (forward && CurrentStep < TotalSteps)
                    Next();
                else if (!forward && CurrentStep > 0)
                    Previous();
            }

            yield break;
        }

        // Switch to (or revert) the correct state before animating.
        LogContainerRotations(step, "[DIAG] BEFORE SwitchState");
        if (forward)
            SwitchState(step);
        else
            RevertState(step);
        LogContainerRotations(step, "[DIAG] AFTER SwitchState");

        // Snapshot the current transform of every container in this step
        // so we interpolate cleanly from wherever they are right now.
        _stepStartSnapshot = Snapshot(step.movements);

        // Save the pre-animation state on forward plays so backward can target it.
        if (forward)
            _forwardStartSnapshots[step] = new Dictionary<int, TransformSnapshot>(_stepStartSnapshot);

        // Resolve forward-start snapshots for backward target lookup.
        _forwardStartSnapshots.TryGetValue(step, out var fwdSnaps);

        float duration = Mathf.Max(step.duration, 0.01f);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyMovements(step.movements, _stepStartSnapshot, smoothT, forward, fwdSnaps);
            yield return null;
        }

        // Snap to exact final state to eliminate float error.
        ApplyMovements(step.movements, _stepStartSnapshot, 1f, forward, fwdSnaps);
        LogContainerRotations(step, "[DIAG] AFTER final ApplyMovements");

        _isAnimating = false;
        OnAnimatingChanged?.Invoke(false);
        LogContainerRotations(step, "[DIAG] AFTER OnAnimatingChanged");
        onComplete?.Invoke();
        LogContainerRotations(step, "[DIAG] AFTER onComplete");

        // Fire sequence complete when the last forward step finishes.
        if (forward && CurrentStep == TotalSteps)
        {
            onSequenceComplete?.Invoke();
            LogContainerRotations(step, "[DIAG] AFTER onSequenceComplete");
        }

        // autoAdvance: chain to next / previous without waiting for user input.
        if (step.autoAdvance)
        {
            if (forward && CurrentStep < TotalSteps)
                Next();
            else if (!forward && CurrentStep > 0)
                Previous();
        }
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
        bool                               forward,
        Dictionary<int, TransformSnapshot> forwardStart = null)
    {
        foreach (ContainerMovement m in movements)
        {
            if (m.container == null) continue;

            int id = m.container.GetInstanceID();
            if (!snapshots.TryGetValue(id, out TransformSnapshot snap)) continue;

            Quaternion targetRot;
            if (forward)
            {
                targetRot = Quaternion.Euler(m.targetEulerAngles);
            }
            else
            {
                // Target = rotation from BEFORE this step's forward animation played.
                targetRot = (forwardStart != null && forwardStart.TryGetValue(id, out var fwd))
                    ? fwd.localRotation
                    : snap.localRotation; // fallback (step was never played forward)
            }

            m.container.localRotation = Quaternion.Slerp(snap.localRotation, targetRot, t);
        }
    }

    // -------------------------------------------------------------------------
    // Types
    // -------------------------------------------------------------------------

    /// <summary>Logs the current local euler angles of every container in the step for debugging.</summary>
    private static void LogContainerRotations(ContainerFoldStep step, string tag)
    {
        if (step.movements == null) return;
        foreach (ContainerMovement m in step.movements)
        {
            if (m.container == null) continue;
            Vector3 euler = m.container.localEulerAngles;
            Debug.Log($"{tag} | {m.container.name} localEuler=({euler.x:F2}, {euler.y:F2}, {euler.z:F2})");
        }
    }

    private struct TransformSnapshot
    {
        public Transform  source;
        public Vector3    localPosition;
        public Quaternion localRotation;
    }
}
