using UnityEngine;

namespace Enemy
{
    // Attached to the Attack state so each animation can deal damage only once.
    public class EnemyAttackImpact : StateMachineBehaviour
    {
        [Range(0f, 1f)] public float impactTime = 0.45f;
        private bool _applied;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _applied = false;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_applied || stateInfo.normalizedTime < impactTime || animator.IsInTransition(layerIndex))
                return;
            _applied = true;
            EnemyManager enemy = animator.GetComponentInParent<EnemyManager>();
            if (enemy != null) enemy.TryDealAttackDamage();
        }
    }
}
