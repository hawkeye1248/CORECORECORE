using UnityEngine;

public class MapBorder : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.TryGetComponent<Health>(out Health health))
        {
            health.KillCharacter();
        }       
    }
}
