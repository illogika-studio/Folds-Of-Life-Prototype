using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FoldsOfLife.Input
{
    /// <summary>
    /// Rotates a target Transform around world Y and X axes based on pointer drag input.
    /// Can be attached to any GameObject. If <see cref="_rotationTarget"/> is left unassigned,
    /// falls back to this GameObject's parent, or itself when there is no parent.
    /// Works with both the legacy and new Unity Input System.
    /// </summary>
    public class DragRotate : MonoBehaviour
    {
        /// <summary>The Transform to rotate. Defaults to this object's parent if left unassigned.</summary>
        [SerializeField] private Transform _rotationTarget;

        /// <summary>Degrees rotated per pixel of drag distance.</summary>
        [SerializeField, Range(0.1f, 2f)]
        private float _sensitivity = 0.4f;

        /// <summary>Smoothing applied to rotation velocity. Higher = more inertia.</summary>
        [SerializeField, Range(0f, 20f)]
        private float _inertia = 8f;

        /// <summary>How quickly inertia decays when the pointer is released (per second).</summary>
        [SerializeField, Range(1f, 20f)]
        private float _decaySpeed = 5f;

        private Vector2 _velocity;
        private bool _isDragging;
        private Vector2 _previousPointerPosition;

        private void Awake()
        {
            if (_rotationTarget == null)
                _rotationTarget = transform.parent != null ? transform.parent : transform;
        }

        private void Update()
        {
            // Start drag on primary button press, unless pointer is over a UI element.
            if (GetPrimaryButtonDown() && !IsPointerOverUI())
            {
                _isDragging = true;
                _previousPointerPosition = GetPointerPosition();
            }

            // End drag when the button is released.
            if (!GetPrimaryButtonHeld())
                _isDragging = false;

            if (_isDragging)
            {
                Vector2 currentPosition  = GetPointerPosition();
                Vector2 delta            = currentPosition - _previousPointerPosition;
                _previousPointerPosition = currentPosition;

                // Smooth velocity toward the current frame delta.
                _velocity = Vector2.Lerp(_velocity, delta * _sensitivity, Time.deltaTime * _inertia + 1f);
            }
            else
            {
                // Decay velocity when not dragging.
                _velocity = Vector2.Lerp(_velocity, Vector2.zero, Time.deltaTime * _decaySpeed);
            }

            if (_velocity.sqrMagnitude > 0.0001f)
                ApplyRotation(_velocity);
        }

        /// <summary>
        /// Rotates around the world Y axis (horizontal drag) and world X axis (vertical drag).
        /// </summary>
        private void ApplyRotation(Vector2 delta)
        {
            _rotationTarget.Rotate(Vector3.up,   -delta.x, Space.World);
            _rotationTarget.Rotate(Vector3.right,  delta.y, Space.World);
        }

        private static Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return UnityEngine.Input.mousePosition;
#endif
        }

        private static bool GetPrimaryButtonDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        private static bool GetPrimaryButtonHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return UnityEngine.Input.GetMouseButton(0);
#endif
        }

        /// <summary>Returns true when the pointer is over a UI element so drags don't bleed through panels.</summary>
        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
