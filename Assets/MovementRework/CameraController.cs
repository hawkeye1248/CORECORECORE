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
        private Player player;
        [SerializeField] private float cameraTiltMultiplier;
        [SerializeField] private float cameraTiltSmoothTime;
        float cameraZTilt = 0;

        [SerializeField] private float amplitute;
        [SerializeField] private float frequency;
        private Vector3 startPos;

        [SerializeField] private float minFov;
        [SerializeField] private float maxFov;

        float time = 0;
        float elapsedTime = 0;

        private void Awake() {
            cam = GetComponentInChildren<Camera>();
            player = GetComponentInParent<Player>();
            startPos = cam.transform.localPosition;

            cam.fieldOfView = minFov;
        }

        private void Update() {
            RotateCam(MovementInput.Instance.GetLookVector().x, MovementInput.Instance.GetLookVector().y);

            //Headbob(player.GetMovementSpeed(), player.GetSpeedPercentage());
            SetFOV(player.GetMovementSpeed(), player.GetHorizontalSpeedPercentage());
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

            cameraZTilt = Mathf.Lerp(cameraZTilt, -MovementInput.Instance.GetMovementVector().x * cameraTiltMultiplier, cameraTiltSmoothTime);

            transform.localRotation = Quaternion.Euler(new Vector3(currentXAngle, currentYAngle, cameraZTilt));
        }

        private void SetFOV(float speed, float speedPercentage)
        {
            if(speed >= 0.5f)
            {
                cam.fieldOfView = Mathf.Lerp(minFov, maxFov, speedPercentage);
            } else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, minFov, Time.deltaTime);
            }
            
        }

        public void Headbob(float speed, float speedPercentage)
        {
            if(speed >= 0.5f)
            {
                cam.transform.localPosition += FootStepMotion(speedPercentage * frequency, speed);
            } else
            {
                elapsedTime = Time.time;
                if(cam.transform.localPosition == startPos)
                {
                    return;
                } else
                {
                    cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, startPos, Time.deltaTime);
                }
            }
            
            //cam.transform.LookAt(FocusTarget());
        }

        private Vector3 FootStepMotion(float newFreq, float speed)
        {
            time = Time.time - elapsedTime;
            Vector3 pos = Vector3.zero;
            pos.y = Mathf.Sin(time * newFreq) * amplitute;
            pos.x = Mathf.Cos(time * newFreq) * amplitute;
            return pos;
        }

        private Vector3 FocusTarget()
        {
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + startPos.y, transform.position.z);
            pos += transform.forward * 15f;
            return pos;
        }
    } 
}
