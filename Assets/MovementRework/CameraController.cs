using UnityEngine;

namespace MovementRework 
{
    public class CameraController : MonoBehaviour
    {
        private float currentXAngle;
        private float currentYAngle;

        [SerializeField] private float upperVerticalLimit;
        [SerializeField] private float lowerVerticalLimit;

        private float oldHorizontalInput = 0;
        private float oldVerticalInput = 0;

        [SerializeField] private float camSmoothingFactor;
        [SerializeField] private float camSpeed;

        public Vector3 facingDirection;
        public Vector3 upwardsDirection;

        private Camera cam;

        private void Awake() {
            cam = GetComponentInChildren<Camera>();
        }

        private void Update() {
            RotateCam(MovementInput.Instance.GetLookVector().x, MovementInput.Instance.GetLookVector().y);
        }


        private void RotateCam(float inputX, float inputY)
        {
            oldHorizontalInput = Mathf.Lerp(oldHorizontalInput, inputX, Time.deltaTime * camSmoothingFactor);
            oldVerticalInput = Mathf.Lerp(oldVerticalInput, inputY, Time.deltaTime * camSmoothingFactor);

            currentXAngle -= oldVerticalInput * camSpeed * Time.deltaTime;
            currentYAngle += oldHorizontalInput * camSpeed * Time.deltaTime;

            currentXAngle = Mathf.Clamp(currentXAngle, -upperVerticalLimit, lowerVerticalLimit);

            transform.localRotation = Quaternion.Euler(new Vector3(0, currentYAngle ,0));

            facingDirection = transform.forward;
            upwardsDirection = transform.up;

            transform.localRotation = Quaternion.Euler(new Vector3(currentXAngle, currentYAngle, 0));
        }
    } 
}
