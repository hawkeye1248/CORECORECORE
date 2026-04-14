using UnityEngine;

namespace MovementRework {
    public class PlayerModel : MonoBehaviour
    {
        private Player playerScript;
        private Animator animator;

        [SerializeField] private float turningSpeed = 20f;

        [SerializeField] private Transform handModel;

        private void Awake() {
            playerScript = GetComponentInParent<Player>();
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            CheckStatus();
        }

        public void SimplePosition(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y - 0.5f, position.z);
            handModel.forward = Vector3.Lerp(handModel.forward, playerScript.cameraController.transform.forward, Time.deltaTime * turningSpeed);
        }

        private void CheckStatus()
        {
            if(playerScript.IsMantling)
            {
                animator.SetBool("IsMantling", true);
                return;
            } else
            {
                animator.SetBool("IsMantling", false);   
            }

            if(playerScript.IsWallrunning)
            {
                animator.SetBool("IsWallrunning", true);
                return;
            } else
            {
                animator.SetBool("IsWallrunning", false);   
            }

            if(playerScript.IsJumped)
            {
                if(animator.GetCurrentAnimatorStateInfo(layerIndex:0).IsName("Jumping"))
                {
                    animator.SetBool("Jumped", false);
                } else if(animator.GetBool("Jumped"))
                {
                    animator.SetBool("Jumped", false);
                } else
                {
                    animator.SetBool("Jumped", true);
                    return;
                }
            } else
            {
                animator.SetBool("Jumped", false);
            }

            if(playerScript.IsCrouching && playerScript.core.linearVelocity.magnitude >= 1f)
            {
                animator.SetBool("IsSliding", true);
                return;
            } else
            {
                animator.SetBool("IsSliding", false);
            }

            if(playerScript.IsGrounded && playerScript.core.linearVelocity.magnitude >= 1f)
            {
                animator.SetBool("IsRunning", true);
                return;
            } else
            {
                animator.SetBool("IsRunning", false);
            }

            if(!playerScript.IsGrounded)
            {
                animator.SetBool("IsAirborne", true);
                return;
            } else
            {
                animator.SetBool("IsAirborne", false);
            }
        }

        public void PunchRightTrigger()
        {
            animator.SetTrigger("PunchRight");
        }

        public void PunchLeftTrigger()
        {
            animator.SetTrigger("PunchLeft");
        }
    }
}
