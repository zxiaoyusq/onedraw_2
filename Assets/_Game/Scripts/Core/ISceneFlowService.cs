using UnityEngine;

namespace OneStrokeDemon.Core
{
    /// <summary>
    /// 定义生产场景之间的跳转入口，使业务代码无需直接依赖SceneManager。
    /// </summary>
    public interface ISceneFlowService
    {
        /// <summary>
        /// 异步加载主菜单场景。
        /// </summary>
        /// <returns>Unity场景加载操作，调用方可用它观察进度或完成状态。</returns>
        AsyncOperation LoadMainMenu();

        /// <summary>
        /// 异步加载战斗场景。
        /// </summary>
        /// <returns>Unity场景加载操作。</returns>
        AsyncOperation LoadBattle();
    }
}
