using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float verticalSpeed = 4f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
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

        Vector3 input = Vector3.zero;
        if (keyboard.wKey.isPressed) input += Vector3.forward;
        if (keyboard.sKey.isPressed) input += Vector3.back;
        if (keyboard.aKey.isPressed) input += Vector3.left;
        if (keyboard.dKey.isPressed) input += Vector3.right;

        Vector3 vertical = Vector3.zero;
        if (keyboard.qKey.isPressed) vertical += Vector3.up;
        if (keyboard.eKey.isPressed) vertical += Vector3.down;

        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = mainCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = (camForward * input.z + camRight * input.x) * moveSpeed;
        move += vertical * verticalSpeed;

        transform.position += move * Time.deltaTime;
    }
}
