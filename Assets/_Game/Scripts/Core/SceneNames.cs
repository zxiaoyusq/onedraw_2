namespace OneStrokeDemon.Core
{
    /// <summary>
    /// 集中保存Build Settings中的稳定场景名，避免各模块散落字符串常量。
    /// </summary>
    public static class SceneNames
    {
        /// <summary>启动场景，负责初始化配置和跨场景服务。</summary>
        public const string Bootstrap = "Bootstrap";

        /// <summary>生产主菜单场景。</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>生产战斗场景。</summary>
        public const string Battle = "Battle";
    }
}
