using UnityEngine;

public interface MatchInfo 
{
    public bool wasUserInitiatedMatch();
}

public interface PlayerMoveInfo
{
    int MoveId { get; }
    bool AllPlayerGroupsExactlyFour { get; }
    bool BothSwapTilesMatched { get; }
    int BoardWidth { get; }
    int BoardHeight { get; }
}
