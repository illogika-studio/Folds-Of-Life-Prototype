using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that creates the PaperPlane and Butterfly OrigamiModel ScriptableObject assets.
/// Run via the menu: Origami ▶ Build Model Assets.
/// Safe to re-run: existing assets are overwritten in-place.
/// </summary>
public static class OrigamiModelBuilder
{
    private const string DataFolder = "Assets/Data";

    [MenuItem("Origami/Build Model Assets")]
    public static void BuildAll()
    {
        EnsureFolder(DataFolder);
        BuildPaperPlane();
        BuildButterfly();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[OrigamiModelBuilder] Model assets created/updated successfully.");
    }

    // -------------------------------------------------------------------------
    // Paper Plane  (A4 proportions: 2.97 × 2.1, 20 × 14 subdivisions, 7 steps)
    // -------------------------------------------------------------------------

    private static void BuildPaperPlane()
    {
        string path = $"{DataFolder}/PaperPlane.asset";
        OrigamiModel model = LoadOrCreate<OrigamiModel>(path);

        model.modelName = "Paper Plane";
        model.paperWidth = 2.97f;
        model.paperHeight = 2.1f;
        model.subdivisionsX = 20;
        model.subdivisionsY = 14;

        model.steps = new List<OrigamiStep>
        {
            // Step 1 — Show flat rectangle.
            new OrigamiStep
            {
                instructionText = "Start with a flat sheet of paper.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(0f,  1.05f),
                foldAngle       = 0f,
                duration        = 0.5f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = false, width = 0.02f },
                arrow           = new ArrowSettings { visible = false }
            },

            // Step 2 — Fold in half vertically (center crease), then unfold.
            new OrigamiStep
            {
                instructionText = "Fold in half along the center crease, then unfold.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(0f,  1.05f),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0.8f, 0f),
                    arrowDirection = new Vector2(-1f, 0f)
                }
            },

