using UnityEngine;

public class TileObject : MonoBehaviour {
    private Tile parentTile;

    // Asignar el Tile correspondiente al objeto
    public void SetParentTile(Tile tile) {
        parentTile = tile;
        UpdateVisibility();
    }

    void Update() {
        if (parentTile != null) {
            UpdateVisibility();
        }
    }

    // Cambiar la visibilidad del objeto según el estado del Tile
    void UpdateVisibility() {
        gameObject.SetActive(parentTile.discovered);
    }
}
