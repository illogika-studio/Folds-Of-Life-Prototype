using UnityEngine;

/// <summary>
/// Renders the dotted fold-line guide and directional arrow guide for the current origami step.
/// Expects two LineRenderer child components: one for the fold line, one for the arrow.
/// </summary>
public class OrigamiGuideRenderer : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Tooltip("LineRenderer used to draw the dotted fold line.")]
    [SerializeField] private LineRenderer _foldLineRenderer;

    [Tooltip("LineRenderer used to draw the directional arrow shaft + head.")]
    [SerializeField] private LineRenderer _arrowRenderer;

    [Tooltip("How many times the dot texture tiles per unit length of the fold line.")]
    [SerializeField] private float _tilingFactor = 10f;

    [Tooltip("Length of the arrowhead lines in world units.")]
    [SerializeField] private float _arrowHeadLength = 0.08f;

    [Tooltip("Half-angle of the arrowhead in degrees.")]
    [SerializeField] private float _arrowHeadAngle = 30f;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Updates both renderers to match the given step and shows them.</summary>
    public void ShowStep(OrigamiStep step, Transform paperTransform)
    {
        ShowFoldLine(step, paperTransform);
        ShowArrow(step, paperTransform);
    }

    /// <summary>Hides both guide renderers.</summary>
    public void Hide()
    {
        if (_foldLineRenderer != null) _foldLineRenderer.enabled = false;
        if (_arrowRenderer != null) _arrowRenderer.enabled = false;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void ShowFoldLine(OrigamiStep step, Transform paperTransform)
    {
        if (_foldLineRenderer == null) return;

        if (!step.guideLine.visible)
        {
            _foldLineRenderer.enabled = false;
            return;
        }

        _foldLineRenderer.enabled = true;
        _foldLineRenderer.positionCount = 2;

        Vector3 start = paperTransform.TransformPoint(
            new Vector3(step.foldLineStart.x, 0.005f, step.foldLineStart.y));
        Vector3 end = paperTransform.TransformPoint(
            new Vector3(step.foldLineEnd.x, 0.005f, step.foldLineEnd.y));

        _foldLineRenderer.SetPosition(0, start);
        _foldLineRenderer.SetPosition(1, end);

        float width = Mathf.Max(step.guideLine.width, 0.005f);
        _foldLineRenderer.startWidth = width;
        _foldLineRenderer.endWidth = width;

        _foldLineRenderer.textureMode = LineTextureMode.Tile;
        float lineLength = Vector3.Distance(start, end);
        _foldLineRenderer.textureScale = new Vector2(_tilingFactor * lineLength, 1f);
    }

    private void ShowArrow(OrigamiStep step, Transform paperTransform)
    {
        if (_arrowRenderer == null) return;

        if (!step.arrow.visible)
        {
            _arrowRenderer.enabled = false;
            return;
        }

        _arrowRenderer.enabled = true;

        Vector3 origin = paperTransform.TransformPoint(
            new Vector3(step.arrow.arrowOrigin.x, 0.01f, step.arrow.arrowOrigin.y));
        Vector3 dir = paperTransform.TransformDirection(
            new Vector3(step.arrow.arrowDirection.x, 0f, step.arrow.arrowDirection.y).normalized);
        Vector3 tip = origin + dir * 0.3f;

        // Arrow shaft + two arrowhead lines (5 positions: origin → tip, tip → head1, tip, tip → head2).
        Vector3 headLeft = tip - Quaternion.Euler(0f, -_arrowHeadAngle, 0f) * dir * _arrowHeadLength;
        Vector3 headRight = tip - Quaternion.Euler(0f, _arrowHeadAngle, 0f) * dir * _arrowHeadLength;

        _arrowRenderer.positionCount = 5;
        _arrowRenderer.SetPosition(0, origin);
        _arrowRenderer.SetPosition(1, tip);
        _arrowRenderer.SetPosition(2, headLeft);
        _arrowRenderer.SetPosition(3, tip);
        _arrowRenderer.SetPosition(4, headRight);
    }
}
