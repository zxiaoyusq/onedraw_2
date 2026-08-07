using System.Collections;
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

namespace OneStrokeDemon.Tests.PlayMode.T699
{
    // 验证真实Bootstrap到Battle路径中的敌人推进与接触扣血闭环。
    [Category("T699")]
    public sealed class EnemyContactProductionPlayModeTests : InputTestFixture
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
        public IEnumerator RightSideEnemyApproachesAndDamagesOnlyAfterBodyContact()
        {
            yield return SceneManager.LoadSceneAsync(
                SceneNames.Bootstrap,
                LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);

            var results = new ResultService(
                GameplayConfigRuntime.Current,
                new PlayerPrefsProgressSaveStore());
            Assert.That(
                results.MarkTutorialCompleted(ConfigIds.Tutorials.TutorialLevel001),
                Is.True);
            results.Settle(new ResultRequest(
                "T699_unlock_normal",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(100000L, 0, 0L, 0d)));

            yield return SceneManager.LoadSceneAsync(
                SceneNames.MainMenu,
                LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            MainMenuCompositionRoot menu = Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(menu, Is.Not.Null);
            menu.View.StartButton.onClick.Invoke();
            FindChoice(menu.View, ConfigIds.Levels.Lv002Cave).Button.onClick.Invoke();

            yield return WaitForScene(SceneNames.Battle);
            BattleCompositionRoot battleRoot = Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(battleRoot, Is.Not.Null);
            ProductionBattleSession session = battleRoot.CurrentSession;
            yield return WaitForPlaying(session);
            EnemyController wheel = null;
            yield return WaitForEnemy(
                ConfigIds.Enemies.EnemyWheelZombie,
                found => wheel = found);

            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(
                FindObjectsSortMode.None);
            for (int index = 0; index < enemies.Length; index++)
            {
                if (enemies[index].IsAlive &&
                    !string.Equals(
                        enemies[index].Definition.EnemyId,
                        ConfigIds.Enemies.EnemyWheelZombie,
                        System.StringComparison.Ordinal))
                {
                    enemies[index].ApplyDamage(
                        enemies[index].Damage.CurrentHp + enemies[index].Damage.CurrentArmor,
                        "T699_isolate_contact_enemy",
                        Time.timeAsDouble);
                }
            }

            Collider2D playerBody = session.Player.GetComponent<Collider2D>();
            Collider2D enemyBody = wheel.GetComponent<Collider2D>();
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(enemyBody, Is.Not.Null);
            Physics2D.SyncTransforms();
            Assert.That(enemyBody.bounds.Intersects(playerBody.bounds), Is.False);

            Vector2 initialScreen = Camera.main.WorldToScreenPoint(wheel.transform.position);
            Assert.That(initialScreen.x, Is.GreaterThanOrEqualTo(Screen.width * 0.5f));
            float initialX = wheel.transform.localPosition.x;
            long hpBefore = session.Player.Current.CurrentHp;

            yield return new WaitForSecondsRealtime(1f);

            Physics2D.SyncTransforms();
            Assert.That(wheel.transform.localPosition.x, Is.LessThan(initialX));
            Assert.That(enemyBody.bounds.Intersects(playerBody.bounds), Is.False);
            Assert.That(
                session.Player.Current.CurrentHp,
                Is.EqualTo(hpBefore),
                "远处敌人的攻击演出不能直接扣除玩家生命。");

            // Test Runner 失焦会触发生产自动暂停；这里明确模拟玩家仍停留在游戏前台。
            session.SetApplicationPaused(false);
            session.SetApplicationFocus(true);
            session.Advance(30f);
            Physics2D.SyncTransforms();

            Assert.That(enemyBody.bounds.Intersects(playerBody.bounds), Is.True);
            Assert.That(
                session.Player.Current.CurrentHp,
                Is.EqualTo(hpBefore - wheel.Definition.ContactDamage));
        }

        private static MainMenuLevelChoice FindChoice(MainMenuView view, string levelId)
        {
            for (int index = 0; index < view.LevelChoices.Count; index++)
            {
                if (view.LevelChoices[index].LevelId == levelId)
                {
                    return view.LevelChoices[index];
                }
            }

            Assert.Fail($"Missing configured level choice '{levelId}'.");
            return null;
        }

        private static IEnumerator WaitForPlaying(ProductionBattleSession session)
        {
            session.SetApplicationPaused(false);
            session.SetApplicationFocus(true);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (session.FlowState != BattleFlowState.Playing &&
                   Time.realtimeSinceStartup < deadline)
            {
                session.SetApplicationPaused(false);
                session.SetApplicationFocus(true);
                yield return null;
            }

            Assert.That(session.FlowState, Is.EqualTo(BattleFlowState.Playing));
        }

        private static IEnumerator WaitForEnemy(
            string enemyId,
            System.Action<EnemyController> assign)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline)
            {
                EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(
                    FindObjectsSortMode.None);
                for (int index = 0; index < enemies.Length; index++)
                {
                    if (enemies[index].IsAlive &&
                        string.Equals(
                            enemies[index].Definition.EnemyId,
                            enemyId,
                            System.StringComparison.Ordinal))
                    {
                        assign(enemies[index]);
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for configured enemy '{enemyId}'.");
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!string.Equals(
                       SceneManager.GetActiveScene().name,
                       sceneName,
                       System.StringComparison.Ordinal) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }
    }
}
