using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHitReaction : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _pushDistance = 0.65f;
        [SerializeField, Min(0.01f)] private float _pushDuration = 0.15f;

        private PlayerHealth _health;
        private Vector3 _direction;
        private float _elapsed;
        private float _duration;
        private float _distance;

        public bool IsActive => isActiveAndEnabled && _elapsed < _duration;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            _health.DamageReceived += OnDamageReceived;
            _health.Died += Cancel;
        }

        private void OnDisable()
        {
            _health.DamageReceived -= OnDamageReceived;
            _health.Died -= Cancel;
            Cancel();
        }

        private void OnDamageReceived(Vector3 attackerPosition)
        {
            if (_health.IsDead || _pushDistance <= 0f) return;
            _direction = transform.position - attackerPosition;
            _direction.y = 0f;
            if (_direction.sqrMagnitude < 0.0001f)
                _direction = Vector3.back;
            _direction.Normalize();

            // A new hit replaces the push instead of stacking unlimited force.
            _elapsed = 0f;
            _duration = Mathf.Max(0.01f, _pushDuration);
            _distance = _pushDistance;
        }

        // PlayerController consumes this once per frame and owns all movement.
        public Vector3 ConsumeDisplacement(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f) return Vector3.zero;
            float start = _elapsed / _duration;
            _elapsed = Mathf.Min(_elapsed + deltaTime, _duration);
            float end = _elapsed / _duration;
            // Integrate a linearly decreasing speed, including the final partial frame.
            float fraction = (2f * end - end * end) - (2f * start - start * start);
            return _direction * (_distance * fraction);
        }

        public void Cancel()
        {
            _elapsed = _duration;
        }
    }
}
