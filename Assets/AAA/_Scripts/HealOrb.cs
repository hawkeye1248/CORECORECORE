using UnityEngine;

public class HealOrb : MonoBehaviour
{
    [SerializeField] private float healAmount = 50f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private float sidewaysDrift = 1.5f;

    private Health targetHealth;
    private Transform target;
    private Vector3 startPos;
    private Vector3 driftDir;
    private float driftAmount;
    private float elapsed;

    public void Init(Health playerHealth, Transform followTarget)
    {
        targetHealth = playerHealth;
        target = followTarget;
        startPos = transform.position;
        driftDir = Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up).normalized;
        driftAmount = Random.Range(0f, sidewaysDrift);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        Vector3 arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * arcHeight;
        Vector3 drift = driftDir * Mathf.Sin(t * Mathf.PI) * driftAmount;
        transform.position = Vector3.Lerp(startPos, target.position, t) + arc + drift;

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            HealAndDestroy();
        }
    }

    void HealAndDestroy()
    {
        if (targetHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        float amount = healAmount;
        HealthData data = targetHealth.GetHealthData();
        if (data is PlayerHealthData pd)
            amount = pd.healOnKill;
        targetHealth.HealHealth(amount);
        Destroy(gameObject);
    }
}
