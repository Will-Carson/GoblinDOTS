using Unity.Collections;

public struct DataPoint
{
    // int pointId; // TODO might not be needed. ID might just be it's key in a hashset.
    public string name;
    public TypePoint type;
    public NativeArray<int> occupants;
    public int maxOccupants;
    public int parentStage;
    public int parentSite;
}
