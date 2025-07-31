using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RobotUnit : MonoBehaviour
{
    public int teamId;
    public Vector2Int gridPos;
    public int hp = 5;
    public int energy = 5;
    public bool canMove = true, canAttack = true, canSkill = true;
    public bool isActive = true;
    public float moveDuration = 1.0f;

    private bool isMoving = false;
    private float moveHeight = 1f;

    public Material[] colorMaterials;
    public Sprite[] sprites;
    public GameObject standee;
    public GameObject sprite;

    public void Start()
    {
        // Debug movement
        //MoveTo(new Vector2Int(5, 5));
    }
    private void LateUpdate()
    {
        // Billboard for the robot sprite, the x axis should also follow
        if (sprite != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 lookDirection = cameraPos - transform.position;
            lookDirection.y = 0; // Keep it horizontal
            sprite.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public void SetColor() {
        GetComponent<Renderer>().material = colorMaterials[teamId];
        standee.GetComponent<Renderer>().material = colorMaterials[teamId];
        sprite.GetComponent<SpriteRenderer>().sprite = sprites[teamId];
    }
    public void ResetActions()
    {
        canMove = true;
        canAttack = true;
        canSkill = true;
    }

    public void PlaceAtGridPosition()
    {
        Vector3 worldPos = GridManager.Instance.GetWorldPosition(gridPos);
        worldPos.y = moveHeight;
        transform.position = worldPos;
    }

    public void MoveTo(Vector2Int targetGridPos, HashSet<Vector2Int> allowedTiles)
    {
        List<Vector2Int> path = AStarPathfinder.FindPath(gridPos, targetGridPos, allowedTiles);

        if (path != null && path.Count > 1) // Exclude current tile
            StartCoroutine(MoveAlongPath(path));
        else
            Debug.LogWarning("No path found!");
    }

    private IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        isMoving = true;

        List<Vector3> worldPath = new List<Vector3>();
        foreach (var grid in path)
        {
            Vector3 pos = GridManager.Instance.GetWorldPosition(grid);
            worldPath.Add(new Vector3(pos.x, moveHeight, pos.z));
        }

        float totalDistance = 0f;
        for (int i = 0; i < worldPath.Count - 1; i++)
            totalDistance += Vector3.Distance(worldPath[i], worldPath[i + 1]);

        float speed = path.Count * totalDistance / (moveDuration * path.Count); // control overall speed
        int currentIndex = 0;

        Vector3 currentStart = worldPath[0];
        Vector3 currentEnd = worldPath[1];

        while (currentIndex < worldPath.Count - 1)
        {
            float segmentLength = Vector3.Distance(currentStart, currentEnd);
            float segmentProgress = 0f;

            while (segmentProgress < 1f)
            {
                segmentProgress += Time.deltaTime * speed / segmentLength;
                transform.position = Vector3.Lerp(currentStart, currentEnd, segmentProgress);
                yield return null;
            }

            currentIndex++;
            currentStart = worldPath[currentIndex];
            if (currentIndex + 1 < worldPath.Count)
                currentEnd = worldPath[currentIndex + 1];

            Camera.main.GetComponent<CameraController>().HandleFocusOnRobot();
        }

        // Finalize position
        transform.position = worldPath[^1]; // C# 8 syntax for last element
        gridPos = path[^1];

        canMove = false;
        isMoving = false;

        Debug.Log($"Finished moving to {gridPos}.");
    }



    public bool IsMoving() => isMoving;

    public void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"{name} took {amount} damage. Remaining HP: {hp}");
        Debug.Log($"Active: {isActive}");

        if (hp <= 0)
        {
            hp = 0;
            isActive = false;
            gameObject.SetActive(false); 
            Debug.Log($"{name} has been destroyed.");
            GameManager.Instance.CheckForVictory();
            
        }
    }
}
