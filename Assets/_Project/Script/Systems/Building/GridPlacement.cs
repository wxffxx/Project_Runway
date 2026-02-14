using UnityEngine;
using UnityEngine.InputSystem;

public class GridPlacement : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1.0f;
    public LayerMask groundLayer; 
    public LayerMask obstacleLayer; // Used for build validation (overlap detection)

    [Header("Prefabs")]
    public GameObject buildingPrefab; // Actual building to place
    public GameObject previewPrefab;  // Ghost/Preview model
    
    private GameObject _previewInstance;
    private MeshRenderer _previewRenderer;
    
    [Header("Build Validation")]
    public Color validColor = new Color(0, 1, 0, 0.5f);   // Green
    public Color invalidColor = new Color(1, 0, 0, 0.5f); // Red
    private bool _isValidPosition = true;

    [Header("Drag-to-Build")]
    private Vector3 _startDragPos;
    private bool _isDragging = false;

    void Start()
    {
        if (previewPrefab != null && _previewInstance == null)
        {
            _previewInstance = Instantiate(previewPrefab);
            _previewRenderer = _previewInstance.GetComponentInChildren<MeshRenderer>();
        }
    }

    void Update()
    {
        // 1. Get Mouse Position
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // 2. Raycast to Ground
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Vector3 currentSnappedPos = SnapToGrid(hit.point);

            // 3. Handle Input Logic
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartDragging(currentSnappedPos);
            }

            if (_isDragging)
            {
                UpdateDragPreview(currentSnappedPos);
            }
            else
            {
                UpdateSinglePreview(currentSnappedPos);
            }

            // 4. Finalize Construction
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (_isValidPosition)
                {
                    PlaceConstruction(currentSnappedPos);
                }
                StopDragging();
            }
        }
    }

    void UpdateSinglePreview(Vector3 position)
    {
        if (_previewInstance == null) return;

        _previewInstance.transform.position = position;
        _previewInstance.transform.localScale = Vector3.one;

        ValidatePosition(position, Vector3.one);
    }

    void StartDragging(Vector3 position)
    {
        _isDragging = true;
        _startDragPos = position;
    }

    void UpdateDragPreview(Vector3 currentPos)
    {
        // Calculate center and scale for stretched construction (e.g., runways)
        Vector3 center = (_startDragPos + currentPos) / 2f;
        Vector3 scale = new Vector3(
            Mathf.Abs(currentPos.x - _startDragPos.x) + cellSize,
            1f,
            Mathf.Abs(currentPos.z - _startDragPos.z) + cellSize
        );

        _previewInstance.transform.position = center;
        _previewInstance.transform.localScale = scale;

        ValidatePosition(center, scale);
    }

    void ValidatePosition(Vector3 position, Vector3 scale)
    {
        // Check if the area overlaps with objects on the obstacleLayer
        // Using a slightly smaller box (0.9f) to avoid false positives with adjacent grids
        _isValidPosition = !Physics.CheckBox(position, (scale * 0.9f) / 2f, Quaternion.identity, obstacleLayer);

        // Feedback: Update Shader color
        if (_previewRenderer != null)
        {
            _previewRenderer.material.SetColor("_BaseColor", _isValidPosition ? validColor : invalidColor);
        }
    }

    void PlaceConstruction(Vector3 endPos)
    {
        // Instantiate the actual building with the preview's current transform
        GameObject newBuilding = Instantiate(buildingPrefab, _previewInstance.transform.position, Quaternion.identity);
        newBuilding.transform.localScale = _previewInstance.transform.localScale;

        // Ensure the new building is on the Obstacle layer so it blocks future builds
        newBuilding.layer = (int)Mathf.Log(obstacleLayer.value, 2); 
    }

    void StopDragging()
    {
        _isDragging = false;
        if (_previewInstance) _previewInstance.transform.localScale = Vector3.one;
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;
        return new Vector3(x, 0.1f, z);
    }
}
