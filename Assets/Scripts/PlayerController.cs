using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración del Jugador")]
    public BoardManager board;          // Referencia al BoardManager
    public float moveSpeed = 5f;        // Velocidad de movimiento de la cápsula
    public int maxMovement = 3;         // Número máximo de casillas que puede recorrer por turno

    private Vector2Int currentTilePos;  // Posición actual del jugador en la cuadrícula
    private List<Tile> currentPath;     // Camino actualmente resaltado

    IEnumerator  Start()
    {
        // Esperar hasta que el tablero esté generado
        while (board.tiles == null)
        {
            yield return null;
        }

        currentTilePos = new Vector2Int(0, 0);

        // Espera a que el tablero esté generado
        if (board.tiles != null && board.GetTile(currentTilePos.x, currentTilePos.y) != null)
        {
            transform.position = board.GetTile(currentTilePos.x, currentTilePos.y).transform.position;
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y);
        }
        else
        {
            Debug.LogError("El tablero no se ha generado correctamente.");
        }

    }

    void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
    }

    // Al mover el ratón, se detecta sobre qué casilla se posiciona y se calcula el camino desde la posición actual.
    void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null && tile.discovered)
            {
                List<Tile> path = FindPath(currentTilePos, new Vector2Int(tile.gridX, tile.gridY));
                // Se comprueba que el camino exista y que la cantidad de casillas a recorrer no exceda el límite.
                if (path != null && (path.Count - 1) <= maxMovement)
                {
                    ClearPathHighlight();
                    currentPath = path;
                    foreach (Tile t in currentPath)
                    {
                        t.Highlight();
                    }
                    return;
                }
            }
        }
        ClearPathHighlight();
    }

    // Al hacer clic se verifica que la casilla clickeada sea el destino del camino resaltado.
    void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0) && currentPath != null && currentPath.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && tile.discovered)
                {
                    // Si la casilla clickeada es el último nodo del camino resaltado, se inicia el movimiento.
                    if (tile.gridX == currentPath[currentPath.Count - 1].gridX &&
                        tile.gridY == currentPath[currentPath.Count - 1].gridY)
                    {
                        StartCoroutine(MoveAlongPath(currentPath));
                    }
                }
            }
        }
    }

    // Movimiento animado a lo largo del camino calculado.
    IEnumerator MoveAlongPath(List<Tile> path)
    {
        foreach (Tile t in path)
        {
            Vector3 targetPos = t.transform.position;
            // Se mantiene la altura actual del jugador
            targetPos.y = transform.position.y;
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            currentTilePos = new Vector2Int(t.gridX, t.gridY);
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y);
        }
        ClearPathHighlight();
        yield return null;
    }

    // Quita el resaltado de las casillas del camino actual.
    void ClearPathHighlight()
    {
        if (currentPath != null)
        {
            foreach (Tile t in currentPath)
            {
                t.UnHighlight();
            }
            currentPath = null;
        }
    }

    // Implementación de un algoritmo A* para encontrar el camino en la cuadrícula.
    List<Tile> FindPath(Vector2Int start, Vector2Int end)
    {
        if (start == end)
        {
            List<Tile> singlePath = new List<Tile>();
            Tile startTile = board.GetTile(start.x, start.y);
            singlePath.Add(startTile);
            return singlePath;
        }
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
        PriorityQueue<Vector2Int> frontier = new PriorityQueue<Vector2Int>();
        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            if (current == end)
            {
                List<Tile> path = new List<Tile>();
                Vector2Int cur = end;
                while (cur != start)
                {
                    path.Add(board.GetTile(cur.x, cur.y));
                    cur = cameFrom[cur];
                }
                path.Add(board.GetTile(start.x, start.y));
                path.Reverse();
                return path;
            }
            foreach (Vector2Int next in GetNeighbors(current))
            {
                Tile nextTile = board.GetTile(next.x, next.y);
                if (nextTile == null || !nextTile.discovered)
                    continue; // Solo se pueden transitar casillas descubiertas
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
        return null;  // No se encontró camino
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Retorna las casillas vecinas (en 4 direcciones) de una posición dada.
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int> {
            new Vector2Int(pos.x + 1, pos.y),
            new Vector2Int(pos.x - 1, pos.y),
            new Vector2Int(pos.x, pos.y + 1),
            new Vector2Int(pos.x, pos.y - 1)
        };
        return neighbors;
    }
}
