using UnityEngine;

namespace MovementRework
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraData cameraData;

        private float currentXAngle;
        private float currentYAngle;

        private float oldHorizontalInput = 0;
        private float oldVerticalInput = 0;

        [HideInInspector] public Vector3 facingDirection;
        [HideInInspector] public Vector3 upwardsDirection;

        private Camera cam;
        private Player player;
        float cameraZTilt = 0;

        private void Awake() {
            cam = GetComponentInChildren<Camera>();
            player = GetComponentInParent<Player>();

            cam.fieldOfView = cameraData.minFov;
        }

        private void Update() {
            RotateCam(MovementInput.Instance.GetLookVector().x, MovementInput.Instance.GetLookVector().y);

            SetFOV(player.GetMovementSpeed(), player.GetHorizontalSpeedPercentage());
        }

        private void RotateCam(float inputX, float inputY)
        {
            oldHorizontalInput = Mathf.Lerp(oldHorizontalInput, inputX, Time.deltaTime * cameraData.camSmoothingFactor);
            oldVerticalInput = Mathf.Lerp(oldVerticalInput, inputY, Time.deltaTime * cameraData.camSmoothingFactor);

            currentXAngle -= oldVerticalInput * cameraData.camSpeed * Time.deltaTime;
            currentYAngle += oldHorizontalInput * cameraData.camSpeed * Time.deltaTime;

            currentXAngle = Mathf.Clamp(currentXAngle, -cameraData.upperVerticalLimit, cameraData.lowerVerticalLimit);

            transform.localRotation = Quaternion.Euler(new Vector3(0, currentYAngle ,0));

            if(!player.IsWallrunning && !player.IsMantling)
            {
                facingDirection = transform.forward;
            }

            upwardsDirection = transform.up;

            if(player.IsWallrunning)
            {
                cameraZTilt = Mathf.Lerp(cameraZTilt, (player.isWallLeft ? - 1 : 1) * cameraData.wallrunTiltMultiplier, cameraData.cameraTiltSmoothTime);
            } else
            {
                float strafeTilt = -MovementInput.Instance.GetMovementVector().x * cameraData.cameraTiltMultiplier;
                float lookTilt = Mathf.Clamp(-oldHorizontalInput * cameraData.lookTiltMultiplier, -cameraData.maxLookTilt, cameraData.maxLookTilt);
                cameraZTilt = Mathf.Lerp(cameraZTilt, strafeTilt + lookTilt, cameraData.cameraTiltSmoothTime);
            }

            transform.localRotation = Quaternion.Euler(new Vector3(currentXAngle, currentYAngle, cameraZTilt));
        }

        private void SetFOV(float speed, float speedPercentage)
        {
            if(speed >= 0.5f)
            {
                float desiredFov = Mathf.Lerp(cameraData.minFov, cameraData.maxFov, speedPercentage);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFov, cameraData.fovChangeMultiplier * Time.deltaTime);
            } else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, cameraData.minFov, cameraData.fovChangeMultiplier * Time.deltaTime);
            }
        }
    }
}
