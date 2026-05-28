using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the two-panel flow (selection screen ↔ folding view) with a
/// fade-to-white transition between them.
/// </summary>
public class FoldingSceneManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [SerializeField] private CanvasGroup _selectionPanel;
    [SerializeField] private CanvasGroup _foldingPanel;

    [Tooltip("Full-screen white Image placed as the last child of the Canvas.")]
    [SerializeField] private Image _fadeOverlay;

    [SerializeField] private float _fadeDuration = 0.4f;

    [SerializeField] private ModelSelectController _modelSelector;
    [SerializeField] private ContainerFoldStepUI _stepUI;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private FoldableModelEntry _activeEntry;
    private bool _isFading;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Selection panel starts visible and interactive.
        SetPanelVisible(_selectionPanel, visible: true);

        // Folding panel starts hidden.
        SetPanelVisible(_foldingPanel, visible: false);

        // Overlay fully transparent.
        if (_fadeOverlay != null)
            _fadeOverlay.color = Color.clear;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transitions from the selection screen to the folding view for the currently
    /// selected model. Called by the Start button's onClick event.
    /// </summary>
    public void StartFolding()
    {
        if (_isFading) return;

        FoldableModelEntry entry = _modelSelector.CurrentModel;

        if (entry.controller == null)
        {
            Debug.LogWarning("[FoldingSceneManager] CurrentModel.controller is null. Cannot transition to folding view.");
            return;
        }

        StartCoroutine(FadeTransition(toFolding: true, entry: entry));
    }

    /// <summary>
    /// Transitions back to the selection screen. Called by the Back button's onClick event.
    /// </summary>
    public void BackToSelection()
    {
        if (_isFading || _activeEntry == null) return;
        StartCoroutine(FadeTransition(toFolding: false, entry: _activeEntry));
    }

    // -------------------------------------------------------------------------
    // Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FadeTransition(bool toFolding, FoldableModelEntry entry)
    {
        _isFading = true;

        // Block input on both panels during the transition.
        SetPanelInteractable(_selectionPanel, false);
        SetPanelInteractable(_foldingPanel, false);

        // Fade out (0 → 1).
        yield return StartCoroutine(LerpOverlay(0f, 1f));

        // Midpoint swap.
        if (toFolding)
        {
            // Deactivate all other model roots, activate the selected one.
            if (_activeEntry != null && _activeEntry.modelRoot != null)
                _activeEntry.modelRoot.SetActive(false);

            _activeEntry = entry;

            if (_activeEntry.modelRoot != null)
                _activeEntry.modelRoot.SetActive(true);

            _activeEntry.controller.Reset();

            _stepUI.Bind(_activeEntry.controller, _activeEntry.aliveAnimation);

            SetPanelVisible(_selectionPanel, visible: false);
            SetPanelVisible(_foldingPanel, visible: true);
        }
        else
        {
            // Stop alive animation and reset the controller before returning.
            _activeEntry.aliveAnimation?.Stop();
            _activeEntry.controller.Reset();

            if (_activeEntry.modelRoot != null)
                _activeEntry.modelRoot.SetActive(false);

            SetPanelVisible(_foldingPanel, visible: false);
            SetPanelVisible(_selectionPanel, visible: true);
        }

        // Fade in (1 → 0).
        yield return StartCoroutine(LerpOverlay(1f, 0f));

        _isFading = false;
    }

    private IEnumerator LerpOverlay(float from, float to)
    {
        if (_fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color color = _fadeOverlay.color;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _fadeDuration);
            color.a = Mathf.Lerp(from, to, t);
            _fadeOverlay.color = color;
            yield return null;
        }

        color.a = to;
        _fadeOverlay.color = color;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void SetPanelVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static void SetPanelInteractable(CanvasGroup group, bool interactable)
    {
        if (group == null) return;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}
