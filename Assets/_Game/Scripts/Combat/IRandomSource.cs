namespace OneStrokeDemon.Combat
{
    /// <summary>为纯战斗规则提供可注入、可测试的单位区间随机源。</summary>
    public interface IRandomSource
    {
        /// <summary>返回有限且位于半开区间 [0,1) 的随机值。</summary>
        double NextUnitInterval();
    }
}
