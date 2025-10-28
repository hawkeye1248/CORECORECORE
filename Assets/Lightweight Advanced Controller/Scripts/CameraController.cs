using System.Linq;
using UnityEngine;

namespace peterkcodes.AdvancedMovement.Demo
{
    /// <summary>
    /// Basic camera look script. Controls the position and rotation of the camera depending on input and movement states.
    /// </summary>

    //I recommend replacing the camera scripts with your own
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;

        [Header("Options")]
        [SerializeField] private float sensitivity;

        [Header("Camera Controls")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private new Camera camera;
        [SerializeField] private float stateTransitionTime;

        //The camera changes position based on the movement state of the player.
        //States in the uprightStates array will move the camera to the default position, states in the crouchStates array to the crouchPosition, etc.
        [Header("Camera States")]
        private Vector3 standingPosition;
        [SerializeField] MoveState[] uprightStates;
        [SerializeField] MoveState[] crouchStates;
        [SerializeField] MoveState[] slideStates;
        [SerializeField] private Vector3 slidePosition;
        [SerializeField] private Vector3 crouchPosition;
        private Vector3 targetPosition;

        CameraState currentState;
        private float x;
        private float y;

        // Start is called before the first frame update
        void Start()
        {
            standingPosition = cameraTransform.localPosition;
            LockMouse();
            y = transform.eulerAngles.y;
            x = cameraTransform.localEulerAngles.x;
            movement.onStateChanged += RefreshState;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            x -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
            y += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            x = Mathf.Clamp(x, -90, 90);

            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, y, transform.eulerAngles.z);
            cameraTransform.localRotation = Quaternion.Euler(x, cameraTransform.localEulerAngles.y, cameraTransform.localEulerAngles.z);
            cameraTransform.localPosition = Vector3.MoveTowards(cameraTransform.localPosition, targetPosition, Time.deltaTime * (1 / stateTransitionTime));
        }

        public void LockMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RefreshState(MoveState _state)
        {
            if (uprightStates.Contains(_state))
            {
                SetCameraState(CameraState.upright);
                return;
            }

            if (crouchStates.Contains(_state))
            {
                SetCameraState(CameraState.crouch);
                return;
            }

            if (slideStates.Contains(_state))
            {
                SetCameraState(CameraState.crouch);
                return;
            }
        }

        public void SetCameraState(CameraState _newState)
        {
            currentState = _newState;
            switch (currentState)
            {
                case CameraState.upright:
                    targetPosition = standingPosition;
                    break;
                case CameraState.crouch:
                    targetPosition = crouchPosition;
                    break;
                case CameraState.slide:
                    targetPosition = slidePosition;
                    break;
            }
        }
    }

    public enum CameraState
    {
        upright,
        crouch,
        slide
    }
}