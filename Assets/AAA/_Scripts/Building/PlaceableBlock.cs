using UnityEngine;

namespace Building
{
    /// <summary>
    /// Marker added by <see cref="BuildingSystem"/> to every building the player places. The
    /// placement raycast uses it to tell "a block the player built" (full position + rotation snap)
    /// apart from everything else — terrain and pre-placed buildings, which only rotation-align.
    /// </summary>
    public class PlaceableBlock : MonoBehaviour
    {
    }
}
