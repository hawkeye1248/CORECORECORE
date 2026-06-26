using System.Collections;
using UnityEngine;
namespace MovementRework {
    public class CamPositioner : MonoBehaviour
    {
        [SerializeField] private CameraData cameraData;
        [SerializeField] private Transform cam;

        private float camVerticalOffset = 0.5f;
        private Coroutine moveRoutine;

        public void SimplePosition(Vector3 position)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(position.x, position.y - camVerticalOffset, position.z), Time.deltaTime * 10f);
        }

        // Instantly snaps to the target instead of lerping, so teleports aren't shown as a smooth slide.
        public void SnapPosition(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y - camVerticalOffset, position.z);
        }

        public void MoveCamToCrouching()
        {
            if(moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveCam(cameraData.crouchingVerticalOffset));
        }

        public void MoveCamToStanding()
        {
            if(moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveCam(cameraData.standingVerticalOffset));
        }

        private IEnumerator MoveCam(float newPos)
        {
            for(float i = 0; i < cameraData.camMovementTime; i += Time.deltaTime)
            {
                camVerticalOffset = Mathf.Lerp(camVerticalOffset, newPos, i);
                yield return null;
            }
        }

        public void Jolt(float verticalSpeed, float power)
        {
            if(-verticalSpeed >= cameraData.crashForceLimit)
            {
                StartCoroutine(JoltCoroutine(power));
            }
        }

        private IEnumerator JoltCoroutine(float power)
        {
            float joltLength = cameraData.joltLength;
            float joltAmplitute = cameraData.joltAmplitute;

            for (float f = 0; f < joltLength / 2; f += Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute * power, cameraData.joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            for (float f = joltLength / 2; f > 0; f -= Time.deltaTime)
            {
                cam.localEulerAngles = new Vector3(Mathf.Lerp(0, joltAmplitute * power, cameraData.joltCurve.Evaluate(f / joltLength / 2)), cam.localEulerAngles.y, cam.localEulerAngles.z);
                yield return null;
            }

            cam.localEulerAngles = new Vector3(0, cam.localEulerAngles.y, cam.localEulerAngles.z);
        }
    }
}
