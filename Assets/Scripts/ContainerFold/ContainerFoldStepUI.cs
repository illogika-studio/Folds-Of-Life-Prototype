using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects Prev / Next / Reset / PlayAlive / Back UGUI buttons to a <see cref="ContainerFoldController"/>
/// and keeps the step label and progress counter up to date.
/// Buttons are automatically disabled while an animation is running.
/// Call <see cref="Bind"/> at runtime to hot-swap the active controller (e.g. when transitioning models).
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

    [Tooltip("Optional. Visible only when the sequence reaches its last step.")]
    [SerializeField] private Button _playAliveButton;

    [Tooltip("Optional. Wire its onClick to FoldingSceneManager.BackToSelection() in the Inspector.")]
    [SerializeField] private Button _backButton;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private ButterflyAliveAnimation _aliveAnimation;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (_controller != null)
            SubscribeTo(_controller);

        _prevButton?.onClick.AddListener(() => _controller?.Previous());
        _nextButton?.onClick.AddListener(() => _controller?.Next());
        _resetButton?.onClick.AddListener(() => _controller?.Reset());

        if (_playAliveButton != null)
            _playAliveButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (_controller == null) return;
        HandleStepChanged(_controller.CurrentStep);
    }

    private void OnDestroy()
    {
        if (_controller != null)
            UnsubscribeFrom(_controller);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Hot-swaps the active controller and alive animation reference.
    /// Unsubscribes from the previous controller's events, subscribes to the new one,
    /// and immediately syncs the UI to the current step.
    /// </summary>
    public void Bind(ContainerFoldController controller, ButterflyAliveAnimation alive)
    {
        if (_controller != null)
            UnsubscribeFrom(_controller);

        _controller = controller;
        _aliveAnimation = alive;

        // Re-wire the PlayAlive button to the new animation.
        if (_playAliveButton != null)
        {
            _playAliveButton.onClick.RemoveAllListeners();
            if (_aliveAnimation != null)
                _playAliveButton.onClick.AddListener(_aliveAnimation.Play);
        }

        if (_controller != null)
        {
            SubscribeTo(_controller);
            HandleStepChanged(_controller.CurrentStep);
        }
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

        // PlayAlive button is visible only once the sequence is fully complete.
        if (_playAliveButton != null)
        {
            bool sequenceDone = stepIndex >= _controller.TotalSteps && !isAnimating;
            _playAliveButton.gameObject.SetActive(sequenceDone);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void SubscribeTo(ContainerFoldController controller)
    {
        controller.OnStepChanged      += HandleStepChanged;
        controller.OnAnimatingChanged += HandleAnimatingChanged;
    }

    private void UnsubscribeFrom(ContainerFoldController controller)
    {
        controller.OnStepChanged      -= HandleStepChanged;
        controller.OnAnimatingChanged -= HandleAnimatingChanged;
    }
}
