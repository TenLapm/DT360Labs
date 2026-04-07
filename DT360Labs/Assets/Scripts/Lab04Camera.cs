using UnityEngine;

public class Lab04Camera : MonoBehaviour
{
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public Vector2 panLimit = new Vector2(100, 100);
    public float scrollSpeed = 20f;
    public float minY = 5f;
    public float maxY = 30f;

    void Update()
    {
        Vector3 pos = transform.position;

        // 1. Get Keyboard Input (WASD or Arrow Keys)
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        // Calculate flattened forward/right directions (ignores the camera's downward tilt)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // Apply Keyboard Movement
        pos += (forward * v + right * h) * panSpeed * Time.deltaTime;

        //// 2. Get Mouse Edge Panning (Only applies if mouse is near the edges)
        //if (Input.mousePosition.y >= Screen.height - panBorderThickness) pos += forward * panSpeed * Time.deltaTime;
        //if (Input.mousePosition.y <= panBorderThickness) pos -= forward * panSpeed * Time.deltaTime;
        //if (Input.mousePosition.x >= Screen.width - panBorderThickness) pos += right * panSpeed * Time.deltaTime;
        //if (Input.mousePosition.x <= panBorderThickness) pos -= right * panSpeed * Time.deltaTime;

        // 3. Scroll Wheel Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;

        // Clamp boundaries
        pos.x = Mathf.Clamp(pos.x, -panLimit.x, panLimit.x);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, -panLimit.y, panLimit.y);

        transform.position = pos;
    }
}