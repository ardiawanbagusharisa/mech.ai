using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    private Dictionary<Vector2Int, Tile> gridTiles = new Dictionary<Vector2Int, Tile>();

    private void Awake()
    {
        Instance = this;
        CacheTiles();
    }

    private void Start()
    {
        //CacheTiles();
    }

    void CacheTiles()
    {
        gridTiles.Clear();
        Tile[] tiles = FindObjectsOfType<Tile>();
        foreach (var tile in tiles)
        {
            gridTiles[tile.GridPos] = tile;
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        if (gridTiles.TryGetValue(gridPos, out Tile tile))
            return tile.transform.position;
        else
            return new Vector3(gridPos.x, 1, gridPos.y);
    }

    public Tile GetTileAt(Vector2Int pos) => gridTiles.TryGetValue(pos, out Tile t) ? t : null;
}
