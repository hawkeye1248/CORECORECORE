using UnityEngine;
using MovementRework;

public class SpeedlinesController : MonoBehaviour
{
    [SerializeField] private ParticleSystem speedlines;
    [SerializeField] private float maxEmissionRate = 50f;

    // X axis: speed fraction (0 = still, 1 = max speed). Y axis: emission rate fraction (0-1).
    // Adjust to make speedlines kick in only above a certain speed threshold.
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private ParticleSystem.EmissionModule emission;

    private void Start()
    {
        emission = speedlines.emission;
    }

    private void Update()
    {
        float speedFraction = Player.Instance.GetHorizontalSpeedPercentage();
        emission.rateOverTime = maxEmissionRate * speedCurve.Evaluate(speedFraction);
    }
}
