using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileType
    {
        Grass = 0,
        Sand = 1,
        Water = 2
    }
    public TileType tileType;
    public Color defaultColor = Color.white;

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

    public void SetType(TileType type)
    {
        tileType = type;

        switch (tileType)
        {
            case TileType.Grass:
                Highlight(Color.green);
                defaultColor = Color.green;
                break;
            case TileType.Sand:
                Highlight(new Color(0.9f, 0.8f, 0.5f));
                defaultColor = new Color(0.9f, 0.8f, 0.5f);
                break;
            case TileType.Water:
                Highlight(Color.blue);
                defaultColor = Color.blue;
                break;
            
        }
    }

}
