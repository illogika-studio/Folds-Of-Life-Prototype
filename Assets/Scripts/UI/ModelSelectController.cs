using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Describes a single selectable origami model entry in the carousel.
/// </summary>
[Serializable]
public class FoldableModelEntry
{
    /// <summary>Display name shown in the carousel label.</summary>
    public string displayName;

    /// <summary>Thumbnail sprite shown in the carousel image.</summary>
    public Sprite thumbnail;

    /// <summary>The scene root GameObject to activate when this model is selected.</summary>
    public GameObject modelRoot;

    /// <summary>The fold controller that drives this model's sequence.</summary>
    public ContainerFoldController controller;

    /// <summary>Optional alive animation. May be null for models without a wing-flap phase.</summary>
    public ButterflyAliveAnimation aliveAnimation;
}

/// <summary>
/// Manages the model selection carousel on the selection screen.
/// Exposes <see cref="CurrentModel"/> for the <see cref="FoldingSceneManager"/> to read when
/// the user confirms their choice.
/// </summary>
public class ModelSelectController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [SerializeField] private List<FoldableModelEntry> _models;

    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _thumbnailImage;

    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private int _currentIndex;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns the currently highlighted model entry.</summary>
    public FoldableModelEntry CurrentModel => _models[_currentIndex];

    /// <summary>Cycles the carousel one step to the left and refreshes the UI.</summary>
    public void SelectPrev()
    {
        _currentIndex = (_currentIndex - 1 + _models.Count) % _models.Count;
        RefreshUI();
    }

    /// <summary>Cycles the carousel one step to the right and refreshes the UI.</summary>
    public void SelectNext()
    {
        _currentIndex = (_currentIndex + 1) % _models.Count;
        RefreshUI();
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _prevButton?.onClick.AddListener(SelectPrev);
        _nextButton?.onClick.AddListener(SelectNext);
    }

    private void Start()
    {
        if (_models == null || _models.Count == 0)
        {
            Debug.LogError("[ModelSelectController] _models list is empty or null. Assign at least one FoldableModelEntry.");
            return;
        }

        RefreshUI();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void RefreshUI()
    {
        if (_models == null || _models.Count == 0) return;

        FoldableModelEntry entry = CurrentModel;

        if (_nameText != null)
            _nameText.text = entry.displayName;

        if (_thumbnailImage != null)
            _thumbnailImage.sprite = entry.thumbnail;

        bool multiModel = _models.Count > 1;
        _prevButton?.gameObject.SetActive(multiModel);
        _nextButton?.gameObject.SetActive(multiModel);
    }
}
