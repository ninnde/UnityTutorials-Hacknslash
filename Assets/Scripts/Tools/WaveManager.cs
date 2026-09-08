using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Tools
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private Enemy.EnemyManager _enemyTemplate;
        [SerializeField] private int[] _enemyCounts = { 2, 3, 4 };
        [SerializeField, Min(1f)] private float _betweenWaves = 5f;
        [SerializeField, Min(3f)] private float _spawnRadius = 6f;
        private readonly HashSet<Enemy.EnemyManager> _alive = new HashSet<Enemy.EnemyManager>();
        private Player.PlayerHealth _health;
        private Transform _player;
        private bool _finished;
        public event System.Action Completed;
        public string Status { get; private set; } = "Preparando arena...";
        public int CurrentWave { get; private set; }
        public int Defeated { get; private set; }
        public int TotalEnemies { get; private set; }
        public float Progress => TotalEnemies == 0 ? 0f : (float)Defeated / TotalEnemies;

        private IEnumerator Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _health = _player.GetComponent<Player.PlayerHealth>();
            _health.Died += StopWaves;
            if (_enemyTemplate == null || _enemyCounts == null || _enemyCounts.Length == 0)
            {
                Debug.LogError("WaveManager requires an enemy template and wave counts.", this);
                Status = "Falta configurar las oleadas";
                yield break;
            }
            _enemyTemplate.gameObject.SetActive(false);
            foreach (int count in _enemyCounts) TotalEnemies += Mathf.Max(1, count);
            for (int wave = 0; wave < _enemyCounts.Length && !_finished; wave++)
            {
                float delay = wave == 0 ? 3f : _betweenWaves;
                while (delay > 0f && !_finished)
                {
                    Status = $"Oleada {wave + 1}/{_enemyCounts.Length} en {Mathf.CeilToInt(delay)} s";
                    delay -= Time.deltaTime;
                    yield return null;
                }
                if (_finished) yield break;
                int count = Mathf.Max(1, _enemyCounts[wave]);
                var positions = new List<Vector3>();
                while (!FindSpawnPositions(count, positions) && !_finished)
                {
                    Status = "Buscando espacio para la oleada...";
                    yield return new WaitForSeconds(1f);
                }
                if (_finished) yield break;
                CurrentWave = wave + 1;
                foreach (Vector3 position in positions)
                {
                    Enemy.EnemyManager enemy = Instantiate(_enemyTemplate, position, Quaternion.identity);
                    enemy.name = $"Wave {CurrentWave} - Brute";
                    enemy.Died += OnEnemyDied;
                    _alive.Add(enemy);
                    enemy.gameObject.SetActive(true);
                    enemy.Engage(_player);
                }
                UpdateStatus();
                while (_alive.Count > 0 && !_finished) yield return null;
            }
            if (_finished) yield break;
            _finished = true;
            Status = "Todas las oleadas completadas";
            Completed?.Invoke();
        }

        private bool FindSpawnPositions(int count, List<Vector3> positions)
        {
            positions.Clear();
            if (!NavMesh.SamplePosition(_player.position, out NavMeshHit playerHit, 3f, NavMesh.AllAreas))
                return false;
            var path = new NavMeshPath();
            for (int attempt = 0; attempt < 80 && positions.Count < count; attempt++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 candidate = _player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spawnRadius;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
                if (Vector3.Distance(hit.position, _player.position) < 3f) continue;
                if (positions.Exists(p => Vector3.Distance(p, hit.position) < 1.5f)) continue;
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
            Status = $"Oleada {CurrentWave}/{_enemyCounts.Length} - Enemigos restantes: {_alive.Count}";
        }

        private void StopWaves()
        {
            _finished = true;
            Status = "Arena terminada";
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= StopWaves;
            foreach (var enemy in _alive)
                if (enemy != null) enemy.Died -= OnEnemyDied;
        }
    }
}
