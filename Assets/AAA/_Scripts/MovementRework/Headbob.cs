using UnityEngine;

namespace MovementRework
{
    /// <summary>
    /// Velocity-driven head bob. Lives on a dedicated transform between the
    /// CameraController (which owns look rotation) and the Camera itself.
    /// This script is the ONLY thing that writes to this transform's
    /// localPosition / localRotation, so it composes additively with the
    /// rotation and positioning handled by parent transforms.
    /// </summary>
    public class Headbob : MonoBehaviour
    {
        [SerializeField] private CameraData cameraData;

        private Player player;

        // Self-accumulated phase so frequency tracks real movement and never
        // jitters when speed changes (the failure mode of driving off Time.time).
        private float phase;

        // Smoothed 0..1 strength so the bob fades instead of popping on/off.
        private float currentStrength;

        private void Awake()
        {
            player = GetComponentInParent<Player>();
        }

        private void Update()
        {
            float speedPercentage = player.GetHorizontalSpeedPercentage();
            bool canBob = (player.IsGrounded || player.IsWallrunning)
                          && !player.IsMantling
                          && !player.IsCrouching
                          && speedPercentage > cameraData.bobMinSpeedThreshold;

            // Fade strength toward 1 while moving on the ground, toward 0 otherwise.
            float targetStrength = canBob ? Mathf.Clamp01(speedPercentage) : 0f;
            currentStrength = Mathf.Lerp(currentStrength, targetStrength, cameraData.bobAmplitudeSmoothing * Time.deltaTime);

            // Advance phase proportional to speed so steps land faster when sprinting.
            // Only advance meaningfully while actually bobbing.
            phase += Time.deltaTime * cameraData.bobFrequency * Mathf.Max(currentStrength, 0.0001f);

            // Figure-8 footstep motion: vertical at 2x the horizontal frequency.
            Vector3 targetPos = Vector3.zero;
            targetPos.y = Mathf.Sin(phase * 2f) * cameraData.bobVerticalAmplitude * currentStrength;
            targetPos.x = Mathf.Cos(phase) * cameraData.bobHorizontalAmplitude * currentStrength;

            // Roll into the sway for weight; tied to the horizontal phase.
            Vector3 targetEuler = Vector3.zero;
            targetEuler.z = -Mathf.Cos(phase) * cameraData.bobRollAmplitude * currentStrength;

            // Ease toward the target pose. When strength is ~0 the targets are
            // ~zero, so this naturally settles back to identity with no snap.
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, cameraData.bobPoseSmoothing * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(targetEuler), cameraData.bobPoseSmoothing * Time.deltaTime);
        }
    }
}
