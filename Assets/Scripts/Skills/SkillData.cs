using UnityEngine;

namespace Skills
{
    public enum SkillType
    {
        Damage,
        Heal,
    }

    [CreateAssetMenu(fileName = "Skill", menuName = "Scriptable Objects/Skill")]
    public class SkillData : ScriptableObject
    {
        public SkillCode code;
        public string displayName;
        public SkillType type;
        public float cooldown;
        public float damage;
        public Sprite icon;
        public GameObject fxPrefab;

        private float _readyAt;

        private void Awake()
        {
            _readyAt = 0f;
        }

        public bool Cast()
        {
            if (Time.time < _readyAt) return false;
            SkillEffects.EFFECTS[code](this);

            _readyAt = Time.time + Mathf.Max(0f, cooldown);
            return true;
        }

    }

}
