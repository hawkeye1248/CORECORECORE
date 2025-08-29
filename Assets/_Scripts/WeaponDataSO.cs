using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDataSO", menuName = "WeaponDataSO", order = 0)]
public class WeaponDataSO : ScriptableObject
{
    [Header("Stats")]
    public float damage;
    public bool isAutomatic;
    public int magazineSize;
    public float fireRate;
    public int burstAmount;
    public float burstRate;
    public float reloadTime;
    public Vector2 xSpread;
    public Vector2 ySpread;
    public float range;
    [Header("Visuals")]
    public GameObject gunModel;
    public GameObject wallBulletHole;
}
