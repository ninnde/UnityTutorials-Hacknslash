using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerDodge : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _distance = 2.5f;
        [SerializeField, Min(0.01f)] private float _duration = 0.2f;
        [SerializeField, Min(0f)] private float _cooldown = 0.8f;

        private PlayerHealth _health;
        private Vector3 _direction;
        private float _elapsed;
        private float _activeDuration;
        private float _activeDistance;
        private float _readyAt;

        public bool IsActive => isActiveAndEnabled && _elapsed < _activeDuration;
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            _health.Died += Cancel;
        }

        private void OnDisable()
        {
            _health.Died -= Cancel;
            Cancel();
        }

        public bool TryBegin(Vector3 direction)
        {
            if (!isActiveAndEnabled || _health.IsDead || IsActive || CooldownRemaining > 0f)
                return false;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return false;
            _direction = direction.normalized;
            _activeDuration = Mathf.Max(0.01f, _duration);
            _activeDistance = Mathf.Max(0f, _distance);
            _elapsed = 0f;
            // Cooldown starts after the dodge finishes.
            _readyAt = Time.time + _activeDuration + Mathf.Max(0f, _cooldown);
            _health.GrantInvulnerability(_activeDuration);
            return true;
        }

        public Vector3 ConsumeDisplacement(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f) return Vector3.zero;
            float step = Mathf.Min(deltaTime, _activeDuration - _elapsed);
            _elapsed += step;
            return _direction * (_activeDistance * step / _activeDuration);
        }

        public void Cancel()
        {
            if (_elapsed < _activeDuration) _health.ClearInvulnerability();
            _elapsed = _activeDuration;
        }
    }
}
