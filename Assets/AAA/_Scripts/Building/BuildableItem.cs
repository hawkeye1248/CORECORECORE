using UnityEngine;

namespace Building
{
    /// <summary>
    /// One entry in the build catalog. Edit the list of these on <see cref="BuildingSystem"/>
    /// from the inspector (or from code) to control which buildings the player can place.
    /// </summary>
    [System.Serializable]
    public class BuildableItem
    {
        [Tooltip("Name shown in the hotbar / used for debugging.")]
        public string displayName;

        [Tooltip("The prefab that gets placed. Use a static prefab — if it self-generates randomly " +
                 "the translucent ghost preview won't match the final placed result.")]
        public GameObject prefab;

        [Tooltip("Icon shown in the bottom-of-screen hotbar.")]
        public Sprite icon;

        [Tooltip("How many of this building the player may place. Set to a negative number for " +
                 "unlimited (the hotbar then shows no count).")]
        public int maxCount = 5;

        /// <summary>Runtime stock left to place. Initialised from <see cref="maxCount"/> by
        /// BuildingSystem; not serialised. Unlimited when <see cref="maxCount"/> is negative.</summary>
        [System.NonSerialized] public int Remaining;

        public bool Unlimited => maxCount < 0;
        public bool HasStock => Unlimited || Remaining > 0;
    }
}
