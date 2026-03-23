using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementRework {
    public class MovementInput : MonoBehaviour
    {
        public static MovementInput Instance { get; private set; }
        private CoreInputActions inputActions;
        public event EventHandler OnJumpPerformed;
        public event EventHandler OnFirePerformed;
        public event EventHandler OnFireCanceled;
        public event EventHandler OnReloadPerformed;
        public event EventHandler OnCrouchPerformed;
        public event EventHandler OnCrouchCanceled;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            inputActions = new CoreInputActions();
            inputActions.Gameplay.Enable();

            inputActions.Gameplay.Jump.performed += on_jump_performed;
            inputActions.Gameplay.Fire.performed += on_fire_performed;
            inputActions.Gameplay.Fire.canceled += on_fire_canceled;
            inputActions.Gameplay.Reload.performed += on_reload_performed;
            inputActions.Gameplay.Slide.performed += on_slide_performed;
            inputActions.Gameplay.Slide.canceled += on_slide_canceled;
        }

        private void on_reload_performed(InputAction.CallbackContext context)
        {
            OnReloadPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void on_slide_performed(InputAction.CallbackContext context)
        {
            OnCrouchPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void on_slide_canceled(InputAction.CallbackContext context)
        {
            OnCrouchCanceled?.Invoke(this, EventArgs.Empty);
        }

        private void on_fire_performed(InputAction.CallbackContext context)
        {
            OnFirePerformed?.Invoke(this, EventArgs.Empty);
        }
            private void on_fire_canceled(InputAction.CallbackContext context)
        {
            OnFireCanceled?.Invoke(this, EventArgs.Empty);
        }

        private void on_jump_performed(InputAction.CallbackContext context)
        {
            OnJumpPerformed?.Invoke(this, EventArgs.Empty);
        }

        public Vector2 GetMovementVector()
        {
            return inputActions.Gameplay.Movement.ReadValue<Vector2>().normalized;
        }

        public Vector2 GetLookVector()
        {
            return inputActions.Gameplay.Look.ReadValue<Vector2>();
        }
    }
}