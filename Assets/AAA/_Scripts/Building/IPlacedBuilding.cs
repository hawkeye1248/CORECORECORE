namespace Building
{
    /// <summary>
    /// Implement on a component of a buildable prefab to react the moment it is actually placed
    /// (as opposed to being shown as a translucent ghost preview). <see cref="BuildingSystem.Place"/>
    /// calls <see cref="OnPlaced"/> on the fresh instance; the ghost never receives it, so preview
    /// copies stay inert.
    /// </summary>
    public interface IPlacedBuilding
    {
        void OnPlaced();
    }
}
