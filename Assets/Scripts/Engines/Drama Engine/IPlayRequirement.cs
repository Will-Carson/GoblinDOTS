public interface IPlayRequirement
{
    // TODO may need to pass in some kind of world state object.
    bool Requirements(out EventPlayRequest playRequest, DataWorldState worldState);
}
