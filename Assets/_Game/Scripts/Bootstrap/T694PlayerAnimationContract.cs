using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    /// <summary>
    /// 集中定义T694主角Animator的纯表现参数，避免运行时与Editor作者工具重复字符串。
    /// </summary>
    public static class T694PlayerAnimationContract
    {
        public const string AttackTriggerName = "Attack";

        public static readonly int AttackTriggerHash =
            Animator.StringToHash(AttackTriggerName);
    }
}
