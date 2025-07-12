using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPos;
    public bool isInteractable = true;

    public void Init(int x, int y)
    {
        GridPos = new Vector2Int(x, y);
    }

    public void Highlight(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;
    }

}
