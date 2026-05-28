using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects Prev / Next / Reset UGUI buttons to a <see cref="FaceFoldController"/>
/// and displays the current step label and progress counter.
/// </summary>
public class FaceFoldStepUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [SerializeField] private FaceFoldController _controller;

    [Tooltip("Displays the current step's label string.")]
    [SerializeField] private TextMeshProUGUI _stepLabelText;

    [Tooltip("Displays '1 / N' style progress.")]
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
            Debug.LogError("[FaceFoldStepUI] No FaceFoldController assigned.");
            return;
        }

        // Subscribe early so we never miss an event fired during Start.
        _controller.OnStepChanged += HandleStepChanged;

        _prevButton?.onClick.AddListener(_controller.Previous);
        _nextButton?.onClick.AddListener(_controller.Next);
        _resetButton?.onClick.AddListener(_controller.Reset);
    }

    private void Start()
    {
        if (_controller == null) return;

        // Refresh button states after all Awake calls (including the controller's) have run.
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

        if (_stepCounterText != null)
            _stepCounterText.text = $"Step {stepIndex + 1} / {total}";

        if (_stepLabelText != null && stepIndex >= 0 && stepIndex < total)
            _stepLabelText.text = _controller.model.steps[stepIndex].label;

        if (_prevButton != null)
            _prevButton.interactable = stepIndex > 0;

        if (_nextButton != null)
            _nextButton.interactable = stepIndex < total;
    }
}
