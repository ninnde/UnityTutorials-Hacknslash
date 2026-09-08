using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Enemy
{

    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyManager : MonoBehaviour
    {
        private enum State
        {
            Idle,
            MoveTo,
            Return,
            Attack,
            Die,
        };

        [SerializeField] private EnemyData _data;
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _modelCollider;
        [SerializeField] private Inventory.LootGroupData _lootGroup;
        [SerializeField, Min(0f)] private float _attackDamage = 10f;
        private NavMeshAgent _agent;
        public event System.Action<EnemyManager> Died;
        private bool _persistentTarget;

        public void Engage(Transform target)
        {
            _persistentTarget = true;
            _target = target;
            _currentState = State.MoveTo;
            _agent.isStopped = false;
            _agent.SetDestination(target.position);
            _animator.SetBool(_animRunningParamHash, true);
        }

        #region Variables: General
        private float _hp;
        #endregion

        #region Variables: FSM
        private State _currentState;
        private Transform _target;
        #endregion

        #region Variables: Animation
        private int _animRunningParamHash;
        private int _animAttackParamHash;
        private int _animTakeHitParamHash;
        #endregion

        #region Variables: Misc
        private Vector3 _spawnpointPosition;
        private float _attackDelay;
        private bool _canAttack;
        private Coroutine _attackCoroutine;
        #endregion

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            _animRunningParamHash = Animator.StringToHash("Running");
            _animAttackParamHash = Animator.StringToHash("Attack");
            _animTakeHitParamHash = Animator.StringToHash("TakeHit");

            _hp = _data.healthpoints;
            _spawnpointPosition = transform.position;
            _currentState = State.Idle;
            _attackCoroutine = null;

            GetComponent<SphereCollider>().radius = _data.fovRadius;
        }

        private void Update()
        {
            if (_currentState == State.MoveTo || _currentState == State.Return)
                _MoveToOrReturn();
            else if (_currentState == State.Attack)
                _Attack();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_currentState == State.Die || _persistentTarget) return;
            if (other.CompareTag("Player"))
            {
                _target = other.transform;
                _agent.destination = _target.position;
                transform.rotation = Quaternion.LookRotation(
                    _target.position - transform.position,
                    Vector3.up);
                _currentState = State.MoveTo;
                _animator.SetBool(_animRunningParamHash, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_currentState == State.Die || _persistentTarget) return;
            if (other.CompareTag("Player"))
            {
                _target = null;
                _agent.destination = _spawnpointPosition;
                _agent.velocity = Vector3.zero;
                transform.rotation = Quaternion.LookRotation(
                    _spawnpointPosition - transform.position,
                    Vector3.up);
                _currentState = State.Return;
                _agent.isStopped = false;
                _animator.SetBool(_animRunningParamHash, true);
            }
        }

        private void _MoveToOrReturn()
        {
            if (_agent.isStopped)
                return;

            if (
                _currentState == State.Return &&
                _agent.remainingDistance < 0.1f
            )
            {
                _currentState = State.Idle;
                _agent.destination = transform.position;
                _agent.velocity = Vector3.zero;
                _target = null;
                _animator.SetBool(_animRunningParamHash, false);
            }
            else if (
                _currentState == State.MoveTo &&
                _agent.remainingDistance <= _data.attackRadius
            )
            {
                if (_target != null && _target.CompareTag("Player"))
                {
                    _currentState = State.Attack;
                    _agent.destination = transform.position;
                    _agent.velocity = Vector3.zero;
                    _animator.SetBool(_animRunningParamHash, false);
                    _attackDelay = 0f;
                    _canAttack = true;
                    // (disable movement)
                    _agent.isStopped = true;
                }
            }
            else if (_target != null)
            {
                _agent.destination = _target.position;
            }
        }

        private void _Attack()
        {
            if (_target == null) return;
            Player.PlayerHealth health = _target.GetComponent<Player.PlayerHealth>();
            if (health != null && health.IsDead) return;
            if (!_canAttack)
                return;

            if ((_target.position - transform.position).magnitude > _data.attackRadius)
            {
                _currentState = State.MoveTo;
                _agent.destination = _target.position;
                _animator.SetBool(_animRunningParamHash, true);
                _animator.ResetTrigger(_animAttackParamHash);
                if (_attackCoroutine == null)
                    _agent.isStopped = false;
                return;
            }

            // keep looking at the target
            transform.rotation = Quaternion.LookRotation(
                _target.position - transform.position,
                Vector3.up);

            if (_attackDelay >= _data.attackRate && _attackCoroutine == null)
            {
                // attack
                _animator.SetTrigger(_animAttackParamHash);
                _attackDelay = 0;
                _attackCoroutine = StartCoroutine(Tools.Utils.WaitingForCurrentAnimation(
                    _animator,
                    () =>
                    {
                        // (re-enable movement)
                        _agent.isStopped = false;
                        _attackCoroutine = null;
                    }));
            }
            else
            {
                _attackDelay += Time.deltaTime;
            }
        }

        public void TakeHit(float amount)
        {
            if (_currentState == State.Die) return;
            _hp -= amount;
            if (_hp <= 0)
            {
                _animator.SetTrigger("Die");
                _modelCollider.enabled = false;
                _currentState = State.Die;
                Died?.Invoke(this);
                _agent.isStopped = true;
                GetComponent<SphereCollider>().enabled = false;
                if (_attackCoroutine != null)
                {
                    StopCoroutine(_attackCoroutine);
                    _attackCoroutine = null;
                }
                StartCoroutine(Tools.Utils.WaitingForCurrentAnimation(
                    _animator,
                    () =>
                    {
                        Vector3 pos = transform.position;
                        Destroy(gameObject);
                        Tools.AddressablesLoader.instance.lootBagPrefab.InstantiateAsync(
                            pos, Quaternion.identity).Completed +=
                            (AsyncOperationHandle<GameObject> obj) =>
                            {
                                GameObject g = obj.Result;
                                List<(Inventory.InventoryItemData, int)> contents =
                                   _lootGroup.PickRandomItems();
                                g.GetComponent<Inventory.LootBagManager>()
                                    .contents = contents;
                            };
                    },
                    waitForAnimName: "Die",
                    extraWait: 1f));
            }
            else
            {
                _animator.SetTrigger(_animTakeHitParamHash);
                _attackDelay = 0f;
                _canAttack = false;
                if (_attackCoroutine != null)
                {
                    StopCoroutine(_attackCoroutine);
                    _attackCoroutine = null;
                    _agent.isStopped = false;
                }
                StartCoroutine(Tools.Utils.WaitingForCurrentAnimation(
                    _animator,
                    () =>
                    {
                        _canAttack = true;
                    },
                    waitForAnimName: "TakeHit"));
            }

            float height = _modelCollider.bounds.extents.y / 2f;
            Vector3 popupPosition = transform.position + transform.up * height;
            Tools.Graphics.CreateDamagePopup(amount, popupPosition);

        }

        public void TryDealAttackDamage()
        {
            if (_currentState != State.Attack || !_canAttack || _target == null) return;
            Vector3 offset = _target.position - transform.position;
            if (offset.sqrMagnitude > _data.attackRadius * _data.attackRadius) return;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.001f && Vector3.Dot(transform.forward, offset.normalized) < 0.5f)
                return;
            Player.PlayerHealth health = _target.GetComponent<Player.PlayerHealth>();
            if (health != null) health.TakeDamage(_attackDamage, transform.position);
        }

    }

}
