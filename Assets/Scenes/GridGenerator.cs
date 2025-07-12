using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 10, height = 10;

    private void Start()
    {
        GenerateGrid();
    }
    public void GenerateGrid()
    {
        ClearGrid();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                tileObj.name = $"Tile_{x}_{y}";

                Tile tile = tileObj.GetComponent<Tile>();
                if (tile != null)
                    tile.Init(x, y);
            }
        }
    }

    void ClearGrid()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}
