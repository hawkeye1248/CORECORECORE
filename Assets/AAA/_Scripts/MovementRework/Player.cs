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
        private Vector3 lastPlatformPosition = Vector3.zero;
        private Quaternion lastPlatformRotation = Quaternion.identity;

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
            lastPlatformPosition = core.position;
            lastPlatformRotation = core.rotation;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start() {
            GameInput.Instance.OnJumpPerformed += OnJumpPerformed;
            GameInput.Instance.OnCrouchPerformed += OnCrouchPerformed;
            GameInput.Instance.OnCrouchCanceled += OnCrouchCanceled;

            Health.OnDeath += (object sender, EventArgs args) => {
                RespawnAtStart();
            };
        }

        private void OnDisable() {
            GameInput.Instance.OnJumpPerformed -= OnJumpPerformed;
            GameInput.Instance.OnCrouchPerformed -= OnCrouchPerformed;
            GameInput.Instance.OnCrouchCanceled -= OnCrouchCanceled;

            Health.OnDeath -= (object sender, EventArgs args) => {
                RespawnAtStart();
            };
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
                    // Only accelerate while under the speed cap. Above it (e.g. momentum
                    // carried in from a slide) we coast and let SoftCapSpeed bleed the
                    // excess off, so input can never push past maxSpeed. This keeps the
                    // cap uniform across all directions instead of letting forward/strafe
                    // settle at different equilibrium speeds.
                    if(core.linearVelocity.magnitude < movementData.maxSpeed)
                    {
                        core.AddForce(inputDir * movementData.acceleration * Time.deltaTime);
                    }

                    orientation.LookAt(core.position + inputDir);

                    Vector3 localVelocity = orientation.transform.InverseTransformVector(core.linearVelocity); 
                    localVelocity.x = Mathf.Lerp(localVelocity.x, 0, movementData.sidewayDamping * Time.deltaTime);
                    localVelocity = orientation.transform.TransformVector(localVelocity);
                    core.linearVelocity = localVelocity;

                    if(Vector3.Dot(orientation.forward, core.linearVelocity.normalized) < 0)
                    {
                        core.AddForce(-core.linearVelocity.normalized * movementData.backwardStoppingPower);
                    }

                    SoftCapSpeed(movementData.maxSpeed);
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

                    SoftCapHorizontalSpeed(movementData.airborneMaxSpeed);
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

        // Eases speed down toward the cap instead of snapping to it, so momentum built up
        // (e.g. sliding down a slope) bleeds off gradually instead of vanishing in one frame.
        // Below the cap this is a no-op, matching the old ClampMagnitude behavior.
        private void SoftCapSpeed(float cap)
        {
            float speed = core.linearVelocity.magnitude;
            if (speed <= cap)
            {
                return;
            }

            float newSpeed = Mathf.MoveTowards(speed, cap, movementData.overspeedDecay * Time.deltaTime);
            core.linearVelocity = core.linearVelocity.normalized * newSpeed;
        }

        // Same gradual ease-down as SoftCapSpeed, but only on the horizontal (x/z) plane so
        // jump/fall (y) velocity is left untouched. Used for the airborne speed cap.
        private void SoftCapHorizontalSpeed(float cap)
        {
            Vector3 horizontal = new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z);
            float speed = horizontal.magnitude;
            if (speed <= cap)
            {
                return;
            }

            float newSpeed = Mathf.MoveTowards(speed, cap, movementData.overspeedDecay * Time.deltaTime);
            horizontal = horizontal.normalized * newSpeed;
            core.linearVelocity = new Vector3(horizontal.x, core.linearVelocity.y, horizontal.z);
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

                if(IsStableGround())
                {
                    lastPlatformPosition = core.position;
                    lastPlatformRotation = core.rotation;
                }

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

        // How far around the player ground must exist for a respawn point to count as "safe".
        private const float respawnGroundCheckRadius = 0.75f;
        private const float respawnGroundCheckLength = 1.5f;

        // Returns true only when there is ground beneath the player on all sides,
        // so respawn points are never recorded right at the edge of a platform.
        private bool IsStableGround()
        {
            Vector3 origin = new Vector3(core.position.x, core.position.y - 0.4f, core.position.z);
            Vector3[] offsets = {
                Vector3.forward * respawnGroundCheckRadius,
                Vector3.back * respawnGroundCheckRadius,
                Vector3.left * respawnGroundCheckRadius,
                Vector3.right * respawnGroundCheckRadius,
            };

            foreach(Vector3 offset in offsets)
            {
                if(!Physics.Raycast(origin + offset, Vector3.down, respawnGroundCheckLength, movementData.groundLayers))
                {
                    return false;
                }
            }

            return true;
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
            if(ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FlashBlack(DoRespawn);
            }
            else
            {
                DoRespawn();
            }
        }

        private void DoRespawn()
        {
            Health.ResetCharacter();
            core.position = lastPlatformPosition;
            core.rotation = lastPlatformRotation;
            core.linearVelocity = Vector3.zero;
            core.angularVelocity = Vector3.zero;

            // Snap visuals so the camera/model don't smoothly slide to the new position.
            camParent.SnapPosition(core.position);
            if(!_isPlayerModelNull) playerModel.SnapPosition(core.position);
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
