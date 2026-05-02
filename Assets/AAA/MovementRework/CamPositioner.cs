using System.Collections;
using UnityEngine;
namespace MovementRework {
    public class CamPositioner : MonoBehaviour
    {
        [SerializeField] private Transform cam;

        [SerializeField] private float standingVerticalOffset = 0.5f;
        [SerializeField] private float crouchingVerticalOffset = 1.5f;
        [SerializeField] private float camMovementTime = 0.5f;
        private float camVerticalOffset = 0.5f;

        [Header("Jolt Effect")]
        [SerializeField] private float crashForceLimit = 1f;
        [SerializeField] private float joltAmplitute;
        [SerializeField] private float joltLength;
        [SerializeField] private AnimationCurve joltCurve;

        public void SimplePosition(Vector3 position)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(position.x, position.y - camVerticalOffset, position.z), Time.deltaTime * 10f);
        }

        public void MoveCamToCrouching()
        {
            StopCoroutine(MoveCam(standingVerticalOffset));
            StartCoroutine(MoveCam(crouchingVerticalOffset));
        }

        public void MoveCamToStanding()
        {
            StopCoroutine(MoveCam(crouchingVerticalOffset));
            StartCoroutine(MoveCam(standingVerticalOffset));
        }

        private IEnumerator MoveCam(float newPos)
        {
            for(float i = 0; i < camMovementTime; i += Time.deltaTime)
            {
                camVerticalOffset = Mathf.Lerp(camVerticalOffset, newPos, i);
                yield return null;
            }
        }

        public void Jolt(float verticalSpeed, float power)
        {
            if(-verticalSpeed >= crashForceLimit)
            {
                StartCoroutine(JoltCoroutine(power));
            }
        }

        private IEnumerator JoltCoroutine(float power)
        {
            for (float f = 0; f < joltLength / 2; f += Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute * power, joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            for (float f = joltLength / 2; f > 0; f -= Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute * power, joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            cam.localEulerAngles = new Vector3(0, cam.localEulerAngles.y, cam.localEulerAngles.z);
        }
    }
}
