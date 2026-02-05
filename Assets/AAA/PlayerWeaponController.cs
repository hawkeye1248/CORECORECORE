using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    private bool canShoot = true;
    [SerializeField] private Transform weaponHolder;
    private WeaponScript weapon;
    private Transform mainCam;
    [SerializeField] private LayerMask weaponLayer;
    void Start()
    {
        mainCam = GetComponent<peterkcodes.AdvancedMovement.Demo.CameraController>().cameraTransform;
        if(weaponHolder.GetComponentInChildren<WeaponScript>() != null)
        {
            weapon = weaponHolder.GetComponentInChildren<WeaponScript>();
        }      
    }
    void Update()
    {
        if(canShoot)
        {
            if(Input.GetMouseButtonDown(0))
            {
                if(weapon != null)
                {
                    weapon.Shoot(SpawnPos(), mainCam.rotation, false);
                }
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            if(weapon != null)
            {
                weapon.Throw();
                weapon.isEquippedByPlayer = false;
                weapon = null;
            }
        }

        RaycastHit hit;
        if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit,10, weaponLayer))
        {
            if (Input.GetMouseButtonDown(0) && weapon == null)
            {
                hit.transform.GetComponent<WeaponScript>().Pickup(weaponHolder);
                weapon = hit.transform.GetComponent<WeaponScript>();
            }
        }
    }

    Vector3 SpawnPos()
    {
        return mainCam.position + (mainCam.forward * .5f) + (mainCam.up * -.02f);
    }
}
