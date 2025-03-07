using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Para la UI

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public BoardManager boardManager;
    public CameraController cameraController;
    
    // Referencia al objeto UI para mostrar información del turno actual (opcional)
    public Text turnInfoText;

    private PlayerController[] players = new PlayerController[2];
    private int currentPlayerIndex = 0;

    void Start()
    {
        StartCoroutine(InitializePlayers());
    }

    IEnumerator InitializePlayers()
    {
        // Esperar hasta que el tablero esté realmente generado.
        while (boardManager.tiles == null ||
               boardManager.tiles.GetLength(0) == 0 ||
               boardManager.tiles.GetLength(1) == 0)
        {
            yield return null;
        }

        // 1) Instanciar al primer jugador
        Tile startTile1 = GetStartTile();
        if (startTile1 != null)
        {
            GameObject player1GO = Instantiate(
                playerPrefab,
                startTile1.transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );

            players[0] = player1GO.GetComponent<PlayerController>();
            players[0].SetBoardManager(boardManager);
            player1GO.name = "Jugador 1";
        }

        // 2) Instanciar al segundo jugador
        Tile startTile2 = GetStartTile();
        if (startTile2 != null)
        {
            GameObject player2GO = Instantiate(
                playerPrefab,
                startTile2.transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );

            players[1] = player2GO.GetComponent<PlayerController>();
            players[1].SetBoardManager(boardManager);
            player2GO.name = "Jugador 2";
        }

        // 3) Iniciar el primer turno
        StartPlayerTurn(0);

        if (cameraController != null)
        {
            cameraController.SetPlayer(players[0].transform);
        }
        
        // Actualizar información del turno en UI
        UpdateTurnInfo();
    }

    Tile GetStartTile()
    {
        // Buscar una casilla válida en el tablero
        foreach (Tile tile in boardManager.tiles)
        {
            if (tile != null && !IsTileBlocked(tile))
            {
                return tile;
            }
        }
        return null; // No encontró tiles válidos
    }

    bool IsTileBlocked(Tile tile)
    {
        Vector3 position = tile.transform.position;
        return Physics.CheckBox(position, new Vector3(0.4f, 0.4f, 0.4f), Quaternion.identity, LayerMask.GetMask("Obstaculos"));
    }

    void Update()
    {
        // Mantener opción de terminar turno manualmente con la tecla T
        if (Input.GetKeyDown(KeyCode.T))
        {
            EndTurn();
        }
    }

    // Método que inicia el turno de un jugador específico
    private void StartPlayerTurn(int playerIndex)
    {
        currentPlayerIndex = playerIndex;
        
        // Activar al jugador del turno actual
        for (int i = 0; i < players.Length; i++)
        {
            if (i == currentPlayerIndex)
            {
                players[i].StartTurn();  // Nuevo método en PlayerController
            }
            else
            {
                players[i].isActive = false;
            }
        }
        
        // Mover la cámara al jugador activo
        if (cameraController != null)
        {
            cameraController.SetPlayer(players[currentPlayerIndex].transform);
        }
        
        // Actualizar UI
        UpdateTurnInfo();
        
        Debug.Log("Comienza el turno del " + players[currentPlayerIndex].name);
    }

    // Cambia el turno al otro jugador - ahora es público para ser llamado desde PlayerController
    public void EndTurn()
    {
        // Desactivar al jugador actual
        players[currentPlayerIndex].isActive = false;

        // Cambiar el índice (si era 0, pasa a 1; si era 1, pasa a 0)
        currentPlayerIndex = 1 - currentPlayerIndex;

        // Iniciar turno del nuevo jugador
        StartPlayerTurn(currentPlayerIndex);
        
        Debug.Log("Cambio de turno al " + players[currentPlayerIndex].name);
    }
    
    // Actualiza la información del turno en la UI
    private void UpdateTurnInfo()
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = "Turno: " + players[currentPlayerIndex].name;
        }
    }
}
