using UnityEngine;

namespace MovementRework
{
    [CreateAssetMenu(fileName = "PlayerMovementData", menuName = "MovementRework/PlayerMovementData")]
    public class PlayerMovementData : ScriptableObject
    {
        [Header("Walking Parameters")]
        public float acceleration = 2500f;
        public float maxSpeed = 25f;
        /// <summary>
        /// How quickly the player decelerates when no input is given. Higher values will result in a more responsive stop, while lower values will create a more gradual slowdown.
        /// Values bigger than <value>maxSpeed</value> will result in an immediate stop.
        /// As declared in <see cref="Player"/>
        /// <code>core.AddForce(-core.linearVelocity * movementData.stoppingPower);</code>
        /// </summary>
        [Tooltip("Shouldn't be above maxSpeed attribute. See documentation.")] 
        public float stoppingPower = 5f;
        public float sidewayDamping = 0.999f;
        public float backwardStoppingPower = 45f;

        [Header("Ground Check Parameters")]
        public Vector3 groundCheckScale = new Vector3(0.4f, 0.3f, 0.4f);
        public LayerMask groundLayers;
        public float coyoteTime = 0.25f;

        [Header("Jumping Parameters")]
        public float jumpForce = 10f;
        public float fallGravity = 50f;
        public float landingJoltPower = 2f;

        [Header("Airborne Movement Parameters")]
        public float airborneAcceleration = 1500f;
        public float airborneMaxSpeed = 35f;
        public float airborneStoppingPower = 3f;
        public float airborneSidewayDamping = 0.7f;
        public float airborneBackwardStoppingPower = 10f;

        [Header("Slide Parameters")]
        public float slideForce = 5f;
        public float slideStoppingPower = 2f;
        public float slideEndSpeed = 1f;

        [Header("Mantle Parameters")]
        public Vector3 mantleRaycastPoint = new Vector3(0, 1.3f, 0);
        public float mantleDistance = 0.7f;
        public float mantleLength = 1f;
        public float mantleJoltPower = 5f;
        public float mantleJumpForce = 5f;
        public float mantleCooldown = 0.25f;

        [Header("Wallrunning Parameters")]
        public float wallrunAcceleration = 1500f;
        public float wallrunMaxSpeed = 25f;
        public float wallCheckDistance = 1f;
        public float wallrunCooldown = 0.25f;
        public float wallJumpForce = 10f;
        public float wallJumpNormalForce = 8f;
        public float wallrunUpwardForce = 25f;
        public float wallrunDecayTime = 2f;
        public float wallrunBurstForce = 10f;
        
        [Header("Lunge Parameters")]
        public float lungeForce = 10f;
    }
}
