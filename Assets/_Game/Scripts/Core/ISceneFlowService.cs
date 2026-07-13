using UnityEngine;

namespace OneStrokeDemon.Core
{
    public interface ISceneFlowService
    {
        AsyncOperation LoadMainMenu();

        AsyncOperation LoadBattle();
    }
}
