using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OneStrokeDemon.Tests.PlayMode
{
    public sealed class InputBaselinePlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator MouseAndTouchCanDriveTheSamePointerAction()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            using var point = new InputAction("Point", InputActionType.PassThrough);
            point.AddBinding("<Mouse>/position");
            point.AddBinding("<Touchscreen>/touch*/position");
            point.Enable();

            var mousePosition = new Vector2(120f, 240f);
            Set(mouse.position, mousePosition);
            yield return null;
            Assert.That(Vector2.Distance(point.ReadValue<Vector2>(), mousePosition), Is.LessThan(0.01f));

            var touchPosition = new Vector2(360f, 480f);
            BeginTouch(1, touchPosition, screen: touchscreen);
            yield return null;
            Assert.That(Vector2.Distance(point.ReadValue<Vector2>(), touchPosition), Is.LessThan(0.01f));
        }
    }
}
