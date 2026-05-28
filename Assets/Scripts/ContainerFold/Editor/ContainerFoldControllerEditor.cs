using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="ContainerFoldController"/>.
/// Renders a reorderable list of fold steps, each expandable to show its movements.
/// Steps live on the MonoBehaviour so scene Transform references can be assigned directly.
/// </summary>
[CustomEditor(typeof(ContainerFoldController))]
public class ContainerFoldControllerEditor : Editor
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float LineHeight = 20f;
    private const float Padding    = 4f;
    private const float LabelWidth = 116f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private SerializedProperty _model;
    private SerializedProperty _paperRoot;
    private SerializedProperty _steps;

    private ReorderableList _stepList;
    private int _expandedStep = -1;

    // -------------------------------------------------------------------------
    // Enable
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        _model     = serializedObject.FindProperty(nameof(ContainerFoldController.model));
        _paperRoot = serializedObject.FindProperty(nameof(ContainerFoldController.paperRoot));
        _steps     = serializedObject.FindProperty(nameof(ContainerFoldController.steps));

        BuildStepList();
    }

    // -------------------------------------------------------------------------
    // Inspector draw
    // -------------------------------------------------------------------------

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── References ────────────────────────────────────────────────────
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_model,     new GUIContent("Model (materials / thickness)"));
        EditorGUILayout.PropertyField(_paperRoot, new GUIContent("Paper Root"));

        // Show paper thickness read-only if model is assigned.
        var modelAsset = _model.objectReferenceValue as ContainerFoldModel;
        if (modelAsset != null)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.FloatField("  Paper Thickness", modelAsset.paperThickness);
        }

        EditorGUILayout.Space(6f);

        // ── Steps ──────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Fold Steps", EditorStyles.boldLabel);
        _stepList.DoLayoutList();

        // ── Playback buttons (Edit Mode convenience) ───────────────────────
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            var ctrl = (ContainerFoldController)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(ctrl.CurrentStep <= 0 || ctrl.IsAnimating))
                    if (GUILayout.Button("◀ Prev")) ctrl.Previous();

                using (new EditorGUI.DisabledScope(ctrl.CurrentStep >= ctrl.TotalSteps || ctrl.IsAnimating))
                    if (GUILayout.Button("Next ▶")) ctrl.Next();

                using (new EditorGUI.DisabledScope(ctrl.IsAnimating))
                    if (GUILayout.Button("Reset")) ctrl.Reset();
            }

            EditorGUILayout.LabelField($"Current step: {ctrl.CurrentStep} / {ctrl.TotalSteps}",
                EditorStyles.miniLabel);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // -------------------------------------------------------------------------
    // ReorderableList construction
    // -------------------------------------------------------------------------

    private void BuildStepList()
    {
        _stepList = new ReorderableList(serializedObject, _steps,
            draggable: true, displayHeader: true,
            displayAddButton: true, displayRemoveButton: true)
        {
            drawHeaderCallback    = rect => EditorGUI.LabelField(rect, "Steps"),
            elementHeightCallback = GetStepHeight,
            drawElementCallback   = DrawStep,
            onAddCallback         = list =>
            {
                _steps.arraySize++;
                serializedObject.ApplyModifiedProperties();
                InitNewStep(_steps.GetArrayElementAtIndex(_steps.arraySize - 1));
                serializedObject.ApplyModifiedProperties();
            },
        };
    }

    private static void InitNewStep(SerializedProperty step)
    {
        step.FindPropertyRelative("label").stringValue   = "New Step";
        step.FindPropertyRelative("duration").floatValue = 0.4f;
        step.FindPropertyRelative("movements").arraySize = 0;
    }

    // -------------------------------------------------------------------------
    // Element height
    // -------------------------------------------------------------------------

    private float GetStepHeight(int index)
    {
        // Label + duration + movement count badge.
        float height = (LineHeight + Padding) * 3f;

        if (_expandedStep != index) return height;

        SerializedProperty movements = _steps.GetArrayElementAtIndex(index)
            .FindPropertyRelative("movements");

        // "Movements" header + 2 rows per movement (Container + Target) + Add button.
        height += LineHeight + Padding;
        height += movements.arraySize * (LineHeight + Padding) * 2f;
        height += LineHeight + Padding;
        return height;
    }

    // -------------------------------------------------------------------------
    // Element draw
    // -------------------------------------------------------------------------

    private void DrawStep(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty step      = _steps.GetArrayElementAtIndex(index);
        SerializedProperty label     = step.FindPropertyRelative("label");
        SerializedProperty duration  = step.FindPropertyRelative("duration");
        SerializedProperty movements = step.FindPropertyRelative("movements");

        float x = rect.x;
        float y = rect.y + Padding * 0.5f;
        float w = rect.width;

        // ── Label + expand toggle ─────────────────────────────────────────
        EditorGUI.PropertyField(new Rect(x, y, w - 64f, LineHeight), label, GUIContent.none);

        bool expanded    = _expandedStep == index;
        bool newExpanded = EditorGUI.Foldout(new Rect(x + w - 60f, y, 60f, LineHeight), expanded, "Edit");
        if (newExpanded != expanded)
            _expandedStep = newExpanded ? index : -1;

        y += LineHeight + Padding;

        // ── Duration ──────────────────────────────────────────────────────
        EditorGUI.LabelField(new Rect(x, y, LabelWidth, LineHeight), "Duration (s)");
        duration.floatValue = EditorGUI.FloatField(new Rect(x + LabelWidth, y, w - LabelWidth, LineHeight),
            duration.floatValue);
        y += LineHeight + Padding;

        // ── Movement count badge ──────────────────────────────────────────
        EditorGUI.LabelField(new Rect(x, y, w, LineHeight),
            $"Movements: {movements.arraySize}", EditorStyles.miniLabel);
        y += LineHeight + Padding;

        if (_expandedStep != index) return;

        // ── Movements ─────────────────────────────────────────────────────
        EditorGUI.LabelField(new Rect(x, y, w, LineHeight), "Movements", EditorStyles.boldLabel);
        y += LineHeight + Padding;

        for (int mi = 0; mi < movements.arraySize; mi++)
        {
            SerializedProperty mov       = movements.GetArrayElementAtIndex(mi);
            SerializedProperty container = mov.FindPropertyRelative("container");
            SerializedProperty targetRot = mov.FindPropertyRelative("targetEulerAngles");

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.9f, 1f);
            GUI.Box(new Rect(x, y, w, (LineHeight + Padding) * 2f - Padding), GUIContent.none);
            GUI.backgroundColor = prev;

            DrawField(ref y, x, w, "Container",       container);
            DrawField(ref y, x, w, "Target Rotation", targetRot);

            if (GUI.Button(new Rect(x + w - 62f, y - (LineHeight + Padding), 62f, LineHeight - 2f),
                "Remove"))
            {
                movements.DeleteArrayElementAtIndex(mi);
                break;
            }
        }

        // ── Add movement ──────────────────────────────────────────────────
        if (GUI.Button(new Rect(x, y, w, LineHeight), "+ Add Movement"))
            movements.arraySize++;
    }

    private static void DrawField(ref float y, float x, float w, string fieldLabel, SerializedProperty prop)
    {
        EditorGUI.LabelField(new Rect(x, y, LabelWidth, LineHeight), fieldLabel);
        EditorGUI.PropertyField(new Rect(x + LabelWidth, y, w - LabelWidth, LineHeight),
            prop, GUIContent.none);
        y += LineHeight + Padding;
    }
}
