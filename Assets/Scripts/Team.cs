using System.Collections.Generic;

public enum TeamType { Red, Blue }

public class Team
{
    public TeamType teamType;
    public List<PlayerController> players = new List<PlayerController>();
    public List<TileObject> buildings = new List<TileObject>();

    public Team(TeamType type)
    {
        teamType = type;
    }

    public void AddPlayer(PlayerController player)
    {
        players.Add(player);
    }

    public void AddBuilding(TileObject building)
    {
        buildings.Add(building);
    }
}
