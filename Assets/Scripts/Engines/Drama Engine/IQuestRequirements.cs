using Unity.Collections;

public interface IQuestRequirements
{
    bool Requirements(out DataValidQuest vq, out NativeList<int> qs, out NativeList<int> qo, DataWorldState wsd);
}