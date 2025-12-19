using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InsideMatter.Interaction
{
    /// <summary>
    /// Simple first-person controller that works with the new Input System
    /// while also supporting the legacy input manager as a fallback.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        [Tooltip("Transform that holds the camera to tilt for vertical look")]
        public Transform cameraPivot;
        
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float sprintMultiplier = 1.7f;
        public float gravity = -9.81f;
        
        [Header("Look")]
        [Tooltip("Mouse sensitivity multiplier")]
        public float lookSensitivity = 0.15f;
        public bool lockCursor = true;
        public float minPitch = -80f;
        public float maxPitch = 80f;
        
        private CharacterController controller;
        private float cameraPitch;
        private float verticalVelocity;
        private bool sprintHeld;
        
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            
            if (cameraPivot == null)
            {
                cameraPivot = GetComponentInChildren<Camera>()?.transform;
            }
        }
        
        void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        void OnDisable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        void Update()
        {
            Vector2 moveInput = ReadMoveInput();
            Vector2 lookInput = ReadLookInput();
            
            HandleLook(lookInput);
            HandleMovement(moveInput);
        }
        
        private void HandleMovement(Vector2 moveInput)
        {
            float targetSpeed = sprintHeld ? moveSpeed * sprintMultiplier : moveSpeed;
            Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y);
            move = Vector3.ClampMagnitude(move, 1f) * targetSpeed;
            
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }
            verticalVelocity += gravity * Time.deltaTime;
            
            move += Vector3.up * verticalVelocity;
            controller.Move(move * Time.deltaTime);
        }
        
        private void HandleLook(Vector2 lookInput)
        {
            float mouseX = lookInput.x * lookSensitivity;
            float mouseY = lookInput.y * lookSensitivity;
            
            cameraPitch = Mathf.Clamp(cameraPitch - mouseY, minPitch, maxPitch);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
            
            transform.Rotate(Vector3.up * mouseX);
        }
        
        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                sprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
            else
            {
                sprintHeld = false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            sprintHeld = false;
#endif
            return input;
        }
        
        private Vector2 ReadLookInput()
        {
            Vector2 look = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                look = mouse.delta.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            look.x = Input.GetAxis("Mouse X");
            look.y = Input.GetAxis("Mouse Y");
#endif
            return look;
        }
    }
}
