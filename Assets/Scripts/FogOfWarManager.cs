using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Terreno (plano) del que se obtiene el tamaño automáticamente")]
    public Transform terrainTransform;

    [Header("Tamaño de cada celda en unidades del mundo")]
    public float tileSize = 1f;

    [Header("Material para la niebla (shader que soporte transparencia, ej: Unlit/Transparent)")]
    public Material fogMaterial;

    [Header("Radio de revelado por defecto (en celdas)")]
    public int revealRadius = 2;

    private Texture2D fogTexture;
    private Color32[] fogPixels;
    private int texWidth;
    private int texHeight;

    private Bounds terrainBounds;
    private float offsetY = 0.1f;

    private GameObject fogQuad;
    private GameObject highlightQuad;

    void Start()
    {
        if (terrainTransform == null)
        {
            Debug.LogError("No se ha asignado el terreno (terrainTransform).");
            return;
        }
        SetupFog();
    }

    void CreateGridOverlay()
    {
        // 1. Creamos un objeto vacío para la malla de líneas
        GameObject gridOverlay = new GameObject("GridOverlay");
        gridOverlay.transform.SetParent(transform);

        // 2. Agregamos los componentes de malla
        MeshFilter mf = gridOverlay.AddComponent<MeshFilter>();
        MeshRenderer mr = gridOverlay.AddComponent<MeshRenderer>();

        // 3. Creamos la malla para las líneas
        Mesh mesh = new Mesh();
        mesh.name = "GridLinesMesh";

        // Calculamos el área donde dibujaremos las líneas
        float startX = terrainBounds.min.x;
        float startZ = terrainBounds.min.z;
        float endX = terrainBounds.min.x + texWidth * tileSize;
        float endZ = terrainBounds.min.z + texHeight * tileSize;

        // Cantidad de líneas: verticales (texWidth+1) y horizontales (texHeight+1)
        int verticalLines = texWidth + 1;
        int horizontalLines = texHeight + 1;
        // Cada línea necesita 2 vértices, por lo que:
        int totalLines = verticalLines + horizontalLines;
        int vertexCount = totalLines * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] indices = new int[vertexCount];

        int idx = 0;

        // Líneas verticales
        for (int i = 0; i < verticalLines; i++)
        {
            float x = startX + i * tileSize;

            // Primer vértice de la línea
            vertices[idx] = new Vector3(
                x,
                terrainBounds.max.y + offsetY + 0.02f,
                startZ
            );
            indices[idx] = idx;
            idx++;

            // Segundo vértice
            vertices[idx] = new Vector3(
                x,
                terrainBounds.max.y + offsetY + 0.02f,
                endZ
            );
            indices[idx] = idx;
            idx++;
        }

        // Líneas horizontales
        for (int j = 0; j < horizontalLines; j++)
        {
            float z = startZ + j * tileSize;

            // Primer vértice
            vertices[idx] = new Vector3(
                startX,
                terrainBounds.max.y + offsetY + 0.02f,
                z
            );
            indices[idx] = idx;
            idx++;

            // Segundo vértice
            vertices[idx] = new Vector3(
                endX,
                terrainBounds.max.y + offsetY + 0.02f,
                z
            );
            indices[idx] = idx;
            idx++;
        }

        // Asignamos vértices e índices a la malla
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        // Asignar la malla al MeshFilter
        mf.mesh = mesh;

        // 4. Creamos un material semitransparente para las líneas
        Material gridMat = new Material(Shader.Find("Unlit/Transparent"));
        // Color blanco con alpha=0.25 (ajusta a gusto)
        gridMat.color = new Color(1f, 1f, 1f, 0.25f);
        // Opcional: hacer que se pinte por encima de la niebla
        gridMat.renderQueue = 2000; // un poco por encima de la niebla (3000)

        mr.material = gridMat;

        // Si prefieres que la cuadrícula quede POR DEBAJO de la niebla
        // (y la niebla la cubra en las zonas no reveladas),
        // podrías setear un renderQueue menor, p.ej. 2000 (Geometry)
        // gridMat.renderQueue = 2000;
    }


    /// <summary>
    /// Crea un material con un shader URP Unlit o Unlit/Transparent en el pipeline estándar.
    /// Ajusta el color para que la textura se muestre correctamente (blanco/1,1,1,1) si es transparente.
    /// </summary>
    private Material CreateCompatibleMaterial(bool transparent)
    {
        Material mat;

        // Detección de pipeline
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            // URP
            Shader shader = Shader.Find("Custom/FoWShader");
            mat = new Material(shader);
            // Forzamos el modo transparente
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent (en URP)
            mat.renderQueue = 3000;       // Transparent queue
            mat.SetFloat("_AlphaClip", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            // Built-in pipeline
            Shader shader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Color");
            mat = new Material(shader);
            // Forzamos la cola de render para transparencia
            if (transparent)
            {
                mat.renderQueue = 3000;
            }
        }

        // Si es transparente, ponemos el color en blanco opaco (1,1,1,1)
        // para no tintar la textura.
        // Si no es transparente, lo dejamos en blanco por defecto.
        if (transparent)
            mat.color = new Color(1f, 1f, 1f, 1f);
        else
            mat.color = Color.white;

        return mat;
    }

    void SetupFog()
    {
        MeshRenderer terrainMR = terrainTransform.GetComponent<MeshRenderer>();
        if (terrainMR == null)
        {
            Debug.LogError("El terreno no tiene MeshRenderer.");
            return;
        }
        terrainBounds = terrainMR.bounds;

        // Calculamos el numero de celdas
        texWidth = Mathf.FloorToInt(terrainBounds.size.x / tileSize);
        texHeight = Mathf.FloorToInt(terrainBounds.size.z / tileSize);

        if (texWidth <= 0 || texHeight <= 0)
        {
            Debug.LogError("El tamaño calculado de la niebla es inválido. Revisa el tileSize y el tamaño del terreno.");
            return;
        }

        // Creamos la textura de la niebla
        fogTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        // Rellenamos con negro opaco (alpha=255)
        fogPixels = new Color32[texWidth * texHeight];
        for (int i = 0; i < fogPixels.Length; i++)
        {
            fogPixels[i] = new Color32(0, 0, 0, 255);
        }
        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();

        CreateFogQuad();
        CreateHighlightQuad();
        CreateGridOverlay();
    }

    void CreateFogQuad()
    {
        fogQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fogQuad.name = "FogOfWarQuad";
        fogQuad.transform.SetParent(transform);

        // Girarlo para que mire hacia abajo
        fogQuad.transform.eulerAngles = new Vector3(90f, 0f, 0f);

        // Posicionarlo sobre el terreno
        fogQuad.transform.position = new Vector3(
            terrainBounds.center.x,
            terrainBounds.max.y + offsetY,
            terrainBounds.center.z
        );

        // Escalarlo para que cubra el terreno
        fogQuad.transform.localScale = new Vector3(
            terrainBounds.size.x,
            terrainBounds.size.z,
            1f
        );

        MeshRenderer quadMR = fogQuad.GetComponent<MeshRenderer>();
        quadMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadMR.receiveShadows = false;

        // Si no se asigna un fogMaterial en el inspector, creamos uno transparente
        if (fogMaterial == null)
        {
            fogMaterial = CreateCompatibleMaterial(true);
        }
        quadMR.material = fogMaterial;

        // Asignamos la textura
        quadMR.material.mainTexture = fogTexture;
        quadMR.material.SetTexture("_MainTex", fogTexture);
    }

    void CreateHighlightQuad()
    {
        highlightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightQuad.name = "HighlightQuad";
        highlightQuad.transform.SetParent(transform);
        highlightQuad.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        highlightQuad.transform.localScale = new Vector3(tileSize, tileSize, 1f);

        // Material para el highlight con color amarillo semitransparente
        Material hlMat = CreateCompatibleMaterial(true);
        hlMat.color = new Color(1f, 1f, 0f, 0.5f);
        MeshRenderer hlMR = highlightQuad.GetComponent<MeshRenderer>();
        hlMR.material = hlMat;
        hlMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hlMR.receiveShadows = false;

        highlightQuad.SetActive(false);
    }

    /// <summary>
    /// Revela un área alrededor de worldPos, con un radio en celdas.
    /// Pone alpha=0 en esas celdas para que se vea transparente.
    /// </summary>
    public void RevealArea(Vector3 worldPos, int? overrideRadius = null)
    {
        if (fogTexture == null || fogPixels == null)
        {
            Debug.LogWarning("La niebla no está inicializada.");
            return;
        }

        int r = overrideRadius.HasValue ? overrideRadius.Value : revealRadius;
        int centerX = Mathf.FloorToInt((worldPos.x - terrainBounds.min.x) / tileSize);
        int centerZ = Mathf.FloorToInt((worldPos.z - terrainBounds.min.z) / tileSize);

        // Cambiamos alpha=0 para las celdas en el radio
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dz = -r; dz <= r; dz++)
            {
                int px = centerX + dx;
                int pz = centerZ + dz;
                if (px >= 0 && px < texWidth && pz >= 0 && pz < texHeight)
                {
                    int index = pz * texWidth + px;
                    fogPixels[index] = new Color32(0, 0, 0, 0);
                }
            }
        }

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply();

        // Actualizamos la textura en el material
        fogQuad.GetComponent<MeshRenderer>().material.mainTexture = fogTexture;
    }

    void Update()
    {
        UpdateHighlight();
    }

    /// <summary>
    /// Resalta la celda bajo el ratón si está revelada (alpha=0).
    /// </summary>
    void UpdateHighlight()
    {
        if (terrainTransform == null || fogTexture == null)
            return;

        Plane plane = new Plane(Vector3.up, terrainBounds.min);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            int cellX = Mathf.FloorToInt((hitPoint.x - terrainBounds.min.x) / tileSize);
            int cellZ = Mathf.FloorToInt((hitPoint.z - terrainBounds.min.z) / tileSize);

            if (cellX >= 0 && cellX < texWidth && cellZ >= 0 && cellZ < texHeight)
            {
                int index = cellZ * texWidth + cellX;
                // Si alpha=0 => celda descubierta
                if (fogPixels[index].a == 0)
                {
                    highlightQuad.transform.position = new Vector3(
                        terrainBounds.min.x + (cellX + 0.5f) * tileSize,
                        terrainBounds.max.y + offsetY + 0.03f,
                        terrainBounds.min.z + (cellZ + 0.5f) * tileSize
                    );
                    highlightQuad.SetActive(true);
                    return;
                }
            }
        }
        highlightQuad.SetActive(false);
    }
}
