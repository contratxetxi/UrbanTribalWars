using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración del Jugador")]
    internal BoardManager board;          // Referencia al BoardManager
    public float moveSpeed = 5f;        // Velocidad de movimiento de la cápsula
    public int maxMovement = 3;         // Número máximo de casillas que puede recorrer por turno
    public int revealRadius = 3;        // Radio de casillas a descubrir alrededor del jugador

    private Vector2Int currentTilePos;  // Posición actual del jugador en la cuadrícula
    private List<Tile> currentPath;     // Camino actualmente resaltado
    private bool isMoving = false; // Indica si el jugador está en movimiento

    private Animator animator;

    IEnumerator Start()
    {
        // Esperar hasta que el tablero esté generado
        while (board.tiles == null)
        {
            yield return null;
        }

        animator = GetComponent<Animator>();

        // Obtener una casilla válida
        Tile startTile = GetStartTile();

        if (startTile != null)
        {
            currentTilePos = new Vector2Int(startTile.gridX, startTile.gridY);
            startTile.SetDiscovered(true);
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y, revealRadius);

            // Ajustar la posición del jugador al tile seleccionado
            transform.position = startTile.transform.position + Vector3.up * 0.5f; // Levanta el jugador un poco sobre el tile
        }
        else
        {
            Debug.LogError("No se pudo encontrar una casilla válida para generar al jugador.");
        }
    }

    public void SetBoardManager(BoardManager boardManager)
    {
        board = boardManager;
    }


    Tile GetStartTile()
    {
        int startX = -1; // Coordenadas específicas (opcional)
        int startY = -1;

        // Si se ha definido una posición específica, validarla antes de usarla.
        if (startX >= 0 && startY >= 0)
        {
            Tile specificTile = board.GetTile(startX, startY);
            if (specificTile != null && !IsTileBlocked(specificTile))
            {
                return specificTile;
            }
        }

        // Si no se ha definido una posición, elegir una aleatoria sin obstáculos
        List<Tile> validTiles = new List<Tile>();

        foreach (Tile tile in board.tiles)
        {
            if (tile != null && !IsTileBlocked(tile)) // Solo agregar tiles sin obstáculos
            {
                validTiles.Add(tile);
            }
        }

        if (validTiles.Count > 0)
        {
            return validTiles[Random.Range(0, validTiles.Count)];
        }

        return null; // No hay tiles válidos disponibles
    }

    bool IsTileBlocked(Tile tile)
    {
        Vector3 position = tile.transform.position;
        bool hasObstacle = Physics.CheckBox(position, new Vector3(0.4f, 0.4f, 0.4f), Quaternion.identity, LayerMask.GetMask("Obstaculos"));

        return hasObstacle; // Devuelve true si hay un obstáculo en la casilla
    }


    void Update()
    {
        HandleMouseHover();
        HandleMouseClick();
    }

    // Al mover el ratón, se detecta sobre qué casilla se posiciona y se calcula el camino desde la posición actual.
    void HandleMouseHover()
    {
        if (isMoving) return; // No resaltar camino si el jugador está moviéndose

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null && tile.discovered)
            {
                List<Tile> path = FindPath(currentTilePos, new Vector2Int(tile.gridX, tile.gridY));
                if (path != null && (path.Count - 1) <= maxMovement && PathIsClear(path))
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
        if (isMoving) return; // No permitir clics si el jugador está moviéndose

        if (Input.GetMouseButtonDown(0) && currentPath != null && currentPath.Count > 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && tile.discovered)
                {
                    if (tile.gridX == currentPath[currentPath.Count - 1].gridX &&
                        tile.gridY == currentPath[currentPath.Count - 1].gridY)
                    {
                        StartCoroutine(MoveAlongPath(currentPath));
                    }
                }
            }
        }
    }


    // Verifica si el camino está libre de obstáculos.
    bool PathIsClear(List<Tile> path)
    {
        foreach (Tile t in path)
        {
            if (IsTileBlocked(t))
            {
                return false;  // Si hay un obstáculo en este tile, el camino no es válido
            }
        }
        return true;  // El camino está libre
    }

    IEnumerator MoveAlongPath(List<Tile> path)
    {
        isMoving = true;
        Rigidbody rb = GetComponent<Rigidbody>();

        animator.SetBool("isWalking", true);

        foreach (Tile t in path)
        {
            Vector3 targetPos = t.transform.position;
            // Mantén la altura del jugador
            targetPos.y = transform.position.y;

            // Mientras no lleguemos al tile
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                // 1) Calcular dirección “plana” (sin inclinaciones en Y)
                Vector3 direction = targetPos - transform.position;
                direction.y = 0f; // Evitar inclinación vertical

                // 2) Rotación suave cada frame
                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    // Ajusta rotationSpeed según la rapidez que quieras para girar
                    float rotationSpeed = 10f;
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }

                // 3) Movimiento suave hacia el objetivo
                Vector3 newPos = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );
                rb.MovePosition(newPos);

                // Esperar 1 frame
                yield return null;
            }

            // Asegurarnos de la posición final exacta
            rb.MovePosition(targetPos);

            // Actualizar posicion en grid
            currentTilePos = new Vector2Int(t.gridX, t.gridY);
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y, revealRadius);
        }

        // Pequeña pausa antes de desactivar la animación
        yield return new WaitForSeconds(0.1f);

        animator.SetBool("isWalking", false);
        isMoving = false;
        ClearPathHighlight();
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
            List<Tile> singlePath = new List<Tile> { board.GetTile(start.x, start.y) };
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
                if (nextTile == null || IsTileBlocked(nextTile))
                    continue; // Saltar si hay un obstáculo

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

        return null;  // No se encontró un camino válido
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
