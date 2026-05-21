using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Central controller that loads an OrigamiModel, manages fold step state,
/// drives smooth fold animations via coroutines, and exposes Next / Previous / Reset.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OrigamiPaperController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("The origami model asset to load on Awake.")]
    public OrigamiModel model;

    [Tooltip("Optional reference to the OrigamiGuideRenderer child.")]
    public OrigamiGuideRenderer guideRenderer;

    [Tooltip("Optional reference to the back-face child's MeshFilter.")]
    public MeshFilter backMeshFilter;

    // -------------------------------------------------------------------------
    // Public state
    // -------------------------------------------------------------------------

    /// <summary>Zero-based index of the step that is currently displayed.</summary>
    public int CurrentStep { get; private set; }

    /// <summary>Total number of steps in the loaded model.</summary>
    public int TotalSteps => model != null ? model.steps.Count : 0;

    /// <summary>Fires after CurrentStep has changed (passes the new step index).</summary>
    public event Action<int> OnStepChanged;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private MeshFilter _meshFilter;
    private Mesh _frontMesh;
    private Mesh _backMesh;

    /// <summary>Always-flat reference positions used to reset the paper.</summary>
    private Vector3[] _baseVertices;

    /// <summary>Accumulated vertex positions after all folds up to CurrentStep.</summary>
    private Vector3[] _currentVertices;

    /// <summary>
    /// Per-vertex Y-offset accumulated from paper thickness across folds.
    /// Kept separate from geometric rotation so fold-within-fold correctly doubles the lift.
    /// </summary>
    private float[] _vertexHeightOffset;

    private bool _isAnimating;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();

        if (model == null)
        {
            Debug.LogWarning("[OrigamiPaperController] No model assigned.");
            return;
        }

        InitialiseMesh();
    }

    private void Start()
    {
        if (model == null) return;

        GetComponent<MeshRenderer>().material = model.frontMaterial;

        if (backMeshFilter != null && model.backMaterial != null)
        {
            var backRenderer = backMeshFilter.GetComponent<MeshRenderer>();
            if (backRenderer != null)
                backRenderer.material = model.backMaterial;
        }

        ShowGuide(CurrentStep);
        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Advances to the next fold step and animates it.</summary>
    public void Next()
    {
        if (_isAnimating || CurrentStep >= TotalSteps - 1) return;
        StartCoroutine(AnimateFold(model.steps[CurrentStep], forward: true, onComplete: () =>
        {
            CurrentStep++;
            ShowGuide(CurrentStep);
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Reverses the current fold step and returns to the previous one.</summary>
    public void Previous()
    {
        if (_isAnimating || CurrentStep <= 0) return;
        int stepIndex = CurrentStep - 1;
        StartCoroutine(AnimateFold(model.steps[stepIndex], forward: false, onComplete: () =>
        {
            CurrentStep--;
            ShowGuide(CurrentStep);
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Immediately resets the paper to its initial flat state.</summary>
    public void Reset()
    {
        StopAllCoroutines();
        _isAnimating = false;
        CurrentStep = 0;

        if (_baseVertices != null && _currentVertices != null)
        {
            Array.Copy(_baseVertices, _currentVertices, _baseVertices.Length);
            Array.Clear(_vertexHeightOffset, 0, _vertexHeightOffset.Length);
            ApplyVerticesToMesh();
        }

        ShowGuide(CurrentStep);
        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void InitialiseMesh()
    {
        _frontMesh = PaperMeshGenerator.Generate(
            model.paperWidth, model.paperHeight,
            model.subdivisionsX, model.subdivisionsY);

        _meshFilter.mesh = _frontMesh;

        _baseVertices = _frontMesh.vertices;
        _currentVertices = (Vector3[])_baseVertices.Clone();
        _vertexHeightOffset = new float[_baseVertices.Length];

        if (backMeshFilter != null)
        {
            _backMesh = PaperMeshGenerator.GenerateFlippedNormals(_frontMesh);
            backMeshFilter.mesh = _backMesh;
        }
    }

    private void ApplyVerticesToMesh()
    {
        _frontMesh.vertices = _currentVertices;
        _frontMesh.RecalculateNormals();
        _frontMesh.RecalculateBounds();

        if (_backMesh != null)
        {
            // Push back-mesh vertices slightly inward along the inverted front normal
            // to eliminate Z-fighting at all angles, including along the fold crease.
            Vector3[] frontNormals = _frontMesh.normals;
            Vector3[] backVerts = new Vector3[_currentVertices.Length];
            Vector3[] backNormals = new Vector3[frontNormals.Length];

            for (int i = 0; i < _currentVertices.Length; i++)
            {
                backVerts[i] = _currentVertices[i] - frontNormals[i] * 0.002f;
                backNormals[i] = -frontNormals[i];
            }

            _backMesh.vertices = backVerts;
            _backMesh.normals = backNormals;
            _backMesh.RecalculateBounds();
        }
    }

    private void ShowGuide(int stepIndex)
    {
        if (guideRenderer == null) return;
        if (model == null) { guideRenderer.Hide(); return; }

        if (stepIndex >= 0 && stepIndex < TotalSteps)
            guideRenderer.ShowStep(model.steps[stepIndex], transform);
        else
            guideRenderer.Hide();
    }

    // -------------------------------------------------------------------------
    // Fold animation coroutine
    // -------------------------------------------------------------------------

    private IEnumerator AnimateFold(OrigamiStep step, bool forward, Action onComplete)
    {
        _isAnimating = true;

        // Snapshot of fully-accumulated vertex positions at the start of this animation.
        Vector3[] preFoldVertices = (Vector3[])_currentVertices.Clone();

        // Derive "flat geometry" positions by stripping out the accumulated height offset.
        // This allows the rotation to work on the pure geometric shape, while height
        // accumulates independently — so fold-within-fold vertices correctly get 2× lift.
        Vector3[] preFlatVerts = new Vector3[preFoldVertices.Length];
        for (int i = 0; i < preFlatVerts.Length; i++)
        {
            preFlatVerts[i] = preFoldVertices[i];
            preFlatVerts[i].y -= _vertexHeightOffset[i];
        }

        Vector3 lineStart = transform.TransformPoint(new Vector3(step.foldLineStart.x, 0f, step.foldLineStart.y));
        Vector3 lineEnd   = transform.TransformPoint(new Vector3(step.foldLineEnd.x,   0f, step.foldLineEnd.y));
        Vector3 axisDir   = (lineEnd - lineStart).normalized;

        float targetAngle = forward ? step.foldAngle : -step.foldAngle;
        int[] foldIndices = ClassifyVertices(_baseVertices, step, transform);

        float thickness  = model != null ? model.paperThickness : 0f;
        float heightSign = forward ? 1f : -1f;

        float elapsed  = 0f;
        float duration = Mathf.Max(step.duration, 0.01f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float angle   = Mathf.Lerp(0f, targetAngle, smoothT);

            Quaternion rotation     = Quaternion.AngleAxis(angle, axisDir);
            float      heightDelta  = thickness * smoothT * heightSign;

            Array.Copy(preFoldVertices, _currentVertices, preFoldVertices.Length);

            foreach (int idx in foldIndices)
            {
                // Rotate the flat geometry position (height stripped out).
                Vector3 worldFlat = transform.TransformPoint(preFlatVerts[idx]);
                Vector3 rotated   = rotation * (worldFlat - lineStart);
                Vector3 localPos  = transform.InverseTransformPoint(lineStart + rotated);

                // Re-add the existing accumulated height PLUS the in-progress height gain.
                localPos.y += _vertexHeightOffset[idx] + heightDelta;
                _currentVertices[idx] = localPos;
            }

            ApplyVerticesToMesh();
            yield return null;
        }

        // Commit the permanent height change for this fold step.
        float committedDelta = thickness * heightSign;
        foreach (int idx in foldIndices)
            _vertexHeightOffset[idx] += committedDelta;

        // Snap to the exact final positions using the now-committed heights.
        Quaternion finalRotation = Quaternion.AngleAxis(targetAngle, axisDir);
        Array.Copy(preFoldVertices, _currentVertices, preFoldVertices.Length);
        foreach (int idx in foldIndices)
        {
            Vector3 worldFlat = transform.TransformPoint(preFlatVerts[idx]);
            Vector3 rotated   = finalRotation * (worldFlat - lineStart);
            Vector3 localPos  = transform.InverseTransformPoint(lineStart + rotated);
            localPos.y += _vertexHeightOffset[idx];
            _currentVertices[idx] = localPos;
        }

        ApplyVerticesToMesh();

        _isAnimating = false;
        onComplete?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Vertex classification
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the indices of all vertices that belong to the fold region
    /// defined by <paramref name="step"/>.
    /// Classification is done in the XZ plane using the base (flat) vertex positions.
    /// </summary>
    private static int[] ClassifyVertices(Vector3[] baseVerts, OrigamiStep step, Transform paperTransform)
    {
        Vector2 lineStart = step.foldLineStart;
        Vector2 lineEnd = step.foldLineEnd;
        Vector2 lineDir = (lineEnd - lineStart).normalized;

        System.Collections.Generic.List<int> result = new System.Collections.Generic.List<int>();

        for (int i = 0; i < baseVerts.Length; i++)
        {
            // Work in paper-local XZ space.
            Vector2 v = new Vector2(baseVerts[i].x, baseVerts[i].z);
            Vector2 toVertex = v - lineStart;

            // 2D cross product (Y component of 3D cross).
            float cross = lineDir.x * toVertex.y - lineDir.y * toVertex.x;

            bool select = step.regionMode switch
            {
                FoldRegionMode.Right         => cross < 0f,
                FoldRegionMode.Left          => cross > 0f,
                FoldRegionMode.Top           => baseVerts[i].z > lineStart.y,
                FoldRegionMode.Bottom        => baseVerts[i].z < lineStart.y,
                FoldRegionMode.AboveDiagonal => cross > 0f,
                FoldRegionMode.BelowDiagonal => cross < 0f,
                _                            => false
            };

            if (select) result.Add(i);
        }

        return result.ToArray();
    }
}
