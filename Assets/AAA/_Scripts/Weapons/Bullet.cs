using UnityEngine;

public class Bullet : MonoBehaviour
{
    public enum BulletSource { Player, Enemy }

    [Header("Properties")]
    public BulletSource source = BulletSource.Player;
    public float speed;
    public float damage;
    public float knockbackForce;

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (source == BulletSource.Player && collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBodyPartScript bp = collision.gameObject.GetComponent<EnemyBodyPartScript>();
            if (bp != null)
                bp.ApplyDamage(damage, transform.forward, knockbackForce);
        }
        else if (source == BulletSource.Enemy && collision.gameObject.CompareTag("Player"))
        {
            Health h = collision.gameObject.GetComponentInParent<Health>();
            if (h != null)
                h.DamageHealth(damage);
        }
        Destroy(gameObject);
    }
}
