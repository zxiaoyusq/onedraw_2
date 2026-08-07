namespace OneStrokeDemon.Input
{
    /// <summary>输入层支持的笔势分类结果。</summary>
    public enum GestureType
    {
        /// <summary>没有规则匹配。</summary>
        None,
        /// <summary>任意达到最低长度的有效笔迹。</summary>
        Any,
        /// <summary>近似水平笔迹。</summary>
        Horizontal,
        /// <summary>近似垂直笔迹。</summary>
        Vertical,
        /// <summary>近似四十五度或一百三十五度的斜向笔迹。</summary>
        Diagonal,
        /// <summary>曲率达到阈值的弧线。</summary>
        Arc,
        /// <summary>闭合距离、面积和曲率均达到阈值的圆形。</summary>
        Circle,
        /// <summary>首尾闭合且可拟合为三条直边和三个有效角点的三角形。</summary>
        Triangle,
        /// <summary>起笔停留达到阈值后形成的笔迹。</summary>
        Charged
    }
}
