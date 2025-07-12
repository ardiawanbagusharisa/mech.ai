using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float dragSpeed = 0.5f;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 10f;
    public float smoothTime = 0.1f;
    private Vector3 dragOrigin;
    private float targetZoom;
    private float zoomVelocity = 0f;

    private Camera cam;
    public Transform gridManager; 

    void Start()
    {
        cam = Camera.main;
        targetZoom = cam.orthographicSize;
        CenterOnGrid();
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, smoothTime);
    }


    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 difference = cam.ScreenToWorldPoint(dragOrigin) - cam.ScreenToWorldPoint(Input.mousePosition);
            difference.y = 0f; // lock Y axis
            transform.position += difference;
            dragOrigin = Input.mousePosition;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    void CenterOnGrid()
    {
        if (gridManager == null || gridManager.childCount == 0)
        {
            Debug.LogWarning("GridManager or its tiles are not initialized.");
            return;
        }

        // Calculate bounds based on all tile children
        Bounds bounds = new Bounds(gridManager.GetChild(0).position, Vector3.zero);

        foreach (Transform tile in gridManager)
        {
            bounds.Encapsulate(tile.position);
        }

        Vector3 center = bounds.center;
        Camera.main.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.z) * 0.6f;
    }
}
