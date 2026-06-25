using UnityEngine;

namespace MovementRework
{
    public class FootstepController : MonoBehaviour
    {
        [Header("Audio Source (on core object)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip[] landClips;
        [SerializeField] private AudioClip wallrunStartClip;

        [Header("Timing")]
        [SerializeField] private float stepIntervalSlow = 0.55f;
        [SerializeField] private float stepIntervalFast = 0.28f;

        [Header("Volume")]
        [SerializeField] private float footstepVolume = 0.6f;
        [SerializeField] private float landVolume = 0.85f;
        [SerializeField] private float wallrunVolume = 0.5f;

        private float stepTimer;
        private int lastFootstepIndex = -1;
        private bool wasGrounded;
        private bool wasWallrunning;
        private Player player;

        private void Start()
        {
            player = Player.Instance;
            wasGrounded = player.IsGrounded;
            wasWallrunning = player.IsWallrunning;
        }

        private void Update()
        {
            HandleLanding();
            HandleWallrunStart();
            HandleFootsteps();

            wasGrounded = player.IsGrounded;
            wasWallrunning = player.IsWallrunning;
        }

        private void HandleFootsteps()
        {
            if (player.IsCrouching || player.IsMantling)
                return;

            if (!player.IsGrounded && !player.IsWallrunning)
                return;

            float speedPct = player.GetHorizontalSpeedPercentage();
            if (speedPct < 0.1f)
                return;

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
                return;

            PlayRandom(footstepClips, footstepVolume);
            stepTimer = Mathf.Lerp(stepIntervalSlow, stepIntervalFast, speedPct);
        }

        private void HandleLanding()
        {
            if (!wasGrounded && player.IsGrounded)
            {
                PlayRandom(landClips, landVolume);
                stepTimer = stepIntervalSlow * 0.5f;
            }
        }

        private void HandleWallrunStart()
        {
            if (!wasWallrunning && player.IsWallrunning && wallrunStartClip != null)
                audioSource.PlayOneShot(wallrunStartClip, wallrunVolume);
        }

        private void PlayRandom(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0)
                return;

            int index = clips.Length == 1 ? 0 : RandomExcluding(clips.Length, lastFootstepIndex);
            lastFootstepIndex = index;
            audioSource.PlayOneShot(clips[index], volume);
        }

        private int RandomExcluding(int max, int exclude)
        {
            int result = Random.Range(0, max - 1);
            if (result >= exclude) result++;
            return result;
        }
    }
}
