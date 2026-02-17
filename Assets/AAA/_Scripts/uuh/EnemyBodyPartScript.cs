using System.Collections;
using UnityEngine;

public class EnemyBodyPartScript : MonoBehaviour
{
    public Rigidbody rb;
    public NewEnemyTest enemy;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        enemy = GetComponentInParent<NewEnemyTest>();  
    }

    void Start()
    {
          
    }

    public void Die(Vector3 pos)
    {
        enemy.Ragdoll();
        rb.AddForce(pos.normalized * 25f, ForceMode.Impulse);
        
    }
}
