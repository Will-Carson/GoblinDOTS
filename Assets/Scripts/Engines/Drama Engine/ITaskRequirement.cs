public interface ITaskRequirement
{
    bool Requirements(out EventTaskRequest eventTaskRequest, WorldStateData worldState);
}