using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI panel controller. Listens to OrigamiPaperController events
/// and keeps the title, step counter, instruction text, and button states in sync.
/// </summary>
public class OrigamiStepUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] private OrigamiPaperController _controller;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _stepText;
    [SerializeField] private TextMeshProUGUI _instructionText;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _resetButton;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (_controller == null)
        {
            Debug.LogError("[OrigamiStepUI] No OrigamiPaperController assigned.");
            return;
        }

        _controller.OnStepChanged += HandleStepChanged;

        _prevButton?.onClick.AddListener(_controller.Previous);
        _nextButton?.onClick.AddListener(_controller.Next);
        _resetButton?.onClick.AddListener(_controller.Reset);

        // Initialise with the current state.
        HandleStepChanged(_controller.CurrentStep);
    }

    private void OnDestroy()
    {
        if (_controller != null)
            _controller.OnStepChanged -= HandleStepChanged;
    }

    // -------------------------------------------------------------------------
    // Event handler
    // -------------------------------------------------------------------------

    private void HandleStepChanged(int stepIndex)
    {
        if (_controller.model == null) return;

        int total = _controller.TotalSteps;

        if (_titleText != null)
            _titleText.text = _controller.model.modelName;

        if (_stepText != null)
            _stepText.text = $"Step {stepIndex + 1} / {total}";

        if (_instructionText != null && stepIndex >= 0 && stepIndex < total)
            _instructionText.text = _controller.model.steps[stepIndex].instructionText;

        if (_prevButton != null)
            _prevButton.interactable = stepIndex > 0;

        if (_nextButton != null)
            _nextButton.interactable = stepIndex < total - 1;
    }
}
