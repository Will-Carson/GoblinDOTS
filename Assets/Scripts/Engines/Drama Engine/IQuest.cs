public interface IQuest
{
    bool InitialRequirements();
    bool TurnInRequirements();
    void TurnInRewards(); // TODO add a type to input as the rewardee. Perhaps an int?
}
