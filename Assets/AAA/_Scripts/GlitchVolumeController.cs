using System;
using UnityEngine;
using UnityEngine.Rendering;

public class GlitchVolumeController : MonoBehaviour
{
    // Curve maps "damage fraction" (0 = full health, 1 = dead) to volume weight.
    // Adjust in Inspector to control when the glitch kicks in (e.g. ease in below 30% health).
    [SerializeField] private AnimationCurve glitchCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Health playerHealth;
    private Volume glitchVolume;

    private void Start()
    {
        playerHealth = GetComponent<Health>();
        glitchVolume = GetComponent<Volume>();
        playerHealth.OnHealthUpdated += OnHealthUpdated;
        UpdateVolumeWeight();
    }

    private void OnDestroy()
    {
        playerHealth.OnHealthUpdated -= OnHealthUpdated;
    }

    private void OnHealthUpdated(object sender, EventArgs e)
    {
        UpdateVolumeWeight();
    }

    private void UpdateVolumeWeight()
    {
        float damageFraction = 1f - playerHealth.GetHealthPercentage();
        glitchVolume.weight = glitchCurve.Evaluate(damageFraction);
    }
}
