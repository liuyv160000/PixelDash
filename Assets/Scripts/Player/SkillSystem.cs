using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Player
{
    /// <summary>
    /// 技能系统：扫描本节点上的 BaseSkill 组件，按 skillId 注册到 Map，
    /// 提供 EnableSkill / DisableSkill / UpdateSkills。
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        private readonly Dictionary<string, BaseSkill> _skills = new Dictionary<string, BaseSkill>();
        private PlayerController _owner;

        /// <summary>由 PlayerController 调用：确保技能组件存在并按 skillId 注册。</summary>
        public void Init(PlayerController owner)
        {
            _owner = owner;
            EnsureSkills();
            var skills = GetComponents<BaseSkill>();
            _skills.Clear();
            foreach (var s in skills)
            {
                if (s == null) continue;
                s.Init(owner);
                _skills[s.SkillId] = s;
            }
        }

        /// <summary>确保 4 个技能组件已挂载（重复调用不重复添加）。</summary>
        private void EnsureSkills()
        {
            if (GetComponent<BulletAutoSkill>() == null) gameObject.AddComponent<BulletAutoSkill>();
            if (GetComponent<TripleBulletAutoSkill>() == null) gameObject.AddComponent<TripleBulletAutoSkill>();
            if (GetComponent<MultiBulletSkill>() == null) gameObject.AddComponent<MultiBulletSkill>();
            if (GetComponent<DaoSkill>() == null) gameObject.AddComponent<DaoSkill>();
        }

        /// <summary>启用技能（拾取技能球时调用）。</summary>
        public void EnableSkill(string skillId)
        {
            if (_skills.TryGetValue(skillId, out var skill))
                skill.EnableSkill();
        }

        /// <summary>停用技能。</summary>
        public void DisableSkill(string skillId)
        {
            if (_skills.TryGetValue(skillId, out var skill))
                skill.DisableSkill();
        }

        /// <summary>每帧驱动已注册技能的计时。</summary>
        public void UpdateSkills(float dt)
        {
            foreach (var kv in _skills)
                kv.Value.UpdateSkill(dt);
        }
    }
}