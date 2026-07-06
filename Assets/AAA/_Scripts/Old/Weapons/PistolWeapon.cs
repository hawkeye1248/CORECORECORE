using System;
using AAA._Scripts.Enums;
using Unity.VisualScripting;
using UnityEngine;

public class PistolWeapon : WeaponScript
{
    [Header("Prefabs")]
    [SerializeField] public GameObject bulletPrefab;
    
    private void Awake()
    {
        WeaponData.weaponType = Weapon.Pistol;
        rb = GetComponent<Rigidbody>();
        weaponCollider = GetComponent<Collider>();
    }

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
        if (bullet.TryGetComponent<Bullet>(out var bm))
        {
            bm.damage = WeaponData.damage;
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
