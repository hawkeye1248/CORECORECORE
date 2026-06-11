using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [Header("Properties")]
    public float speed;
    public float knockbackForce;
    public float damage = 25f;
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
            bp.ApplyDamage(damage, transform.forward, knockbackForce);
        }
        Destroy(gameObject);
    }
}
