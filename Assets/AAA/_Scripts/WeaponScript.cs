using System.Collections;
using UnityEngine;
using DG.Tweening;
using MovementRework;

public class WeaponScript : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] protected WeaponDataSO weaponData;

    [Header("Components")]
    private Rigidbody rb;
    private Collider weaponCollider;
    protected Transform mainCam;
    public Outline outline;

    [Header("State")]
    public bool isEquippedByPlayer = false;
    [SerializeField] public bool isFireInterval = false;
    protected int currentAmmo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        weaponCollider = GetComponent<Collider>();
        outline = GetComponent<Outline>();
        currentAmmo = weaponData != null ? weaponData.maxAmmo : 3;
        ChangeSettings();
    }

    private void Start()
    {
        mainCam = MovementRework.Player.Instance.cameraController.transform;
    }

    private void ChangeSettings()
    {
        if (transform.parent != null)
        {
            return;
        }

        rb.isKinematic = isEquippedByPlayer ? true : false;
        rb.interpolation = isEquippedByPlayer ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        weaponCollider.isTrigger = isEquippedByPlayer;
    }

    public virtual void Shoot(Vector3 pos, Quaternion rot, bool isEnemy)
    {

    }

    public void Throw(Vector3 hitpoint)
    {
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOMove(transform.position + transform.forward, .01f)).SetUpdate(true);
        s.AppendCallback(() => transform.parent = null);
        s.AppendCallback(() => ChangeSettings());
        s.AppendCallback(() => rb.AddForce((hitpoint - transform.position).normalized * 25, ForceMode.Impulse));
        s.AppendCallback(() => rb.AddForce(Vector3.up * 2, ForceMode.Impulse));
        s.AppendCallback(() => rb.AddTorque(transform.transform.up * 20, ForceMode.Impulse));
    }

    public void Pickup(Transform weaponHolder)
    {
        isEquippedByPlayer = true;
        ChangeSettings();

        transform.parent = weaponHolder;

        transform.DOLocalMove(Vector3.zero, .25f).SetEase(Ease.OutBack).SetUpdate(true);
        transform.DOLocalRotate(Vector3.zero, .25f).SetUpdate(true);
    }

    public void Release()
    {
        transform.parent = null;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        weaponCollider.isTrigger = false;

        rb.AddForce((Camera.main.transform.position - transform.position) * 2, ForceMode.Impulse);
        rb.AddForce(Vector3.up * 2, ForceMode.Impulse);
    }

    public IEnumerator FireInterval()
    {
        isFireInterval = true;
        yield return new WaitForSeconds(weaponData.fireInterval);
        isFireInterval = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBodyPartScript bp = collision.gameObject.GetComponent<EnemyBodyPartScript>();
            bp.ApplyDamage(weaponData.damage, rb.linearVelocity, Mathf.Lerp(weaponData.minKnockbackForce, weaponData.maxKnockbackForce, rb.linearVelocity.magnitude / 40));

            rb.AddForce((mainCam.position - transform.position).normalized, ForceMode.Impulse);
            rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }
}
