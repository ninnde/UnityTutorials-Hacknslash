using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tools
{
    public class ArenaEncounterManager : MonoBehaviour
    {
        [SerializeField] private Enemy.EnemyManager _enemyTemplate;
        [SerializeField, Min(1)] private int _enemyCount = 10;
        [SerializeField, Min(1f)] private float _initialDistance = 14f;
        [SerializeField, Min(1f)] private float _enemySpacing = 8f;
        [SerializeField, Min(1f)] private float _detectionRadius = 6f;
        private readonly HashSet<Enemy.EnemyManager> _alive = new HashSet<Enemy.EnemyManager>();
        private Player.PlayerHealth _health;
        private Transform _player;
        private Vector3 _spawnOrigin;
        private Vector3 _spawnForward;
        private bool _finished;
        public event System.Action Completed;
        public string Status { get; private set; } = "Preparando arena...";
        public int Defeated { get; private set; }
        public int TotalEnemies { get; private set; }
        public float Progress => TotalEnemies == 0 ? 0f : (float)Defeated / TotalEnemies;

        private IEnumerator Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _spawnOrigin = _player.position;
            _spawnForward = _player.GetComponent<Player.PlayerController>().FacingDirection;
            _health = _player.GetComponent<Player.PlayerHealth>();
            _health.Died += StopEncounter;
            TotalEnemies = Mathf.Max(1, _enemyCount);
            if (_enemyTemplate == null)
            {
                Debug.LogError("ArenaEncounterManager requires an enemy template.", this);
                Status = "Falta configurar los enemigos";
                yield break;
            }
            _enemyTemplate.gameObject.SetActive(false);
            var positions = new List<Vector3>();
            while (!_finished && !FindSpawnPositions(TotalEnemies, positions))
            {
                Status = "Buscando espacio para los enemigos...";
                yield return new WaitForSeconds(1f);
            }
            if (_finished) yield break;
            foreach (Vector3 position in positions)
            {
                Enemy.EnemyManager enemy = Instantiate(_enemyTemplate, position, Quaternion.identity);
                enemy.name = $"Arena Enemy {_alive.Count + 1}";
                enemy.Died += OnEnemyDied;
                _alive.Add(enemy);
                enemy.gameObject.SetActive(true);
                enemy.SetDetectionRadius(_detectionRadius);
            }
            UpdateStatus();
            while (_alive.Count > 0 && !_finished) yield return null;
            if (_finished) yield break;
            _finished = true;
            Status = "Todos los enemigos derrotados";
            Completed?.Invoke();
        }

        private bool FindSpawnPositions(int count, List<Vector3> positions)
        {
            positions.Clear();
            if (!NavMesh.SamplePosition(_spawnOrigin, out NavMeshHit playerHit, 3f, NavMesh.AllAreas))
                return false;
            var path = new NavMeshPath();
            Vector3 right = Vector3.Cross(Vector3.up, _spawnForward);
            float safeDistance = Mathf.Max(_initialDistance, _detectionRadius + 5f);
            float spacing = Mathf.Max(_enemySpacing, _detectionRadius + 2f);
            // Search a forward grid. Never fall back to surrounding the player.
            int columns = Mathf.Min(3, count);
            for (int slot = 0; slot < count * 4 && positions.Count < count; slot++)
            {
                int row = slot / columns;
                int column = slot % columns;
                Vector3 candidate = _spawnOrigin + _spawnForward * (safeDistance + row * spacing)
                    + right * ((column - (columns - 1) * 0.5f) * spacing);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                Vector3 fromStart = Vector3.ProjectOnPlane(hit.position - _spawnOrigin, Vector3.up);
                if (Vector3.Dot(fromStart, _spawnForward) < safeDistance - 2f) continue;
                if (positions.Exists(p => Vector3.ProjectOnPlane(p - hit.position, Vector3.up).magnitude < _enemySpacing)) continue;
                if (!NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete) continue;
                positions.Add(hit.position);
            }
            return positions.Count == count;
        }

        private void OnEnemyDied(Enemy.EnemyManager enemy)
        {
            if (_finished || !_alive.Remove(enemy)) return;
            enemy.Died -= OnEnemyDied;
            Defeated++;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            Status = $"Enemigos restantes: {_alive.Count}";
        }

        private void StopEncounter()
        {
            _finished = true;
            Status = "Arena terminada";
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= StopEncounter;
            foreach (var enemy in _alive)
                if (enemy != null) enemy.Died -= OnEnemyDied;
        }
    }
}
