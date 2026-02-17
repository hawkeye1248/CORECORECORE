using System.Collections;
using UnityEngine;
using DG.Tweening;
using peterkcodes.AdvancedMovement;

public class WeaponScript : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody rb;
    private Collider weaponCollider;
    private Transform mainCam;
    public Outline outline;

    [Header("Weapon Properties")]
    public float fireInterval = 0.3f;
    public int bulletAmount = 3;


    [Header("Bools")]
    public bool isEquippedByPlayer = false;
    [SerializeField] public bool isFireInterval = false;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        weaponCollider = GetComponent<Collider>();
        outline = GetComponent<Outline>();

        ChangeSettings();
    }

    private void Start() {
        mainCam = PlayerWeaponController.instance.GetComponent<peterkcodes.AdvancedMovement.Demo.CameraController>().cameraTransform;
    }

    private void ChangeSettings()
    {
        if(transform.parent != null)
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
        //s.AppendCallback(() => transform.position = mainCam.position + (mainCam.right * .1f) + (mainCam.forward * 3f));
        s.AppendCallback(() => ChangeSettings());
        s.AppendCallback(() => rb.AddForce((hitpoint - transform.position).normalized * 25, ForceMode.Impulse));
        s.AppendCallback(() => rb.AddForce(Vector3.up * 2, ForceMode.Impulse));
        s.AppendCallback(() => rb.AddTorque(transform.transform.right + transform.transform.up * 20, ForceMode.Impulse));
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
        yield return new WaitForSeconds(fireInterval);
        isFireInterval = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBodyPartScript bp = collision.gameObject.GetComponent<EnemyBodyPartScript>();

            //if (!bp.enemy.isDead)
                //Instantiate(SuperHotScript.instance.hitParticlePrefab, transform.position, transform.rotation);

            bp.Die(transform.GetComponent<Rigidbody>().linearVelocity);

            rb.AddForce((mainCam.position - transform.position).normalized * 2, ForceMode.Impulse);
            //rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }
}
