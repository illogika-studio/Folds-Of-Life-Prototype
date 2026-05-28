// Note: this file lives in an Editor folder but the class itself is NOT editor-only so it
// can be added as a component via the Inspector. Only the UnityEditor API calls are guarded.
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gizmo that draws each triangle's face index at its centroid and each unique vertex's
/// index plus local XZ position in the Scene view.
/// Attach to the same GameObject that holds the <see cref="MeshFilter"/> you want to inspect.
/// </summary>
[AddComponentMenu("Origami/Face Index Gizmo")]
public class FaceIndexGizmo : MonoBehaviour
{
    [Tooltip("Draw the face index number at each triangle centroid.")]
    public bool showFaceIndices = true;

    [Tooltip("Draw each unique vertex index and its local XZ coordinates.")]
    public bool showVertexPositions = true;

    [Tooltip("Colour of face index labels.")]
    public Color faceColour = Color.yellow;

    [Tooltip("Colour of vertex labels.")]
    public Color vertexColour = Color.cyan;

    private void OnDrawGizmos()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        Mesh mesh = mf.sharedMesh;
        if (mesh == null) return;

        Vector3[] vertices  = mesh.vertices;
        int[]     triangles = mesh.triangles;
        int       faceCount = triangles.Length / 3;

#if UNITY_EDITOR
        // --- Face indices ---
        if (showFaceIndices)
        {
            GUIStyle faceStyle = new GUIStyle();
            faceStyle.normal.textColor = faceColour;
            faceStyle.fontSize = 14;
            faceStyle.fontStyle = FontStyle.Bold;

            for (int f = 0; f < faceCount; f++)
            {
                int b        = f * 3;
                Vector3 v0   = transform.TransformPoint(vertices[triangles[b]]);
                Vector3 v1   = transform.TransformPoint(vertices[triangles[b + 1]]);
                Vector3 v2   = transform.TransformPoint(vertices[triangles[b + 2]]);
                Vector3 cent = (v0 + v1 + v2) / 3f;
                Handles.Label(cent, $"F{f}", faceStyle);
            }
        }

        // --- Vertex positions (unique, deduplicated) ---
        if (showVertexPositions)
        {
            GUIStyle vtxStyle = new GUIStyle();
            vtxStyle.normal.textColor = vertexColour;
            vtxStyle.fontSize = 11;

            HashSet<int> labelled = new HashSet<int>();

            for (int i = 0; i < triangles.Length; i++)
            {
                int vi = triangles[i];
                if (!labelled.Add(vi)) continue;

                Vector3 local = vertices[vi];
                Vector3 world = transform.TransformPoint(local);
                Handles.Label(world, $"V{vi}\n({local.x:F2},{local.z:F2})", vtxStyle);

                Gizmos.color = vertexColour;
                Gizmos.DrawSphere(world, 0.005f);
            }
        }
#endif
    }
}
