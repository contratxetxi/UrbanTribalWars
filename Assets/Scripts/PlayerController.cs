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

    // Tile del enemigo que se resaltó (si lo hay)
    private Tile highlightedEnemyTile = null;

    private Animator animator;

    IEnumerator Start()
    {
        // Ignorar colisiones entre jugadores (asegúrate que todos los jugadores estén en el layer "Players")
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Players"), LayerMask.NameToLayer("Players"), true);

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
            transform.position = startTile.transform.position + Vector3.up * 0.5f;
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

    // ------------------------------
    // MÉTODOS PARA VERIFICAR DISPONIBILIDAD
    // ------------------------------

    bool IsTileOccupied(Tile tile, PlayerController ignore = null)
    {
        Collider[] colliders = Physics.OverlapBox(tile.transform.position, new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, LayerMask.GetMask("Players"));
        foreach (Collider col in colliders)
        {
            PlayerController p = col.GetComponent<PlayerController>();
            if (p != null && p != ignore)
                return true;
        }
        return false;
    }

    bool IsTileAvailable(Tile tile, PlayerController ignore = null)
    {
        return !IsTileBlocked(tile) && !IsTileOccupied(tile, ignore);
    }

    Tile GetStartTile()
    {
        int startX = -1; 
        int startY = -1;

        if (startX >= 0 && startY >= 0)
        {
            Tile specificTile = board.GetTile(startX, startY);
            if (specificTile != null && IsTileAvailable(specificTile))
            {
                return specificTile;
            }
        }

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

        return null;
    }

    bool IsTileBlocked(Tile tile)
    {
        Vector3 position = tile.transform.position;
        bool hasObstacle = Physics.CheckBox(position, new Vector3(0.4f, 0.4f, 0.4f), Quaternion.identity, LayerMask.GetMask("Obstaculos"));
        return hasObstacle;
    }

    bool PathIsClear(List<Tile> path, bool ignoreLastTile = false)
    {
        int count = path.Count;
        if (ignoreLastTile)
            count--;
        for (int i = 0; i < count; i++)
        {
            if (!IsTileAvailable(path[i], this))
                return false;
        }
        return true;
    }

    PlayerController GetPlayerOnTile(Tile tile)
    {
        Collider[] colliders = Physics.OverlapBox(tile.transform.position, new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, LayerMask.GetMask("Players"));
        foreach (Collider col in colliders)
        {
            PlayerController p = col.GetComponent<PlayerController>();
            if (p != null && p != this)
                return p;
        }
        return null;
    }

    // ------------------------------
    // MÉTODO PARA CALCULAR CAMINO DE ATAQUE
    // ------------------------------
    List<Tile> GetAttackPath(PlayerController target)
    {
        if (Mathf.Max(Mathf.Abs(currentTilePos.x - target.currentTilePos.x), Mathf.Abs(currentTilePos.y - target.currentTilePos.y)) <= 1)
        {
            return new List<Tile>(); // Ya estamos adyacentes.
        }

        List<Tile> bestPath = null;
        int bestLength = int.MaxValue;
        Vector2Int enemyPos = target.currentTilePos;

        List<Vector2Int> neighbors = GetNeighbors(enemyPos);
        foreach (Vector2Int pos in neighbors)
        {
            Tile candidate = board.GetTile(pos.x, pos.y);
            if (candidate == null || !candidate.discovered) continue;
            if (!IsTileAvailable(candidate)) continue;
            List<Tile> path = FindPath(currentTilePos, new Vector2Int(candidate.gridX, candidate.gridY), false);
            if (path != null && (path.Count - 1) <= maxMovement)
            {
                if (path.Count < bestLength)
                {
                    bestLength = path.Count;
                    bestPath = path;
                }
            }
        }
        return bestPath;
    }

    // ------------------------------
    // UPDATE Y GESTIÓN DE INPUT
    // ------------------------------
    void Update()
    {
        if (!isActive) return;

        HandleMouseHover();

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    void HandleMouseHover()
    {
        if (isMoving)
        {
            ClearPathHighlight();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                List<Tile> attackPath = GetAttackPath(targetPlayer);
                if (attackPath != null)
                {
                    ClearPathHighlight();
                    currentPath = attackPath;
                    foreach (Tile t in currentPath)
                    {
                        t.HighlightAttack();
                    }
                    Tile enemyTile = board.GetTile(targetPlayer.currentTilePos.x, targetPlayer.currentTilePos.y);
                    if (enemyTile != null)
                    {
                        enemyTile.HighlightAttack();
                        highlightedEnemyTile = enemyTile;
                    }
                    Vector3 directionToTarget = targetPlayer.transform.position - transform.position;
                    directionToTarget.y = 0f;
                    if (directionToTarget.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                    }
                    return;
                }
            }

            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null && tile.discovered)
            {
                PlayerController targetOnTile = GetPlayerOnTile(tile);
                if (targetOnTile != null)
                {
                    List<Tile> attackPath = GetAttackPath(targetOnTile);
                    if (attackPath != null)
                    {
                        ClearPathHighlight();
                        currentPath = attackPath;
                        foreach (Tile t in currentPath)
                        {
                            t.HighlightAttack();
                        }
                        Tile enemyTile = board.GetTile(targetOnTile.currentTilePos.x, targetOnTile.currentTilePos.y);
                        if (enemyTile != null)
                        {
                            enemyTile.HighlightAttack();
                            highlightedEnemyTile = enemyTile;
                        }
                        Vector3 directionToTarget = targetOnTile.transform.position - transform.position;
                        directionToTarget.y = 0f;
                        if (directionToTarget.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                        }
                        return;
                    }
                }
                else
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
        }
        ClearPathHighlight();
    }

    void HandleMouseClick()
    {
        if (isMoving) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            PlayerController targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != this)
            {
                List<Tile> attackPath = GetAttackPath(targetPlayer);
                if (attackPath != null)
                {
                    StartCoroutine(MoveAndAttack(attackPath, targetPlayer));
                    return;
                }
            }
            else
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null && tile.discovered)
                {
                    PlayerController targetOnTile = GetPlayerOnTile(tile);
                    if (targetOnTile != null)
                    {
                        List<Tile> attackPath = GetAttackPath(targetOnTile);
                        if (attackPath != null)
                        {
                            StartCoroutine(MoveAndAttack(attackPath, targetOnTile));
                            return;
                        }
                    }
                    else
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
    }

    // Movimiento y ataque sin comprobar rango (se ataca siempre que se active el camino de ataque).
    IEnumerator MoveAndAttack(List<Tile> path, PlayerController target)
    {
        isMoving = true;
        animator.SetBool("isWalking", true);

        if (path.Count == 0)
        {
            Vector3 finalDir = target.transform.position - transform.position;
            finalDir.y = 0f;
            if (finalDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(finalDir, Vector3.up);
            }
        }
        else
        {
            foreach (Tile t in path)
            {
                Vector3 targetPos = t.transform.position;
                targetPos.y = transform.position.y;

                while (Vector3.Distance(transform.position, targetPos) > 0.1f)
                {
                    Vector3 direction = targetPos - transform.position;
                    direction.y = 0f;
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

            Vector3 finalDirection = target.transform.position - transform.position;
            finalDirection.y = 0f;
            if (finalDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(finalDirection, Vector3.up);
            }
        }

        animator.SetBool("isWalking", false);
        isMoving = false;

        // Se ataca siempre, independientemente de la distancia, ya que se basa en el tile ocupado.
        StartCoroutine(Attack(target));
    }

    IEnumerator Attack(PlayerController target)
    {
        animator.SetTrigger("AttackTrigger");
        yield return new WaitForSeconds(0.5f);
        target.TakeDamage();
        yield return new WaitForSeconds(0.5f);
    }

    public void TakeDamage()
    {
        animator.SetTrigger("GetDamageTrigger");
        Debug.Log(name + " ha recibido daño.");
    }

    // Movimiento normal.
    IEnumerator MoveAlongPath(List<Tile> path)
    {
        isMoving = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        animator.SetBool("isWalking", true);

        foreach (Tile t in path)
        {
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
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
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
        if (highlightedEnemyTile != null)
        {
            highlightedEnemyTile.UnHighlight();
            highlightedEnemyTile = null;
        }
    }

    // ------------------------------
    // ALGORITMO A* (permitiendo casilla final ocupada para ataques)
    // ------------------------------
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
        return null;
    }

    int Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy);
    }

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
