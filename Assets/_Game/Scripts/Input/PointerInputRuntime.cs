using System;
using System.Globalization;
using UnityEngine;

namespace OneStrokeDemon.Input
{
    public readonly struct PointerInputRuntimeSummary
    {
        public PointerInputRuntimeSummary(Vector2 referenceResolution)
        {
            ReferenceResolution = referenceResolution;
        }

        public Vector2 ReferenceResolution { get; }

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

    public static class PointerInputRuntime
    {
        private const string RuntimeObjectName = "PointerInputRuntime";
        private static InputSystemPointerAdapter current;
        private static PointerInputRuntimeSummary currentSummary;

        public static bool IsReady => current != null;

        public static IPointerInput Current => current != null
            ? current
            : throw new InvalidOperationException("PTR001: Pointer input runtime has not been initialized.");

        public static PointerInputRuntimeSummary CurrentSummary => current != null
            ? currentSummary
            : throw new InvalidOperationException("PTR001: Pointer input runtime summary is unavailable.");

        public static PointerInputRuntimeSummary Initialize(Vector2 referenceResolution)
        {
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Reset(PointerCancelReason.RuntimeReset);
        }

        internal static void ResetForTests()
        {
            Reset(PointerCancelReason.RuntimeReset);
        }

        internal static void Release(InputSystemPointerAdapter adapter)
        {
            if (ReferenceEquals(current, adapter))
            {
                current = null;
                currentSummary = default;
            }
        }

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
