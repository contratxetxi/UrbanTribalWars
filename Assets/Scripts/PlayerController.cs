using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración del Jugador")]
    internal BoardManager board;          // Referencia al BoardManager
    public float moveSpeed = 5f;          // Velocidad de movimiento de la cápsula
    public int maxMovement = 3;           // Número máximo de casillas que puede recorrer por turno
    public int revealRadius = 6;          // Radio de casillas a descubrir alrededor del jugador

    public bool isActive = false;

    private Vector2Int currentTilePos;    // Posición actual del jugador en la cuadrícula
    private List<Tile> currentPath;         // Camino actualmente resaltado
    private bool isMoving = false;          // Indica si el jugador está en movimiento

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

    // MÉTODOS DE DISPONIBILIDAD DE CASILLAS

    // Comprueba si una casilla está ocupada por otro jugador (ignorando, opcionalmente, al propio)
    bool IsTileOccupied(Tile tile, PlayerController ignore = null)
    {
        Collider[] colliders = Physics.OverlapBox(tile.transform.position, new Vector3(0.3f, 0.3f, 0.3f), Quaternion.identity, LayerMask.GetMask("Players"));
        foreach (Collider col in colliders)
        {
            PlayerController p = col.GetComponent<PlayerController>();
            if (p != null && p != ignore)
                return true;
        }
        return false;
    }

    // Comprueba si la casilla está libre de obstáculos y jugadores.
    bool IsTileAvailable(Tile tile, PlayerController ignore = null)
    {
        return !IsTileBlocked(tile) && !IsTileOccupied(tile, ignore);
    }

    // Se utiliza para generar el tile inicial. Ahora se comprueba que esté disponible.
    Tile GetStartTile()
    {
        int startX = -1; // Coordenadas específicas (opcional)
        int startY = -1;

        if (startX >= 0 && startY >= 0)
        {
            Tile specificTile = board.GetTile(startX, startY);
            if (specificTile != null && IsTileAvailable(specificTile))
            {
                return specificTile;
            }
        }

        // Si no se ha definido una posición, elegir una aleatoria sin obstáculos y sin otro jugador.
        List<Tile> validTiles = new List<Tile>();

        foreach (Tile tile in board.tiles)
        {
            if (tile != null && IsTileAvailable(tile))
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

    // Método original que solo comprobaba obstáculos.
    bool IsTileBlocked(Tile tile)
    {
        Vector3 position = tile.transform.position;
        bool hasObstacle = Physics.CheckBox(position, new Vector3(0.4f, 0.4f, 0.4f), Quaternion.identity, LayerMask.GetMask("Obstaculos"));
        return hasObstacle;
    }

    // Comprueba que cada casilla del camino esté disponible. Para ataques se puede ignorar la última (ocupada por el enemigo).
    bool PathIsClear(List<Tile> path, bool ignoreLastTile = false)
    {
        int count = path.Count;
        if (ignoreLastTile)
            count--; // Se ignora la última casilla (objetivo)
        for (int i = 0; i < count; i++)
        {
            if (!IsTileAvailable(path[i], this))
                return false;
        }
        return true;
    }

    void Update()
    {
        if (!isActive) return;

        HandleMouseHover();
        HandleMouseClick();
    }

    // AL PASAR EL RATÓN: Resalta el camino.
    void HandleMouseHover()
    {
        if (isMoving) return; // No resaltar camino si el jugador está moviéndose

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Si el rayo golpea a otro jugador, se asume que es para ataque.
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                // Permitir que se calcule el camino aunque el tile del enemigo esté ocupado.
                List<Tile> fullPath = FindPath(currentTilePos, targetPlayer.currentTilePos, true);
                if (fullPath != null && fullPath.Count > 1)
                {
                    // Se elimina el último tile (ocupado por el enemigo) para obtener la casilla de ataque.
                    List<Tile> attackPath = new List<Tile>(fullPath);
                    attackPath.RemoveAt(attackPath.Count - 1);
                    if ((attackPath.Count - 1) <= maxMovement && PathIsClear(attackPath))
                    {
                        ClearPathHighlight();
                        currentPath = attackPath;
                        // Se resalta el camino en rojo (suponiendo que Tile tiene HighlightRed())
                        foreach (Tile t in currentPath)
                        {
                            t.HighlightAttack();
                        }
                        return;
                    }
                }
            }

            // Caso de movimiento normal: se ha golpeado una casilla.
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
                        t.HighlightWalk();
                    }
                    return;
                }
            }
        }
        ClearPathHighlight();
    }

    // AL HACER CLIC: Se decide si mover o atacar.
    void HandleMouseClick()
    {
        if (isMoving) return; // No permitir clics si el jugador está moviéndose

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Caso de ataque: se hace clic sobre otro jugador.
                PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
                if (targetPlayer != null && targetPlayer != this)
                {
                    // Permitir que se calcule el camino incluso si la casilla del enemigo está ocupada.
                    List<Tile> fullPath = FindPath(currentTilePos, targetPlayer.currentTilePos, true);
                    if (fullPath != null && fullPath.Count > 1)
                    {
                        List<Tile> attackPath = new List<Tile>(fullPath);
                        attackPath.RemoveAt(attackPath.Count - 1);
                        if ((attackPath.Count - 1) <= maxMovement && PathIsClear(attackPath))
                        {
                            StartCoroutine(MoveAndAttack(attackPath, targetPlayer));
                            return; // Evitar procesar otro caso
                        }
                    }
                }

                // Caso de movimiento normal: se hace clic sobre una casilla.
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && tile.discovered)
                {
                    List<Tile> path = FindPath(currentTilePos, new Vector2Int(tile.gridX, tile.gridY));
                    if (path != null && (path.Count - 1) <= maxMovement && PathIsClear(path))
                    {
                        StartCoroutine(MoveAlongPath(path));
                    }
                }
            }
        }
    }

    IEnumerator MoveAndAttack(List<Tile> path, PlayerController target)
    {
        isMoving = true;
        animator.SetBool("isWalking", true);

        foreach (Tile t in path)
        {
            Vector3 targetPos = t.transform.position;
            targetPos.y = transform.position.y; // Mantener altura

            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                Vector3 direction = targetPos - transform.position;
                direction.y = 0f; // No cambiar la altura

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            currentTilePos = new Vector2Int(t.gridX, t.gridY);
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y, revealRadius);
        }

        animator.SetBool("isWalking", false);
        isMoving = false;

        // Una vez que llegó a la casilla adyacente, iniciar el ataque si el objetivo está a rango.
        if (Vector3.Distance(transform.position, target.transform.position) <= 1.5f)
        {
            StartCoroutine(Attack(target));
        }
    }

    IEnumerator Attack(PlayerController target)
    {
        animator.SetTrigger("AttackTrigger");
        yield return new WaitForSeconds(0.5f); // Duración de la animación de ataque

        if (Vector3.Distance(transform.position, target.transform.position) <= 1.5f)
        {
            target.TakeDamage();
        }
        yield return new WaitForSeconds(0.5f);
    }

    public void TakeDamage()
    {
        animator.SetTrigger("GetDamageTrigger");
        Debug.Log(name + " ha recibido daño.");
    }

    IEnumerator MoveAlongPath(List<Tile> path)
    {
        isMoving = true;
        Rigidbody rb = GetComponent<Rigidbody>();

        animator.SetBool("isWalking", true);

        foreach (Tile t in path)
        {
            // Revalida que la casilla sigue libre antes de movernos a ella.
            if (!IsTileAvailable(t, this))
            {
                Debug.Log("El camino se ha bloqueado en la casilla (" + t.gridX + ", " + t.gridY + "). Movimiento cancelado.");
                animator.SetBool("isWalking", false);
                isMoving = false;
                ClearPathHighlight();
                yield break;
            }

            Vector3 targetPos = t.transform.position;
            targetPos.y = transform.position.y;

            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                Vector3 direction = targetPos - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    float rotationSpeed = 10f;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                rb.MovePosition(newPos);
                yield return null;
            }

            rb.MovePosition(targetPos);
            currentTilePos = new Vector2Int(t.gridX, t.gridY);
            board.RevealTilesAt(currentTilePos.x, currentTilePos.y, revealRadius);
        }

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

    // IMPLEMENTACIÓN DEL ALGORITMO A* CON OPCIÓN DE PERMITIR CASILLA FINAL OCUPADA (para ataques)
    List<Tile> FindPath(Vector2Int start, Vector2Int end, bool allowEndOccupied = false)
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
                if (nextTile == null)
                    continue;

                // Si el vecino no es la casilla final o no se permite que esté ocupado, se debe estar disponible.
                if (next != end || !allowEndOccupied)
                {
                    if (!IsTileAvailable(nextTile))
                        continue;
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

        return null;  // No se encontró un camino válido
    }

    // Calcula la distancia en 8 direcciones.
    int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

    // Retorna las casillas vecinas (en 8 direcciones) de una posición dada.
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int> {
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
