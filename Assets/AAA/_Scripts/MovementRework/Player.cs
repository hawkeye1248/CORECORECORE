using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace MovementRework
{
    public class Player : MonoBehaviour
    {
        public static Player Instance {get; private set;}
        [Header("Objects and Components")]
        public PlayerModel playerModel {get; private set;}

        [SerializeField] private GameObject overlayCamera;
        public Health Health {get; private set;}
        [SerializeField] private Transform orientation;
        private CamPositioner camParent;
        [SerializeField] public Rigidbody core;
        [SerializeField] private CapsuleCollider bodyCollider;
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
        private Vector3 groundNormal = Vector3.up;
        private float jumpCooldown = 0.1f;
        public Vector3 facingDirection {get; private set;} = Vector3.zero;

        [Header("Slide State")]
        [SerializeField] private bool tryingToSlide = false;
        // Standing capsule dimensions, cached at Awake so crouch can shrink and restore them.
        private float standingHeight;
        private Vector3 standingCenter;

        [Header("Wallrun State")]
        private Vector3 wallForward = Vector3.zero;
        private Vector3 wallNormal = Vector3.zero;
        private bool didWallrun = false;
        public bool isWallLeft {get; private set;} = false;
        private float wallrunTimer = 0f;

        [Header("Mantle State")]
        private Vector3 mantleHoldPoint = Vector3.zero;
        private bool didMantle = false;
        private Vector3 mantleTopPoint = Vector3.zero;
        private bool isClimbing = false;
        private Vector3 climbStart = Vector3.zero;
        private Vector3 climbTarget = Vector3.zero;
        private float climbTimer = 0f;

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
            OverlayCamInitialize();
            Instance = this;

            camParent = GetComponentInChildren<CamPositioner>();
            //! playerModel = GetComponentInChildren<PlayerModel>(); // Obsolete, PlayerModel class isn't used since we switched to 2D Player
            if (!playerModel)
            {
                _isPlayerModelNull = true;
            }
            Health = GetComponent<SimpleHealth>();

            if(!bodyCollider) bodyCollider = core.GetComponentInChildren<CapsuleCollider>();
            standingHeight = bodyCollider.height;
            standingCenter = bodyCollider.center;

            jumpCooldown += movementData.coyoteTime;

            startPosition = core.position;
            startRotation = core.rotation;
            lastPlatformPosition = core.position;
            lastPlatformRotation = core.rotation;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            return;


            void OverlayCamInitialize()
            {
                if (!overlayCamera)
                {
                    overlayCamera = Resources.Load<GameObject>("PlayerArmsOverlayCam");
                }

                GameObject instantiatedOverlayCamera = Instantiate(overlayCamera);
                cameraController.AddOverlayCamera(instantiatedOverlayCamera.GetComponent<Camera>());
            }
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
                RespawnAtMapStart();
            };
        }

        private void Update()
        {
            if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RespawnAtStart();
            }

            if(Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                RespawnAtMapStart();
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
                if(isClimbing)
                {
                    ClimbStep();
                }
                else
                {
                    core.linearVelocity = Vector3.zero;
                    // Hold forward from the mantle hold to pull up onto the ledge.
                    if(movementInput.y > 0.1f)
                    {
                        StartClimb();
                    }
                }
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
                    tryingToSlide = false;
                    StartCrouch();
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
                else
                {
                    // On a slope: actively push the slide downhill so slope slides speed up
                    // instead of just coasting. ProjectOnPlane(down, normal) points down the
                    // slope with magnitude sin(slopeAngle), so steeper slopes accelerate harder.
                    Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal);
                    core.AddForce(slopeDir * movementData.slideSlopeAcceleration);
                }

                if(core.linearVelocity.magnitude <= movementData.slideEndSpeed)
                {
                    StopCrouch();
                }
                return;
            }

            if(movementInput != Vector2.zero)
            {
                Vector3 flatFacingDir = new Vector3(facingDirection.x, 0, facingDirection.z).normalized;
                Vector3 rightDir = new Vector3(flatFacingDir.z, 0, -flatFacingDir.x);
                Vector3 inputDir = movementInput.y * flatFacingDir + movementInput.x * rightDir;

                if(CheckGround())
                {
                    // Redirect the (horizontal) input along the slope surface so the player
                    // walks *up* an incline instead of pushing straight into it. Skipped on
                    // slopes steeper than the walkable limit, so steep faces act like walls
                    // and give no climbing assist. Magnitude is preserved so analog input
                    // and flat-ground behavior are unchanged.
                    Vector3 accelDir = inputDir;
                    if(Vector3.Angle(Vector3.up, groundNormal) <= movementData.maxWalkableSlopeAngle)
                    {
                        Vector3 projected = Vector3.ProjectOnPlane(inputDir, groundNormal);
                        if(projected.sqrMagnitude > 0.0001f)
                        {
                            accelDir = projected.normalized * inputDir.magnitude;
                        }
                    }

                    // Only accelerate while under the speed cap. Above it (e.g. momentum
                    // carried in from a slide) we coast and let SoftCapSpeed bleed the
                    // excess off, so input can never push past maxSpeed. This keeps the
                    // cap uniform across all directions instead of letting forward/strafe
                    // settle at different equilibrium speeds.
                    if(core.linearVelocity.magnitude < movementData.maxSpeed)
                    {
                        core.AddForce(accelDir * movementData.acceleration * Time.deltaTime);
                    }

                    // Face the movement direction. Set rotation directly from inputDir instead of
                    // LookAt(core.position + inputDir): LookAt derives forward from the vector between
                    // the orientation transform's OWN position and the target, and since that transform
                    // doesn't follow the player, its forward ended up pointing from the world origin to
                    // the player. That made facing (and the sideways/backward damping below) depend on
                    // the player's position relative to origin instead of the actual input direction.
                    if(inputDir.sqrMagnitude > 0.0001f)
                    {
                        orientation.rotation = Quaternion.LookRotation(inputDir);
                    }

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

                    // Face the movement direction. Set rotation directly from inputDir instead of
                    // LookAt(core.position + inputDir): LookAt derives forward from the vector between
                    // the orientation transform's OWN position and the target, and since that transform
                    // doesn't follow the player, its forward ended up pointing from the world origin to
                    // the player. That made facing (and the sideways/backward damping below) depend on
                    // the player's position relative to origin instead of the actual input direction.
                    if(inputDir.sqrMagnitude > 0.0001f)
                    {
                        orientation.rotation = Quaternion.LookRotation(inputDir);
                    }

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
            if(isClimbing) return;
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
                    groundNormal = hit.normal;
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

        // Returns true only when there is spawnable ground beneath the player on all sides,
        // so respawn points are never recorded right at the edge of a platform — or on
        // surfaces the player can walk on but shouldn't respawn onto (e.g. placed blocks).
        private bool IsStableGround()
        {
            // Respawn points use spawnableLayers, which is separate from the movement groundLayers
            // so the player can stand/wallrun on things without them counting as safe spawn ground.
            // Falls back to groundLayers when spawnableLayers is left unset (Nothing).
            LayerMask spawnMask = movementData.spawnableLayers == 0
                ? movementData.groundLayers
                : movementData.spawnableLayers;

            Vector3 origin = new Vector3(core.position.x, core.position.y - 0.4f, core.position.z);
            Vector3[] offsets = {
                Vector3.forward * respawnGroundCheckRadius,
                Vector3.back * respawnGroundCheckRadius,
                Vector3.left * respawnGroundCheckRadius,
                Vector3.right * respawnGroundCheckRadius,
            };

            foreach(Vector3 offset in offsets)
            {
                if(!Physics.Raycast(origin + offset, Vector3.down, respawnGroundCheckLength, spawnMask))
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
                        mantleTopPoint = verticalHit.point;
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

        // Begin the scripted pull-up from a mantle hold. Position is captured now; the
        // player is frozen (gravity off, zero velocity) while mantling so it stays valid.
        private void StartClimb()
        {
            isClimbing = true;
            climbTimer = 0f;
            climbStart = core.position;
            climbTarget = mantleTopPoint + Vector3.up * movementData.mantleClimbHeightOffset;
            core.linearVelocity = Vector3.zero;
        }

        // Drives the climb each physics step. Two-phase so we clear the lip: rise straight
        // up first, then move forward onto the top surface. Position is set directly so the
        // ledge collider can't block the sweep; gravity is still off until FinishClimb.
        private void ClimbStep()
        {
            climbTimer += Time.deltaTime;
            float t = Mathf.Clamp01(climbTimer / movementData.mantleClimbDuration);

            Vector3 next;
            if(t < 0.5f)
            {
                float phase = t / 0.5f;
                next = new Vector3(climbStart.x, Mathf.Lerp(climbStart.y, climbTarget.y, phase), climbStart.z);
            }
            else
            {
                float phase = (t - 0.5f) / 0.5f;
                next = new Vector3(Mathf.Lerp(climbStart.x, climbTarget.x, phase), climbTarget.y, Mathf.Lerp(climbStart.z, climbTarget.z, phase));
            }

            core.position = next;
            core.linearVelocity = Vector3.zero;

            if(t >= 1f)
            {
                FinishClimb();
            }
        }

        private void FinishClimb()
        {
            core.position = climbTarget;
            core.linearVelocity = Vector3.zero;
            isClimbing = false;
            LeaveMantle();
        }

        private IEnumerator MantleCooldownTimer()
        {
            didMantle = true;
            
            yield return new WaitForSeconds(movementData.mantleCooldown);

            didMantle = false;
        }

        // Enter the crouch/slide state: lower the camera and shrink the body capsule.
        private void StartCrouch()
        {
            IsCrouching = true;
            camParent.MoveCamToCrouching();
            ApplyColliderHeight(movementData.crouchHeight);
        }

        // Leave the crouch/slide state: raise the camera and restore the standing capsule.
        private void StopCrouch()
        {
            IsCrouching = false;
            camParent.MoveCamToStanding();
            ApplyColliderHeight(standingHeight);
        }

        // Resize the body capsule while keeping its bottom (the feet) fixed in place, so
        // crouching lowers the head to fit under obstacles instead of lifting the feet off
        // the ground. The bottom is always the standing bottom; only the top moves.
        private void ApplyColliderHeight(float targetHeight)
        {
            float bottom = standingCenter.y - standingHeight * 0.5f;
            bodyCollider.height = targetHeight;
            Vector3 center = bodyCollider.center;
            center.y = bottom + targetHeight * 0.5f;
            bodyCollider.center = center;
        }

        private void OnCrouchPerformed(object sender, EventArgs e)
        {
            if(isClimbing) return;
            if(IsMantling)
            {
                LeaveMantle();
            } else if(CheckGround())
            {
                StartCrouch();
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
                StopCrouch();
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

        // Respawn at the last platform the player stood on.
        public void RespawnAtStart()
        {
            Respawn(lastPlatformPosition, lastPlatformRotation);
        }

        // Respawn back at the very start of the map. T key and death are full level resets:
        // clear every player-placed building and restore building counts.
        public void RespawnAtMapStart()
        {
            Building.BuildingSystem.Instance?.ResetBuildings();
            Respawn(startPosition, startRotation);
        }

        private void Respawn(Vector3 position, Quaternion rotation)
        {
            if(ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FlashBlack(() => DoRespawn(position, rotation));
            }
            else
            {
                DoRespawn(position, rotation);
            }
        }

        private void DoRespawn(Vector3 position, Quaternion rotation)
        {
            Health.ResetCharacter();
            core.position = position;
            core.rotation = rotation;
            core.linearVelocity = Vector3.zero;
            core.angularVelocity = Vector3.zero;

            // Clear any in-progress mantle/climb so we don't respawn frozen or without gravity.
            isClimbing = false;
            IsMantling = false;
            core.useGravity = true;

            // Snap visuals so the camera/model don't smoothly slide to the new position.
            camParent.SnapPosition(core.position);
            if(!_isPlayerModelNull) playerModel.SnapPosition(core.position);
        }

        public void LungeForward()
        {
            core.AddForce(facingDirection * movementData.lungeForce, ForceMode.Impulse);
        }

        // Bounce the player straight up (e.g. off a jump pad), keeping horizontal momentum.
        // Setting the vertical velocity gives a consistent launch height regardless of fall speed;
        // flagging IsJumped (via the jump cooldown) stops the grounded "stopping power" from damping it.
        public void JumpPadLaunch(float upwardVelocity)
        {
            Vector3 v = core.linearVelocity;
            v.y = upwardVelocity;
            core.linearVelocity = v;
            StartCoroutine(JumpCooldownTimer());
        }

        // Give the player a horizontal speed boost (e.g. from a boost pad / speed strip).
        // Only ever speeds the player up along `direction` — never slows them — and leaves vertical
        // velocity untouched so jumps/falls are unaffected. Because this sets speed above maxSpeed,
        // SoftCapSpeed eases it back down to normal on its own once the player leaves the pad, which
        // gives the "shoot forward then coast back to normal" feel of a racing boost.
        public void SpeedBoost(float boostSpeed, Vector3 direction)
        {
            Vector3 dir = new Vector3(direction.x, 0f, direction.z);
            if (dir.sqrMagnitude < 1e-6f) return;
            dir.Normalize();

            Vector3 v = core.linearVelocity;
            // Speed we already carry along the boost direction. Only boost when we're going slower
            // than the target that way, so re-touching the pad can't stack past the intended speed
            // and it never fights a faster entry (e.g. a slide onto the strip).
            float speedAlong = Vector3.Dot(new Vector3(v.x, 0f, v.z), dir);
            if (speedAlong >= boostSpeed) return;

            core.linearVelocity = new Vector3(dir.x * boostSpeed, v.y, dir.z * boostSpeed);
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
