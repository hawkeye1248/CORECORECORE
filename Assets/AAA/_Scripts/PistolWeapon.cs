using UnityEngine;

public class PistolWeapon : WeaponScript
{
    [Header("Prefabs")]
    [SerializeField] public GameObject bulletPrefab;

    public override void Shoot(Vector3 pos, Quaternion rot, bool isEnemy)
    {
        if (isFireInterval)
        {
            return;
        }
        if (currentAmmo <= 0)
        {
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, pos, rot);
        if (bullet.TryGetComponent<BulletMovement>(out var bm))
        {
            bm.damage = weaponData.damage;
        }
        currentAmmo--;

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }

        if (isEquippedByPlayer)
        {
            StartCoroutine(FireInterval());
        }
    }
}
