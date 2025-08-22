using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Other Objects and Children")]
    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform weaponObject;
    private Camera mainCamera;
    [SerializeField] private CinemachineBasicMultiChannelPerlin cinemachinePerlin;

    [Header("Components")]
    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7;
    [SerializeField] private float groundDrag = 5;
    private Vector2 movementInput = Vector2.zero;
    private Vector3 moveDirection = Vector3.zero;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded = false;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;
    private bool readyToJump = true;
    [SerializeField] private float airMaxSpeed = 1;

    [Header("Slope")]
    [SerializeField] private float maxSlopeAngle = 40f;
    private RaycastHit slopeHit;
    private bool exitingSlope = false;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime = 0.75f;
    [SerializeField] private float slideForce = 200;
    private float slideTimer = 0;
    [SerializeField] private float slideYscale = 0.5f;
    private float startYscale;
    private bool isSliding = false;

    [Header("Wall Run")]
    private bool isWallRunning = false;
    private bool wallRunCooldown = false;
    private float wallRunCooldownTimer = 0f;
    [SerializeField] private float wallRunMaxCooldown = 0.05f;
    private bool wallLeft = false;
    private bool wallRight = false;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsWall;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    [SerializeField] private float minJumpHeight;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;

    private void Awake()
    {
        Instance = this;

        startYscale = transform.localScale.y;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        mainCamera = Camera.main;

        GameInput.Instance.OnJumpPerformed += on_jump_performed;
        GameInput.Instance.OnSlidePerformed += on_slide_performed;
        GameInput.Instance.OnSlideCanceled += on_slide_canceled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MyInput();
        GroundCheck();
        LimitSpeed();
        //Headbob();
        CheckWall();
    }

    void FixedUpdate()
    {
        MovePlayer();
        Aiming();
        Sliding();
    }

    private void MyInput()
    {
        movementInput = GameInput.Instance.GetMovementVector();
    }

    private void Aiming()
    {
        Vector3 cameraForward = mainCamera.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward != Vector3.zero)
        {
            Quaternion newRotation = Quaternion.LookRotation(cameraForward);
            Quaternion weaponRotation = Quaternion.LookRotation(mainCamera.transform.forward);
            rb.MoveRotation(newRotation);
            weaponObject.DORotateQuaternion(weaponRotation, 0.1f);
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * movementInput.y + orientation.right * movementInput.x;

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        if ((wallLeft || wallRight) && movementInput.y > 0 && AboveWallRunLimit() && !wallRunCooldown)
        {
            WallRunning();
            isWallRunning = true;
        }
        else if (wallRunCooldown)
        {
            isWallRunning = false;
            wallRunCooldownTimer += Time.deltaTime;
            if (wallRunCooldownTimer > wallRunMaxCooldown)
            {
                wallRunCooldown = false;
                wallRunCooldownTimer = 0f;
            }
        }
        else if (isGrounded)
        {
            rb.AddForce(10f * moveSpeed * moveDirection.normalized, ForceMode.Force);
            isWallRunning = false;
        }
        else
        {
            rb.AddForce(10f * airMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Force);
            isWallRunning = false;
        }

        rb.useGravity = !OnSlope();
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, -transform.up, out slopeHit, playerHeight * 0.5f + 0.2f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle <= maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private void LimitSpeed()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else if (isGrounded)
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed * airMaxSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void GroundCheck()
    {
        isGrounded = Physics.Raycast(playerModel.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        if (isGrounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0f;
        }
    }

    private void on_jump_performed(object sender, EventArgs e)
    {
        if (readyToJump && isGrounded)
        {
            exitingSlope = true;
            readyToJump = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if (isWallRunning)
        {
            WallJump();
            wallRunCooldown = true;
        }
    }

    private void ResetJump()
    {
        exitingSlope = false;
        readyToJump = true;
    }

    private void on_slide_performed(object sender, EventArgs e)
    {
        if (movementInput != Vector2.zero)
        {
            isSliding = true;

            transform.localScale = new Vector3(transform.localScale.x, slideYscale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

            slideTimer = 0;
        }
    }

    private void Sliding()
    {
        if (isSliding)
        {
            if (!OnSlope() || rb.linearVelocity.y > -0.1f)
            {
                rb.AddForce(moveDirection.normalized * slideForce, ForceMode.Force);
                slideTimer += Time.deltaTime;
            }
            else
            {
                rb.AddForce(GetSlopeMoveDirection() * slideForce, ForceMode.Force);
            }


            if (slideTimer >= maxSlideTime)
            {
                StopSlide();
            }
        }
    }

    private void on_slide_canceled(object sender, EventArgs e)
    {
        if (isSliding)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
        isSliding = false;
        transform.localScale = new Vector3(transform.localScale.x, startYscale, transform.localScale.z);
    }

    private void Headbob()
    {
        if (isGrounded && movementInput != Vector2.zero)
        {
            cinemachinePerlin.FrequencyGain = 2;
            cinemachinePerlin.AmplitudeGain = 2;
        }
        else
        {
            cinemachinePerlin.FrequencyGain = 0.5f;
            cinemachinePerlin.AmplitudeGain = 0.5f;
        }
    }

    private void CheckWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveWallRunLimit()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void WallRunning()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (!(wallLeft && movementInput.x > 0) && !(wallRight && movementInput.x < 0))
        {
            rb.AddForce(-wallForward * 100, ForceMode.Force);
        }
    }

    private void WallJump()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}