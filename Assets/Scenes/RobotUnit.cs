using UnityEngine;
using System.Collections;

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

    public void Start()
    {
        // Debug movement
        //MoveTo(new Vector2Int(5, 5));
    }

    public void SetColor() {
        GetComponent<Renderer>().material = colorMaterials[teamId];
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

        Debug.Log($"Placed robot at grid position {gridPos} with world position {transform.position}");
    }

    public void MoveTo(Vector2Int newGridPos)
    {
        if (isMoving || !canMove) return;

        Vector3 targetWorld = GridManager.Instance.GetWorldPosition(newGridPos);
        targetWorld.y = moveHeight;
        StartCoroutine(MoveToPosition(targetWorld, newGridPos));
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, Vector2Int newGridPos)
    {
        isMoving = true;

        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(start, targetPosition, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        gridPos = newGridPos;
        canMove = false;
        isMoving = false;

        Debug.Log($"Finished moving to {gridPos} at {transform.position}");
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
