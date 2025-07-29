using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private RobotUnit[] team0Robots; 
    [SerializeField] private RobotUnit[] team1Robots;
    
    public GameObject robotPrefab;
    public Transform team0Parent, team1Parent;
    
    private int currentTeam = 0;
    private int currentRobotIndex = 0;
    public RobotUnit currentRobot;

    public TMP_Text uiTurnText, uiActionText, uiTimerText;

    private float turnTime = 30f;
    private float timer;

    private Vector2Int[] team0SpawnPoints = new Vector2Int[] {
    new Vector2Int(4, 3),
    new Vector2Int(5, 3),
    new Vector2Int(6, 3)
};

    private Vector2Int[] team1SpawnPoints = new Vector2Int[] {
    new Vector2Int(4, 7),
    new Vector2Int(5, 7),
    new Vector2Int(6, 7)
};

    private void Awake() => Instance = this;

    void Start()
    {
        GridManager.Instance?.SendMessage("CacheTiles");
        SpawnRobotsFromPrefab();
        StartTurn();
    }

    void SpawnRobotsFromPrefab()
    {
        team0Robots = new RobotUnit[3];
        team1Robots = new RobotUnit[3];

        for (int i = 0; i < 3; i++)
        {
            var r0 = Instantiate(robotPrefab, team0Parent);
            var r1 = Instantiate(robotPrefab, team1Parent);
            r0.name = $"Team0_Robot_{i}";
            r1.name = $"Team1_Robot_{i}";
            team0Robots[i] = r0.GetComponent<RobotUnit>();
            team1Robots[i] = r1.GetComponent<RobotUnit>();
        }

        SpawnRobots(team0Robots, team0SpawnPoints, 0);
        SpawnRobots(team1Robots, team1SpawnPoints, 1);
    }


    void SpawnRobots(RobotUnit[] robots, Vector2Int[] spawnPoints, int teamId)
    {
        for (int i = 0; i < robots.Length && i < spawnPoints.Length; i++)
        {
            robots[i].teamId = teamId;
            robots[i].gridPos = spawnPoints[i];
            robots[i].SetColor();
            robots[i].PlaceAtGridPosition(); 
        }
    }



    void Update()
    {
        timer -= Time.deltaTime;
        uiTimerText.text = $"Timer: {timer:F1}";

        if (timer <= 0)
        {
            EndTurn();
        }
    }

    void StartTurn()
    {
        timer = turnTime;
        currentRobot = GetCurrentTeam()[currentRobotIndex];
        currentRobot.ResetActions();
        UpdateUI();
        Camera.main.GetComponent<CameraController>().HandleFocusOnRobot();
    }

    public void EndTurnEarly()
    {
        TryAdvanceTurn();
    }

    public void EndTurn()
    {
        Debug.Log("Ending turn manually.");
        currentRobotIndex++; 
        TryAdvanceTurn();
    }

    public void ForceSkipTurn()
    {
        RobotUnit current = GetCurrentRobot();
        if (current != null)
        {
            //Debug.Log($"Robot {current.name} forcibly skipped.");
            current.isActive = false;
        }
        TryAdvanceTurn();
    }

    private void TryAdvanceTurn()
    {
        RobotUnit[] team = GetCurrentTeam();
        int count = team.Length;

        // Start from the *next* robot
        int nextIndex = (currentRobotIndex + 1) % count;

        for (int i = 0; i < count; i++)
        {
            int idx = (nextIndex + i) % count;
            if (team[idx].isActive)
            {
                currentRobotIndex = idx;
                StartTurn();
                return;
            }
        }

        // No active robots left in this team — switch to the other team
        currentTeam = 1 - currentTeam;

        // Reactivate all non-dead robots in the new team
        ReactivateTeam(GetCurrentTeam());

        RobotUnit[] nextTeam = GetCurrentTeam();
        for (int i = 0; i < nextTeam.Length; i++)
        {
            if (nextTeam[i].isActive)
            {
                currentRobotIndex = i;
                StartTurn();
                return;
            }
        }

        Debug.Log("Game Over — No active robots remaining in either team.");
        // TODO: Trigger Game Over UI
    }


    public RobotUnit[] GetCurrentTeam()
    {
        return currentTeam == 0 ? team0Robots : team1Robots;
    }

    public RobotUnit GetCurrentRobot()
    {
        return GetCurrentTeam()[currentRobotIndex];
    }

    public RobotUnit GetRobotAtGridPos(Vector2Int pos)
    {
        foreach (var r in team0Robots.Concat(team1Robots))
        {
            if (r != null && r.gameObject.activeInHierarchy && r.hp > 0 &&  r.gridPos == pos)
                return r;
        }
        return null;
    }

    void UpdateUI()
    {
        uiTurnText.text = $"Team {currentTeam}'s Turn\n{currentRobot.name}";
        uiActionText.text = $"Robot Energy: {currentRobot.energy}";
    }

    private void ReactivateTeam(RobotUnit[] team)
    {
        foreach (var r in team)
        {
            if (r.hp > 0)
                r.isActive = true;
        }
    }

    public void CheckForVictory()
    {
        bool team0HasUnits = HasAliveUnits(team0Robots);
        bool team1HasUnits = HasAliveUnits(team1Robots);

        if (!team0HasUnits && team1HasUnits)
        {
            Debug.Log("🏆 Team 1 Wins!");
            Debug.Break();
            // TODO: Trigger win UI
        }
        else if (!team1HasUnits && team0HasUnits)
        {
            Debug.Log("🏆 Team 0 Wins!");
            Debug.Break();
            // TODO: Trigger win UI
        }
        else if (!team0HasUnits && !team1HasUnits)
        {
            Debug.Log("⚠️ Draw! No teams have surviving robots.");
            Debug.Break();
            // TODO: Draw handling if needed
        }
    }

    private bool HasAliveUnits(RobotUnit[] team)
    {
        foreach (var r in team)
        {
            if (r != null && r.hp > 0)
                return true;
        }
        return false;
    }

    public Tile GetTileAtGridPos(Vector2Int pos)
    {
        foreach (var tile in FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tile.GridPos == pos)
                return tile;
        }
        return null;
    }

    public bool IsBlocked(Vector2Int pos)
    {
        // Block if there's a robot on it or outside bounds
        return GetRobotAtGridPos(pos) != null;
    }
}
