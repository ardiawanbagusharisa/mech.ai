using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public LayerMask tileMask;
    private enum ActionType { None, Move, Attack, Skill }
    private ActionType selectedAction = ActionType.None;

    

    bool IsRobotDone(RobotUnit robot)
    {
        return !robot.canMove && !robot.canAttack && !robot.canSkill;
    }

    void Update()
    {
        if (GameManager.Instance.GetCurrentRobot().IsMoving()) return;

        if (Input.GetKeyDown(KeyCode.M)) SelectAction(ActionType.Move);
        if (Input.GetKeyDown(KeyCode.A)) SelectAction(ActionType.Attack);
        if (Input.GetKeyDown(KeyCode.S)) SelectAction(ActionType.Skill);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Debug.Log("Manual skip turn.");
            GameManager.Instance.ForceSkipTurn();
            ClearAllHighlights(); 
            selectedAction = ActionType.None;
        }

        if (Input.GetMouseButtonDown(0) && selectedAction != ActionType.None)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var robot = GameManager.Instance.GetCurrentRobot();

                Tile clickedTile = hit.collider.GetComponent<Tile>();
                RobotUnit clickedRobot = hit.collider.GetComponent<RobotUnit>();

                // If robot is clicked (e.g., attack)
                if (clickedRobot != null && selectedAction == ActionType.Attack)
                {
                    Tile robotTile = GameManager.Instance.GetTileAtGridPos(clickedRobot.gridPos);
                    if (robotTile != null && robotTile.isInteractable && clickedRobot.teamId != robot.teamId && robot.canAttack)
                    {
                        Debug.Log("Clicked valid enemy robot on interactable tile.");
                        clickedRobot.TakeDamage(1);
                        robot.canAttack = false;
                        ClearAllHighlights();
                        selectedAction = ActionType.None;

                        if (IsRobotDone(robot))
                        {
                            GameManager.Instance.EndTurnEarly();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Clicked robot is not on an interactable tile or not a valid target.");
                    }
                    return;
                }

                // If a tile is clicked (e.g., move or skill)
                if (clickedTile != null && clickedTile.isInteractable)
                {
                    ExecuteAction(clickedTile.GridPos);
                }
                else
                {
                    Debug.LogWarning("Clicked object is not a valid interactable tile or robot.");
                }
            }
            else
            {
                Debug.LogWarning("Raycast did not hit anything.");
            }
        }

    }

    void SelectAction(ActionType action)
    {
        selectedAction = action;
        Debug.Log($"Selected Action: {selectedAction}");
        HighlightTiles(action);
    }

    void ExecuteAction(Vector2Int targetPos)
    {
        var robot = GameManager.Instance.GetCurrentRobot();
        int dist = Mathf.Abs(robot.gridPos.x - targetPos.x) + Mathf.Abs(robot.gridPos.y - targetPos.y);
        Debug.Log($"Trying {selectedAction} to {targetPos} | Dist: {dist}");

        switch (selectedAction)
        {
            case ActionType.Move:
                Debug.Log("Move triggered");

                // Prevent moving onto another robot
                if (GameManager.Instance.GetRobotAtGridPos(targetPos) != null)
                {
                    Debug.LogWarning("❌ Cannot move to a tile occupied by another robot!");
                    ClearAllHighlights();
                    return;
                }

                if (robot.canMove)
                {
                    HashSet<Vector2Int> allowedTiles = new HashSet<Vector2Int>();
                    foreach (var tile in FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        if (tile.isInteractable)
                            allowedTiles.Add(tile.GridPos);
                    }

                    List<Vector2Int> path = AStarPathfinder.FindPath(robot.gridPos, targetPos, allowedTiles);
                    if (path == null || path.Count <= 1)
                    {
                        Debug.Log("❌ No valid path to target");
                        ClearAllHighlights();
                        return;
                    }


                    robot.MoveTo(targetPos, allowedTiles);
                    
                    //Camera.main.GetComponent<CameraController>().HandleFocusOnRobot();
                }

                ClearAllHighlights();
                break;

            case ActionType.Attack:
                Debug.Log("Attack triggered");
                RobotUnit target = GameManager.Instance.GetRobotAtGridPos(targetPos);
                if (target != null && target.teamId != robot.teamId && robot.canAttack)
                {
                    target.TakeDamage(1);
                    robot.canAttack = false;
                }
                ClearAllHighlights();
                break;

            case ActionType.Skill:
                Debug.Log("Skill triggered");
                if (dist <= 2 && robot.canSkill && robot.energy >= 3)
                {
                    robot.energy -= 3;
                    robot.canSkill = false;
                }
                ClearAllHighlights();
                break;
        }

        selectedAction = ActionType.None;

        if (IsRobotDone(robot))
        {
            GameManager.Instance.EndTurnEarly();
        }
    }

    
    void HighlightTiles(ActionType action)
    {
        ClearAllHighlights();

        var robot = GameManager.Instance.GetCurrentRobot();

        bool isUsed = action switch
        {
            ActionType.Move => !robot.canMove,
            ActionType.Attack => !robot.canAttack,
            ActionType.Skill => !robot.canSkill,
            _ => true
        };

        int range = action switch
        {
            ActionType.Move => 3,
            ActionType.Attack => 1,
            ActionType.Skill => 2,
            _ => 0
        };

        var reachable = new HashSet<Vector2Int>();
        foreach (var tile in FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            int dist = Mathf.Abs(tile.GridPos.x - robot.gridPos.x) + Mathf.Abs(tile.GridPos.y - robot.gridPos.y);

            if (dist <= range)
            {
                if (isUsed)
                {
                    tile.Highlight(Color.gray);
                    tile.SetInteractable(false);
                }
                else
                {
                    switch (action)
                    {
                        case ActionType.Move:
                            bool isBlocked = GameManager.Instance.GetRobotAtGridPos(tile.GridPos) != null;
                            Color moveColor = isBlocked ? Color.gray : Color.cyan;
                            tile.Highlight(moveColor);
                            tile.SetInteractable(!isBlocked);
                            if (!isBlocked) reachable.Add(tile.GridPos);
                            break;

                        case ActionType.Attack:
                            {
                                RobotUnit target = GameManager.Instance.GetRobotAtGridPos(tile.GridPos);
                                if (target != null && target.teamId != robot.teamId)
                                {
                                    tile.Highlight(Color.green);
                                    tile.SetInteractable(true);
                                }
                                else
                                {
                                    tile.Highlight(Color.red);
                                    tile.SetInteractable(false);
                                }
                                break;
                            }

                        case ActionType.Skill:
                            {
                                RobotUnit target = GameManager.Instance.GetRobotAtGridPos(tile.GridPos);
                                if (target != null && target.teamId != robot.teamId)
                                {
                                    tile.Highlight(new Color(1f, 0.5f, 0f)); // orange
                                    tile.SetInteractable(true);
                                }
                                else
                                {
                                    tile.Highlight(new Color(1f, 0.5f, 0f, 0.3f)); // lighter orange or semi-transparent
                                    tile.SetInteractable(false);
                                }
                                break;
                            }
                    }
                }
            }
            else
            {
                tile.Highlight(tile.defaultColor);
                tile.SetInteractable(false);
            }
        }

        if (action == ActionType.Move)
            GridManager.Instance.reachableMoveTiles = reachable;
    }

    void ClearAllHighlights()
    {
        foreach (var tile in FindObjectsByType<Tile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            tile.Highlight(tile.defaultColor);
        }
    }
}
