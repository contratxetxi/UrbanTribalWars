using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Configuración del Tablero")]
    public int rows = 10;                // Número de filas
    public int columns = 10;             // Número de columnas
    public float tileSize = 1.0f;        // Tamaño de cada casilla
    public GameObject tilePrefab;        // Prefab que representa cada casilla

    [HideInInspector]
    public Tile[,] tiles;                // Matriz de casillas generadas

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        tiles = new Tile[columns, rows];

        // Calcular el offset para centrar el tablero
        float offsetX = (columns - 1) / 2f;
        float offsetY = (rows - 1) / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Ajustar la posición del tile con respecto al centro
                Vector3 tilePos = new Vector3(x - offsetX, 0, y - offsetY);
                GameObject tileGO = Instantiate(tilePrefab, tilePos, Quaternion.identity, transform);
                Tile tile = tileGO.GetComponent<Tile>();

                tile.gridX = x;
                tile.gridY = y;
                tile.SetDiscovered(false);
                tiles[x, y] = tile;

                // Vincular objetos en la casilla
                Collider[] objectsOnTile = Physics.OverlapBox(tilePos, new Vector3(0.5f, 0.5f, 0.5f));
                foreach (Collider col in objectsOnTile)
                {
                    TileObject obj = col.GetComponent<TileObject>();
                    if (obj != null)
                    {
                        tile.AssignTileObject(obj);
                    }
                }
            }
        }
    }





    // Devuelve la casilla ubicada en (x, y) o null si está fuera de rango.
    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < columns && y >= 0 && y < rows)
            return tiles[x, y];
        return null;
    }

    // Revela la casilla en (x, y) y sus casillas adyacentes (se puede ajustar el radio de revelado).
    // Revela la casilla en (x, y) y sus casillas adyacentes según el radio de revelado.
    public void RevealTilesAt(int x, int y, int revealRadius = 1)
    {
        for (int i = -revealRadius; i <= revealRadius; i++)
        {
            for (int j = -revealRadius; j <= revealRadius; j++)
            {
                Tile t = GetTile(x + i, y + j);
                if (t != null && !t.discovered)
                {
                    Debug.Log($"Tile({t.gridX}, {t.gridY}) descubierto.");
                    t.SetDiscovered(true);
                }
            }
        }
    }

    // BoardManager.cs

    public bool IsTileBlocked(Tile tile)
    {
        // Comprueba colisiones con la capa Obstaculos
        Vector3 position = tile.transform.position;
        bool hasObstacle = Physics.CheckBox(
            position,
            new Vector3(0.4f, 0.4f, 0.4f),
            Quaternion.identity,
            LayerMask.GetMask("Obstaculos")
        );
        return hasObstacle;
    }

    public bool IsTileOccupied(Tile tile, PlayerController ignore = null)
    {
        // Comprueba colisiones con la capa Players
        Collider[] colliders = Physics.OverlapBox(
            tile.transform.position,
            new Vector3(0.5f, 0.5f, 0.5f),
            Quaternion.identity,
            LayerMask.GetMask("Players")
        );
        foreach (Collider col in colliders)
        {
            PlayerController p = col.GetComponent<PlayerController>();
            if (p != null && p != ignore)
                return true;
        }
        return false;
    }

    public bool IsTileAvailable(Tile tile, PlayerController ignore = null)
    {
        return !IsTileBlocked(tile) && !IsTileOccupied(tile, ignore);
    }

    public bool PathIsClear(List<Tile> path, PlayerController ignore = null, bool ignoreLastTile = false)
    {
        int count = path.Count;
        if (ignoreLastTile)
            count--;

        for (int i = 0; i < count; i++)
        {
            if (!IsTileAvailable(path[i], ignore))
                return false;
        }
        return true;
    }

    public bool IsBoardFullyGenerated()
    {
        if (tiles == null || tiles.Length == 0)
            return false;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (tiles[x, y] == null) // Comprobamos si hay algún tile nulo
                    return false;
            }
        }

        return true; // Si todos los tiles son válidos, el tablero está listo
    }

}
