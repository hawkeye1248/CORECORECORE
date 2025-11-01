using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace peterkcodes.AdvancedMovement.Demo
{
    /// <summary>
    /// Handles audio based on the actions of the controller
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("State-based Sound")]
        [SerializeField] [Tooltip("The audio source used by looping sound effects such as walking.")] private AudioSource loopingAudioSource;
        [SerializeField] [Tooltip("The audio source used by impulse sound effects such as jumping.")] private AudioSource impulseSoundSource;
        [SerializeField] [Tooltip("These rules define what clips should be played under what condition.")] private StateAudioRule[] rules;
        [SerializeField] [Tooltip("When in this state, looping audio will require the player to be moving. Useful for walk sound effects preventing them from looping while stationary.")] private MoveState[] mustMoveForLoop;
        [SerializeField] [Tooltip("For sound effects requiring movement, the velocity must meet this threshold.")] private float minMoveVelocity;

        [Header("Wind Sound")]
        [SerializeField] AudioSource windSource;
        [SerializeField] private float maximumWindVolume;
        [SerializeField] private float windSpeedThreshold;
        [SerializeField] AnimationCurve windCurve;

        private PlayerMovement _movement;
        private MoveState _previousState = MoveState.none;

        // Start is called before the first frame update
        void Start()
        {
            _movement = GetComponent<PlayerMovement>();
            _movement.onStateChanged += HandleStateChange;
        }

        private void Update()
        {
            //Manage any looping audio that is reliant upon movement
            if (mustMoveForLoop.Contains(_previousState))
            {
                Vector3 nonVerticalVelocity = Vector3.Scale(_movement.velocity, new Vector3(1, 0, 1));
                if (nonVerticalVelocity.magnitude < minMoveVelocity)
                {
                    loopingAudioSource.Pause();
                } else
                {
                    loopingAudioSource.UnPause();
                }
            }

            //Handle wind sound movement based on player velocity.
            windSource.volume = Mathf.Lerp(0, maximumWindVolume, windCurve.Evaluate((_movement.velocity.magnitude - windSpeedThreshold) / windSpeedThreshold));
        }

        /// <summary>
        /// When the controller updates its state, set the audio source to the appropriate looping clip.
        /// Also, handle any temporary sound effects such as becoming airborne or landing.
        /// </summary>
        void HandleStateChange(MoveState newState)
        {
            if (_previousState == newState)
            {
                return;
            }

            bool foundSuitableLoopingAudioTrack = false;
            foreach (StateAudioRule rule in rules)
            {
                //Validate against the rule's allowed states. If either of the allowed states arrays are empty, treat the condition as met.
                if ((rule.AllowedCurrentStates.Length == 0 || rule.AllowedCurrentStates.Contains(newState)) && (rule.AllowedPreviousStates.Length == 0 || rule.AllowedPreviousStates.Contains(_previousState)))
                {
                    if (rule.LoopingSoundEffect != null)
                    {
                        foundSuitableLoopingAudioTrack = true;
                        loopingAudioSource.clip = rule.LoopingSoundEffect;
                        loopingAudioSource.Play();
                    }

                    if (rule.ImpulseSoundEffect != null)
                    {
                        impulseSoundSource.clip = rule.ImpulseSoundEffect;
                        impulseSoundSource.Play();
                    }
                }
            }

            //Ensure that looping audio from previous states doesn't continue playing if no suitable looping track is found
            if (!foundSuitableLoopingAudioTrack)
            {
                loopingAudioSource.Pause();
            }

            _previousState = newState;
        }
    }

    /// <summary>
    /// Defines how a new state should effect audio.
    /// </summary>
    [System.Serializable]
    public struct StateAudioRule
    {
        public AudioClip LoopingSoundEffect;
        public AudioClip ImpulseSoundEffect;
        [Tooltip("The rule will be applied if the new state matches any of the allowed current states.")] public MoveState[] AllowedCurrentStates;
        [Tooltip("The rule will be applied if the previous state matches any of the allowed previous states. Leave blank to apply to any previous state")] public MoveState[] AllowedPreviousStates;
    }
}