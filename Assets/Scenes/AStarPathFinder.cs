using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private class Node
    {
        public Vector2Int pos;
        public float gCost, hCost;
        public float fCost => gCost + hCost;
        public Node parent;

        public Node(Vector2Int pos) => this.pos = pos;
    }

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, HashSet<Vector2Int> allowedTiles)
    {
        var manager = GameManager.Instance;
        var open = new List<Node>();
        var closed = new HashSet<Vector2Int>();

        Node startNode = new Node(start) { gCost = 0, hCost = Heuristic(start, goal) };
        open.Add(startNode);

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.fCost.CompareTo(b.fCost));
            Node current = open[0];
            open.RemoveAt(0);
            closed.Add(current.pos);

            if (current.pos == goal)
                return ReconstructPath(current);

            foreach (Vector2Int neighbor in GetNeighbors(current.pos))
            {
                if (!allowedTiles.Contains(neighbor)) continue; // ❌ skip tiles outside highlight
                if (closed.Contains(neighbor)) continue;
                if (manager.IsBlocked(neighbor)) continue;

                float tentativeG = current.gCost + Vector2Int.Distance(current.pos, neighbor);

                Node neighborNode = open.Find(n => n.pos == neighbor);
                if (neighborNode == null)
                {
                    neighborNode = new Node(neighbor)
                    {
                        gCost = tentativeG,
                        hCost = Heuristic(neighbor, goal),
                        parent = current
                    };
                    open.Add(neighborNode);
                }
                else if (tentativeG < neighborNode.gCost)
                {
                    neighborNode.gCost = tentativeG;
                    neighborNode.parent = current;
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructPath(Node endNode)
    {
        var path = new List<Vector2Int>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.pos);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b); // diagonal ok
    }

    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        foreach (var dir in directions)
        {
            yield return pos + dir;
        }
    }
}
