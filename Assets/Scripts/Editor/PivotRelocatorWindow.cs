using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window that relocates the mesh pivot of a selected GameObject without
/// visually moving the object or any of its children.
/// Open via: Tools ▶ Pivot Relocator
/// </summary>
public sealed class PivotRelocatorWindow : EditorWindow
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string MenuPath = "Tools/Pivot Relocator";
    private const string SaveFolder = "Assets/Meshes/Modified";

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private enum PivotPreset
    {
        BoundsCenter,
        BoundsBottom,
        BoundsTop,
        BoundsLeft,
        BoundsRight,
        BoundsFront,
        BoundsBack,
        CustomLocalPosition
    }

    private PivotPreset _preset = PivotPreset.BoundsCenter;
    private Vector3 _customLocalPosition = Vector3.zero;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    [MenuItem(MenuPath)]
    public static void ShowWindow()
    {
        PivotRelocatorWindow window = GetWindow<PivotRelocatorWindow>("Pivot Relocator");
        window.minSize = new Vector2(300f, 240f);
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(4f);

        GameObject target = GetValidTarget();
        if (target == null) return;

        DrawPresetField();
        DrawCustomPositionField();
        EditorGUILayout.Space(8f);
        DrawPreview(target);
        EditorGUILayout.Space(8f);
        DrawApplyButton(target);
    }

    private void OnSelectionChange() => Repaint();

    // -------------------------------------------------------------------------
    // GUI sections
    // -------------------------------------------------------------------------

    private static void DrawHeader()
    {
        EditorGUILayout.LabelField("Pivot Relocator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Shifts the mesh pivot without moving the object visually. " +
            "A new mesh asset is saved so the original is never modified.",
            MessageType.Info);
    }

    /// <summary>Returns the selected GameObject if it has a usable mesh; draws an error otherwise.</summary>
    private static GameObject GetValidTarget()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorGUILayout.HelpBox("Select a GameObject with a MeshFilter or SkinnedMeshRenderer.", MessageType.Warning);
            return null;
        }

        if (!HasEditableMesh(go))
        {
            EditorGUILayout.HelpBox($"'{go.name}' has no MeshFilter or SkinnedMeshRenderer.", MessageType.Warning);
            return null;
        }

        EditorGUILayout.LabelField("Target", go.name, EditorStyles.helpBox);
        return go;
    }

    private void DrawPresetField()
    {
        _preset = (PivotPreset)EditorGUILayout.EnumPopup("New Pivot", _preset);
    }

    private void DrawCustomPositionField()
    {
        using (new EditorGUI.DisabledScope(_preset != PivotPreset.CustomLocalPosition))
        {
            _customLocalPosition = EditorGUILayout.Vector3Field("Local Position", _customLocalPosition);
        }
    }

    private void DrawPreview(GameObject target)
    {
        Mesh mesh = GetMesh(target);
        if (mesh == null) return;

        Vector3 pivotLocal = CalculatePivotLocal(mesh, _preset, _customLocalPosition);

        EditorGUILayout.LabelField("Resolved Pivot (local)", EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Vector3Field(GUIContent.none, pivotLocal);
        }
    }

    private void DrawApplyButton(GameObject target)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);

        if (GUILayout.Button("Apply Pivot", GUILayout.Height(32f)))
            Apply(target);

        GUI.backgroundColor = prev;
    }

    // -------------------------------------------------------------------------
    // Core logic
    // -------------------------------------------------------------------------

    private void Apply(GameObject target)
    {
        Mesh originalMesh = GetMesh(target);
        if (originalMesh == null)
        {
            Debug.LogError("[PivotRelocator] Could not read mesh from target.");
            return;
        }

        Vector3 pivotLocal = CalculatePivotLocal(originalMesh, _preset, _customLocalPosition);

        // No-op guard — avoid creating a mesh copy when nothing changes.
        if (pivotLocal.sqrMagnitude < Mathf.Epsilon)
        {
            Debug.Log("[PivotRelocator] Pivot is already at the target position. No changes made.");
            return;
        }

        Undo.SetCurrentGroupName("Relocate Pivot");
        int undoGroup = Undo.GetCurrentGroup();

        Mesh modifiedMesh = BuildShiftedMesh(originalMesh, pivotLocal);
        SaveMeshAsset(modifiedMesh, target.name);

        AssignMesh(target, modifiedMesh, undoGroup);
        ShiftTransformAndChildren(target, pivotLocal, undoGroup);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[PivotRelocator] Pivot relocated for '{target.name}'. New mesh saved to '{SaveFolder}'.");
    }

    // -------------------------------------------------------------------------
    // Pivot calculation
    // -------------------------------------------------------------------------

    /// <summary>Resolves the desired new pivot in the mesh's local space.</summary>
    private static Vector3 CalculatePivotLocal(Mesh mesh, PivotPreset preset, Vector3 customLocal)
    {
        Bounds b = mesh.bounds;

        return preset switch
        {
            PivotPreset.BoundsCenter         => b.center,
            PivotPreset.BoundsBottom         => new Vector3(b.center.x, b.min.y, b.center.z),
            PivotPreset.BoundsTop            => new Vector3(b.center.x, b.max.y, b.center.z),
            PivotPreset.BoundsLeft           => new Vector3(b.min.x,    b.center.y, b.center.z),
            PivotPreset.BoundsRight          => new Vector3(b.max.x,    b.center.y, b.center.z),
            PivotPreset.BoundsFront          => new Vector3(b.center.x, b.center.y, b.min.z),
            PivotPreset.BoundsBack           => new Vector3(b.center.x, b.center.y, b.max.z),
            PivotPreset.CustomLocalPosition  => customLocal,
            _                                => b.center
        };
    }

    // -------------------------------------------------------------------------
    // Mesh manipulation
    // -------------------------------------------------------------------------

    /// <summary>Creates a new mesh with all vertices shifted by -<paramref name="pivotLocal"/>.</summary>
    private static Mesh BuildShiftedMesh(Mesh source, Vector3 pivotLocal)
    {
        Vector3[] srcVertices = source.vertices;
        Vector3[] dstVertices = new Vector3[srcVertices.Length];

        for (int i = 0; i < srcVertices.Length; i++)
            dstVertices[i] = srcVertices[i] - pivotLocal;

        Mesh shifted = Object.Instantiate(source);
        shifted.name = source.name + "_PivotShifted";
        shifted.vertices = dstVertices;
        shifted.RecalculateBounds();
        return shifted;
    }

    private static void SaveMeshAsset(Mesh mesh, string objectName)
    {
        if (!AssetDatabase.IsValidFolder(SaveFolder))
        {
            System.IO.Directory.CreateDirectory(SaveFolder);
            AssetDatabase.Refresh();
        }

        string path = $"{SaveFolder}/{objectName}_PivotShifted.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            // Overwrite in-place so prior references stay valid.
            EditorUtility.CopySerialized(mesh, existing);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
        }
    }

    // -------------------------------------------------------------------------
    // Component & transform updates
    // -------------------------------------------------------------------------

    private static void AssignMesh(GameObject target, Mesh mesh, int undoGroup)
    {
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf != null)
        {
            Undo.RecordObject(mf, "Assign Shifted Mesh");
            mf.sharedMesh = mesh;
            return;
        }

        SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
        {
            Undo.RecordObject(smr, "Assign Shifted Mesh");
            smr.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Moves the GameObject's world position by the world-space equivalent of
    /// <paramref name="pivotLocal"/>, then compensates every direct child so
    /// nothing visually moves.
    /// </summary>
    private static void ShiftTransformAndChildren(GameObject target, Vector3 pivotLocal, int undoGroup)
    {
        Transform t = target.transform;

        // Cache child world positions before any transform changes.
        int childCount = t.childCount;
        Vector3[] childWorldPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
            childWorldPositions[i] = t.GetChild(i).position;

        // Move pivot in world space: newWorldPos = oldWorldPos + R * (S * pivotLocal)
        Vector3 worldOffset = t.TransformVector(pivotLocal);
        Undo.RecordObject(t, "Shift Transform Pivot");
        t.position += worldOffset;

        // Restore each child to its original world position.
        for (int i = 0; i < childCount; i++)
        {
            Transform child = t.GetChild(i);
            Undo.RecordObject(child, "Compensate Child Transform");
            child.position = childWorldPositions[i];
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool HasEditableMesh(GameObject go)
    {
        return go.GetComponent<MeshFilter>() != null
            || go.GetComponent<SkinnedMeshRenderer>() != null;
    }

    private static Mesh GetMesh(GameObject go)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null) return mf.sharedMesh;

        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        return smr != null ? smr.sharedMesh : null;
    }
}
