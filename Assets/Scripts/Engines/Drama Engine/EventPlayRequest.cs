public struct EventPlayRequest
{
    public int playId;
    public int stageId;

    public EventPlayRequest(int _playId, int _stageId)
    {
        playId = _playId;
        stageId = _stageId;
    }
}