//CameraController.cs 
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool stableYaw = true;
    public float dragSpeed = 0.5f;
    private Vector3 lastMousePositionDrag;

    public float rotationSpeed = 0.2f;
    public float minPitchAngle = 5f;
    public float maxPitchAngle = 85f;
    public float orbitDistance = 10f;
    private Vector3 lookAtPoint;
    private float currentYaw;
    private float currentPitch;
    private Vector3 lastMousePositionRotation;

    private float snapTargetYaw;
    public float snapRotateSmoothTime = 0.3f;
    private float snapYawVelocity;


    public float zoomSpeed = 5f;
    public float minFOV = 10f;
    public float maxFOV = 60f;
    public float smoothTime = 0.1f;
    private float targetFOV;
    private float fovVelocity = 0f;

    public float focusSmoothTime = 0.5f;
    public float focusOrbitDistance = 15f;

    private Vector3 smoothTargetLookAtPoint;
    private float smoothTargetYaw;
    private float smoothTargetPitch;
    private float smoothTargetOrbitDistance;

    private Vector3 lookAtPointVelocity;
    private float yawVelocity;
    private float pitchVelocity;
    private float orbitDistanceVelocity;

    private Camera cam;
    public Transform gridManager;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found! Please ensure your camera is tagged 'MainCamera'.");
            enabled = false;
            return;
        }

        cam.orthographic = false;

        targetFOV = cam.fieldOfView;
        targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);

        Vector3 currentEuler = transform.eulerAngles;
        currentYaw = currentEuler.y;
        currentPitch = currentEuler.x;
        if (currentPitch > 180) currentPitch -= 360;
        currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);
        snapTargetYaw = currentYaw;

        smoothTargetLookAtPoint = lookAtPoint;
        smoothTargetYaw = currentYaw;
        smoothTargetPitch = currentPitch;
        smoothTargetOrbitDistance = orbitDistance;

        CenterOnGrid();

        //UpdateCameraPositionAndRotation();
    }

    void Update()
    {
        lookAtPoint = Vector3.SmoothDamp(lookAtPoint, smoothTargetLookAtPoint, ref lookAtPointVelocity, focusSmoothTime);
        if (stableYaw)
        {
            // Smooth snapping yaw only, fixed pitch
            currentYaw = Mathf.SmoothDampAngle(currentYaw, snapTargetYaw, ref snapYawVelocity, snapRotateSmoothTime);
            // Optionally fix pitch as well, e.g. isometric
            currentPitch = 45f;
        }
        else
        {
            // Smooth towards target yaw/pitch from mouse input
            currentYaw = Mathf.SmoothDampAngle(currentYaw, smoothTargetYaw, ref yawVelocity, focusSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, smoothTargetPitch, ref pitchVelocity, focusSmoothTime);
        }
        orbitDistance = Mathf.SmoothDamp(orbitDistance, smoothTargetOrbitDistance, ref orbitDistanceVelocity, focusSmoothTime);

        HandleZoom();

        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFOV, ref fovVelocity, smoothTime);

        if (Input.GetKeyDown(KeyCode.F))
            HandleFocusOnRobot();

        if (stableYaw)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                SnapRotateLeft();

            if (Input.GetKeyDown(KeyCode.E))
                SnapRotateRight();
        }

        UpdateCameraPositionAndRotation();
    }

    void LateUpdate()
    {
        HandleCameraDrag();
        HandleCameraRotation();
    }

    void SnapRotateLeft()
    {
        snapTargetYaw -= 90f;
        snapTargetYaw = Mathf.Round(snapTargetYaw / 90f) * 90f;
    }

    void SnapRotateRight()
    {
        snapTargetYaw += 90f;
        snapTargetYaw = Mathf.Round(snapTargetYaw / 90f) * 90f;
    }

    void HandleCameraDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePositionDrag = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Plane plane = new Plane(Vector3.up, lookAtPoint.y);

            Ray startRay = cam.ScreenPointToRay(lastMousePositionDrag);
            Ray currentRay = cam.ScreenPointToRay(Input.mousePosition);

            float enterStart;
            float enterCurrent;

            if (plane.Raycast(startRay, out enterStart) && plane.Raycast(currentRay, out enterCurrent))
            {
                Vector3 startWorldPoint = startRay.GetPoint(enterStart);
                Vector3 currentWorldPoint = currentRay.GetPoint(enterCurrent);

                Vector3 difference = startWorldPoint - currentWorldPoint;

                smoothTargetLookAtPoint += new Vector3(difference.x, 0, difference.z) * dragSpeed;

                lookAtPoint = smoothTargetLookAtPoint;
            }

            lastMousePositionDrag = Input.mousePosition;
        }
    }

    void HandleCameraRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePositionRotation = Input.mousePosition;

            // Sync rotation targets to current to avoid sudden jump
            smoothTargetYaw = currentYaw;
            smoothTargetPitch = currentPitch;

            // Get center screen point
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray centerRay = cam.ScreenPointToRay(screenCenter);

            Plane groundPlane = new Plane(Vector3.up, gridManager != null ? gridManager.position.y : 0f);
            float dist;
            if (groundPlane.Raycast(centerRay, out dist))
            {
                smoothTargetLookAtPoint = centerRay.GetPoint(dist);
            }
            else
            {
                smoothTargetLookAtPoint = transform.position + transform.forward * orbitDistance;
            }

            // Set lookAtPoint, but only if you want to move focus (optional)
            //lookAtPoint = smoothTargetLookAtPoint;
            lookAtPoint = Vector3.Lerp(lookAtPoint, smoothTargetLookAtPoint, Time.deltaTime * 1f);

        }


        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePositionRotation;

            if (!stableYaw)
            {
                smoothTargetYaw += delta.x * rotationSpeed;
            }

            smoothTargetPitch -= delta.y * rotationSpeed;
            smoothTargetPitch = Mathf.Clamp(smoothTargetPitch, minPitchAngle, maxPitchAngle);

            lastMousePositionRotation = Input.mousePosition;
        }
    }


    void UpdateCameraPositionAndRotation()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        Vector3 negDistance = new Vector3(0.0f, 0.0f, -orbitDistance);
        Vector3 position = rotation * negDistance;
        position += lookAtPoint;

        transform.rotation = rotation;
        transform.position = position;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetFOV -= scroll * zoomSpeed;

            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }
    }

    public void CenterOnGrid()
    {
        if (gridManager == null || gridManager.childCount == 0)
        {
            Debug.LogWarning("GridManager or its tiles are not initialized. Cannot center camera.");
            return;
        }

        Bounds bounds = new Bounds(gridManager.GetChild(0).position, Vector3.zero);

        foreach (Transform tile in gridManager)
        {
            bounds.Encapsulate(tile.position);
        }

        smoothTargetLookAtPoint = new Vector3(bounds.center.x, gridManager.position.y, bounds.center.z);

        lookAtPoint = smoothTargetLookAtPoint;

        UpdateCameraPositionAndRotation();
    }

    public void HandleFocusOnRobot()
    {

        Transform currentRobotTransform = null;

        if (GameManager.Instance != null && GameManager.Instance.currentRobot != null)
        {
            currentRobotTransform = GameManager.Instance.currentRobot.transform;
        }

        if (currentRobotTransform != null)
        {
            smoothTargetLookAtPoint = currentRobotTransform.position;

            smoothTargetOrbitDistance = focusOrbitDistance;

            Vector3 directionToRobot = (currentRobotTransform.position - transform.position).normalized;

            smoothTargetYaw = Mathf.Atan2(directionToRobot.x, directionToRobot.z) * Mathf.Rad2Deg;

            Vector3 horizontalDirection = new Vector3(directionToRobot.x, 0, directionToRobot.z).normalized;
            smoothTargetPitch = -Mathf.Atan2(directionToRobot.y, horizontalDirection.magnitude) * Mathf.Rad2Deg;
            smoothTargetPitch = Mathf.Clamp(smoothTargetPitch, minPitchAngle, maxPitchAngle);

            //Debug.Log($"Camera targeting robot at: {currentRobotTransform.position} with targetYaw: {smoothTargetYaw}, targetPitch: {smoothTargetPitch}");
        }
        else
        {
            Debug.LogWarning("GameManager or GameManager.currentRobot is null. Cannot focus on robot. Ensure GameManager exists in the scene and its 'currentRobot' is assigned.");
        }

    }
}