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

    // Genera la cuadrícula de casillas y las posiciona sobre el plano.
    void GenerateBoard()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("TilePrefab no está asignado en el BoardManager.");
            return;
        }

        tiles = new Tile[columns, rows];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 tilePos = new Vector3(x, 0, y);
                GameObject tileGO = Instantiate(tilePrefab, tilePos, Quaternion.identity, transform);
                Tile tile = tileGO.GetComponent<Tile>();

                if (tile == null)
                {
                    Debug.LogError($"Tile prefab no tiene el script Tile.cs en ({x},{y}).");
                    continue;
                }

                tile.gridX = x;
                tile.gridY = y;
                tile.SetDiscovered(false);
                tiles[x, y] = tile;

                // Vincular cualquier objeto que esté en esa casilla
                Collider[] objectsOnTile = Physics.OverlapBox(tilePos, new Vector3(tileSize / 2, 0.5f, tileSize / 2));
                foreach (Collider col in objectsOnTile)
                {
                    TileObject obj = col.GetComponent<TileObject>();
                    if (obj != null)
                    {
                        obj.SetParentTile(tile);
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
    public void RevealTilesAt(int x, int y)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Tile t = GetTile(x + i, y + j);
                if (t != null && !t.discovered)
                {
                    t.SetDiscovered(true);
                }
            }
        }
    }
}
