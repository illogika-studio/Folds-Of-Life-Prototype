using System.Collections.Generic;
using UnityEngine;

/// <summary>ScriptableObject asset that fully describes one origami model and all its fold steps.</summary>
[CreateAssetMenu(fileName = "OrigamiModel", menuName = "Origami/Model")]
public class OrigamiModel : ScriptableObject
{
    public string modelName;
    public float paperWidth;
    public float paperHeight;
    public int subdivisionsX;
    public int subdivisionsY;

    /// <summary>
    /// Physical thickness of one paper layer in world units.
    /// Each fold adds this offset to the folded vertices; fold-within-fold doubles it.
    /// </summary>
    public float paperThickness;

    public Material frontMaterial;
    public Material backMaterial;
    public List<OrigamiStep> steps;
}
