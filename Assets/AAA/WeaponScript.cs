using System.Collections;
using UnityEngine;
using DG.Tweening;

public class WeaponScript : MonoBehaviour
{
    public bool isEquippedByPlayer = false;
    public bool isFireInterval = false;
    private Rigidbody rb;
    private Collider collider;
    private Renderer renderer;

    public int bulletAmount = 3;
    public float fireInterval = 0.3f;
    [SerializeField] private GameObject bulletPrefab;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        renderer = GetComponent<Renderer>();

        ChangeSettings();
    }

    private void ChangeSettings()
    {
        if(transform.parent != null)
        {
            return;
        }

        rb.isKinematic = isEquippedByPlayer ? true : false;
        rb.interpolation = isEquippedByPlayer ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
        collider.isTrigger = isEquippedByPlayer;
    }

    public void Shoot(Vector3 pos, Quaternion rot, bool isEnemy)
    {
        if(isFireInterval)
        {
            return;
        }

        if(bulletAmount <= 0)
        {
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, pos, rot);

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }

        if(isEquippedByPlayer)
        {
            StartCoroutine(FireInterval());
        }
    }

    public void Throw()
    {
        Sequence s = DOTween.Sequence();
        s.Append(transform.DOMove(transform.position - transform.forward, .01f)).SetUpdate(true);
        s.AppendCallback(() => transform.parent = null);
        s.AppendCallback(() => transform.position = Camera.main.transform.position + (Camera.main.transform.right * .1f));
        s.AppendCallback(() => ChangeSettings());
        s.AppendCallback(() => rb.AddForce(Camera.main.transform.forward * 10, ForceMode.Impulse));
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
        
    }

    IEnumerator FireInterval()
    {
        isFireInterval = true;
        yield return new WaitForSeconds(fireInterval);
        isFireInterval = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && collision.relativeVelocity.magnitude < 15)
        {
            EnemyBodyPartScript bp = collision.gameObject.GetComponent<EnemyBodyPartScript>();

            //if (!bp.enemy.isDead)
                //Instantiate(SuperHotScript.instance.hitParticlePrefab, transform.position, transform.rotation);

            bp.Die(transform.position);
        }
    }
}
