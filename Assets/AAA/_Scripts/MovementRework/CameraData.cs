using UnityEngine;

namespace MovementRework
{
    [CreateAssetMenu(fileName = "CameraData", menuName = "MovementRework/CameraData")]
    public class CameraData : ScriptableObject
    {
        [Header("Look Rotation")]
        public float upperVerticalLimit = 85f;
        public float lowerVerticalLimit = 85f;
        [Tooltip("How quickly raw look input is smoothed. Higher = snappier.")]
        public float camSmoothingFactor = 15f;
        [Tooltip("Look sensitivity / rotation speed.")]
        public float camSpeed = 200f;

        [Header("Camera Tilt (Z Roll)")]
        [Tooltip("Roll applied while strafing on the ground.")]
        public float cameraTiltMultiplier = 5f;
        [Tooltip("Roll applied while turning the camera left/right. Negate to flip lean direction.")]
        public float lookTiltMultiplier = 0.5f;
        [Tooltip("Maximum roll (degrees) the look-based tilt can contribute, so fast flicks don't over-roll.")]
        public float maxLookTilt = 5f;
        [Tooltip("Roll applied while wallrunning.")]
        public float wallrunTiltMultiplier = 15f;
        [Tooltip("Lerp factor used to smooth the tilt toward its target.")]
        public float cameraTiltSmoothTime = 0.1f;

        [Header("Field of View")]
        public float minFov = 60f;
        public float maxFov = 90f;
        [Tooltip("How quickly the FOV eases toward its speed-based target.")]
        public float fovChangeMultiplier = 5f;

        [Header("Camera Position")]
        public float standingVerticalOffset = 0.5f;
        public float crouchingVerticalOffset = 1.5f;
        [Tooltip("Time taken to move between standing and crouching height.")]
        public float camMovementTime = 0.5f;

        [Header("Landing Jolt")]
        [Tooltip("Minimum downward speed required to trigger a landing jolt.")]
        public float crashForceLimit = 1f;
        public float joltAmplitute = 5f;
        public float joltLength = 0.3f;
        public AnimationCurve joltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Head Bob - Shape")]
        [Tooltip("How fast the bob cycles. Phase also scales with movement speed.")]
        public float bobFrequency = 10f;
        [Tooltip("Vertical (bounce) offset at full speed, in metres.")]
        public float bobVerticalAmplitude = 0.05f;
        [Tooltip("Horizontal (sway) offset at full speed, in metres.")]
        public float bobHorizontalAmplitude = 0.05f;
        [Tooltip("Camera roll (degrees) at full speed, sells the weight of each step.")]
        public float bobRollAmplitude = 0.6f;

        [Header("Head Bob - Response")]
        [Tooltip("Below this speed percentage the bob fades out completely.")]
        [Range(0f, 1f)] public float bobMinSpeedThreshold = 0.05f;
        [Tooltip("How quickly the bob fades in/out when starting/stopping (higher = snappier).")]
        public float bobAmplitudeSmoothing = 8f;
        [Tooltip("How quickly the transform eases toward the target bob pose.")]
        public float bobPoseSmoothing = 14f;
    }
}
