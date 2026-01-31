using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float verticalSpeed = 4f;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private Camera mainCamera;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        var forward = transform.forward;
        yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        pitch = 0f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
            yaw += delta.x;
            pitch -= delta.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        Vector3 input = Vector3.zero;
        if (keyboard.wKey.isPressed) input += Vector3.forward;
        if (keyboard.sKey.isPressed) input += Vector3.back;
        if (keyboard.aKey.isPressed) input += Vector3.left;
        if (keyboard.dKey.isPressed) input += Vector3.right;

        Vector3 vertical = Vector3.zero;
        if (keyboard.qKey.isPressed) vertical += Vector3.up;
        if (keyboard.eKey.isPressed) vertical += Vector3.down;

        Vector3 camForward = transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = (camForward * input.z + camRight * input.x) * moveSpeed;
        move += vertical * verticalSpeed;

        transform.position += move * Time.deltaTime;
    }
}
