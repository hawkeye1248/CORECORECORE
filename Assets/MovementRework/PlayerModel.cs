using UnityEngine;

namespace MovementRework {
    public class PlayerModel : MonoBehaviour
    {
        public void SimplePosition(Vector3 position)
        {
            transform.position = new Vector3(position.x, position.y - 0.5f, position.z);
        }
    }
}
