using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [DefaultExecutionOrder(-100)]
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private float _distance = 8f;
        [SerializeField] private float _targetHeight = 1.3f;
        [SerializeField] private float _pitch = 18f;
        [SerializeField] private float _mouseSensitivity = 0.12f;
        [SerializeField] private float _stickSpeed = 120f;
        private Transform _player;
        private PlayerHealth _health;
        private Transform _pivot;
        private float _yaw;
        private bool _released;
        private bool _focused = true;

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _health = _player.GetComponent<PlayerHealth>();
            Camera main = Camera.main;
            main.orthographic = false;
            // Retain the original virtual camera in the scene for comparison.
            foreach (CinemachineVirtualCamera camera in FindObjectsOfType<CinemachineVirtualCamera>())
                camera.gameObject.SetActive(false);
            _pivot = new GameObject("Third Person Target").transform;
            _pivot.SetParent(transform);
            _pivot.position = _player.position + Vector3.up * _targetHeight;
            _pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            GameObject rig = new GameObject("CM Third Person");
            rig.transform.SetParent(transform);
            CinemachineVirtualCamera vcam = rig.AddComponent<CinemachineVirtualCamera>();
            vcam.Follow = _pivot;
            vcam.LookAt = _pivot;
            vcam.m_Lens.FieldOfView = 60f;
            var body = vcam.AddCinemachineComponent<CinemachineTransposer>();
            body.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
            body.m_FollowOffset = new Vector3(0f, 0f, -_distance);
            body.m_XDamping = body.m_YDamping = body.m_ZDamping = 0.15f;
            vcam.AddCinemachineComponent<CinemachineHardLookAt>();
            var collision = rig.AddComponent<CinemachineCollider>();
            collision.m_CollideAgainst = 1; // Environment on Default; ignore enemy detection volumes.
            collision.m_IgnoreTag = "Player";
            collision.m_CameraRadius = 0.25f;
            _player.GetComponent<PlayerController>().MovementCamera = main.transform;
        }

        private void Update()
        {
            if (_player == null) return;
            bool menu = !Inputs.InputManager.InputActions.Player.Move.enabled || Inventory.InventoryManager.inLootPanel;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) _released = true;
            if (!menu && !_health.IsDead && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                _released = false;
            bool capture = _focused && !menu && !_health.IsDead && !_released;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
            if (capture)
            {
                Vector2 look = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * _mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * (_stickSpeed * Time.deltaTime);
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -10f, 55f);
            }
            _pivot.position = _player.position + Vector3.up * _targetHeight;
            _pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void OnApplicationFocus(bool focused) { _focused = focused; }
        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
