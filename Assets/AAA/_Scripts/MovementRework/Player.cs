using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MovementRework
{
    public class Player : MonoBehaviour
    {
        public static Player Instance {get; private set;}
        [Header("Objects and Components")]
        public PlayerModel playerModel {get; private set;}
        public Health Health {get; private set;}
        [SerializeField] private Transform orientation;
        private CamPositioner camParent;
        [SerializeField] public Rigidbody core;
        public CameraController cameraController;

        [Header("Data")]
        [SerializeField] private PlayerMovementData movementData;

        [Header("Status Bools")]
        [SerializeField] public bool IsGrounded {get; private set;} = false;
        [SerializeField] public bool IsWallrunning {get; private set;} = false;
        [SerializeField] public bool IsJumped {get; private set;} = false;
        [SerializeField] public bool IsCrouching {get; private set;} = false;
        [SerializeField] public bool IsMantling {get; private set;} = false;

        private float coyoteTimer = 0f;
        private float groundDotValue = 0f;
        private float jumpCooldown = 0.1f;
        public Vector3 facingDirection {get; private set;} = Vector3.zero;

        [Header("Slide State")]
        [SerializeField] private bool tryingToSlide = false;

        [Header("Wallrun State")]
        private Vector3 wallForward = Vector3.zero;
        private Vector3 wallNormal = Vector3.zero;
        private bool didWallrun = false;
        public bool isWallLeft {get; private set;} = false;
        private float wallrunTimer = 0f;

        [Header("Mantle State")]
        private Vector3 mantleHoldPoint = Vector3.zero;
        private bool didMantle = false;

        /// <summary>
        /// For compability reasons
        /// </summary>
        private bool _isPlayerModelNull;
        [Header("Respawn State")]
        private Vector3 startPosition = Vector3.zero;
        private Quaternion startRotation = Quaternion.identity;

        private void Awake() {
            Instance = this;

            camParent = GetComponentInChildren<CamPositioner>();
            playerModel = GetComponentInChildren<PlayerModel>();
            if (!playerModel)
            {
                _isPlayerModelNull = true;
            }
            Health = GetComponent<Health>();

            jumpCooldown += movementData.coyoteTime;

            startPosition = core.position;
            startRotation = core.rotation;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start() {
            GameInput.Instance.OnJumpPerformed += OnJumpPerformed;
            GameInput.Instance.OnCrouchPerformed += OnCrouchPerformed;
            GameInput.Instance.OnCrouchCanceled += OnCrouchCanceled;
        }

        private void Update()
        {
            if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RespawnAtStart();
            }

            SetFacingDirection();
            if (!_isPlayerModelNull) playerModel.SimplePosition(core.position);
            camParent.SimplePosition(core.position);
            
        }

        private void FixedUpdate()
        {
            MovePlayer(GameInput.Instance.GetMovementVector());

            CheckMantle();

            CheckWallrun();

            if(core.linearVelocity.y < 0)
            {
                core.AddForce(Vector3.down * movementData.fallGravity);
            }
        }

        private void SetFacingDirection()
        {
            facingDirection = cameraController.facingDirection;
        }

        private void MovePlayer(Vector2 movementInput)
        {
            if(IsMantling)
            {
                core.linearVelocity = Vector3.zero;
                return;
            }

            if(IsWallrunning)
            {
                wallrunTimer += Time.deltaTime;
                float wallrunDecay = Mathf.Clamp01(1f - wallrunTimer / movementData.wallrunDecayTime);
                core.AddForce(wallForward * movementData.wallrunAcceleration * wallrunDecay * Time.deltaTime);
                core.AddForce(Vector3.up * movementData.wallrunUpwardForce * wallrunDecay);
                core.AddForce(-wallNormal * 10);
                core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, movementData.maxSpeed);
                return;
            }

            if(core.linearVelocity.magnitude <= 0.1f)
            {
                core.linearVelocity = Vector3.zero;
            }

            if(tryingToSlide)
            {
                if(CheckGround())
                {
                    IsCrouching = true;
                    tryingToSlide = false;
                    camParent.MoveCamToCrouching();
                    if(core.linearVelocity.magnitude >= 0.1f)
                    {
                        core.AddForce(core.linearVelocity.normalized * movementData.slideForce, ForceMode.Impulse);
                    }
                }
            }

            if(IsCrouching)
            {
                CheckGround();
                if(groundDotValue >= 0.95f)
                {
                    core.AddForce(-core.linearVelocity.normalized * movementData.slideStoppingPower);
                }

                if(core.linearVelocity.magnitude <= movementData.slideEndSpeed)
                {
                    IsCrouching = false;
                    camParent.MoveCamToStanding(); 
                }
                return;
            }

            if(movementInput != Vector2.zero)
            {
                Vector3 inputDir = movementInput.y * facingDirection + movementInput.x * new Vector3(facingDirection.z, 0, -facingDirection.x);

                if(CheckGround())
                {
                    core.AddForce(inputDir * movementData.acceleration * Time.deltaTime);

                    orientation.LookAt(core.position + inputDir);

                    Vector3 localVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    localVelocity.x = Mathf.Lerp(localVelocity.x, 0, movementData.sidewayDamping * Time.deltaTime);
                    localVelocity = orientation.transform.TransformVector(localVelocity);
                    core.linearVelocity = localVelocity;

                    if(Vector3.Dot(orientation.forward, core.linearVelocity.normalized) < 0)
                    {
                        core.AddForce(-core.linearVelocity.normalized * movementData.backwardStoppingPower);
                    }

                    core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, movementData.maxSpeed);
                } else
                {
                    core.AddForce(inputDir * movementData.airborneAcceleration * Time.deltaTime);

                    orientation.LookAt(core.position + inputDir);

                    Vector3 localVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    localVelocity.x = Mathf.Lerp(localVelocity.x, 0, movementData.airborneSidewayDamping * Time.deltaTime);
                    localVelocity = orientation.transform.TransformVector(localVelocity);
                    core.linearVelocity = localVelocity;

                    if(Vector3.Dot(orientation.forward, new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).normalized) < 0)
                    {
                        core.AddForce(-new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).normalized * movementData.airborneBackwardStoppingPower);
                    }

                    Vector3 clampedVelocity = Vector3.ClampMagnitude(new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z), movementData.airborneMaxSpeed);
                    core.linearVelocity = new Vector3(clampedVelocity.x, core.linearVelocity.y, clampedVelocity.z);
                }
                
            } else
            {
                if(CheckGround() && !IsJumped)
                {
                    core.AddForce(-core.linearVelocity * movementData.stoppingPower);
                } else
                {
                    core.AddForce(-new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).normalized * movementData.airborneStoppingPower);
                }
            }
        }

        private void OnJumpPerformed(object sender, EventArgs e)
        {   
            if(IsMantling)
            {
                LeaveMantle();
                MantleJump();
            } else if(IsWallrunning)
            {
                LeaveWallrunning();
                WallJump();
            } else if(CanJump())
            {
                Jump();   
            }
        }

        private void Jump()
        {
            core.AddForce(Vector3.up * movementData.jumpForce, ForceMode.Impulse);
            StartCoroutine(JumpCooldownTimer());
        }

        private void WallJump()
        {
            core.AddForce(Vector3.up * movementData.jumpForce, ForceMode.Impulse);
            core.AddForce(cameraController.transform.forward * movementData.wallJumpForce, ForceMode.Impulse);
            core.AddForce(wallNormal * movementData.wallJumpNormalForce, ForceMode.Impulse);
            StartCoroutine(JumpCooldownTimer());
        }

        private void MantleJump()
        {
            core.AddForce(Vector3.up * movementData.jumpForce, ForceMode.Impulse);
            if(Vector3.Dot(new Vector3(core.position.x, 0, core.position.z) - new Vector3(mantleHoldPoint.x, 0, mantleHoldPoint.z), cameraController.transform.forward) >= 0)
            {
                core.AddForce(cameraController.transform.forward * movementData.mantleJumpForce, ForceMode.Impulse);
            }
            StartCoroutine(JumpCooldownTimer());
        }

        private bool CanJump ()
        {
            if(IsJumped)
            {
                return false;
            }
            return CheckGround()|| coyoteTimer <= movementData.coyoteTime;
        }

        private IEnumerator JumpCooldownTimer()
        {
            IsJumped = true;
            
            yield return new WaitForSeconds(jumpCooldown);

            IsJumped = false;
        }

        private bool CheckGround()
        {
            Collider[] colliders = Physics.OverlapBox(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), movementData.groundCheckScale, transform.rotation, movementData.groundLayers);
            bool newGrounded = colliders.Length > 0;
            
            if(!IsGrounded && newGrounded)
            {
                camParent.Jolt(core.linearVelocity.y, movementData.landingJoltPower);
            }

            IsGrounded = newGrounded;

            if(IsGrounded)
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

            return IsGrounded;
        }

        private void CheckMantle()
        {
            if(!CheckGround() && core.linearVelocity.y < 0 && !didMantle)
            {
                if(Physics.Raycast(movementData.mantleRaycastPoint + core.position + facingDirection * movementData.mantleDistance, Vector3.down, out RaycastHit verticalHit, movementData.mantleLength, movementData.groundLayers))
                {
                    if(Physics.Raycast(new Vector3(core.position.x, verticalHit.point.y - 0.1f, core.position.z), facingDirection, out RaycastHit horizontalHit, 1f, movementData.groundLayers) && !Physics.Raycast(new Vector3(core.position.x, verticalHit.point.y + 0.2f, core.position.z), facingDirection, 1f, movementData.groundLayers))
                    {
                        IsMantling = true;
                        mantleHoldPoint = horizontalHit.point;
                        camParent.Jolt(core.linearVelocity.y, movementData.mantleJoltPower);
                        core.useGravity = false;
                        core.linearVelocity = Vector3.zero;
                        
                    }
                }
            }
        }

        private void LeaveMantle()
        {
            IsMantling = false;
            core.useGravity = true;
            StartCoroutine(MantleCooldownTimer());
        }

        private IEnumerator MantleCooldownTimer()
        {
            didMantle = true;
            
            yield return new WaitForSeconds(movementData.mantleCooldown);

            didMantle = false;
        }

        private void OnCrouchPerformed(object sender, EventArgs e)
        {
            if(IsMantling)
            {
                LeaveMantle();
            } else if(CheckGround())
            {
                IsCrouching = true;
                camParent.MoveCamToCrouching();
                if(core.linearVelocity.magnitude >= 0.1f)
                {
                    core.AddForce(core.linearVelocity.normalized * movementData.slideForce, ForceMode.Impulse);
                }
            } else
            {
                tryingToSlide = true;
            }
        }

        private void OnCrouchCanceled(object sender, EventArgs e)
        {
            tryingToSlide = false;
            if(IsCrouching)
            {
                IsCrouching = false;
                camParent.MoveCamToStanding(); 
            }
        }

        private void CheckWallrun()
        {
            if(!CheckGround() && !IsMantling && !didWallrun)
            {
                bool wallLeft = Physics.Raycast(core.position + Vector3.up * 0.5f, new Vector3(-facingDirection.z, 0, facingDirection.x), out RaycastHit hitLeft, movementData.wallCheckDistance, movementData.groundLayers);
                bool wallRight = Physics.Raycast(core.position + Vector3.up * 0.5f, new Vector3(facingDirection.z, 0, -facingDirection.x), out RaycastHit hitRight, movementData.wallCheckDistance, movementData.groundLayers);
                
                if((wallLeft || wallRight) &&  GameInput.Instance.GetMovementVector().y > 0)
                {
                    IsWallrunning = true;
                    core.linearVelocity = new Vector3(core.linearVelocity.x, core.linearVelocity.y * 0.7f, core.linearVelocity.z);
                    wallNormal = wallLeft ? hitLeft.normal : hitRight.normal;
                    isWallLeft = wallLeft;
                    wallForward = wallLeft ? Vector3.Cross(wallNormal, Vector3.up) : -Vector3.Cross(wallNormal, Vector3.up) ;
                    core.AddForce(wallForward * movementData.wallrunBurstForce);
                } else
                {
                    if(IsWallrunning)
                    {
                        LeaveWallrunning();
                    }
                }
            } else
            {
                if(IsWallrunning)
                {
                    LeaveWallrunning();
                }
            }
        }

        private void LeaveWallrunning()
        {
            IsWallrunning = false;
            wallrunTimer = 0f;
            StartCoroutine(WallrunCooldownTimer());
        }

        private IEnumerator WallrunCooldownTimer()
        {
            didWallrun = true;
            
            yield return new WaitForSeconds(movementData.wallrunCooldown);

            didWallrun = false;
        }

        public void RespawnAtStart()
        {
            core.position = startPosition;
            core.rotation = startRotation;
            core.linearVelocity = Vector3.zero;
            core.angularVelocity = Vector3.zero;
        }

        public void LungeForward()
        {
            core.AddForce(facingDirection * movementData.lungeForce, ForceMode.Impulse);
        }

        private void OnDrawGizmos()
        {
            if (movementData == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), movementData.groundCheckScale);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(core.position, core.position + wallForward * 2);

            Gizmos.color = Color.red;
            Vector3 vStart = movementData.mantleRaycastPoint + core.position + facingDirection * movementData.mantleDistance;
            Vector3 vDirection = Vector3.down * movementData.mantleLength;
            Gizmos.DrawLine(vStart, vStart + vDirection);

            if (Physics.Raycast(vStart, Vector3.down, out RaycastHit vHit, 1f, movementData.groundLayers))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(vHit.point, 0.05f);

                Gizmos.color = Color.blue;
                Vector3 hStart = new Vector3(core.position.x, vHit.point.y - 0.1f, core.position.z);
                Vector3 hDirection = orientation.forward * 1f;
                Gizmos.DrawLine(hStart, hStart + hDirection);

                if (Physics.Raycast(hStart, orientation.forward, out RaycastHit hHit, 1f, movementData.groundLayers))
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(hHit.point, 0.1f);
                }
            }
        }

        public float GetMovementSpeed()
        {
            return core.linearVelocity.magnitude;
        }

        public float GetSpeedPercentage()
        {
            return core.linearVelocity.magnitude / movementData.maxSpeed;
        }

        public float GetHorizontalSpeedPercentage()
        {
            return new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).magnitude / movementData.maxSpeed;
        }

        public Transform GetCamera()
        {
            return cameraController.transform;
        }
    }
}
