using Unity.Cinemachine;
using UnityEngine;

public class FPSCam : MonoBehaviour
{
    private float startPan;
    private float startTilt;
    private CinemachinePanTilt panTilt;
    void Start()
    {
        panTilt = GetComponent<CinemachinePanTilt>();

        startPan = panTilt.PanAxis.Value;
        startTilt = panTilt.TiltAxis.Value;
        
    }

    public void ResetCam()
    {
        panTilt.PanAxis.Value = startPan;
        panTilt.TiltAxis.Value = startTilt;
    }
}
