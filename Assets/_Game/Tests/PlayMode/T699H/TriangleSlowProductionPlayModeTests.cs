using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OneStrokeDemon.Tests.PlayMode.T699H
{
    /// <summary>验证玩家从生产Battle输入闭合三角形后，当前全部存活敌人获得配置减速。</summary>
    [Category("T699H")]
    public sealed class TriangleSlowProductionPlayModeTests : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            GameplayConfigRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            PointerInputRuntime.ResetForTests();
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
            base.Setup();
        }

        [TearDown]
        public override void TearDown()
        {
            BattleLaunchContext.Clear();
            PlayerPrefs.DeleteKey(PlayerPrefsProgressSaveStore.StorageKey);
            PointerInputRuntime.ResetForTests();
            AssetRegistryRuntime.ResetForTests();
            GameplayConfigRuntime.ResetForTests();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator RealTriangleStrokeAppliesConfiguredSlowToAllActiveEnemies()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            IConfigProvider config = GameplayConfigRuntime.Current;
            var results = new ResultService(config, new PlayerPrefsProgressSaveStore());
            results.MarkTutorialCompleted(ConfigIds.Tutorials.TutorialLevel001);

            MainMenuCompositionRoot menu = Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(menu, Is.Not.Null);
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv001Tutorial).Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);

            BattleCompositionRoot battleRoot = Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(battleRoot, Is.Not.Null);
            ProductionBattleSession session = battleRoot.CurrentSession;
            yield return WaitForPlaying(session);
            List<EnemyController> enemies = null;
            yield return WaitForEnemies(found => enemies = found);

            StrokeRuleConfig triangleRule = config.GetStrokeRule(
                ConfigIds.StrokeRules.StrokeTriangle);
            Assert.That(
                triangleRule.OnMatchSkillId,
                Is.EqualTo(ConfigIds.Skills.SkillTriangleSlow));
            int strokesBefore = session.CompletedStrokeCount;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Vector2 first = new Vector2(Screen.width * 0.35f, Screen.height * 0.35f);
            Vector2 second = new Vector2(Screen.width * 0.62f, Screen.height * 0.35f);
            Vector2 third = new Vector2(Screen.width * 0.485f, Screen.height * 0.68f);
            Assert.That(
                new EventSystemPointerUiBlocker().IsBlocked(
                    first,
                    InputSystemPointerAdapter.MousePointerId),
                Is.False);

            Set(mouse.position, first, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, Vector2.Lerp(first, second, 0.5f), queueEventOnly: true);
            yield return null;
            Set(mouse.position, second, queueEventOnly: true);
            yield return null;
            Set(mouse.position, Vector2.Lerp(second, third, 0.5f), queueEventOnly: true);
            yield return null;
            Set(mouse.position, third, queueEventOnly: true);
            yield return null;
            Set(mouse.position, Vector2.Lerp(third, first, 0.5f), queueEventOnly: true);
            yield return null;
            Set(mouse.position, first + Vector2.one * 2f, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(session.CompletedStrokeCount, Is.EqualTo(strokesBefore + 1));
            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyController enemy = enemies[index];
                Assert.That(enemy.IsAlive, Is.True, enemy.Definition.EnemyId);
                Assert.That(
                    enemy.Buffs.TryGet(ConfigIds.Buffs.BuffSlow30, out EnemyBuffSnapshot slow),
                    Is.True,
                    enemy.Definition.EnemyId);
                Assert.That(slow.Magnitude, Is.EqualTo(0.3f));
                Assert.That(enemy.MovementMultiplier, Is.EqualTo(0.7d).Within(0.000001d));
            }
        }

        /// <summary>等待生产流程进入可接受普通笔画的Playing状态。</summary>
        private static IEnumerator WaitForPlaying(ProductionBattleSession session)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (session.FlowState != BattleFlowState.Playing &&
                   Time.realtimeSinceStartup < deadline)
            {
                session.SetApplicationPaused(false);
                session.SetApplicationFocus(true);
                yield return null;
            }

            Assert.That(session.FlowState, Is.EqualTo(BattleFlowState.Playing));
        }

        /// <summary>等待至少一个活动敌人，并冻结本次笔画应影响的目标集合。</summary>
        private static IEnumerator WaitForEnemies(Action<List<EnemyController>> assign)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline)
            {
                EnemyController[] found = Object.FindObjectsByType<EnemyController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.InstanceID);
                var alive = new List<EnemyController>();
                for (int index = 0; index < found.Length; index++)
                {
                    if (found[index].IsAlive)
                    {
                        alive.Add(found[index]);
                    }
                }

                if (alive.Count > 0)
                {
                    assign(alive);
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for an active production enemy.");
        }

        /// <summary>按稳定关卡ID取得主菜单选择项。</summary>
        private static MainMenuLevelChoice FindChoice(MainMenuView view, string levelId)
        {
            for (int index = 0; index < view.LevelChoices.Count; index++)
            {
                MainMenuLevelChoice choice = view.LevelChoices[index];
                if (string.Equals(choice.LevelId, levelId, StringComparison.Ordinal))
                {
                    return choice;
                }
            }

            Assert.Fail($"Missing configured level choice '{levelId}'.");
            return null;
        }

        /// <summary>等待异步场景加载完成。</summary>
        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!string.Equals(
                       SceneManager.GetActiveScene().name,
                       sceneName,
                       StringComparison.Ordinal) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }
    }
}
