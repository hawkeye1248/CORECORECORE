using UnityEngine;

public class PickupableWeapon : MonoBehaviour
{
    [SerializeField] public WeaponDataSO weaponData;

    private Collider weaponCollider;
    private Rigidbody rb;
    private bool isEquipped;
    public static bool slotFull;
    [SerializeField] private float pickUpRange;

    public float dropForwardForce, dropUpwardForce;

    private Camera mainCamera;
    public float throwForce, throwUpForce;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {

        mainCamera = Camera.main;
        //Setup
        if (!isEquipped)
        {
            //gunScript.enabled = false;
            rb.isKinematic = false;
            weaponCollider.isTrigger = false;
        }
        if (isEquipped)
        {
            //gunScript.enabled = true;
            rb.isKinematic = true;
            weaponCollider.isTrigger = true;
            slotFull = true;
        }
    }

    private void Update()
    {
        Vector3 distanceToPlayer = Player.Instance.transform.position - transform.position;
        if (!isEquipped && distanceToPlayer.magnitude <= pickUpRange && Input.GetKeyDown(KeyCode.E) && !slotFull) Pickup();

        //Drop if equipped and "Q" is pressed
        if (isEquipped && Input.GetKeyDown(KeyCode.Q)) Drop();

        if (isEquipped && Input.GetKeyDown(KeyCode.T)) ThrowWeapon();
    }

    public void Pickup()
    {
        isEquipped = true;
        slotFull = true;

        rb.isKinematic = true;
        weaponCollider.isTrigger = true;
        Player.Instance.PickupWeapon(this);
        Debug.Log("heyo");
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);



    }

    public void Drop()
    {
        isEquipped = false;
        slotFull = false;

        //Set parent to null
        transform.SetParent(null);

        //Make Rigidbody not kinematic and BoxCollider normal
        rb.isKinematic = false;
        weaponCollider.isTrigger = false;

        //Gun carries momentum of player
        rb.linearVelocity = Player.Instance.GetComponent<Rigidbody>().linearVelocity;

        //AddForce
        rb.AddForce(mainCamera.transform.forward * dropForwardForce, ForceMode.Impulse);
        rb.AddForce(mainCamera.transform.up * dropUpwardForce, ForceMode.Impulse);
        //Add random rotation
        float random = Random.Range(-1f, 1f);
        rb.AddTorque(new Vector3(random, random, random) * 10);

        //Disable script
        //gunScript.enabled = false;
        Player.Instance.DropWeapon();
    }

    public void ThrowWeapon()
    {
        isEquipped = false;
        slotFull = false;

        transform.SetParent(null);

        rb.isKinematic = false;
        weaponCollider.isTrigger = false;

        Vector3 forceToAdd = mainCamera.transform.forward * throwForce + transform.up * throwUpForce;

        rb.AddForce(forceToAdd, ForceMode.Impulse);
        Player.Instance.DropWeapon();
    }
}
