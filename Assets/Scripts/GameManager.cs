using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;

    public BoardManager boardManager;

    public CameraController cameraController;

    private GameObject playerInstance;

    void Start()
    {
        StartCoroutine(WaitForBoardAndSpawnPlayer());
    }

    IEnumerator WaitForBoardAndSpawnPlayer()
    {
        // Esperar hasta que el tablero esté generado correctamente
        while (boardManager.tiles == null || boardManager.tiles.GetLength(0) == 0 || boardManager.tiles.GetLength(1) == 0)
        {
            yield return null; // Esperar un frame
        }

        // Buscar un tile inicial en el tablero
        Tile startTile = GetStartTile();
        if (startTile == null)
        {
            Debug.LogError("No se encontró una casilla válida para el spawn.");
            yield break;
        }

        // Instanciar al jugador en la casilla inicial
        playerInstance = Instantiate(playerPrefab, startTile.transform.position + Vector3.up * 0.5f, Quaternion.identity);

        // Asignar el BoardManager al jugador
        PlayerController playerController = playerInstance.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetBoardManager(boardManager);
        }

        // Asignar la cámara al jugador
        if (cameraController != null)
        {
            cameraController.SetPlayer(playerInstance.transform);
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



}

