using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace peterkcodes.AdvancedMovement.Demo
{
    /// <summary>
    /// Responsible for the effects on the camera. Creates jolts, bobs the view, tilts the camera, etc.
    /// </summary>
    
    //I recommend replacing the camera scripts with your own.
    public class CameraEffects : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] new private Camera camera;
        private Vector3 _cameraPosition;
        private Vector3 cameraDefaultPosition;

        [Header("Speed Lines")]
        [SerializeField] private ParticleSystem speedLines;
        [SerializeField] private Color hiddenColor;
        [SerializeField] private Color shownColor;
        [SerializeField] private float speedThreshold;
        [SerializeField] private float maximumSpeed;

        [Header("View Bobbing")]
        [SerializeField] private bool enableViewBobbing;
        [SerializeField] private float bobSpeed;
        [SerializeField] private float bobAmplitude;
        [SerializeField] private float bobVelocityAmplitudeModifier;
        [SerializeField] private float bobVelocitySpeedModifier;
        [SerializeField] private AnimationCurve bobCurve;
        float bobTime;
        private Vector3 upPoint;
        private Vector3 startPoint;
        bool up;
        private float velocity;

        [Header("Sliding")]
        private bool isSliding;

        [Header("Tilt")]
        [SerializeField] private bool allowTilt;
        [SerializeField] private float tiltSpeed;
        [SerializeField] private AnimationCurve tiltCurve;
        private float tilt;
        private float startingTilt;
        private float timeSinceLastTilt;

        [Header("Jolt")]
        [SerializeField] AnimationCurve joltCurve;

        private float defaultFov;

        void Start()
        {
            tilt = camera.transform.eulerAngles.z;
            defaultFov = camera.fieldOfView;
            startPoint = camera.transform.localPosition;
            cameraDefaultPosition = camera.transform.localPosition;
            upPoint = CalculateCameraUpPoint();
            movement.onStateChanged += CheckSlide;
            movement.fxSetTilt += SetTilt;
            movement.fxResetFOV += ResetFov;
            movement.fxAdjustFOV += MultiplyFov;
            movement.fxJolt += Jolt;
        }

        private void LateUpdate()
        {
            SetVelocity();

            if (enableViewBobbing)
            {
                ViewBob();
            }

            if (allowTilt)
            {
                Tilt();
            }

            var main = speedLines.main;
            main.startColor = Color.Lerp(hiddenColor, shownColor, (velocity - speedThreshold) / maximumSpeed);

            camera.transform.localPosition = _cameraPosition;
            _cameraPosition = cameraDefaultPosition;
        }

        private void Tilt()
        {
            timeSinceLastTilt += Time.deltaTime * tiltSpeed;
            float zRot = Mathf.Lerp(startingTilt, tilt, tiltCurve.Evaluate(timeSinceLastTilt));
            camera.transform.localRotation = Quaternion.Euler(camera.transform.localEulerAngles.x, camera.transform.localEulerAngles.y, zRot);
        }

        private void ViewBob()
        {
            if (isSliding)
            {
                return;
            }

            _cameraPosition += Vector3.Lerp(startPoint, upPoint, bobCurve.Evaluate(bobTime));

            if (velocity > 0)
            {
                if (up)
                {
                    bobTime += Time.deltaTime * (bobSpeed + (velocity * bobVelocitySpeedModifier));
                    if (bobTime > 1)
                    {
                        up = false;
                    }
                }
                else
                {
                    bobTime -= Time.deltaTime * (bobSpeed + (velocity * bobVelocitySpeedModifier));
                    if (bobTime < 0)
                    {
                        up = true;
                    }
                }
            }
            else
            {
                bobTime = 0;
            }
        }

        public void SetFov(float _target, float _time)
        {
            StartCoroutine(SetFovCoroutine(_target, _time));
        }

        public void MultiplyFov(float _multiplier, float _time)
        {
            StartCoroutine(SetFovCoroutine(defaultFov * _multiplier, _time));
        }

        public void ResetFov(float _time)
        {
            SetFov(defaultFov, _time);
        }

        public IEnumerator SetFovCoroutine(float _target, float _finalTime)
        {
            float _startingFov = camera.fieldOfView;
            for (float _time = 0; _time < _finalTime; _time += Time.deltaTime)
            {
                camera.fieldOfView = Mathf.Lerp(_startingFov, _target, _time / _finalTime);
                yield return null;
            }
        }

        public void SetVelocity()
        {
            velocity = Vector3.Scale(movement.velocity, new Vector3(1,0,1)).magnitude;
            upPoint = CalculateCameraUpPoint();
        }

        public void SetTilt(float _tilt)
        {
            if (_tilt == tilt)
            {
                return;
            }

            timeSinceLastTilt = 0;
            startingTilt = tilt;
            tilt = _tilt;
        }

        public void CheckSlide(MoveState state)
        {
            isSliding = (state == MoveState.slide);
        }

        public void Jolt(float _amplitude, float _length)
        {
            StartCoroutine(StartJolt(_amplitude, _length));
        }

        private IEnumerator StartJolt(float _amplitude, float _length)
        {
            allowTilt = false;
            for (float f = 0; f < _length / 2; f += Time.deltaTime)
            {
                camera.transform.localEulerAngles = new Vector3(Mathf.Lerp(0, _amplitude, joltCurve.Evaluate(f / _length / 2)), camera.transform.localEulerAngles.y, camera.transform.localEulerAngles.z);
                yield return null;
            }

            for (float f = _length / 2; f > 0; f -= Time.deltaTime)
            {
                camera.transform.localEulerAngles = new Vector3(Mathf.Lerp(0, _amplitude, joltCurve.Evaluate(f / _length / 2)), camera.transform.localEulerAngles.y, camera.transform.localEulerAngles.z);
                yield return null;
            }

            camera.transform.localEulerAngles = new Vector3(0, camera.transform.localEulerAngles.y, camera.transform.localEulerAngles.z);
            allowTilt = true;
        }

        Vector3 CalculateCameraUpPoint()
        {
            return startPoint + Vector3.up * bobAmplitude * velocity * bobVelocityAmplitudeModifier;
        }
    }
}