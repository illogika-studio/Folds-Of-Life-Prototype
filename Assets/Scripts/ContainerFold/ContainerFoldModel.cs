using UnityEngine;

/// <summary>
/// ScriptableObject that holds the shared, asset-level paper properties for a
/// container-based origami model: materials and paper thickness.
/// <para>
/// The fold steps (which reference scene <see cref="Transform"/>s) are stored
/// directly on <see cref="ContainerFoldController"/> instead, because ScriptableObject
/// assets cannot hold scene-object references.
/// </para>
/// </summary>
[CreateAssetMenu(menuName = "Origami/Container Fold Model")]
public class ContainerFoldModel : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    /// <summary>Material applied to the front (top) surface of the paper.</summary>
    public Material frontMaterial;

    /// <summary>Material applied to the back (bottom) surface of the paper.</summary>
    public Material backMaterial;

    // -------------------------------------------------------------------------
    // Paper properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Physical thickness of one paper layer in world units.
    /// Reference this when setting <see cref="ContainerMovement.localTranslation"/>
    /// on each step so layers never interpenetrate.
    /// </summary>
    [Min(0f)]
    public float paperThickness = 0.002f;
}
