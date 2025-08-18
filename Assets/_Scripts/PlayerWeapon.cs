using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    private WeaponDataSO currentWeapon;
    private WeaponState currentState;
    private int bulletsLeftInMagazine;
    private int burstBulletsLeft = 0;
    private bool isContinuingShooting = false;
    [SerializeField] private WeaponDataSO defaultWeapon;
    private Camera mainCam;
    private RaycastHit hitInfo;
    private CinemachineImpulseSource impulseSource;


    private void Start()
    {
        GameInput.Instance.OnFirePerformed += on_fire_performed;
        GameInput.Instance.OnFireCanceled += on_fire_canceled;
        GameInput.Instance.OnReloadPerformed += on_reload_performed;

        if (currentWeapon == null)
        {
            if (defaultWeapon == null)
            {
                currentState = WeaponState.DISABLED;
            }
            else
            {
                SwitchWeapon(defaultWeapon);
            }

        }

        mainCam = Camera.main;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case WeaponState.READY_TO_SHOOT:
                break;
            case WeaponState.SHOOTING:
                break;
            case WeaponState.STOPPING_SHOOTING:
                break;
            case WeaponState.RELOADING:
                break;
            case WeaponState.DISABLED:
                break;
            default:
                break;
        }
    }

    private void on_fire_performed(object sender, EventArgs e)
    {
        if (currentState == WeaponState.READY_TO_SHOOT)
        {
            currentState = WeaponState.SHOOTING;
            if (currentWeapon.isAutomatic)
            {
                isContinuingShooting = true;
            }
            burstBulletsLeft = currentWeapon.burstAmount;
            Shoot();
        }
    }

    private void on_fire_canceled(object sender, EventArgs e)
    {
        if (currentState == WeaponState.SHOOTING)
        {
            isContinuingShooting = false;
        }
    }

    private void on_reload_performed(object sender, EventArgs e)
    {
        if (bulletsLeftInMagazine < currentWeapon.magazineSize && currentState != WeaponState.RELOADING && currentState != WeaponState.DISABLED)
        {
            currentState = WeaponState.RELOADING;
            Reload();
        }
    }

    private void Shoot()
    {

        float xSpread = UnityEngine.Random.Range(currentWeapon.xSpread.x, currentWeapon.xSpread.y);
        float ySpread = UnityEngine.Random.Range(currentWeapon.ySpread.x, currentWeapon.ySpread.y);

        Vector3 shootDirection = mainCam.transform.forward + new Vector3(xSpread, ySpread, 0);
        impulseSource.GenerateImpulse();
        if (Physics.Raycast(mainCam.transform.position, shootDirection, out hitInfo, currentWeapon.range))
        {
            if (hitInfo.collider.CompareTag("Enemy"))
            {

            }
            else if (hitInfo.collider.CompareTag("Ground"))
            {
                Instantiate(currentWeapon.wallBulletHole, hitInfo.point, Quaternion.identity);
            }
        }

        bulletsLeftInMagazine--;
        burstBulletsLeft--;
        if (burstBulletsLeft > 0 && bulletsLeftInMagazine > 0)
        {
            Invoke("Shoot", currentWeapon.burstRate);
        }
        Invoke("ResetShoot", currentWeapon.fireRate);
    }

    private void ResetShoot()
    {
        if (bulletsLeftInMagazine > 0)
        {
            currentState = WeaponState.READY_TO_SHOOT;
            if (currentWeapon.isAutomatic && isContinuingShooting)
            {
                currentState = WeaponState.SHOOTING;
                burstBulletsLeft = currentWeapon.burstAmount;
                Shoot();
            }
        }
        else
        {
            currentState = WeaponState.RELOADING;
            Reload();
        }
    }

    private void Reload()
    {
        Invoke("ReloadFinished", currentWeapon.reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeftInMagazine = currentWeapon.magazineSize;
        currentState = WeaponState.READY_TO_SHOOT;
    }

    public void SwitchWeapon(WeaponDataSO weaponData)
    {
        currentWeapon = weaponData;

        //TODO mermi sayısını kontrol edip current state e karar ver
        //TODO diğer silahla alakalı değişkenleri set et
    }
}

public enum WeaponState
{
    READY_TO_SHOOT,
    SHOOTING,
    STOPPING_SHOOTING,
    RELOADING,
    DISABLED
}
