using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementRework {
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }
        private CoreInputActions inputActions;
        public event EventHandler OnJumpPerformed;
        public event EventHandler OnLMBPerformed;
        public event EventHandler OnLMBCanceled;
        public event EventHandler OnRMBPerformed;
        public event EventHandler OnRMBCanceled;
        public event EventHandler OnCrouchPerformed;
        public event EventHandler OnCrouchCanceled;
        public event EventHandler OnInteractPerformed;

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
            inputActions.Gameplay.LMB.performed += on_LMB_performed;
            inputActions.Gameplay.LMB.canceled += on_LMB_canceled;
            inputActions.Gameplay.RMB.performed += on_RMB_performed;
            inputActions.Gameplay.RMB.canceled += on_RMB_canceled;
            inputActions.Gameplay.Slide.performed += on_slide_performed;
            inputActions.Gameplay.Slide.canceled += on_slide_canceled;
            inputActions.Gameplay.Interact.performed += on_interact_performed;
        }

        private void on_RMB_performed(InputAction.CallbackContext context)
        {
            OnRMBPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void on_RMB_canceled(InputAction.CallbackContext context)
        {
            OnRMBCanceled?.Invoke(this, EventArgs.Empty);
        }

        private void on_slide_performed(InputAction.CallbackContext context)
        {
            OnCrouchPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void on_slide_canceled(InputAction.CallbackContext context)
        {
            OnCrouchCanceled?.Invoke(this, EventArgs.Empty);
        }

        private void on_LMB_performed(InputAction.CallbackContext context)
        {
            OnLMBPerformed?.Invoke(this, EventArgs.Empty);
        }
        private void on_LMB_canceled(InputAction.CallbackContext context)
        {
            OnLMBCanceled?.Invoke(this, EventArgs.Empty);
        }

        private void on_jump_performed(InputAction.CallbackContext context)
        {
            OnJumpPerformed?.Invoke(this, EventArgs.Empty);
        }

        private void on_interact_performed(InputAction.CallbackContext context)
        {
            OnInteractPerformed?.Invoke(this, EventArgs.Empty);
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