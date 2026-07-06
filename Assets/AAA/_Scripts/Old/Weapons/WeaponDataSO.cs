using AAA._Scripts.Enums;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    public Weapon weaponType = Weapon.None;
    [Header("Combat")]
    public float damage = 25f;
    public float fireInterval = 0.3f;
    public int maxAmmo = 3;

    [Header("Knockback")]
    public float minKnockbackForce = 5f;
    public float maxKnockbackForce = 20f;

    [Header("Melee")]
    public MeleeShapeData meleeShape;
}
