using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Rotates a target Transform with left mouse-button drag, providing smooth
/// orbit control for the 3D model in the folding view.
/// Input is skipped while the pointer is over a UI element.
/// </summary>
public class ModelOrbitController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("The ModelRoot wrapper GameObject to rotate.")]
    [SerializeField] private Transform _target;

    [Tooltip("Degrees of rotation per pixel of mouse drag.")]
    [SerializeField] private float _sensitivity = 0.4f;

    [Tooltip("Maximum vertical angle in degrees to prevent flipping.")]
    [SerializeField] private float _verticalClamp = 75f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private float _xAngle;
    private float _yAngle;
    private bool _isDragging;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (_target == null)
        {
            Debug.LogWarning("[ModelOrbitController] _target is not assigned. Orbit will not function.");
            return;
        }

        // Seed from the target's current rotation so the first drag is smooth.
        Vector3 euler = _target.localEulerAngles;
        _xAngle = euler.x;
        _yAngle = euler.y;
    }

    private void Update()
    {
        if (_target == null) return;

        // Skip orbit while the pointer is hovering over a UI element.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            _isDragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
            _isDragging = true;

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (_isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            _yAngle += mouseX * _sensitivity * 100f * Time.deltaTime;
            _xAngle -= mouseY * _sensitivity * 100f * Time.deltaTime;
            _xAngle  = Mathf.Clamp(_xAngle, -_verticalClamp, _verticalClamp);

            _target.localRotation = Quaternion.Euler(_xAngle, _yAngle, 0f);
        }
    }
}
