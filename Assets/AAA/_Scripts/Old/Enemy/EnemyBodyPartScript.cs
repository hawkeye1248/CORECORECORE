using UnityEngine;

public class EnemyBodyPartScript : MonoBehaviour
{
    public Rigidbody rb;
    public NewEnemyTest enemy;
    private Health health;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enemy = GetComponentInParent<NewEnemyTest>();
        health = GetComponentInParent<Health>();
    }

    public void ApplyDamage(float damage, Vector3 hitDirection, float knockbackForce)
    {
        health.DamageHealth(damage);
        rb.AddForce(hitDirection.normalized * 25f * knockbackForce, ForceMode.Impulse);
    }
}