            // Step 3 — Fold top-right corner to center line.
            new OrigamiStep
            {
                instructionText = "Fold the top-right corner down to the center crease.",
                foldLineStart   = new Vector2(0f,    0.525f),
                foldLineEnd     = new Vector2(1.485f, 1.05f),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.AboveDiagonal,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(1.0f, 0.8f),
                    arrowDirection = new Vector2(-0.7f, -0.7f)
                }
            },

            // Step 4 — Fold new diagonal edges toward center line.
            new OrigamiStep
            {
                instructionText = "Fold the right diagonal edge toward the center line.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(1.485f, 0.525f),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(1.1f, 0f),
                    arrowDirection = new Vector2(-1f, 0f)
                }
            },

            // Step 5 — Fold the model in half along center line.
            new OrigamiStep
            {
                instructionText = "Fold the entire model in half along the center crease.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(0f,  1.05f),
                foldAngle       = -90f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Left,
                guideLine       = new GuideLineSettings { visible = true, width = 0.025f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(-0.5f, 0f),
                    arrowDirection = new Vector2(1f, 0f)
                }
            },

            // Step 6 — Fold wings down on both sides.
            new OrigamiStep
            {
                instructionText = "Fold the wing down along the body edge.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(1.485f, 0f),
                foldAngle       = -90f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0.7f, 0.6f),
                    arrowDirection = new Vector2(0f, -1f)
                }
            },

            // Step 7 — Final paper plane.
            new OrigamiStep
            {
                instructionText = "Your paper plane is ready! Hold at the fold and throw gently.",
                foldLineStart   = new Vector2(0f, -1.05f),
                foldLineEnd     = new Vector2(0f,  1.05f),
                foldAngle       = 0f,
                duration        = 0.5f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = false, width = 0.02f },
                arrow           = new ArrowSettings { visible = false }
            }
        };

        Save(model, path);
    }

    // -------------------------------------------------------------------------
    // Butterfly  (2.5 × 2.5 square, 20 × 20 subdivisions, 10 steps)
    // -------------------------------------------------------------------------

    private static void BuildButterfly()
    {
        string path = $"{DataFolder}/Butterfly.asset";
        OrigamiModel model = LoadOrCreate<OrigamiModel>(path);

        model.modelName = "Butterfly";
        model.paperWidth = 2.5f;
        model.paperHeight = 2.5f;
        model.subdivisionsX = 20;
        model.subdivisionsY = 20;

        float h = 1.25f; // half-size

        model.steps = new List<OrigamiStep>
        {
            // Step 1 — Show flat square.
            new OrigamiStep
            {
                instructionText = "Start with a flat square sheet of paper.",
                foldLineStart   = new Vector2(-h, 0f),
                foldLineEnd     = new Vector2(h, 0f),
                foldAngle       = 0f,
                duration        = 0.5f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = false, width = 0.02f },
                arrow           = new ArrowSettings { visible = false }
            },

            // Step 2 — Horizontal crease guide.
            new OrigamiStep
            {
                instructionText = "Fold in half horizontally to mark the center crease.",
                foldLineStart   = new Vector2(-h, 0f),
                foldLineEnd     = new Vector2(h, 0f),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0f, 0.6f),
                    arrowDirection = new Vector2(0f, -1f)
                }
            },

            // Step 3 — Vertical crease guide.
            new OrigamiStep
            {
                instructionText = "Unfold, then fold in half vertically to mark the vertical crease.",
                foldLineStart   = new Vector2(0f, -h),
                foldLineEnd     = new Vector2(0f,  h),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0.6f, 0f),
                    arrowDirection = new Vector2(-1f, 0f)
                }
            },

            // Step 4 — Diagonal crease.
            new OrigamiStep
            {
                instructionText = "Unfold, then fold along the diagonal to mark the diagonal crease.",
                foldLineStart   = new Vector2(-h, -h),
                foldLineEnd     = new Vector2(h,   h),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.AboveDiagonal,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(-0.5f, 0.5f),
                    arrowDirection = new Vector2(1f, -1f)
                }
            },

            // Step 5 — Collapse into triangle base.
            new OrigamiStep
            {
                instructionText = "Collapse into a triangle base using the creases.",
                foldLineStart   = new Vector2(-h, 0f),
                foldLineEnd     = new Vector2(h, 0f),
                foldAngle       = 180f,
                duration        = 1.2f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0f, 0.8f),
                    arrowDirection = new Vector2(0f, -1f)
                }
            },

            // Step 6 — Fold side flaps toward center.
            new OrigamiStep
            {
                instructionText = "Fold the left side flap toward the center line.",
                foldLineStart   = new Vector2(0f, -h),
                foldLineEnd     = new Vector2(0f, 0f),
                foldAngle       = 90f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Left,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(-0.6f, -0.4f),
                    arrowDirection = new Vector2(1f, 0f)
                }
            },

            // Step 7 — Flip model.
            new OrigamiStep
            {
                instructionText = "Flip the model over.",
                foldLineStart   = new Vector2(-h, 0f),
                foldLineEnd     = new Vector2(h, 0f),
                foldAngle       = 180f,
                duration        = 0.8f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = false, width = 0.02f },
                arrow           = new ArrowSettings { visible = false }
            },

            // Step 8 — Fold top layer downward.
            new OrigamiStep
            {
                instructionText = "Fold the top layer downward to the bottom edge.",
                foldLineStart   = new Vector2(-h, 0f),
                foldLineEnd     = new Vector2(h, 0f),
                foldAngle       = 180f,
                duration        = 1.0f,
                regionMode      = FoldRegionMode.Top,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0f, 0.4f),
                    arrowDirection = new Vector2(0f, -1f)
                }
            },

            // Step 9 — Fold in half slightly for butterfly body.
            new OrigamiStep
            {
                instructionText = "Fold in half slightly at the center to form the butterfly body.",
                foldLineStart   = new Vector2(0f, -h),
                foldLineEnd     = new Vector2(0f, h),
                foldAngle       = -20f,
                duration        = 0.8f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = true, width = 0.02f },
                arrow           = new ArrowSettings
                {
                    visible        = true,
                    arrowOrigin    = new Vector2(0.3f, 0f),
                    arrowDirection = new Vector2(-1f, 0f)
                }
            },

            // Step 10 — Open wings outward.
            new OrigamiStep
            {
                instructionText = "Gently open the wings outward. Your butterfly is complete!",
                foldLineStart   = new Vector2(0f, -h),
                foldLineEnd     = new Vector2(0f, h),
                foldAngle       = 0f,
                duration        = 0.8f,
                regionMode      = FoldRegionMode.Right,
                guideLine       = new GuideLineSettings { visible = false, width = 0.02f },
                arrow           = new ArrowSettings { visible = false }
            }
        };

        Save(model, path);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        T created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static void Save(Object asset, string path)
    {
        EditorUtility.SetDirty(asset);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }
}
