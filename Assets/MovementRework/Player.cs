using UnityEngine;

namespace MovementRework
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Transform playerModel;
        [SerializeField] private Transform orientation;
        private Rigidbody core;
        private CameraController cameraController;

        private Vector3 movementDir = Vector3.zero;
        private Vector3 movementVector = Vector3.zero;

        [SerializeField] private float acceleration = 5f;
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float stoppingPower = 5f;
        [SerializeField] private float sidewayDamping = 0.9f;
        private Vector3 facingDirection = Vector3.zero;
        

        private void Awake() {
            core = GetComponentInChildren<Rigidbody>();
            cameraController = GetComponentInChildren<CameraController>();
        }

        private void Update()
        {
            SetFacingDirection();
            MovePlayer(MovementInput.Instance.GetMovementVector());

            playerModel.position = core.position;
        }

        private void SetFacingDirection()
        {
            facingDirection = cameraController.facingDirection;
        }

        private void MovePlayer(Vector2 movementInput)
        {
            if(movementInput != Vector2.zero)
            {
                Vector3 inputDir = movementInput.y * facingDirection + movementInput.x * new Vector3(facingDirection.z, 0, -facingDirection.x);

                core.AddForce(inputDir * acceleration * Time.deltaTime);

                orientation.LookAt(inputDir);

                Vector3 rightVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                rightVelocity.x = Mathf.Lerp(rightVelocity.x, 0, sidewayDamping * Time.deltaTime);
                rightVelocity = orientation.transform.TransformVector(rightVelocity);
                core.linearVelocity = rightVelocity;

                core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, maxSpeed);
            } else
            {
                core.AddForce(-core.linearVelocity.normalized * stoppingPower);
            }
        }
    }
}
