using UnityEngine;
using UnityEngine.InputSystem;

namespace InsideMatter.VR
{
    /// <summary>
    /// Simple FPS camera controller for testing VR scenes without headset
    /// WASD = Move, Mouse = Look
    /// </summary>
    public class DesktopCameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 3f;
        public float sprintMultiplier = 2f;
        
        [Header("Look")]
        public float mouseSensitivity = 2f;
        public float maxLookAngle = 80f;
        
        [Header("Settings")]
        public bool enableInVR = false; // Disable when actual VR headset is connected
        
        private float rotationX = 0f;
        private float rotationY = 0f;

        private void Start()
        {
            // Lock cursor in game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            UnityEngine.Debug.Log("DesktopCameraController started! Controls: WASD=Move, Mouse=Look, ESC=Release cursor");
        }

        private void Update()
        {
            if (!enableInVR && UnityEngine.XR.XRSettings.isDeviceActive)
            {
                // Actual VR headset detected, disable desktop controls
                enabled = false;
                return;
            }
            
            // Check if input devices are available
            if (Keyboard.current == null || Mouse.current == null)
            {
                return;
            }
            
            HandleMovement();
            HandleLook();
            HandleCursorToggle();
        }

        private void HandleMovement()
        {
            if (Keyboard.current == null) return;
            
            float horizontal = 0f;
            float vertical = 0f;
            
            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            
            float speed = moveSpeed;
            if (Keyboard.current.leftShiftKey.isPressed)
                speed *= sprintMultiplier;
            
            Vector3 move = transform.right * horizontal + transform.forward * vertical;
            transform.position += move * speed * Time.deltaTime;
        }

        private void HandleLook()
        {
            if (Mouse.current == null) return;
            if (Cursor.lockState != CursorLockMode.Locked)
                return;
            
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            rotationX -= mouseDelta.y * mouseSensitivity * 0.1f;
            rotationY += mouseDelta.x * mouseSensitivity * 0.1f;
            
            rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);
            
            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }

        private void HandleCursorToggle()
        {
            if (Keyboard.current == null || Mouse.current == null) return;
            
            // ESC to unlock cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    UnityEngine.Debug.Log("Cursor unlocked - Press ESC or click to lock again");
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    UnityEngine.Debug.Log("Cursor locked - ESC to unlock");
                }
            }
            
            // Click to lock cursor again
            if (Cursor.lockState != CursorLockMode.Locked && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                UnityEngine.Debug.Log("Cursor locked - ESC to unlock");
            }
        }

        private void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 40, 300, 80), "");
                GUI.Label(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 30, 280, 60), 
                    "<size=16><b>Click to control camera</b>\n\nWASD = Move | Mouse = Look\nShift = Sprint | ESC = Release</size>");
            }
            else
            {
                // Show controls hint
                GUI.Label(new Rect(10, 10, 400, 60), 
                    "<size=14>WASD=Move | Mouse=Look | Shift=Sprint | ESC=Release cursor</size>");
            }
        }
    }
}
