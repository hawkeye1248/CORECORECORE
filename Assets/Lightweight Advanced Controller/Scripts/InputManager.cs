using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace peterkcodes.AdvancedMovement.Demo
{
    /// <summary>
    /// Basic placeholder script for driving movement to the player movement script. You should replace this with your own input handler using Unity's new input system.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class InputManager : MonoBehaviour
    {
        private PlayerMovement movement;

        private void Start()
        {
            movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                movement.FlagJumpInput();
            }

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                movement.FlagSlideInput();
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                movement.FlagSprintInput();
            }

            movement.SetInputAxes(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));
        }
    }
}