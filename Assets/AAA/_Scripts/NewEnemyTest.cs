using System.Collections;
using UnityEngine;

public class NewEnemyTest : MonoBehaviour
{
    Animator enemyAnim;
    public bool isDead;
    //public Transform weaponParent;

    void Awake()
    {
        enemyAnim = GetComponent<Animator>();       
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void Ragdoll()
    {
        enemyAnim.enabled = false;
        EnemyBodyPartScript[] parts = GetComponentsInChildren<EnemyBodyPartScript>();
        foreach (EnemyBodyPartScript bp in parts)
        {
            bp.rb.isKinematic = false;
            bp.rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        isDead = true;
    }
}
