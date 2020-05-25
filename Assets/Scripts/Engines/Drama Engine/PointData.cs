public struct PointData
{
    // int pointId; // TODO might not be needed. ID might just be it's key in a hashset.
    public string name;
    public PointType type;
    public int[] occupants;
    public int maxOccupants;
}
