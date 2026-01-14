public class MapHandler
{
    private readonly MapSet mapSet;
    private readonly int numberOfRounds;
    private int currentRound = 0;

    public MapHandler(MapSet mapSet, int numberOfRounds)
    {
        this.mapSet = mapSet;
        this.numberOfRounds = numberOfRounds;
    }

    public string NextMap
    {
        get
        {
            if (mapSet == null || mapSet.maps == null || mapSet.maps.Length == 0)
            {
                return string.Empty;
            }

            int mapIndex = currentRound % mapSet.maps.Length;
            currentRound++;
            return mapSet.maps[mapIndex];
        }
    }

    public bool IsComplete => currentRound >= numberOfRounds;

    public int CurrentRound => currentRound;
    public int TotalRounds => numberOfRounds;
}
