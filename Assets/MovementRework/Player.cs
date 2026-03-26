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
        [SerializeField] private CameraController cameraController;

        [Header("Status Bools")]
        [SerializeField] private bool isGrounded = false;
        [SerializeField] private bool isWallrunning = false;
        [SerializeField] private bool isJumped = false;
        [SerializeField] private bool isCrouching = false;
        [SerializeField] private bool isMantling = false;

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
        [SerializeField] private float fallGravity;
        [SerializeField] private float landingJoltPower = 2f;

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

        [Header("Mantle Parameters")]
        [SerializeField] private Vector3 mantleRaycastPoint = new Vector3(0, 1.3f, 0);
        [SerializeField] private float mantleDistance = 0.7f;
        [SerializeField] private float mantleLength = 1f;
        [SerializeField] private float mantleJoltPower = 5f;
        [SerializeField] private float mantleJumpForce = 5f;
        private Vector3 mantleHoldPoint = Vector3.zero;

        [Header("Wallrunning Parameters")]
        [SerializeField] private float wallrunAcceleration = 1500f;
        [SerializeField] private float wallrunMaxSpeed = 25f;
        private Vector3 wallForward = Vector3.zero;
        private bool didWallrun = false;
        [SerializeField] private float wallrunCooldown = 0.25f;
        [SerializeField] private float wallJumpForce = 10f;
        

        private void Awake() {
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

            CheckMantle();

            CheckWallrun();
            


            if(core.linearVelocity.y < 0)
            {
                core.AddForce(Vector3.down * fallGravity);
            }
        }

        private void SetFacingDirection()
        {
            facingDirection = cameraController.facingDirection;
        }

        private void MovePlayer(Vector2 movementInput)
        {
            if(isMantling)
            {
                core.linearVelocity = Vector3.zero;
                return;
            }

            if(isWallrunning)
            {
                core.AddForce(wallForward * wallrunAcceleration * Time.deltaTime);
                core.linearVelocity = Vector3.ClampMagnitude(core.linearVelocity, maxSpeed);
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
            if(isMantling)
            {
                LeaveMantle();
                MantleJump();
            } else if(isWallrunning)
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
            core.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            StartCoroutine(JumpCooldownTimer());
        }

        private void WallJump()
        {
            core.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            core.AddForce(cameraController.transform.forward * wallJumpForce, ForceMode.Impulse);
            StartCoroutine(JumpCooldownTimer());
        }

        private void MantleJump()
        {
            core.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if(Vector3.Dot(new Vector3(core.position.x, 0, core.position.z) - new Vector3(mantleHoldPoint.x, 0, mantleHoldPoint.z), cameraController.transform.forward) >= 0)
            {
                core.AddForce(cameraController.transform.forward * mantleJumpForce, ForceMode.Impulse);
            }
            StartCoroutine(JumpCooldownTimer());
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
                camParent.Jolt(core.linearVelocity.y, landingJoltPower);
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

        private void CheckMantle()
        {
            if(!CheckGround() && core.linearVelocity.y < 0)
            {
                if(Physics.Raycast(mantleRaycastPoint + core.position + facingDirection * mantleDistance, Vector3.down, out RaycastHit verticalHit, mantleLength, groundLayers))
                {
                    if(Physics.Raycast(new Vector3(core.position.x, verticalHit.point.y - 0.1f, core.position.z), orientation.forward, out RaycastHit horizontalHit, 1f, groundLayers) && !Physics.Raycast(new Vector3(core.position.x, verticalHit.point.y + 0.2f, core.position.z), orientation.forward, 1f, groundLayers))
                    {
                        isMantling = true;
                        mantleHoldPoint = horizontalHit.point;
                        camParent.Jolt(core.linearVelocity.y, mantleJoltPower);
                        core.useGravity = false;
                        core.linearVelocity = Vector3.zero;
                        
                    }
                }
            }
        }

        private void LeaveMantle()
        {
            isMantling = false;
            core.useGravity = true;
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

        private void CheckWallrun()
        {
            if(!CheckGround() && !isMantling && !didWallrun)
            {
                bool wallLeft = Physics.Raycast(core.position + Vector3.up * 0.5f, new Vector3(-facingDirection.z, 0, facingDirection.x), out RaycastHit hitLeft, 2f, groundLayers);
                bool wallRight = Physics.Raycast(core.position + Vector3.up * 0.5f, new Vector3(facingDirection.z, 0, -facingDirection.x), out RaycastHit hitRight, 2f, groundLayers);
                
                if((wallLeft || wallRight) &&  MovementInput.Instance.GetMovementVector().y > 0)
                {
                    isWallrunning = true;
                    core.useGravity = false;
                    core.linearVelocity = new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z);
                    Vector3 wallNormal = wallLeft ? hitLeft.normal : hitRight.normal;
                    wallForward = wallLeft ? Vector3.Cross(wallNormal, Vector3.up) : -Vector3.Cross(wallNormal, Vector3.up) ;

                } else
                {
                    if(isWallrunning)
                    {
                        LeaveWallrunning();
                    }
                }
            } else
            {
                if(isWallrunning)
                {
                    LeaveWallrunning();
                }
            }
        }

        private void LeaveWallrunning()
        {
            isWallrunning = false;
            StartCoroutine(WallrunCooldownTimer());
            core.useGravity = true;
        }

        private IEnumerator WallrunCooldownTimer()
        {
            didWallrun = true;
            
            yield return new WaitForSeconds(wallrunCooldown);

            didWallrun = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            //Ground Check Box
            Gizmos.DrawWireCube(new Vector3(core.transform.position.x, core.transform.position.y - 0.5f, core.transform.position.z), groundCheckScale);
        
            Gizmos.color = Color.red;
            Vector3 vStart = mantleRaycastPoint + core.position + facingDirection * mantleDistance;
            Vector3 vDirection = Vector3.down * mantleLength;
            Gizmos.DrawLine(vStart, vStart + vDirection);

            // Fizik kontrolü (Görselleştirme için tekrar hesaplanır)
            if (Physics.Raycast(vStart, Vector3.down, out RaycastHit vHit, 1f, groundLayers))
            {
                // Temas noktasına küçük bir küre çiz
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(vHit.point, 0.05f);

                // 2. Yatay Raycast Görselleştirmesi (Mavi)
                Gizmos.color = Color.blue;
                //Vector3 hStart = vHit.point + new Vector3(0, -0.1f, 0);
                Vector3 hStart = new Vector3(core.position.x, vHit.point.y - 0.1f, core.position.z);
                Vector3 hDirection = orientation.forward * 1f;
                Gizmos.DrawLine(hStart, hStart + hDirection);

                if (Physics.Raycast(hStart, orientation.forward, out RaycastHit hHit, 1f, groundLayers))
                {
                    // İkinci temas noktasını işaretle
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
            return core.linearVelocity.magnitude / maxSpeed;
        }

        public float GetHorizontalSpeedPercentage()
        {
            return new Vector3(core.linearVelocity.x, 0, core.linearVelocity.z).magnitude / maxSpeed;
        }
    }
}
