using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    public float speed;
    public float knockbackForce = 20f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();       
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBodyPartScript bp = collision.gameObject.GetComponent<EnemyBodyPartScript>();

            //if (!bp.enemy.dead)
                //Instantiate(SuperHotScript.instance.hitParticlePrefab, transform.position, transform.rotation);

            bp.Die(transform.forward, knockbackForce);
        }
        Destroy(gameObject);
    }
}
