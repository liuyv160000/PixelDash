namespace PixelDash.Player
{
    /// <summary>刀技能（废案）：继承 BaseSkill 占位，不做任何实际逻辑。</summary>
    public class DaoSkill : BaseSkill
    {
        public override string SkillId => "dao";

        /// <summary>废案占位：不发射任何东西。</summary>
        protected override void Fire()
        {
        }
    }
}
