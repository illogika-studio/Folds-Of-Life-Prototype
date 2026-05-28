using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop-in controller for a GameObject that displays a pre-divided paper mesh
/// and animates discrete face-fold steps defined in a <see cref="FaceFoldModel"/> asset.
/// <para>
/// Attach to a GameObject that also has a <see cref="MeshFilter"/> and <see cref="MeshRenderer"/>.
/// Assign a <see cref="FaceFoldModel"/> and, optionally, a back-face <see cref="MeshFilter"/>
/// child for the double-sided paper effect.
/// </para>
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FaceFoldController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("Face fold model asset that holds the source mesh and fold steps.")]
    public FaceFoldModel model;

    [Tooltip("Optional MeshFilter on a child GameObject used for the back-face paper layer.")]
    public MeshFilter backMeshFilter;

    // -------------------------------------------------------------------------
    // Public state
    // -------------------------------------------------------------------------

    /// <summary>Zero-based index of the step currently displayed.</summary>
    public int CurrentStep { get; private set; }

    /// <summary>Total number of steps defined in the loaded model.</summary>
    public int TotalSteps => model != null ? model.steps.Count : 0;

    /// <summary>Fires after <see cref="CurrentStep"/> has changed (passes the new index).</summary>
    public event Action<int> OnStepChanged;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private MeshFilter _meshFilter;
    private Mesh _frontMesh;
    private Mesh _backMesh;

    private FaceMeshData _data;

    /// <summary>Accumulated vertex positions after all folds applied up to CurrentStep.</summary>
    private Vector3[] _currentVertices;

    /// <summary>Per-vertex normal tracked analytically so 180° folds correctly invert normals.</summary>
    private Vector3[] _currentNormals;

    /// <summary>Per-vertex Y-offset accumulated from paper thickness across folds.</summary>
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
            Debug.LogWarning("[FaceFoldController] No model assigned.");
            return;
        }

        if (model.sourceMesh == null)
        {
            Debug.LogWarning("[FaceFoldController] model.sourceMesh is null.");
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

        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Advances to the next fold step and animates it.</summary>
    public void Next()
    {
        if (_isAnimating) { Debug.Log("[FaceFoldController] Next blocked: animating."); return; }
        if (model == null) { Debug.LogWarning("[FaceFoldController] Next blocked: no model assigned."); return; }
        if (_data == null) { Debug.LogWarning("[FaceFoldController] Next blocked: mesh data not built — assign sourceMesh on the model."); return; }
        if (CurrentStep >= TotalSteps) { Debug.Log($"[FaceFoldController] Next blocked: already at last step ({CurrentStep}/{TotalSteps})."); return; }

        StartCoroutine(AnimateFoldStep(model.steps[CurrentStep], forward: true, onComplete: () =>
        {
            CurrentStep++;
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Reverses the most recent fold step and returns to the previous state.</summary>
    public void Previous()
    {
        if (_isAnimating) { Debug.Log("[FaceFoldController] Previous blocked: animating."); return; }
        if (model == null) { Debug.LogWarning("[FaceFoldController] Previous blocked: no model assigned."); return; }
        if (_data == null) { Debug.LogWarning("[FaceFoldController] Previous blocked: mesh data not built."); return; }
        if (CurrentStep <= 0) { Debug.Log("[FaceFoldController] Previous blocked: already at first step."); return; }

        int stepIndex = CurrentStep - 1;
        StartCoroutine(AnimateFoldStep(model.steps[stepIndex], forward: false, onComplete: () =>
        {
            CurrentStep--;
            OnStepChanged?.Invoke(CurrentStep);
        }));
    }

    /// <summary>Immediately resets the paper to its initial flat state.</summary>
    public void Reset()
    {
        StopAllCoroutines();
        _isAnimating = false;
        CurrentStep  = 0;

        if (_data != null && _currentVertices != null)
        {
            Array.Copy(_data.BaseVertices, _currentVertices, _data.BaseVertices.Length);
            Array.Clear(_vertexHeightOffset, 0, _vertexHeightOffset.Length);

            for (int i = 0; i < _currentNormals.Length; i++)
                _currentNormals[i] = Vector3.up;

            ApplyVerticesToMesh();
        }

        OnStepChanged?.Invoke(CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    private void InitialiseMesh()
    {
        _data = FaceMeshImporter.Build(model.sourceMesh);
        if (_data == null) return;

        _frontMesh = new Mesh
        {
            name        = model.sourceMesh.name + "_FaceFold",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        _frontMesh.vertices  = _data.BaseVertices;
        _frontMesh.triangles = _data.Triangles;
        _frontMesh.normals   = _data.BaseNormals;
        _frontMesh.RecalculateBounds();
        _meshFilter.mesh = _frontMesh;

        _currentVertices    = (Vector3[])_data.BaseVertices.Clone();
        _currentNormals     = new Vector3[_data.BaseVertices.Length];
        _vertexHeightOffset = new float[_data.BaseVertices.Length];

        for (int i = 0; i < _currentNormals.Length; i++)
            _currentNormals[i] = Vector3.up;

        if (backMeshFilter != null)
        {
            _backMesh = PaperMeshGenerator.GenerateFlippedNormals(_frontMesh);
            backMeshFilter.mesh = _backMesh;
        }
    }

    // -------------------------------------------------------------------------
    // Vertex application
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pushes <see cref="_currentVertices"/> and <see cref="_currentNormals"/> into both meshes.
    /// Back-face vertices are offset slightly inward to prevent Z-fighting along fold creases.
    /// </summary>
    private void ApplyVerticesToMesh()
    {
        _frontMesh.vertices = _currentVertices;
        _frontMesh.normals  = _currentNormals;
        _frontMesh.RecalculateBounds();

        if (_backMesh != null)
        {
            const float kOffset = 0.002f;

            Vector3[] backVerts   = new Vector3[_currentVertices.Length];
            Vector3[] backNormals = new Vector3[_currentNormals.Length];

            for (int i = 0; i < _currentVertices.Length; i++)
            {
                backVerts[i]   = _currentVertices[i] - _currentNormals[i] * kOffset;
                backNormals[i] = -_currentNormals[i];
            }

            _backMesh.vertices = backVerts;
            _backMesh.normals  = backNormals;
            _backMesh.RecalculateBounds();
        }
    }

    // -------------------------------------------------------------------------
    // Fold animation
    // -------------------------------------------------------------------------

    private IEnumerator AnimateFoldStep(FaceFoldStep step, bool forward, Action onComplete)
    {
        _isAnimating = true;

        // Collect the union of vertex indices for all faces in this step.
        int[] foldVerts = CollectFoldVertices(step);

        // Snapshot positions and normals at the start of this fold.
        Vector3[] preFoldVertices = (Vector3[])_currentVertices.Clone();
        Vector3[] preFoldNormals  = (Vector3[])_currentNormals.Clone();

        // Derive flat positions by stripping accumulated height offsets.
        Vector3[] preFlatVerts = new Vector3[preFoldVertices.Length];
        for (int i = 0; i < preFlatVerts.Length; i++)
        {
            preFlatVerts[i]   = preFoldVertices[i];
            preFlatVerts[i].y -= _vertexHeightOffset[i];
        }

        // Build the world-space fold axis from the step's local XZ axis definition.
        Vector3 lineStart = transform.TransformPoint(new Vector3(step.axisStart.x, 0f, step.axisStart.y));
        Vector3 lineEnd   = transform.TransformPoint(new Vector3(step.axisEnd.x,   0f, step.axisEnd.y));
        Vector3 axisDir   = (lineEnd - lineStart).normalized;

        float targetAngle = forward ? step.foldAngle : -step.foldAngle;
        float thickness   = model != null ? model.paperThickness : 0f;
        float heightSign  = forward ? 1f : -1f;

        float elapsed  = 0f;
        float duration = Mathf.Max(step.duration, 0.01f);

        // Animate frame-by-frame.
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float angle   = Mathf.Lerp(0f, targetAngle, smoothT);

            Quaternion rotation    = Quaternion.AngleAxis(angle, axisDir);
            float      heightDelta = thickness * smoothT * heightSign;

            Array.Copy(preFoldVertices, _currentVertices, preFoldVertices.Length);
            Array.Copy(preFoldNormals,  _currentNormals,  preFoldNormals.Length);

            foreach (int idx in foldVerts)
            {
                // Rotate the flat geometry position around the fold axis.
                Vector3 worldFlat = transform.TransformPoint(preFlatVerts[idx]);
                Vector3 rotated   = rotation * (worldFlat - lineStart);
                Vector3 localPos  = transform.InverseTransformPoint(lineStart + rotated);

                // Re-add accumulated height plus the in-progress gain.
                localPos.y            += _vertexHeightOffset[idx] + heightDelta;
                _currentVertices[idx]  = localPos;

                // Rotate normal analytically.
                Vector3 worldNormal   = transform.TransformDirection(preFoldNormals[idx]);
                Vector3 rotatedNormal = rotation * worldNormal;
                _currentNormals[idx]  = transform.InverseTransformDirection(rotatedNormal);
            }

            ApplyVerticesToMesh();
            yield return null;
        }

        // Commit the permanent height change.
        float committedDelta = thickness * heightSign;
        foreach (int idx in foldVerts)
            _vertexHeightOffset[idx] += committedDelta;

        // Snap to exact final positions and normals.
        Quaternion finalRotation = Quaternion.AngleAxis(targetAngle, axisDir);
        Array.Copy(preFoldVertices, _currentVertices, preFoldVertices.Length);
        Array.Copy(preFoldNormals,  _currentNormals,  preFoldNormals.Length);

        foreach (int idx in foldVerts)
        {
            Vector3 worldFlat = transform.TransformPoint(preFlatVerts[idx]);
            Vector3 rotated   = finalRotation * (worldFlat - lineStart);
            Vector3 localPos  = transform.InverseTransformPoint(lineStart + rotated);
            localPos.y            += _vertexHeightOffset[idx];
            _currentVertices[idx]  = localPos;

            Vector3 worldNormal   = transform.TransformDirection(preFoldNormals[idx]);
            _currentNormals[idx]  = transform.InverseTransformDirection(finalRotation * worldNormal);
        }

        ApplyVerticesToMesh();

        _isAnimating = false;
        onComplete?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the flat union of vertex indices belonging to all faces listed in <paramref name="step"/>.
    /// Logs a warning for any out-of-range face index.
    /// </summary>
    private int[] CollectFoldVertices(FaceFoldStep step)
    {
        List<int> result = new List<int>();

        if (step.faceIndices == null) return result.ToArray();

        int faceCount = _data.Faces.Length;

        foreach (int faceIdx in step.faceIndices)
        {
            if (faceIdx < 0 || faceIdx >= faceCount)
            {
                Debug.LogWarning($"[FaceFoldController] Step '{step.label}': face index {faceIdx} is out of range (0–{faceCount - 1}). Skipping.");
                continue;
            }

            foreach (int vertIdx in _data.Faces[faceIdx].vertexIndices)
                result.Add(vertIdx);
        }

        return result.ToArray();
    }
}
