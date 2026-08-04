using System;
using System.Collections;
using NUnit.Framework;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Bootstrap;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Core;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace OneStrokeDemon.Tests.PlayMode.T699A
{
    // 验证生产Battle中的弹体可见移动、碰撞扣血与真实画笔击落闭环。
    [Category("T699A")]
    public sealed class ProductionProjectilePlayModeTests : InputTestFixture
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
        public IEnumerator VisibleSlowProjectileDamagesPlayerOnlyOnImpact()
        {
            ProductionBattleSession session = null;
            yield return LoadLevel(
                ConfigIds.Levels.Lv002Cave,
                found => session = found);
            ProjectileController projectile = null;
            yield return WaitForProjectile(
                session,
                ConfigIds.Projectiles.ProjGhostFire,
                found => projectile = found);

            Assert.That(
                projectile.Rules.ProjectileId,
                Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
            Assert.That(projectile.Rules.SpeedReferencePixelsPerSecond, Is.EqualTo(180f));
            Assert.That(projectile.Ownership.CurrentOwner.Faction, Is.EqualTo(ProjectileFaction.Enemy));
            SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sortingLayerName, Is.EqualTo("Projectiles"));

            Vector2 initialScreen = Camera.main.WorldToScreenPoint(projectile.transform.position);
            Assert.That(initialScreen.x, Is.GreaterThan(Screen.width * 0.5f));
            Assert.That(initialScreen.x, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(initialScreen.y, Is.InRange(0f, (float)Screen.height));
            float initialX = projectile.ReferencePosition.x;
            long configuredDamage = projectile.Rules.Damage;
            long hpBefore = session.Player.Current.CurrentHp;
            string damageSource = string.Empty;
            session.Player.CombatEventPublished += combatEvent =>
            {
                if (combatEvent.EventType == PlayerCombatEventType.HpChanged &&
                    combatEvent.SignedAmount < 0L)
                {
                    damageSource = combatEvent.SourceId;
                }
            };

            yield return new WaitForSecondsRealtime(0.25f);

            Assert.That(projectile.ReferencePosition.x, Is.LessThan(initialX));
            Assert.That(session.Player.Current.CurrentHp, Is.EqualTo(hpBefore));
            Assert.That(
                projectile.HitCollider.bounds.Intersects(
                    session.Player.GetComponent<Collider2D>().bounds),
                Is.False);

            for (int step = 0; step < 220 && session.Player.Current.CurrentHp == hpBefore; step++)
            {
                session.Advance(0.05f);
            }

            Assert.That(
                session.Player.Current.CurrentHp,
                Is.EqualTo(hpBefore - configuredDamage));
            Assert.That(damageSource, Is.EqualTo(ConfigIds.Projectiles.ProjGhostFire));
            Assert.That(projectile.IsActive, Is.False);
            Assert.That(projectile.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator RealHorizontalStrokeCutsNonReflectableBossProjectile()
        {
            ProductionBattleSession session = null;
            yield return LoadLevel(
                ConfigIds.Levels.Lv003Boss,
                found => session = found);
            ProjectileController projectile = null;
            yield return WaitForProjectile(
                session,
                ConfigIds.Projectiles.ProjSoulShard,
                found => projectile = found);
            session.SwitchStance();
            Assert.That(
                session.Player.Current.StanceId,
                Is.EqualTo(ConfigIds.Stances.StanceTalisman));

            Assert.That(
                projectile.Rules.ProjectileId,
                Is.EqualTo(ConfigIds.Projectiles.ProjSoulShard));
            Assert.That(projectile.Rules.Cuttable, Is.True);
            Assert.That(projectile.Rules.Reflectable, Is.False);
            int countBefore = session.ActiveProjectileCount;
            int strokesBefore = session.CompletedStrokeCount;

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(projectile.transform.position);
            Vector2 start = new Vector2(
                Mathf.Max(1f, screenPoint.x - 100f),
                screenPoint.y);
            Vector2 end = new Vector2(
                Mathf.Min(Screen.width - 1f, screenPoint.x + 100f),
                screenPoint.y);
            Assert.That(start.x, Is.GreaterThan(0f));
            Assert.That(end.x, Is.LessThan(Screen.width));
            Assert.That(screenPoint.y, Is.InRange(0f, (float)Screen.height));

            Set(mouse.position, start, queueEventOnly: true);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Set(mouse.position, screenPoint, queueEventOnly: true);
            yield return null;
            Set(mouse.position, end, queueEventOnly: true);
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;

            Assert.That(session.CompletedStrokeCount, Is.EqualTo(strokesBefore + 1));
            Assert.That(session.LastResolvedHitCount, Is.GreaterThan(0));
            Assert.That(projectile.IsActive, Is.False);
            Assert.That(projectile.gameObject.activeSelf, Is.False);
            Assert.That(session.ActiveProjectileCount, Is.LessThan(countBefore));
        }

        private static IEnumerator LoadLevel(
            string levelId,
            Action<ProductionBattleSession> assign)
        {
            yield return SceneManager.LoadSceneAsync(
                SceneNames.Bootstrap,
                LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            IConfigProvider config = GameplayConfigRuntime.Current;
            var results = new ResultService(config, new PlayerPrefsProgressSaveStore());
            results.MarkTutorialCompleted(ConfigIds.Tutorials.TutorialLevel001);
            results.Settle(new ResultRequest(
                $"T699A_unlock_normal_{levelId}",
                ConfigIds.Levels.Lv001Tutorial,
                BattleSettlement.Victory,
                new BattleResultMetrics(100000L, 0, 0L, 0d)));
            if (string.Equals(levelId, ConfigIds.Levels.Lv003Boss, StringComparison.Ordinal))
            {
                results.Settle(new ResultRequest(
                    "T699A_unlock_boss",
                    ConfigIds.Levels.Lv002Cave,
                    BattleSettlement.Victory,
                    new BattleResultMetrics(100000L, 0, 0L, 0d)));
            }

            string tutorialId = config.GetLevel(levelId).TutorialId;
            if (!string.IsNullOrEmpty(tutorialId))
            {
                results.MarkTutorialCompleted(tutorialId);
            }

            Assert.That(results.Current.IsLevelUnlocked(levelId), Is.True);
            yield return SceneManager.LoadSceneAsync(
                SceneNames.MainMenu,
                LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MainMenu);
            MainMenuCompositionRoot menu = Object.FindAnyObjectByType<MainMenuCompositionRoot>();
            Assert.That(menu, Is.Not.Null);
            menu.View.StartButton.onClick.Invoke();
            MainMenuLevelChoice choice = FindChoice(menu.View, levelId);
            Assert.That(choice.Button.interactable, Is.True);
            choice.Button.onClick.Invoke();
            yield return WaitForScene(SceneNames.Battle);
            BattleCompositionRoot root = Object.FindAnyObjectByType<BattleCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.CurrentSession, Is.Not.Null);
            Assert.That(root.CurrentSession.LevelId, Is.EqualTo(levelId));
            assign(root.CurrentSession);
            yield return WaitForPlaying(root.CurrentSession);
        }

        private static MainMenuLevelChoice FindChoice(MainMenuView view, string levelId)
        {
            for (int index = 0; index < view.LevelChoices.Count; index++)
            {
                if (string.Equals(
                        view.LevelChoices[index].LevelId,
                        levelId,
                        StringComparison.Ordinal))
                {
                    return view.LevelChoices[index];
                }
            }

            Assert.Fail($"Missing configured level choice '{levelId}'.");
            return null;
        }

        private static IEnumerator WaitForPlaying(ProductionBattleSession session)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (session.FlowState != BattleFlowState.Playing &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(session.FlowState, Is.EqualTo(BattleFlowState.Playing));
        }

        private static IEnumerator WaitForProjectile(
            ProductionBattleSession session,
            string projectileId,
            Action<ProjectileController> assign)
        {
            float deadline = Time.realtimeSinceStartup + 9f;
            while (Time.realtimeSinceStartup < deadline)
            {
                for (int index = 0; index < session.ActiveProjectileCount; index++)
                {
                    ProjectileController projectile = session.GetActiveProjectile(index);
                    if (string.Equals(
                            projectile.Rules.ProjectileId,
                            projectileId,
                            StringComparison.Ordinal))
                    {
                        assign(projectile);
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for configured projectile '{projectileId}'.");
        }

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
