using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    public static PlayerWeaponController instance;

    [Header("Child Objects")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private PunchWeapon punchs;
    private Transform mainCam;

    [Header("Weapon Settings")]
    private WeaponScript weapon;
    private bool canShoot = true;
    
    [SerializeField] private LayerMask weaponLayer;
    [SerializeField] private LayerMask throwLayer;


    private GameObject lastWeaponLooked = null;

    private void Awake() {
        if(instance == null)
        {
            instance = this;
        }else
        {
            Destroy(gameObject);
        }
    }

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
                }else
                {
                    punchs.Shoot(SpawnPos(), mainCam.rotation);
                }
            }
        }

        if(Input.GetMouseButtonDown(1))
        {
            if(weapon != null)
            {
                RaycastHit castHit;
                if(Physics.Raycast(mainCam.position, mainCam.forward, out castHit, 100))
                {
                    weapon.isEquippedByPlayer = false;
                    weapon.Throw(castHit.point);
                    weapon = null;
                } else
                {
                    weapon.isEquippedByPlayer = false;
                    weapon.Throw(mainCam.position + (mainCam.forward * 100));
                    weapon = null;
                }
                
                
                
            }
        }

        RaycastHit hit;
        if(Physics.Raycast(mainCam.position, mainCam.forward, out hit,3, weaponLayer))
        {
            if(hit.collider.gameObject != lastWeaponLooked)
            {
                if(lastWeaponLooked != null)
                {
                    lastWeaponLooked.GetComponent<Outline>().enabled = false;
                }
                hit.collider.gameObject.GetComponent<Outline>().enabled = true;
                lastWeaponLooked = hit.collider.gameObject;
            }

            if (Input.GetMouseButtonDown(0) && weapon == null)
            {
                hit.transform.GetComponent<WeaponScript>().Pickup(weaponHolder);
                weapon = hit.transform.GetComponent<WeaponScript>();
            }
        } else
        {
            if(lastWeaponLooked != null)
            {
                lastWeaponLooked.GetComponent<Outline>().enabled = false;
                lastWeaponLooked = null;
            }
        }
    }

    Vector3 SpawnPos()
    {
        return mainCam.position + (mainCam.forward * .5f) + (mainCam.up * -.02f);
    }

    void OnTriggerEnter(UnityEngine.Collider other)

    {
        if(other.gameObject.CompareTag("MapBorder"))
        {
            GetComponent<Health>().DamageHealth(999);
        }       
    }
}