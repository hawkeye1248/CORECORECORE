using System;
using System.ComponentModel;
using AAA._Scripts.Enums;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AAA._Scripts.AnimationRelated.MCAnimations
{
    public class PlayerAnimations : MonoBehaviour
    {
        private Animator _animator;

        #region Animations

        private static readonly int pipeHit = Animator.StringToHash("PipeHit");
        private static readonly int hookPunchL = Animator.StringToHash("HookPunchL");
        private static readonly int pipeIdle = Animator.StringToHash("PipeIdle");
        private static readonly int hookPunchR = Animator.StringToHash("HookPunchR");
        private static readonly int pipeThrow = Animator.StringToHash("PipeThrow");
        private static readonly int pistolIdle =  Animator.StringToHash("PistolIdle");
        private static readonly int punchR = Animator.StringToHash("PunchR");
        private static readonly int running =  Animator.StringToHash("Running");
        private static readonly int punchL = Animator.StringToHash("PunchL");
        private static readonly int pistolThrow = Animator.StringToHash("PistolThrow");
        private static readonly int pistolShoot = Animator.StringToHash("PistolShoot");
        private static readonly int idle =  Animator.StringToHash("Idle");
        private static readonly int setPistol = Animator.StringToHash("SetPistol");

        #endregion

        private Weapon _currentPlayerWeapon;
        public Weapon CurrentPlayerWeapon
        {
            get => _currentPlayerWeapon;
            set
            {
                if (!Enum.IsDefined(typeof(Weapon), value))
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(Weapon));
                _currentPlayerWeapon = value;
                switch (_currentPlayerWeapon)
                {
                    case Weapon.None:
                        _animator.CrossFade(idle, .1f);
                        break;
                    case Weapon.Pipe:
                        _animator.CrossFade(pipeIdle, .1f);
                        break;
                    case Weapon.Pistol:
                        //_animator.Play(hookPunchL);
                        _animator.SetTrigger(setPistol);
                        Debug.Log("PistolPickup");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(CurrentPlayerWeapon));
                }
            } 
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }
        
        

        public void Lmb()
        {
            switch (CurrentPlayerWeapon)
            {
                case Weapon.None:
                    _animator.CrossFade(Random.Range(0, 2) == 0 ? punchL : hookPunchL, .1f);
                    break;
                case Weapon.Pipe:
                    _animator.CrossFade(pipeHit, .1f);
                    break;
                case Weapon.Pistol:
                    _animator.CrossFade(pistolShoot, .1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(CurrentPlayerWeapon));
            }
        }

        public void Rmb()
        {
            switch (CurrentPlayerWeapon)
            {
                case Weapon.None:
                    _animator.CrossFade(Random.Range(0, 2) == 0 ? punchR : hookPunchR, .1f);
                    break;
                case Weapon.Pipe:
                    _animator.CrossFade(pipeThrow, .1f);
                    break;
                case Weapon.Pistol:
                    _animator.CrossFade(pistolThrow, .1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(CurrentPlayerWeapon));
            }
        }

        
    }
}
