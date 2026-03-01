using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed;
    public float knockbackForce;
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
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<Health>().DamageHealth(50);
        }
        Destroy(gameObject);
    }
}
