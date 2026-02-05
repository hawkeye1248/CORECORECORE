using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace peterkcodes.AdvancedMovement
{
    /// <summary>
    /// The primary movement script responsible for driving the player.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        #region Values
        MoveState currentState;

        #region Camera
        [Header("Camera")]
        [Tooltip("The camera transform.")]
        [SerializeField] new private Transform camera;

        //I recommend replacing the camera scripts with your own.

        [Tooltip("The ammount of force required for the camera to jolt on landing.")]
        [SerializeField] private float crashForce;

        [Tooltip("The amplitude with which the camera should jolt after a heavy landing.")]
        [SerializeField] private float joltAmplitude;

        [Tooltip("The duration of the camera jolt after a heavy landing.")]
        [SerializeField] private float joltDuration;
        #endregion

        #region Movement
        [Header("Movement")]
        [Tooltip("The player's move speed when walking.")]
        [SerializeField] private float baseMoveSpeed;

        [Tooltip("The player's move speed when sprinting on flat ground.")]
        [SerializeField] private float sprintMoveSpeed;

        [Tooltip("The player's move speed when crouched.")]
        [SerializeField] private float crouchSpeed;

        [Tooltip("The player's move speed when airborne.")]
        [SerializeField] private float airStrafeSpeed;

        [Tooltip("The force to jump with on flat ground.")]
        [SerializeField] private float jumpForce;

        [Tooltip("The time after stepping of a ledge where the player can still perform a jump.")]
        [SerializeField] private float coyoteTime;

        private bool isAirborneFromJumping;
        private float timeSinceGrounded;
        #endregion

        #region Mantle
        [Header("Mantle")]
        [Tooltip("The origin for the second mantle ray. If the ray from the player's origin returns true, and the ray from this point returns false, the mantle is valid. (More info in script)")]
        [SerializeField] private Vector3 mantleOrigin;

        [Tooltip("The force applied upon mantling.")]
        [SerializeField] private Vector3 mantleForce;

        [Tooltip("The distance ahead of the player used when checking if the player is indeed facing a wall to mantle.")]
        [SerializeField] private float mantleDistance;

        [Tooltip("The stamina granted when the player successfully mantles")]
        [SerializeField] private float mantleStaminaGrant;

        [Tooltip("When enabled, the player will mantle whenever a suitable ledge is found. Otherwise, the player will have to press jump to perform the mantle")]
        [SerializeField] private bool autoMantle;

        [Tooltip("Time to wait after a mantle before the player can mantle again.")]
        [SerializeField] private float mantleCooldown;

        bool isMantleValid;
        float lastMantleTime;
        #endregion

        #region Sprint
        [Header("Sprint")]

        [Tooltip("The minimum velocity required to start and maintain a sprint.")]
        [SerializeField] private float minimumSprintMagnitude;

        [Tooltip("Multiplier applied to camera FOV to give a sense of speed when sprinting.")]
        [SerializeField] private float sprintFovMultiplier = 1.2f;

        [Tooltip("The time taken for the camera to reach the desired FOV when the sprinting state changes.")]
        [SerializeField] private float sprintFovShiftTime = 1f;

        [Tooltip("The acceleration rate towards top speed when sprinting.")]
        [SerializeField] private float sprintAcceleration;

        #endregion

        #region Slide
        [Header("Slide")]
        [Tooltip("The minimum angle required for a slide to be achieved")]
        [SerializeField] private float slideActivationThresholdAngle;

        [Tooltip("When a slope is steeper (less than) than this threshold, ranging from 0 (sheer drop) to 1 (completely flat), the player will gain speed when sliding.")]
        [SerializeField] private float slideAccelerationThreshold;

        [Tooltip("The maximum velocity possible when sliding.")]
        [SerializeField] private float maxSlideVelocity;

        [Tooltip("The rate at which to accelerate when the acceleration threshold is met")]
        [SerializeField] private float slideAccelerationRate;

        [Tooltip("A ray this far ahead of the player, facing downwards, is used to calculate the angle of a given slope.")]
        [SerializeField] private float slopeCheck2Offset;

        [Tooltip("The speed at which the player can move side-to-side when sliding.")]
        [SerializeField] private float slideStrafeSpeed;

        private Vector3 slideDirection;
        #endregion

        #region Wallrun
        [Header("Wallrun")]
        [Tooltip("Rate of acceleration while wallrunning.")]
        [SerializeField] private float wallrunAcceleration;

        [Tooltip("Gravity applied while wallrunning.")]
        [SerializeField] private float wallrunGravity;

        [Tooltip("Sticking force applied towards the wall to keep the player attached. Allows for smooth navigation of curves.")]
        [SerializeField] private float wallrunStickForce;

        [Tooltip("Distance either side of the player used to check for walls.")]
        [SerializeField] private float wallrunCheckRange;

        [Tooltip("Force with which to kick off the wall.")]
        [SerializeField] private Vector2 wallkickForce;

        [Tooltip("Maximum velocity achievable when wallrunning.")]
        [SerializeField] private float maxWallrunVelocity;

        [Tooltip("Minimum velocity for a wallrun to be executed.")]
        [SerializeField] private float minimumWallrunMagnitude;

        [Tooltip("Stamina given after a successful wallkick.")]
        [SerializeField] private float staminaOnWallkickGrant;

        [Tooltip("Sideways tilt applied to the camera when wallrunning.")]
        [SerializeField] private float wallTilt;

        [Tooltip("Gravity applied while against a wall but not moving.")]
        [SerializeField] private float wallSlipGravity;
        
        private bool isAgainstWall;
        private bool wallDirection;
        private RaycastHit wallHit;
        #endregion

        #region Input
        private bool jumpNextUpdate;
        private bool slideNextUpdate;
        private bool sprintNextUpdate;
        private Vector2 inputAxes;
        #endregion

        #region Physics
        [Header("Physics")]
        [Tooltip("The standard rate of gravity.")]
        [SerializeField] private float gravityRate;

        [Tooltip("The rate at which velocity reaches 0 when on the ground")]
        [SerializeField] private float friction;

        [Tooltip("The rate at which velocity reaches 0 when in the air")]
        [SerializeField] private float drag;

        [Tooltip("A downward force applied when grounded so the player doesn't fly off small slopes")]
        [SerializeField] private float stickyForce;

        [Tooltip("The rate at which the player can accelerate while airborne. Higher values give the player more control of their movements while in the air.")]
        [SerializeField] private float airAcceleration;

        public Vector3 velocity { get; private set; }
        private Vector3 forceToAdd;
        private bool wasSprinting;
        private float nonVerticalMagnitude;
        #endregion

        #region Events
        public event OnStateChange onStateChanged;

        //Camera effects are handled by these events. The script tells the camera to jolt, adjust fov, etc, while you can have your own script deciding exactly how to handle those instructions.
        public event FXTiltDelegate fxSetTilt;
        public event fxResetFOVDelegate fxResetFOV;
        public event FXShiftFOVDelegate fxAdjustFOV;
        public event FXJolt fxJolt;
        #endregion

        #region Ground Check
        [Header("Ground Check")]
        [Tooltip("The point below the player to check for ground.")]
        [SerializeField] private Vector3 groundCheckPoint;

        [Tooltip("The size of the cube used to check for ground.")]
        [SerializeField] private Vector3 groundCheckScale;

        [Tooltip("Valid layers for ground. Also used to detect walls for wallrunning/mantling.")]
        [SerializeField] private LayerMask groundLayers;
        private bool grounded;
        #endregion

        private CharacterController cc;
        //private PlayerVisual ps;
        #endregion

        // Start is called before the first frame update
        void Start()
        {
            cc = GetComponent<CharacterController>();
            //ps = GetComponent<PlayerVisual>();
        }

        private void FixedUpdate()
        {
            //Calculate the magnitude of the player's velocity, disregarding verticality.
            //Used when determining if the player meets speed thresholds to prevent them being able to cheat it by just falling.
            Vector3 _velocityNoY = velocity;
            _velocityNoY.y = 0;
            nonVerticalMagnitude = _velocityNoY.magnitude;

            grounded = CheckGround();
            
            //Runs the actual movement itself.
            Think();

            //Apply physics.
            ApplyGravity();
            ApplyForce();

            //Set our position to compare after the move.
            Vector3 preMovementPosition = transform.position;

            //Move the character.
            cc.Move(velocity * Time.fixedDeltaTime);

            //Clamp velocity to the actual affected movement.
            Vector3 actualAffectedVelocity = (transform.position - preMovementPosition) / Time.fixedDeltaTime;
            velocity = Vector3.Min(actualAffectedVelocity, velocity);

            //Reset input flags.
            jumpNextUpdate = false;
            sprintNextUpdate = true;
        }

        #region Input
        //These methods are used to drive input to the controller by external scripts.
        public void FlagJumpInput()
        {
            jumpNextUpdate = true;
        }

        public void FlagSlideInput()
        {
            slideNextUpdate = true;
        }

        public void FlagSprintInput()
        {
            sprintNextUpdate = true;
        }

        public void SetInputAxes (Vector2 value)
        {
            inputAxes = value;
        }
        #endregion

        #region Movement Behaviours
        /// <summary>
        /// Runs the necessary movement behaviour.
        /// </summary>
        void Think()
        {
            switch (currentState)
            {
                case MoveState.walk:
                    ManageWalk();
                    break;
                case MoveState.air:
                    ManageAir();
                    break;
                case MoveState.crouch:
                    ManageCrouch();
                    break;
                case MoveState.sprint:
                    ManageSprint();
                    break;
                case MoveState.slide:
                    ManageSlide();
                    break;
                case MoveState.wallrun:
                    ManageWallrun();
                    break;
            }
        }

        private void ManageWalk()
        {
            fxSetTilt?.Invoke(0);

            //Sprint if allowed and desired.
            if (sprintNextUpdate && nonVerticalMagnitude > minimumSprintMagnitude)
            {
                fxAdjustFOV?.Invoke(sprintFovMultiplier, sprintFovShiftTime);
                SetMovementState(MoveState.sprint);
                return;
            }

            //Crouch if desired.
            if (slideNextUpdate)
            {
                SetMovementState(MoveState.crouch);
            }

            BaseMovement();
        }

        private void ManageCrouch()
        {
            //Uncrouch if the slide button is pressed.
            if (slideNextUpdate)
            {
                SetMovementState(MoveState.walk);
            }

            BaseMovement();
        }

        /// <summary>
        /// Airborne movement works by gradually accelerating towards the max airspeed, with basic simulations of drag.
        /// The airborne state is also used as a jumping off point for mantling and wallrunning
        /// </summary>
        private void ManageAir()
        {
            //Disable mantling if cooldown hasn't expired
            if(Time.time < lastMantleTime + mantleCooldown)
                isMantleValid = false;

            timeSinceGrounded += Time.fixedDeltaTime;
            Vector3 _input = GetMovementInputVector();

            Vector3 _adjusted = Vector3.MoveTowards(velocity, Vector3.zero, drag * Time.fixedDeltaTime);
            _adjusted.y = velocity.y;
            velocity = _adjusted;

            //If we have an input handle air acceleration towards the input
            if (_input != Vector3.zero)
            {
                Vector3 futureVelocity = Vector3.MoveTowards(velocity, _input * airStrafeSpeed, airAcceleration * Time.fixedDeltaTime);
                futureVelocity.y = 0;
                //If we would not exceed the max airstrafe speed, or the input is behind us (allows for airbraking) apply the movement.
                if (futureVelocity.magnitude < airStrafeSpeed || Vector3.Dot(velocity.normalized, _input) < 0)
                {
                    futureVelocity.y = velocity.y;
                    velocity = futureVelocity;
                }
            }

            if (grounded)
            {
                timeSinceGrounded = 0;
                SetMovementState((wasSprinting) ? MoveState.sprint : MoveState.walk);
                fxSetTilt?.Invoke(0);
            }

            if (CanMantle())
            {
                //There is a wall ahead that we can mantle over. If we are allowed to mantle and the player presses jump/has auto-mantle set we mantle.
                if ((jumpNextUpdate || (autoMantle && inputAxes.y > 0)) && isMantleValid)
                {
                    //Reset gravity if necessary.
                    velocity = new Vector3(velocity.x, Mathf.Max(velocity.y, 0), velocity.z);
                    AddImpulseForce(transform.TransformVector(mantleForce));
                    //Reset cooldown.
                    isMantleValid = false;
                    lastMantleTime = Time.time;
                }
            }
            else if (IsPlayerTouchingWall(out wallDirection))
            {
                //Player is touching a wall, wallrun if we are at the appropriate speed.
                isAgainstWall = true;
                if (nonVerticalMagnitude > minimumWallrunMagnitude)
                {
                    SetMovementState(MoveState.wallrun);
                }

                //If jumping, immediately kick off instead of waiting for the state change, allowing for walljumps back and forth without sliding forward.
                if (jumpNextUpdate)
                {
                    Wallkick(wallHit.normal, Vector3.zero);
                    velocity += Vector3.up * gravityRate / 2;
                }

                fxSetTilt?.Invoke((wallDirection) ? wallTilt : -wallTilt);
                return;
            }
            else
            {
                isAgainstWall = false;
                fxSetTilt?.Invoke(0);
            }

            //Even though we are airborne, try to jump anyway to allow for coyote time.
            TryJump();
        }

        private void ManageSprint()
        {
            timeSinceGrounded = 0;

            wasSprinting = true;
            Vector3 _input = GetMovementInputVector();

            isAirborneFromJumping = false;
            
            if (!grounded)
            {
                SetMovementState(MoveState.air);
                return;
            }

            ///Disable sprint if sprint button is pressed, we run out of stamina, or we get too slow.
            /*if (sprintNextUpdate || nonVerticalMagnitude < minimumSprintMagnitude)
            {
                fxResetFOV?.Invoke(sprintFovShiftTime);
                SetMovementState(MoveState.walk);
                return;
            }*/

            //Enable slide if desired.
            if (slideNextUpdate)
            {
                slideDirection = velocity.normalized;
                slideNextUpdate = false;
                SetMovementState(MoveState.slide);
                return;
            }

            TryJump();

            //Accelerate towards maximum sprint speed.
            Vector3 _move = _input * sprintMoveSpeed;
            Vector3 _adjusted = Vector3.MoveTowards(velocity, _move, sprintAcceleration * Time.fixedDeltaTime);
            _adjusted.y = velocity.y;
            velocity = _adjusted;

            slideNextUpdate = false;
        }

        private void ManageSlide()
        {
            timeSinceGrounded = 0;

            float angle = GetSlideAngle(slideDirection);

            Vector3 targetVelocity;
            if (angle < slideAccelerationThreshold)
            {
                //Angle is sufficient to slide and accelerate.
                targetVelocity = slideDirection.normalized * maxSlideVelocity;
            }
            else
            {
                //Angle is not sufficient to slide, target velocity is zero.
                targetVelocity = Vector3.zero;
            }

            if (!grounded)
            {
                SetMovementState(MoveState.air);
                return;
            }


            //Approach target velocity.
            velocity = Vector3.MoveTowards(velocity, targetVelocity, slideAccelerationRate * Time.fixedDeltaTime);
            //Apply strafing
            velocity -= Vector3.Cross(slideDirection, Vector3.up) * inputAxes.x * slideStrafeSpeed * Time.fixedDeltaTime;

            TryJump();

            //Disable slide if button pressed.
            if (slideNextUpdate)
            {
                SetMovementState(MoveState.sprint);
                slideNextUpdate = false;
                return;
            }

            if (nonVerticalMagnitude < minimumSprintMagnitude)
            {
                SetMovementState(MoveState.walk);
                slideNextUpdate = false;
                return;
            }

            slideNextUpdate = false;
        }

        private void ManageWallrun()
        {
            //We are going too slow to wallrun, set state to airborne.
            if (nonVerticalMagnitude < minimumWallrunMagnitude)
            {
                SetMovementState(MoveState.air);
                fxSetTilt?.Invoke(0);
                return;
            }

            if (grounded)
            {
                SetMovementState(MoveState.sprint);
                fxSetTilt?.Invoke(0);
                return;
            }

            if (!IsPlayerTouchingWall(out wallDirection))
            {
                SetMovementState(MoveState.air);
                fxSetTilt?.Invoke(0);
                return;
            }

            Vector3 wallNormal = wallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, camera.transform.up);

            //Vector trickery to make sure the appropriate forward direction is used instead of sending the player sliding backwards.
            if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            float _inputVertical = inputAxes.y;

            //Calculate target velocity based on input.
            Vector3 _targetVelocity = wallForward * maxWallrunVelocity * _inputVertical;
            _targetVelocity.y = wallrunGravity;

            //Apply sticking force to the wall to allow for running around curves and different geometry without slipping off.
            Vector3 _stick = -wallNormal * wallrunStickForce;

            //Accelerate towards target velocity.
            velocity = Vector3.MoveTowards(velocity - _stick, _targetVelocity, wallrunAcceleration * Time.fixedDeltaTime);
            velocity += _stick;

            if (jumpNextUpdate)
            {
                Wallkick(wallNormal, _stick);
            }

            if (velocity.magnitude > maxWallrunVelocity)
            {
                velocity = velocity.normalized * maxWallrunVelocity;
            }
        }
        #endregion

        #region Movement Helper Methods
        private void TryJump()
        {
            if (jumpNextUpdate && CanJump())
            {
                timeSinceGrounded = coyoteTime;
                AddImpulseForce(Vector3.up * (jumpForce - stickyForce));
                isAirborneFromJumping = true;
            }
        }

        /// <returns> True if the player has been airborne for less than coyoteTime seconds, and is not airborne from jumping. </returns>
        private bool CanJump ()
        {
            return timeSinceGrounded < coyoteTime && !isAirborneFromJumping;
        }

        /// <summary>
        /// The most basic movement, shared by the walking and crouch behaviour.
        /// </summary>
        private void BaseMovement()
        {
            timeSinceGrounded = 0;
            if (!grounded)
            {
                wasSprinting = false;
                SetMovementState(MoveState.air);
                return;
            }

            //Not airborne, so not airborne from jumping.
            isAirborneFromJumping = false;

            TryJump();

            Vector3 _move = GetMovementInputVector() * ((currentState == MoveState.walk) ? baseMoveSpeed : crouchSpeed);
            Vector3 _adjusted;
            if (_move != Vector3.zero)
            {
                _adjusted = _move;
            }
            else
            {
                _adjusted = Vector3.MoveTowards(velocity, Vector3.zero, friction * Time.fixedDeltaTime);
            }

            _adjusted.y = velocity.y;
            velocity = _adjusted;

            slideNextUpdate = false;
        }

        private void Wallkick(Vector3 wallNormal, Vector3 stickForce)
        {
            //Calculate upForce, at most it is wallkickForce but it's clamped so you can't kick up to infinity.
            float _upForce = Mathf.Abs(velocity.y - wallkickForce.y);
            _upForce = Mathf.Clamp(_upForce, 0, wallkickForce.y);
            AddImpulseForce((wallNormal * wallkickForce.x + Vector3.up * _upForce) - stickForce);

            //Count this as a jump to prevent coyote timing immediately after a wallkick.
            isAirborneFromJumping = true;
        }
        
        /// <summary>
        /// Converts input axes into a worldspace direction for movement
        /// </summary>
        private Vector3 GetMovementInputVector()
        {
            Vector3 _move = inputAxes.y * transform.forward;
            _move += inputAxes.x * transform.right;
            return _move.normalized;
        }

        /// <summary>
        /// Sets state to _newState, while also updating the camera controller and applying necessary FX.
        /// Some flags are reset to maintain proper behaviour.
        /// </summary>
        public void SetMovementState(MoveState _newState)
        {
            //print($"Changing state from {currentState} to {_newState}");
            
            currentState = _newState;
            onStateChanged?.Invoke(currentState);
            isAgainstWall = false;
            isMantleValid = true;
        
            switch(_newState)
            {
                case MoveState.walk:
                //ps.SetStateToIdle();
                break;
                case MoveState.slide:
                //ps.SetStateToSlide();
                break;
                case MoveState.sprint:
                //ps.SetStateToRunning();
                break;
                case MoveState.wallrun:
                //ps.SetStateToWallrunning();
                break;
                case MoveState.air:
                //ps.SetStateToJumping();
                break;
                default:
                break;
            }
        }
        #endregion

        #region Physics
        private void ApplyGravity()
        {
            //If wallrunning, gravity is handled seperately.
            if (currentState == MoveState.wallrun)
            {
                return;
            }

            //If against a wall but not wallrunning, apply the slip gravity.
            if (isAgainstWall)
            {
                velocity -= Vector3.up * wallSlipGravity * Time.fixedDeltaTime;
            }
            else
            {
                //Otherwise, apply normal gravity.
                velocity -= Vector3.up * gravityRate * Time.fixedDeltaTime;
            }

            //When grounded, clamp the y velocity to not go below the 'stickyForce'
            if (grounded)
            {
                velocity = new Vector3(velocity.x, Mathf.Max(stickyForce, velocity.y), velocity.z);
            }
        }

        /// <summary>
        /// Increments forceToAdd by the desired _force, to be applied at the end of the next FixedUpdate call.
        /// </summary>
        public void AddImpulseForce(Vector3 _force)
        {
            forceToAdd += _force;
        }

        /// <summary>
        /// Applies impulse force at the end of an update and resets 'forceToAdd'.
        /// </summary>
        private void ApplyForce()
        {
            velocity += forceToAdd;
            forceToAdd = Vector3.zero;
        }
        #endregion

        #region Checks
        /// <summary>
        /// Returns true if the player is touching a wall on either side, and ouptuts a flag to signify which side the wall is on.
        /// </summary>
        private bool IsPlayerTouchingWall(out bool isRight)
        {
            isRight = false;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.right, out hit, wallrunCheckRange, groundLayers))
            {
                wallHit = hit;
                isRight = true;
                return true;
            }

            if (Physics.Raycast(transform.position, -transform.right, out hit, wallrunCheckRange, groundLayers))
            {
                wallHit = hit;
                isRight = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return true if the player has a wall in front of them at their origin, but not ahead of the mantleOrigin.
        /// </summary>
        private bool CanMantle()
        {
            if (nonVerticalMagnitude < minimumSprintMagnitude)
            {
                return false;
            }

            bool rayAhead = Physics.Raycast(transform.position, transform.forward, mantleDistance, groundLayers);
            bool rayAboveHead = Physics.Raycast(transform.TransformPoint(mantleOrigin), transform.forward, mantleDistance, groundLayers);
            return rayAhead && !rayAboveHead;
        }

        /// <summary>
        /// Returns the angle of the surface below the player.
        /// </summary>
        private float GetSlideAngle (Vector3 _moveVector)
        {
            float angle = 1;
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                angle = Vector3.Dot(hit.normal, Vector3.up);

                //Offset the movement vector by the sticky force to ensure it isn't inside an uphill slope.
                Vector3 secondRayOrigin = transform.position + _moveVector.normalized + Vector3.up * -stickyForce;
                Debug.DrawRay(secondRayOrigin, Vector3.down, Color.red);
                Debug.DrawRay(transform.position, Vector3.down, Color.red);
                Debug.DrawRay(transform.position, _moveVector, Color.green);

                Vector3 hitPointOne = hit.point;
                if (Physics.Raycast(secondRayOrigin, Vector3.down, out RaycastHit hit2, Mathf.Infinity, groundLayers))
                {
                    Vector3 hitPointTwo = hit2.point;
                    //HitPointTwo is above hitPointOne, we are going uphill so negate the angle.
                    if (hitPointTwo.y > hitPointOne.y)
                    {
                        angle = 2 - angle;
                    }
                }
            }
            return angle;
        }

        /// <summary>
        /// Returns true if the player is grounded according to a box check at their feet.
        /// </summary>
        private bool CheckGround()
        {
            bool newGrounded = Physics.OverlapBox(transform.TransformPoint(groundCheckPoint), groundCheckScale, transform.rotation, groundLayers).Length > 0;

            //Jolt the camera if we land with enough velocity and do not intend to slide.
            if (!grounded && newGrounded)
            {
                if (Mathf.Abs(velocity.y) > crashForce)
                {
                    if (!slideNextUpdate)
                    {
                        fxJolt?.Invoke(joltAmplitude, joltDuration);
                    }
                }
            }

            return newGrounded;
        }
        #endregion

        #region Events
        public delegate void OnStateChange(MoveState _state);
        public delegate void UIStaminaUpdate(float value);
        public delegate void FXTiltDelegate(float tilt); //Event called to update the current tilt effect. Can be subscribed to by an effects script to make effects work.
        public delegate void fxResetFOVDelegate(float time); //Event called to tell the camera effects to reset the fov over the given time.
        public delegate void FXShiftFOVDelegate(float multiplier, float time); //Event called to tell the camera effects to multiply the fov by the given ammount over the given time.
        public delegate void FXJolt(float amplitude, float time); //Event called to tell the camera effects to jolt the camera by the given amplitude over the given time.
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.TransformPoint(groundCheckPoint), groundCheckScale);
            Gizmos.DrawLine(transform.TransformPoint(mantleOrigin), transform.TransformPoint(mantleOrigin) + transform.forward * mantleDistance);
        }
        #endregion
    }

    /// <summary>
    /// This script is built on this. All movement is contained within one of these states to allow for distinct types of movement which don't clash with eachother.
    /// </summary>
    public enum MoveState
    {
        walk,
        crouch,
        air,
        sprint,
        slide,
        wallrun,
        none
    }
}