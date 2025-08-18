using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDataSO", menuName = "WeaponDataSO", order = 0)]
public class WeaponDataSO : ScriptableObject
{
    [SerializeField] private float damage;
    [SerializeField] public int magazineSize;
    [SerializeField] public float reloadTime;
    [SerializeField] public float fireRate;
    [SerializeField] public int burstAmount;
    [SerializeField] public float burstRate;
    [SerializeField] public bool isAutomatic;
    [SerializeField] public Vector2 xSpread;
    [SerializeField] public Vector2 ySpread;
    [SerializeField] public float range;
    public GameObject wallBulletHole;
}
