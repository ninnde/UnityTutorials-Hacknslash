using UnityEngine;

namespace Player
{

    public class PlayerAttackManager : MonoBehaviour
    {
        private static readonly int _ENEMY_LAYER = 1 << 6;

        [SerializeField] private PlayerData _data;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;

        public void PlayerHitLeft(int comboStep)
        {
            _PlayerHit(comboStep, _leftHand);
        }

        public void PlayerHitRight(int comboStep)
        {
            _PlayerHit(comboStep, _rightHand);
        }

        private void _PlayerHit(int comboStep, Transform hand)
        {
            PlayerData runtimeData = Tools.AddressablesLoader.instance.playerData;
            if (runtimeData == null) return;
            PlayerHealth health = GetComponentInParent<PlayerHealth>();
            if (health != null && health.IsDead) return;
            Collider[] closeEnemies = Physics.OverlapSphere(
                hand.position, runtimeData.currentAttackRange, _ENEMY_LAYER);
            foreach (Collider enemy in closeEnemies)
            {
                Enemy.EnemyManager em =
                    enemy.transform.parent.GetComponent<Enemy.EnemyManager>();
                if (em != null)
                {
                    float dmg = runtimeData.AttackDamage * (comboStep + 1);
                    float overrideDamage = PlayerController.overrideDamage;
                    if (overrideDamage != -1f)
                        dmg = overrideDamage;
                    em.TakeHit(dmg);
                }
            }
        }
    }

}
