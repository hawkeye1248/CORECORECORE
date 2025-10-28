using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace peterkcodes.AdvancedMovement.Demo
{
    /// <summary>
    /// A basic UI manager script which in this case is only used to update the stamina bar.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private Transform staminaBar;
        [SerializeField] private PlayerMovement movement;

        private void Start()
        {
            movement.uiStaminaUpdate += UpdateStaminaBar;   
        }

        public void UpdateStaminaBar(float _stamina)
        {
            staminaBar.localScale = new Vector3(_stamina, 1, 1);
        }
    }
}