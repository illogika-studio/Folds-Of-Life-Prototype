using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects Prev / Next / Reset UGUI buttons to a <see cref="ContainerFoldController"/>
/// and keeps the step label and progress counter up to date.
/// Buttons are automatically disabled while an animation is running.
/// </summary>
public class ContainerFoldStepUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [SerializeField] private ContainerFoldController _controller;

    [Tooltip("Displays the current step's label.")]
    [SerializeField] private TextMeshProUGUI _stepLabelText;

    [Tooltip("Displays 'Step X / N' style progress.")]
    [SerializeField] private TextMeshProUGUI _stepCounterText;

    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _resetButton;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_controller == null)
        {
            Debug.LogError("[ContainerFoldStepUI] No ContainerFoldController assigned.");
            return;
        }

        _controller.OnStepChanged      += HandleStepChanged;
        _controller.OnAnimatingChanged += HandleAnimatingChanged;

        _prevButton?.onClick.AddListener(_controller.Previous);
        _nextButton?.onClick.AddListener(_controller.Next);
        _resetButton?.onClick.AddListener(_controller.Reset);
    }

    private void Start()
    {
        if (_controller == null) return;
        HandleStepChanged(_controller.CurrentStep);
    }

    private void OnDestroy()
    {
        if (_controller == null) return;
        _controller.OnStepChanged      -= HandleStepChanged;
        _controller.OnAnimatingChanged -= HandleAnimatingChanged;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private void HandleStepChanged(int stepIndex)
    {
        if (_controller == null) return;

        int total = _controller.TotalSteps;

        if (_stepCounterText != null)
            _stepCounterText.text = $"Step {stepIndex + 1} / {total}";

        if (_stepLabelText != null && stepIndex >= 0 && stepIndex < total)
            _stepLabelText.text = _controller.steps[stepIndex].label;

        RefreshButtonStates(stepIndex, _controller.IsAnimating);
    }

    private void HandleAnimatingChanged(bool isAnimating)
    {
        RefreshButtonStates(_controller.CurrentStep, isAnimating);
    }

    private void RefreshButtonStates(int stepIndex, bool isAnimating)
    {
        if (_prevButton != null)
            _prevButton.interactable = !isAnimating && stepIndex > 0;

        if (_nextButton != null)
            _nextButton.interactable = !isAnimating && stepIndex < _controller.TotalSteps;

        // Reset is always available (even mid-animation it stops and snaps back).
        if (_resetButton != null)
            _resetButton.interactable = true;
    }
}
