using Unity.Cinemachine;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Running,
        Slide,
        Wallrun,
        Jump,
    }

    public bool isTaking = false;
    public bool isFiring = false;
    public bool isThrowing = false;
    public bool isReloading = false;

    [SerializeField] private Animator handAnimator;
    //[SerializeField] private PlayerWeapon playerWeapon;
    //private Animator weaponAnimator;

    public PlayerState currentState = PlayerState.Idle;

    void Start()
    {
        //weaponAnimator = playerWeapon.currentWeaponModel.GetComponent<Animator>();    
    }

    void Update()
    {
        if(!isFiring && !isReloading && !isTaking && !isThrowing)
        {
            switch (currentState)
            {
                case PlayerState.Idle:
                break;
                case PlayerState.Running:
                break;
                case PlayerState.Slide:
                break;
                case PlayerState.Wallrun:
                break;
                case PlayerState.Jump:
                break;
                default:
                break;
            }  
        }
              
    }

    public void SetStateToIdle()
    {
        currentState = PlayerState.Idle;

        handAnimator.SetTrigger("Idle");
    }
    public void SetStateToRunning()
    {
        currentState = PlayerState.Running;
        handAnimator.SetTrigger("Running");

    }
    public void SetStateToSlide()
    {
        currentState = PlayerState.Slide;
        handAnimator.SetTrigger("Slide");

    }
    public void SetStateToWallrunning()
    {
        currentState = PlayerState.Wallrun;
    }
    public void SetStateToJumping()
    {
        currentState = PlayerState.Jump;
        handAnimator.SetTrigger("Jumping");

    }

    public void DidTake()
    {
        isTaking = true;
    }

    public void DidFire()
    {
        isFiring = true;
    }

    public void DidThrow()
    {
        isThrowing = true;
    }

    public void DidReload()
    {
        isReloading = true;
    }
}
