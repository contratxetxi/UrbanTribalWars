using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;

    public BoardManager boardManager;

    public CameraController cameraController;

    // private GameObject playerInstance;

    private PlayerController[] players = new PlayerController[2];
    private int currentPlayerIndex = 0;

    void Start()
    {
        // StartCoroutine(WaitForBoardAndSpawnPlayer());
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
        }

        // 2) Instanciar al segundo jugador
        //    (Podrías usar otro prefab, o el mismo. Aquí uso el mismo para simplificar)
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
        }

        // 3) Activar sólo al primer jugador y situar la cámara sobre él
        players[0].isActive = true;
        players[1].isActive = false;

        if (cameraController != null)
        {
            cameraController.SetPlayer(players[0].transform);
        }
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
        // Como ejemplo: si se pulsa la tecla "T", se acaba el turno actual.
        if (Input.GetKeyDown(KeyCode.T))
        {
            EndTurn();
        }
    }

    // Cambia el turno al otro jugador
    void EndTurn()
    {
        // Desactivar al jugador actual
        players[currentPlayerIndex].isActive = false;

        // Cambiar el índice (si era 0, pasa a 1; si era 1, pasa a 0)
        currentPlayerIndex = 1 - currentPlayerIndex;

        // Activar al nuevo jugador
        players[currentPlayerIndex].isActive = true;

        // Mover la cámara al jugador que entra en turno
        if (cameraController != null)
        {
            cameraController.SetPlayer(players[currentPlayerIndex].transform);
        }
    }

}

