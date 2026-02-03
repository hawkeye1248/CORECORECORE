using UnityEngine;

public class EnemyBodyPartScript : MonoBehaviour
{
    public Rigidbody rb;
    public NewEnemyTest enemy;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemy = GetComponentInParent<NewEnemyTest>();    
    }

    public void Die()
    {
        rb.AddExplosionForce(15, transform.position, 5);
        enemy.Ragdoll();
    }
}
