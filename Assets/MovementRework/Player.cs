using System;
using UnityEngine;

namespace MovementRework
{
    public class Player : MonoBehaviour
    {
        [Header("Objects and Components")]
        private PlayerModel playerModel;
        [SerializeField] private Transform orientation;
        private CamPositioner camParent;
        [SerializeField] private Rigidbody core;
        private CameraController cameraController;

        [Header("Status Bools")]
        [SerializeField] private bool isGrounded = false;
        [SerializeField] private bool isWallrunning = false;
        [SerializeField] private bool isJumped = false;

        [Header("Walking Parameters")]
        [SerializeField] private float acceleration = 2500f;
        [SerializeField] private float maxSpeed = 25f;
        [SerializeField] private float stoppingPower = 5f;
        [SerializeField] private float sidewayDamping = 0.999f;
        private Vector3 facingDirection = Vector3.zero;

        [Header("Ground Check Parameters")]
        [SerializeField] private Vector3 groundCheckScale = new Vector3(0.4f, 0.3f, 0.4f);
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float coyoteTime = 0.25f;
        [SerializeField] private float coyoteTimer = 0f;

        [Header("Jumping Parameters")]
        [SerializeField] private float jumpForce;

        [Header("Airborne Movement Parameters")]
        [SerializeField] private float airborneAcceleration = 1500f;
        [SerializeField] private float airborneMaxSpeed = 35f;
        [SerializeField] private float airborneStoppingPower = 3f;
        [SerializeField] private float airborneSidewayDamping = 0.7f;

        private void Awake() {
            cameraController = GetComponentInChildren<CameraController>();
            camParent = GetComponentInChildren<CamPositioner>();
            playerModel = GetComponentInChildren<PlayerModel>();
        }

        private void Start() {
            MovementInput.Instance.OnJumpPerformed += OnJumpPerformed;
        }

        private void Update()
        {
            SetFacingDirection();

            playerModel.SimplePosition(core.position);
            camParent.SimplePosition(core.position);
        }

        private void FixedUpdate()
        {
            isGrounded = CheckGround();
            MovePlayer(MovementInput.Instance.GetMovementVector());
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

                if(CheckGround())
                {
                    core.AddForce(inputDir * acceleration * Time.deltaTime);

                    orientation.LookAt(inputDir);

                    Vector3 rightVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    rightVelocity.x = Mathf.Lerp(rightVelocity.x, 0, sidewayDamping * Time.deltaTime);
                    rightVelocity = orientation.transform.TransformVector(rightVelocity);
                    core.linearVelocity = rightVelocity;

                    core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, maxSpeed);
                } else
                {
                    core.AddForce(inputDir * airborneAcceleration * Time.deltaTime);

                    orientation.LookAt(inputDir);

                    Vector3 rightVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    rightVelocity.x = Mathf.Lerp(rightVelocity.x, 0, airborneSidewayDamping * Time.deltaTime);
                    rightVelocity = orientation.transform.TransformVector(rightVelocity);
                    core.linearVelocity = rightVelocity;

                    core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, airborneMaxSpeed);
                }
                
            } else
            {
                if(CheckGround())
                {
                    core.AddForce(-core.linearVelocity.normalized * stoppingPower);
                } else
                {
                    core.AddForce(-core.linearVelocity.normalized * airborneStoppingPower);
                }
            }
        }

        private void OnJumpPerformed(object sender, EventArgs e)
        {
            if(isWallrunning)
            {
                WallJump();
            } else if(CanJump())
            {
                Jump();   
            }
        }

        private void Jump()
        {
            
            core.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumped = true;
        }

        private void WallJump()
        {
            
        }

        private bool CanJump ()
        {
            return (CheckGround() || coyoteTimer <= coyoteTime) && !isJumped;
        }

        private bool CheckGround()
        {
            bool newGrounded = Physics.OverlapBox(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), groundCheckScale, transform.rotation, groundLayers).Length > 0;
            
            if(!isGrounded && newGrounded) //Yere iniş yapmış demektir.
            {
                camParent.Jolt(core.linearVelocity.y);
            }

            isGrounded = newGrounded;

            if(isGrounded)
            {
                coyoteTimer = 0;
                isJumped = false;
            } else
            {
                coyoteTimer += Time.deltaTime;
            }

            return isGrounded;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            //Ground Check Box
            Gizmos.DrawWireCube(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), groundCheckScale);
        }

        public float GetMovementSpeed()
        {
            return core.linearVelocity.magnitude;
        }

        public float GetSpeedPercentage()
        {
            return core.linearVelocity.magnitude / maxSpeed;
        }

        public float GetHorizontalSpeedPercentage()
        {
            return new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).magnitude / maxSpeed;
        }
    }
}
