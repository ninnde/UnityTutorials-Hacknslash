using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Player
{
    public enum AnimatorContext
    {
        Base = 0,
        OneHanded,
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private CharacterController _controller;
        private PlayerHealth _health;
        private PlayerHitReaction _hitReaction;
        private PlayerDodge _dodge;
        [SerializeField] private Animator _animator;
        private PlayerData _data;
        private Transform _animatorTransform;

        private bool _inUI;
        public Transform MovementCamera { get; set; }

        private Vector3 MovementDirection(Vector2 input)
        {
            if (MovementCamera == null) return new Vector3(input.x, 0f, input.y);
            Vector3 forward = Vector3.ProjectOnPlane(MovementCamera.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return forward * input.y + right * input.x;
        }

        #region Variables: Inputs
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _dodgeAction;
        #endregion

        #region Variables: Movement
        private Vector2 _move;
        private bool _running;
        private bool _hasOverbudernedState;
        #endregion

        #region Variables: Attack
        private const int _COMBO_MAX_STEP = 2;
        private int _comboHitStep;
        private Coroutine _comboAttackResetCoroutine;
        private bool _attacking;

        public static float overrideDamage = -1f;
        #endregion

        #region Variables: Animation
        private int _animRunningParamHash;
        private int _animOverburdenedParamHash;
        private int _animAttackComboStepParamHash;
        private AnimatorContext _currentContext;
        #endregion

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<PlayerHealth>();
            if (_health == null) _health = gameObject.AddComponent<PlayerHealth>();
            _hitReaction = GetComponent<PlayerHitReaction>();
            if (_hitReaction == null) _hitReaction = gameObject.AddComponent<PlayerHitReaction>();
            _dodge = GetComponent<PlayerDodge>();
            if (_dodge == null) _dodge = gameObject.AddComponent<PlayerDodge>();
            _dodgeAction = new InputAction("Dodge", InputActionType.Button, "<Keyboard>/space");
            _dodgeAction.AddBinding("<Gamepad>/leftStickPress");
            _animatorTransform = _animator.transform;

            _running = false;
            _attacking = false;
            _animRunningParamHash = Animator.StringToHash("Running");
            _animOverburdenedParamHash = Animator.StringToHash("Overburdened");
            _animAttackComboStepParamHash = Animator.StringToHash("AttackComboStep");
            _currentContext = AnimatorContext.Base;

            _comboHitStep = -1;
            _comboAttackResetCoroutine = null;
        }

        private void Start()
        {
            Tools.AddressablesLoader.addressablesLoaded.AddListener(
                _OnAddressablesLoaded);
        }

        private void OnEnable()
        {
            _health.Died += StopForDeath;
            _dodgeAction.performed += OnDodge;
            _dodgeAction.Enable();
            _moveAction = Inputs.InputManager.InputActions.Player.Move;
            _moveAction.Enable();

            _attackAction = Inputs.InputManager.InputActions.Player.Attack;
            _attackAction.performed += _OnAttackAction;
            _attackAction.Enable();
        }

        private void OnDisable()
        {
            _health.Died -= StopForDeath;
            _dodgeAction.performed -= OnDodge;
            _dodgeAction.Disable();
            _dodge.Cancel();
            _hitReaction.Cancel();
            _moveAction.Disable();
            _attackAction.performed -= _OnAttackAction;
            _attackAction.Disable();
        }

        private void Update()
        {
            if (_health.IsDead)
                return;

            if (_dodge.IsActive)
            {
                _controller.Move(_dodge.ConsumeDisplacement(Time.deltaTime));
                return;
            }

            if (_hitReaction.IsActive)
            {
                _running = false;
                _animator.SetBool(_animRunningParamHash, false);
                _controller.Move(_hitReaction.ConsumeDisplacement(Time.deltaTime));
                return;
            }

            if (_attacking || _data == null)
                return;

            _inUI = Cursor.lockState != CursorLockMode.Locked && EventSystem.current.IsPointerOverGameObject();

            _move = _moveAction.ReadValue<Vector2>();
            if (_move.sqrMagnitude > 0.01f)
            {
                if (!_running)
                {
                    _running = true;
                    _animator.SetBool(_animRunningParamHash, true);
                }

                if (_data.overburdened && !_hasOverbudernedState)
                {
                    _hasOverbudernedState = true;
                    _animator.SetBool(_animOverburdenedParamHash, true);
                }
                else if (!_data.overburdened && _hasOverbudernedState)
                {
                    _hasOverbudernedState = false;
                    _animator.SetBool(_animOverburdenedParamHash, false);
                }

                Vector3 v = MovementDirection(_move);
                float s = _data.moveSpeed;
                if (_data.overburdened) s *= 0.33f;
                _animatorTransform.rotation =
                    Quaternion.LookRotation(-v, Vector3.up);
                _controller.Move(v * Time.deltaTime * s);
            }
            else if (_running)
            {
                _running = false;
                _animator.SetBool(_animRunningParamHash, false);
            }
        }

        private void _OnAddressablesLoaded()
        {
            _data = Tools.AddressablesLoader.instance.playerData;
        }

        private void OnDestroy()
        {
            _dodgeAction?.Dispose();
        }

        private void OnDodge(InputAction.CallbackContext context)
        {
            // The standalone action follows the tutorial's Player map/menu gate.
            if (!_moveAction.enabled || _data == null || _health.IsDead || _attacking || overrideDamage != -1f ||
                _hitReaction.IsActive || Inventory.InventoryManager.inLootPanel || Time.timeScale <= 0f)
                return;
            Vector2 input = _moveAction.ReadValue<Vector2>();
            Vector3 direction = input.sqrMagnitude > 0.01f
                ? MovementDirection(input) : -_animatorTransform.forward;
            if (!_dodge.TryBegin(direction)) return;
            _animatorTransform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            _running = false;
            _animator.SetBool(_animRunningParamHash, false);
        }

        private void _OnAttackAction(InputAction.CallbackContext obj)
        {
            if (_health.IsDead || _dodge.IsActive || _data == null) return;
            if (_inUI)
                return;

            if (Inventory.InventoryManager.inLootPanel)
                return;

            _attacking = true;
            if (_comboHitStep == _COMBO_MAX_STEP)
                return;
            float t = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (_comboHitStep == -1 || (t >= 0.1f && t <= 0.8f))
            {
                if (_comboAttackResetCoroutine != null)
                    StopCoroutine(_comboAttackResetCoroutine);
                _comboHitStep++;
                _animator.SetBool(_animRunningParamHash, false);
                _animator.SetInteger(
                    _animAttackComboStepParamHash, _comboHitStep);
                _comboAttackResetCoroutine = StartCoroutine(
                    Tools.Utils.WaitingForCurrentAnimation(
                        _animator,
                        () =>
                        {
                            ResetAttackCombo();
                            _move = _moveAction.ReadValue<Vector2>();
                            if (_move.sqrMagnitude > 0.01f && _running)
                                _animator.SetBool(_animRunningParamHash, true);
                        },
                        stopAfterAnim: true,
                        earlyExit: 0.1f));
            }
        }

        public void SetAnimatorContext(AnimatorContext context)
        {
            if (_currentContext != AnimatorContext.Base)
                _animator.SetLayerWeight((int)_currentContext, 0);
            _animator.SetLayerWeight((int)context, 1);
            _currentContext = context;
        }

        public void TriggerState(string stateName, System.Action onFinish = null)
        {
            if (_health.IsDead) return;
            _animator.SetTrigger(stateName);
            if (onFinish != null)
                StartCoroutine(Tools.Utils.WaitingForCurrentAnimation(_animator, onFinish));
        }

        public void ResetAttackCombo()
        {
            _comboHitStep = -1;
            _animator.SetInteger(
                _animAttackComboStepParamHash, _comboHitStep);
            _attacking = false;
        }

        public void StopForDeath()
        {
            _dodge.Cancel();
            StopAllCoroutines();
            _comboAttackResetCoroutine = null;
            ResetAttackCombo();
            _running = false;
            _animator.SetBool(_animRunningParamHash, false);
            overrideDamage = -1f;
        }
    }

}
