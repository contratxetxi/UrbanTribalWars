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

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 tilePos = new Vector3(x, 0, y);
                GameObject tileGO = Instantiate(tilePrefab, tilePos, Quaternion.identity, transform);
                Tile tile = tileGO.GetComponent<Tile>();

                tile.gridX = x;
                tile.gridY = y;
                tile.SetDiscovered(false); // Todas las casillas comienzan ocultas
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

}
