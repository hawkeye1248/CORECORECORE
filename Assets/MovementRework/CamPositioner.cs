using System.Collections;
using UnityEngine;
namespace MovementRework {
    public class CamPositioner : MonoBehaviour
    {
        [SerializeField] private Transform cam;

        [Header("Jolt Effect")]
        [SerializeField] private float crashForceLimit = 1f;
        [SerializeField] private float joltAmplitute;
        [SerializeField] private float joltLength;
        [SerializeField] private AnimationCurve joltCurve;

        public void SimplePosition(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y - 0.5f, position.z);
        }

        public void Jolt(float verticalSpeed)
        {
            if(-verticalSpeed >= crashForceLimit)
            {
                StartCoroutine(JoltCoroutine());
            }
        }

        private IEnumerator JoltCoroutine()
        {
            for (float f = 0; f < joltLength / 2; f += Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute, joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            for (float f = joltLength / 2; f > 0; f -= Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute, joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            cam.localEulerAngles = new Vector3(0, cam.localEulerAngles.y, cam.localEulerAngles.z);
        }
    }
}
