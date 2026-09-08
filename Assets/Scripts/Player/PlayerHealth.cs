using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxHealth = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public event System.Action<Vector3> DamageReceived;
        public event System.Action Died;
        private float _lastHitTime = -10f;
        private float _invulnerableUntil;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public void GrantInvulnerability(float duration)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + Mathf.Max(0f, duration));
        }

        public void ClearInvulnerability()
        {
            _invulnerableUntil = 0f;
        }

        private void Awake()
        {
            CurrentHealth = Mathf.Max(1f, _maxHealth);
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        public void TakeDamage(float amount, Vector3 attackerPosition)
        {
            if (IsDead || IsInvulnerable || amount <= 0f) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            _lastHitTime = Time.time;
            DamageReceived?.Invoke(attackerPosition);
            if (IsDead)
                Died?.Invoke();
        }

        // Temporary prototype HUD; no scene references are required.
        private void OnGUI()
        {
            Color previousColor = GUI.color;
            GUI.Box(new Rect(16, 16, 244, 60), GUIContent.none);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(28, 50, 220, 14), Texture2D.whiteTexture);
            GUI.color = Time.time - _lastHitTime < 0.25f ? Color.white : Color.red;
            GUI.DrawTexture(new Rect(28, 50, 220 * CurrentHealth / _maxHealth, 14), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(28, 24, 220, 24), $"Vida: {CurrentHealth:0} / {_maxHealth:0}");
        }
    }
}
