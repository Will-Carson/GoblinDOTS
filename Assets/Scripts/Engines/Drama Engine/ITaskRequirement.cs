public interface ITaskRequirement
{
    bool Requirements(out EventTaskRequest eventTaskRequest, DataWorldState worldState);
}