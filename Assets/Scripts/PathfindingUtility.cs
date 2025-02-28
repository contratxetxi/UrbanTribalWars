using System.Collections.Generic;
using UnityEngine;

public static class PathfindingUtility
{
    /// <summary>
    /// Encuentra un camino (lista de Tiles) usando A* entre 'start' y 'end'.
    /// </summary>
    /// <param name="board">El BoardManager que permite obtener tiles.</param>
    /// <param name="start">Coordenada inicial.</param>
    /// <param name="end">Coordenada objetivo.</param>
    /// <param name="allowEndOccupied">
    ///    Indica si se permite que la casilla final esté ocupada 
    ///    (ej. para atacar un objetivo en esa casilla).
    /// </param>
    /// <param name="isTileAvailable">Función que indica si un Tile está disponible u ocupado/bloqueado.</param>
    /// <returns>Retorna la lista de Tiles que forman el camino; null si no hay un camino válido.</returns>
    public static List<Tile> FindPath(
        BoardManager board,
        Vector2Int start,
        Vector2Int end,
        bool allowEndOccupied,
        System.Func<Tile, bool> isTileAvailable)
    {
        // Caso especial: si start == end, devolvemos un camino de un solo elemento
        if (start == end)
        {
            Tile singleStartTile = board.GetTile(start.x, start.y);
            if (singleStartTile != null)
            {
                return new List<Tile> { singleStartTile };
            }
            return null;
        }

        // Diccionarios para almacenar la ruta y el coste de cada tile.
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();

        // Cola de prioridad (basada en tu PriorityQueue)
        PriorityQueue<Vector2Int> frontier = new PriorityQueue<Vector2Int>();
        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            if (current == end)
            {
                // Construimos la lista de Tiles que forman el camino final.
                return ReconstructPath(board, cameFrom, start, end);
            }

            // Obtenemos los vecinos del tile actual
            foreach (Vector2Int next in GetNeighbors(current))
            {
                Tile nextTile = board.GetTile(next.x, next.y);
                if (nextTile == null)
                {
                    continue;
                }

                // Si no se permite una casilla final ocupada, la descartamos
                // a menos que sea exactamente 'end'.
                if (next != end || !allowEndOccupied)
                {
                    // Si la casilla no está disponible, ignorarla
                    if (!isTileAvailable(nextTile))
                    {
                        continue;
                    }
                }

                int newCost = costSoFar[current] + 1;
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    int priority = newCost + Heuristic(next, end);
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }
        // Si el bucle termina, no encontramos camino.
        return null;
    }

    /// <summary>
    /// Retorna la lista de coordenadas (Tiles) que forman el camino, reconstruyendo
    /// desde 'end' hacia 'start', usando el diccionario cameFrom. 
    /// </summary>
    private static List<Tile> ReconstructPath(
        BoardManager board,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int start,
        Vector2Int end)
    {
        List<Tile> path = new List<Tile>();
        Vector2Int current = end;
        while (current != start)
        {
            path.Add(board.GetTile(current.x, current.y));
            current = cameFrom[current];
        }
        path.Add(board.GetTile(start.x, start.y));
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Heurística de distancia para A*: se basa en movimiento octogonal (distancia Chebyshev).
    /// </summary>
    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    /// <summary>
    /// Retorna las coordenadas vecinas (incluyendo diagonales) de un punto 'pos'.
    /// </summary>
    public static List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(pos.x + 1, pos.y),
            new Vector2Int(pos.x - 1, pos.y),
            new Vector2Int(pos.x, pos.y + 1),
            new Vector2Int(pos.x, pos.y - 1),
            new Vector2Int(pos.x + 1, pos.y + 1),
            new Vector2Int(pos.x + 1, pos.y - 1),
            new Vector2Int(pos.x - 1, pos.y + 1),
            new Vector2Int(pos.x - 1, pos.y - 1)
        };
        return neighbors;
    }
}
