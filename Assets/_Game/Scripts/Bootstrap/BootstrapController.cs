using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    public sealed class BootstrapController : MonoBehaviour
    {
        private void Start()
        {
            new SceneFlowService().LoadMainMenu();
        }
    }
}
