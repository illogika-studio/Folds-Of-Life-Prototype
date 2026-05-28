using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject asset that pairs a pre-divided source mesh with an ordered list of
/// <see cref="FaceFoldStep"/>s and the materials used for front and back paper surfaces.
/// </summary>
[CreateAssetMenu(menuName = "Origami/Face Fold Model")]
public class FaceFoldModel : ScriptableObject
{
    /// <summary>Pre-divided mesh asset to fold. Must use unshared (per-face) vertices.</summary>
    public Mesh sourceMesh;

    /// <summary>Material applied to the front (top) surface of the paper.</summary>
    public Material frontMaterial;

    /// <summary>Material applied to the back (bottom) surface of the paper.</summary>
    public Material backMaterial;

    /// <summary>
    /// Physical thickness of one paper layer in world units.
    /// Each fold lifts the folded vertices by this amount to simulate paper stacking.
    /// </summary>
    public float paperThickness;

    /// <summary>Ordered list of fold steps to execute.</summary>
    public List<FaceFoldStep> steps;
}
