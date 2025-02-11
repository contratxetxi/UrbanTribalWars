using UnityEngine;
using System.Collections.Generic;

public class TileObject : MonoBehaviour {
    private List<Tile> parentTiles = new List<Tile>();
    public bool isDiscovered = false;

    public void AddParentTile(Tile tile) {
        if (!parentTiles.Contains(tile)) {
            parentTiles.Add(tile);
            UpdateVisibility();
        }
    }

    public void Discover() {
        if (isDiscovered) return;
        isDiscovered = true;

        // Descubre todos los tiles asociados
        foreach (Tile tile in parentTiles) {
            if (!tile.discovered) {
                tile.SetDiscovered(true);
            }
        }

        UpdateVisibility();
    }

    private void UpdateVisibility() {
        // Solo visible si al menos uno de sus tiles está descubierto
        bool visible = false;
        foreach (Tile tile in parentTiles) {
            if (tile.discovered) {
                visible = true;
                break;
            }
        }
        gameObject.SetActive(visible); // Oculta el objeto si todos los tiles están ocultos
    }
}
