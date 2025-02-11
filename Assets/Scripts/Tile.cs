using UnityEngine;

public class Tile : MonoBehaviour {
    public int gridX;
    public int gridY;
    public float tileSize;

    public bool discovered = false;
    private TileObject tileObject;

    private Renderer rend;
    private Collider col; // Añadido para controlar el Collider
    private Color originalColor;
    public Color highlightColor = Color.yellow;

    void Awake() {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>(); // Referencia al Collider
        originalColor = rend.material.color;
    }

    public void SetDiscovered(bool state) {
        if (discovered == state) return; // Evitar llamadas redundantes

        discovered = state;

        // Controlar la visibilidad y la colisión
        if (rend != null)
            rend.enabled = state;   // Oculta o muestra la casilla
        if (col != null)
            col.enabled = state;    // Impide interactuar si está oculta

        // Notificar al objeto asociado
        if (tileObject != null && state && !tileObject.isDiscovered) {
            tileObject.Discover();
        }
    }

    public void AssignTileObject(TileObject obj) {
        tileObject = obj;
        tileObject.AddParentTile(this);
    }

    public void Highlight() {
        if (discovered && rend != null)
            rend.material.color = highlightColor;
    }

    public void UnHighlight() {
        if (rend != null)
            rend.material.color = originalColor;
    }
}
