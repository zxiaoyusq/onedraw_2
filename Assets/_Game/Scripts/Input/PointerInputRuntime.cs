using System;
using System.Globalization;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    /// <summary>记录统一指针运行时成功初始化后的参考分辨率。</summary>
    public readonly struct PointerInputRuntimeSummary
    {
        /// <summary>创建输入运行时摘要。</summary>
        public PointerInputRuntimeSummary(Vector2 referenceResolution)
        {
            ReferenceResolution = referenceResolution;
        }

        /// <summary>获取运行时使用的参考分辨率。</summary>
        public Vector2 ReferenceResolution { get; }

        /// <summary>生成包含来源、模式、Safe Area 和单指针策略的就绪日志。</summary>
        public string ToLogMessage()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "POINTER_INPUT_READY source=InputSystem modes=Mouse,Touch reference={0:0.###}x{1:0.###} " +
                "safeArea=dynamic uiBeginBlock=true maxActivePointers=1",
                ReferenceResolution.x,
                ReferenceResolution.y);
        }
    }

    /// <summary>创建并持有跨场景唯一的 Input System 指针适配器。</summary>
    public static class PointerInputRuntime
    {
        private const string RuntimeObjectName = "PointerInputRuntime";
        private static InputSystemPointerAdapter current;
        private static PointerInputRuntimeSummary currentSummary;

        /// <summary>获取全局输入运行时是否已初始化。</summary>
        public static bool IsReady => current != null;

        /// <summary>获取已发布输入端口；未初始化时抛出生命周期异常。</summary>
        public static IPointerInput Current => current != null
            ? current
            : throw new InvalidOperationException("PTR001: Pointer input runtime has not been initialized.");

        /// <summary>获取已发布输入摘要；未初始化时抛出生命周期异常。</summary>
        public static PointerInputRuntimeSummary CurrentSummary => current != null
            ? currentSummary
            : throw new InvalidOperationException("PTR001: Pointer input runtime summary is unavailable.");

        /// <summary>以配置参考分辨率初始化唯一跨场景输入对象；相同参数重复调用幂等。</summary>
        public static PointerInputRuntimeSummary Initialize(Vector2 referenceResolution)
        {
            // 先校验分辨率，再判断是否可以复用已经就绪的同参数实例。
            var converter = new ReferencePixelConverter(referenceResolution);
            if (current != null)
            {
                if (current.ReferenceResolution == referenceResolution)
                {
                    return currentSummary;
                }

                throw new InvalidOperationException(
                    $"PTR001: Pointer input runtime is already initialized for {current.ReferenceResolution}.");
            }

            // 只有全部依赖注入成功后才发布 current；失败则销毁半成品对象。
            var runtimeObject = new GameObject(RuntimeObjectName);
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);
            try
            {
                var adapter = runtimeObject.AddComponent<InputSystemPointerAdapter>();
                adapter.Initialize(converter, new ScreenSafeAreaProvider(), new EventSystemPointerUiBlocker());
                current = adapter;
                currentSummary = new PointerInputRuntimeSummary(referenceResolution);
                return currentSummary;
            }
            catch
            {
                Destroy(runtimeObject);
                throw;
            }
        }

        /// <summary>Unity 子系统重新注册时取消并清理静态输入运行时。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Reset(PointerCancelReason.RuntimeReset);
        }

        /// <summary>为测试隔离重置全局输入状态。</summary>
        internal static void ResetForTests()
        {
            Reset(PointerCancelReason.RuntimeReset);
        }

        /// <summary>适配器销毁时仅释放与当前实例相同的全局引用。</summary>
        internal static void Release(InputSystemPointerAdapter adapter)
        {
            if (ReferenceEquals(current, adapter))
            {
                current = null;
                currentSummary = default;
            }
        }

        /// <summary>先断开全局引用，再取消、禁用并销毁旧运行时对象。</summary>
        private static void Reset(PointerCancelReason reason)
        {
            InputSystemPointerAdapter previous = current;
            current = null;
            currentSummary = default;
            if (previous == null)
            {
                return;
            }

            previous.Cancel(reason);
            previous.enabled = false;
            Destroy(previous.gameObject);
        }

        /// <summary>根据是否处于 Play Mode 选择延迟或立即销毁。</summary>
        private static void Destroy(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
