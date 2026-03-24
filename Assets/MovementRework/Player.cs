using System;
using System.Collections;
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
        [SerializeField] private bool isCrouching = false;

        [Header("Walking Parameters")]
        [SerializeField] private float acceleration = 2500f;
        [SerializeField] private float maxSpeed = 25f;
        [SerializeField] private float stoppingPower = 5f;
        [SerializeField] private float sidewayDamping = 0.999f;
        [SerializeField] private float backwardStoppingPower = 45f;
        private Vector3 facingDirection = Vector3.zero;

        [Header("Ground Check Parameters")]
        [SerializeField] private Vector3 groundCheckScale = new Vector3(0.4f, 0.3f, 0.4f);
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float coyoteTime = 0.25f;
        [SerializeField] private float coyoteTimer = 0f;
        private float groundDotValue = 0f;

        [Header("Jumping Parameters")]
        [SerializeField] private float jumpForce;
        private float jumpCooldown = 0.1f;

        [Header("Airborne Movement Parameters")]
        [SerializeField] private float airborneAcceleration = 1500f;
        [SerializeField] private float airborneMaxSpeed = 35f;
        [SerializeField] private float airborneStoppingPower = 3f;
        [SerializeField] private float airborneSidewayDamping = 0.7f;
        [SerializeField] private float airborneBackwardStoppingPower = 10f;

        [Header("Slide Parameters")]
        [SerializeField] private float slideForce = 5f;
        [SerializeField] private float slideStoppingPower = 2f;
        [SerializeField] private float slideEndSpeed = 1f;
        [SerializeField] private bool tryingToSlide = false;

        private void Awake() {
            cameraController = GetComponentInChildren<CameraController>();
            camParent = GetComponentInChildren<CamPositioner>();
            playerModel = GetComponentInChildren<PlayerModel>();

            jumpCooldown += coyoteTime;

            //! sonra başka yere taşınacak
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            //! sonra başka yere taşınacak
        }

        private void Start() {
            MovementInput.Instance.OnJumpPerformed += OnJumpPerformed;
            MovementInput.Instance.OnCrouchPerformed += OnCrouchPerformed;
            MovementInput.Instance.OnCrouchCanceled += OnCrouchCanceled;
        }

        private void Update()
        {
            SetFacingDirection();

            playerModel.SimplePosition(core.position);
            camParent.SimplePosition(core.position);
        }

        private void FixedUpdate()
        {
            MovePlayer(MovementInput.Instance.GetMovementVector());
        }

        private void SetFacingDirection()
        {
            facingDirection = cameraController.facingDirection;
        }

        private void MovePlayer(Vector2 movementInput)
        {

            if(core.linearVelocity.magnitude <= 0.1f)
            {
                core.linearVelocity = Vector3.zero;
            }

            if(tryingToSlide)
            {
                if(CheckGround())
                {
                    isCrouching = true;
                    tryingToSlide = false;
                    camParent.MoveCamToCrouching();
                    if(core.linearVelocity.magnitude >= 0.1f)
                    {
                        core.AddForce(core.linearVelocity.normalized * slideForce, ForceMode.Impulse);
                    }
                }
            }

            if(isCrouching)
            {
                CheckGround();
                if(groundDotValue >= 0.95f)
                {
                    core.AddForce(-core.linearVelocity.normalized * slideStoppingPower);
                }

                if(core.linearVelocity.magnitude <= slideEndSpeed)
                {
                    isCrouching = false;
                    camParent.MoveCamToStanding(); 
                }
                return;
            }

            if(movementInput != Vector2.zero)
            {
                Vector3 inputDir = movementInput.y * facingDirection + movementInput.x * new Vector3(facingDirection.z, 0, -facingDirection.x);

                if(CheckGround())
                {
                    core.AddForce(inputDir * acceleration * Time.deltaTime);

                    orientation.LookAt(inputDir);

                    Vector3 localVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    localVelocity.x = Mathf.Lerp(localVelocity.x, 0, sidewayDamping * Time.deltaTime);
                    localVelocity = orientation.transform.TransformVector(localVelocity);
                    core.linearVelocity = localVelocity;

                    if(Vector3.Dot(orientation.forward, core.linearVelocity.normalized) < 0)
                    {
                        core.AddForce(-core.linearVelocity.normalized * backwardStoppingPower);
                    }

                    core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, maxSpeed);
                } else
                {
                    core.AddForce(inputDir * airborneAcceleration * Time.deltaTime);

                    orientation.LookAt(inputDir);

                    Vector3 localVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    localVelocity.x = Mathf.Lerp(localVelocity.x, 0, airborneSidewayDamping * Time.deltaTime);
                    localVelocity = orientation.transform.TransformVector(localVelocity);
                    core.linearVelocity = localVelocity;

                    if(Vector3.Dot(orientation.forward, core.linearVelocity.normalized) < 0)
                    {
                        core.AddForce(-core.linearVelocity.normalized * airborneBackwardStoppingPower);
                    }

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
            StartCoroutine(JumpCooldownTimer());
        }

        private void WallJump()
        {
            
        }

        private bool CanJump ()
        {
            if(isJumped)
            {
                return false;
            }
            return CheckGround()|| coyoteTimer <= coyoteTime;
        }

        private IEnumerator JumpCooldownTimer()
        {
            isJumped = true;
            
            yield return new WaitForSeconds(jumpCooldown);

            isJumped = false;
        }

        private bool CheckGround()
        {
            Collider[] colliders = Physics.OverlapBox(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), groundCheckScale, transform.rotation, groundLayers);
            bool newGrounded = colliders.Length > 0;
            
            if(!isGrounded && newGrounded) //Yere iniş yapmış demektir.
            {
                camParent.Jolt(core.linearVelocity.y);
            }

            isGrounded = newGrounded;

            if(isGrounded)
            {
                coyoteTimer = 0;

                if(Physics.Raycast(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), Vector3.down, out RaycastHit hit))
                {
                    groundDotValue = Vector3.Dot(Vector3.up, hit.normal);
                }

            } else
            {
                coyoteTimer += Time.deltaTime;
            }

            return isGrounded;
        }

        private void OnCrouchPerformed(object sender, EventArgs e)
        {
            if(CheckGround())
            {
                isCrouching = true;
                camParent.MoveCamToCrouching();
                if(core.linearVelocity.magnitude >= 0.1f)
                {
                    core.AddForce(core.linearVelocity.normalized * slideForce, ForceMode.Impulse);
                }
            } else
            {
                tryingToSlide = true;
            }
        }

        private void OnCrouchCanceled(object sender, EventArgs e)
        {
            tryingToSlide = false;
            if(isCrouching)
            {
                isCrouching = false;
                camParent.MoveCamToStanding(); 
            }
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
