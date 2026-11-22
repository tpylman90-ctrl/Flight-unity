using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Touch and mouse orbit viewer for the single-building art test.</summary>
public sealed class BuildingInspectionViewer : MonoBehaviour
{
    private Transform target;
    private Transform cameraTransform;
    private Camera viewCamera;
    private float yaw = -32f;
    private float pitch = 13f;
    private float distance = 92f;
    private float desiredDistance = 92f;
    private Vector2 previousPointer;
    private bool dragging;
    private static readonly Vector3 Focus = new Vector3(0f, 30.5f, 0f);

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        target = GameObject.Find("Inspection Focus").transform;
        GameObject cameraObject = GameObject.FindWithTag("MainCamera");
        viewCamera = cameraObject.GetComponent<Camera>();
        cameraTransform = cameraObject.transform;
        ApplyView(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetView();
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) yaw -= 70f * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) yaw += 70f * Time.deltaTime;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) pitch = Mathf.Clamp(pitch + 45f * Time.deltaTime, -12f, 68f);
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) pitch = Mathf.Clamp(pitch - 45f * Time.deltaTime, -12f, 68f);

        if (Input.touchCount >= 2)
        {
            dragging = false;
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            Vector2 aPrev = a.position - a.deltaPosition;
            Vector2 bPrev = b.position - b.deltaPosition;
            float before = Vector2.Distance(aPrev, bPrev);
            float after = Vector2.Distance(a.position, b.position);
            desiredDistance = Mathf.Clamp(desiredDistance - (after - before) * 0.11f, 28f, 150f);
        }
        else if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                previousPointer = t.position;
                dragging = true;
            }
            else if (dragging && t.phase == TouchPhase.Moved)
            {
                Orbit(t.deltaPosition);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                dragging = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                previousPointer = Input.mousePosition;
                dragging = true;
            }
            if (dragging && Input.GetMouseButton(0))
            {
                Vector2 current = Input.mousePosition;
                Orbit(current - previousPointer);
                previousPointer = current;
            }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            desiredDistance = Mathf.Clamp(desiredDistance - Input.mouseScrollDelta.y * 4f, 28f, 150f);
        }

        desiredDistance = Mathf.Clamp(desiredDistance, 28f, 150f);
        distance = Mathf.Lerp(distance, desiredDistance, 1f - Mathf.Exp(-7f * Time.deltaTime));
        ApplyView(false);
    }

    private void Orbit(Vector2 delta)
    {
        yaw += delta.x * 0.28f;
        pitch = Mathf.Clamp(pitch - delta.y * 0.20f, -12f, 68f);
    }

    private void ApplyView(bool immediate)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = Focus + rotation * new Vector3(0f, 0f, -distance);
        cameraTransform.position = immediate ? position : Vector3.Lerp(cameraTransform.position, position, 1f - Mathf.Exp(-8f * Time.deltaTime));
        cameraTransform.rotation = rotation;
    }

    private void ResetView()
    {
        yaw = -32f;
        pitch = 13f;
        desiredDistance = 92f;
    }

    private void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.25f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.skin.label.fontSize = Mathf.RoundToInt(23f);
        GUI.skin.button.fontSize = Mathf.RoundToInt(19f);
        GUI.skin.box.fontSize = Mathf.RoundToInt(17f);
        GUI.skin.label.normal.textColor = Color.white;
        GUI.skin.box.normal.textColor = Color.white;
        GUI.skin.button.normal.textColor = Color.white;
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        GUI.skin.box.alignment = TextAnchor.MiddleLeft;
        GUI.Box(new Rect(18, 18, Screen.width / scale - 36, 94), "");
        GUI.Label(new Rect(36, 26, 610, 38), "HALO SPIRE  /  BUILDING STUDY");
        GUI.skin.label.fontSize = Mathf.RoundToInt(16f);
        GUI.Label(new Rect(36, 62, 660, 30), "Drag to orbit  ·  Pinch to zoom  ·  Press R / Reset view");
        if (GUI.Button(new Rect(Screen.width / scale - 154, 34, 118, 52), "RESET VIEW")) ResetView();
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(14, Screen.height / scale - 36, Screen.width / scale - 28, 28), "ECLIPSE  •  ANDROID BUILDING INSPECTION");
    }
}
