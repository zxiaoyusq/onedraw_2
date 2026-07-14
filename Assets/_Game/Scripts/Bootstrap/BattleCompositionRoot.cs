using System;
using OneStrokeDemon.Actors;
using OneStrokeDemon.Combat;
using OneStrokeDemon.Config;
using OneStrokeDemon.Input;
using OneStrokeDemon.Levels;
using OneStrokeDemon.Presentation;
using OneStrokeDemon.Skills;
using UnityEngine;

namespace OneStrokeDemon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class BattleCompositionRoot : MonoBehaviour
    {
        private ResultService results;
        private BattleResultNavigation navigation;
        private SceneFlowService sceneFlow;

        public ProductionBattleSession CurrentSession =>
            navigation?.Current as ProductionBattleSession;

        public uint SessionGeneration => navigation?.Generation ?? 0U;

        private void Start()
        {
            if (!GameplayConfigRuntime.IsReady ||
                !AssetRegistryRuntime.IsReady ||
                !PointerInputRuntime.IsReady)
            {
                return;
            }

            IConfigProvider config = GameplayConfigRuntime.Current;
            results = new ResultService(config, new PlayerPrefsProgressSaveStore());
            sceneFlow = new SceneFlowService();
            string levelId = ResolveInitialLevel(config, results.Current);
            var factory = new ProductionBattleSessionFactory(
                config,
                AssetRegistryRuntime.Current,
                results,
                transform,
                Restart,
                GoNext,
                ReturnToMainMenu);
            navigation = new BattleResultNavigation(factory, levelId);
        }

        private void OnDestroy()
        {
            navigation?.Dispose();
            navigation = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            CurrentSession?.SetApplicationFocus(hasFocus);
        }

        private void OnApplicationPause(bool paused)
        {
            CurrentSession?.SetApplicationPaused(paused);
        }

        private void Restart()
        {
            navigation.Restart();
        }

        private void GoNext()
        {
            ProductionBattleSession current = CurrentSession;
            if (current?.Receipt != null && current.Receipt.CanGoNext)
            {
                navigation.GoNext(current.Receipt);
            }
        }

        private void ReturnToMainMenu()
        {
            BattleLaunchContext.Clear();
            sceneFlow.LoadMainMenu();
        }

        private static string ResolveInitialLevel(
            IConfigProvider config,
            ProgressSnapshot progress)
        {
            if (BattleLaunchContext.HasSelection)
            {
                string selected = config.GetLevel(
                    BattleLaunchContext.SelectedLevelId).LevelId;
                if (progress.IsLevelUnlocked(selected))
                {
                    return selected;
                }
            }

            for (int index = 0; index < config.GetLevels().Count; index++)
            {
                LevelConfig level = config.GetLevels()[index];
                if (progress.IsLevelUnlocked(level.LevelId))
                {
                    return level.LevelId;
                }
            }

            throw new InvalidOperationException(
                "Configured progress must expose at least one playable root level.");
        }
    }

    internal sealed class ProductionBattleSessionFactory : IBattleSessionFactory
    {
        private readonly IConfigProvider config;
        private readonly IAssetRegistry assets;
        private readonly ResultService results;
        private readonly Transform parent;
        private readonly Action restart;
        private readonly Action goNext;
        private readonly Action mainMenu;

        public ProductionBattleSessionFactory(
            IConfigProvider configProvider,
            IAssetRegistry assetRegistry,
            ResultService resultService,
            Transform configuredParent,
            Action restartAction,
            Action goNextAction,
            Action mainMenuAction)
        {
            config = configProvider;
            assets = assetRegistry;
            results = resultService;
            parent = configuredParent;
            restart = restartAction;
            goNext = goNextAction;
            mainMenu = mainMenuAction;
        }

        public IBattleSession Create(string levelId)
        {
            return new ProductionBattleSession(
                config,
                assets,
                results,
                levelId,
                parent,
                restart,
                goNext,
                mainMenu);
        }
    }

    public sealed class ProductionBattleSession : IBattleSession, IBattleHudCommandSink
    {
        private const int PlayerFeedbackTargetId = -1;

        private readonly IConfigProvider config;
        private readonly IAssetRegistry assets;
        private readonly ResultService results;
        private readonly Action restart;
        private readonly Action goNext;
        private readonly Action mainMenu;
        private readonly GameObject root;
        private readonly Transform referenceRoot;
        private readonly Camera battleCamera;
        private readonly PlayerCombatController player;
        private readonly ProductionBattleWorld world;
        private readonly BattleFlowCoordinator battle;
        private readonly TutorialLevelCoordinator tutorial;
        private readonly BossLevelCoordinator boss;
        private readonly ComboService combo;
        private readonly ScoreService score;
        private readonly SkillService skills;
        private readonly StrokeInputCollector strokeCollector;
        private readonly GestureClassifier classifier;
        private readonly StrokeHitResolver resolver;
        private readonly HitRecord[] hits;
        private readonly SystemRandomSource random = new SystemRandomSource();
        private readonly BattleHudStateBinding hudBinding;
        private readonly BattleHudRuntime hud;
        private readonly TutorialOverlayRuntime tutorialOverlay;
        private readonly CombatFeedbackRuntime feedbackRuntime;
        private readonly CombatFeedbackService feedback;
        private readonly PlayerConfig playerConfig;
        private readonly SpriteRenderer playerRenderer;
        private readonly SessionDriver driver;
        private ResultReceipt receipt;
        private long playerDamageTaken;
        private int reflectedProjectileCount;
        private int completedStrokeCount;
        private int lastResolvedHitCount;
        private bool disposed;

        public ProductionBattleSession(
            IConfigProvider configProvider,
            IAssetRegistry assetRegistry,
            ResultService resultService,
            string levelId,
            Transform parent,
            Action restartAction,
            Action goNextAction,
            Action mainMenuAction)
        {
            config = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            assets = assetRegistry ?? throw new ArgumentNullException(nameof(assetRegistry));
            results = resultService ?? throw new ArgumentNullException(nameof(resultService));
            restart = restartAction ?? throw new ArgumentNullException(nameof(restartAction));
            goNext = goNextAction ?? throw new ArgumentNullException(nameof(goNextAction));
            mainMenu = mainMenuAction ?? throw new ArgumentNullException(nameof(mainMenuAction));
            LevelId = config.GetLevel(levelId).LevelId;
            root = new GameObject($"Production Battle Session {LevelId}");
            root.transform.SetParent(parent, false);
            battleCamera = Camera.main ??
                throw new InvalidOperationException("Battle scene requires a Main Camera.");
            referenceRoot = new GameObject("Reference Pixel World").transform;
            referenceRoot.SetParent(root.transform, false);
            ConfigureReferenceSpace(referenceRoot, battleCamera, config);
            CreateBackground(config.GetLevel(LevelId));

            playerConfig = config.GetPlayer(ConfigIds.Players.PlayerMoyan);
            playerRenderer = CreatePlayerVisual(playerConfig);
            player = playerRenderer.gameObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, playerConfig.PlayerId);
            world = new ProductionBattleWorld(config, assets, player, referenceRoot);

            LevelConfig level = config.GetLevel(LevelId);
            if (!string.IsNullOrEmpty(level.BossEnemyId))
            {
                boss = new BossLevelCoordinator(
                    config,
                    playerConfig.PlayerId,
                    LevelId,
                    player,
                    world);
                battle = boss.Battle;
            }
            else if (!string.IsNullOrEmpty(level.TutorialId))
            {
                tutorial = new TutorialLevelCoordinator(
                    config,
                    playerConfig.PlayerId,
                    LevelId,
                    world);
                battle = tutorial.Battle;
            }
            else
            {
                battle = new BattleFlowCoordinator(
                    config,
                    playerConfig.PlayerId,
                    LevelId,
                    world);
            }

            combo = ComboService.FromConfig(config);
            score = new ScoreService();
            skills = new SkillService(config, player);
            world.Bind(battle, skills);
            world.EnemySpawned += OnEnemySpawned;
            world.EnemyReleased += OnEnemyReleased;
            world.AttackExecuted += OnAttackExecuted;

            StrokeRuleConfig sampling = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            classifier = new GestureClassifier(GestureRuleSetFactory.FromConfig(config));
            StrokeHitResolverSettings resolverSettings =
                StrokeHitSettingsFactory.CreateResolverSettings(config);
            resolver = new StrokeHitResolver(
                resolverSettings,
                new Physics2DStrokeHitQuery(
                    resolverSettings.QueryCapacity,
                    Physics2D.AllLayers,
                    includeTriggers: true,
                    referenceRoot));
            hits = new HitRecord[resolverSettings.MaximumUniqueTargets];
            strokeCollector = new StrokeInputCollector(
                PointerInputRuntime.Current,
                StrokeSamplingSettingsFactory.FromConfig(sampling));
            strokeCollector.StrokeCompleted += OnStrokeCompleted;

            hudBinding = new BattleHudStateBinding(
                LevelId,
                player,
                combo,
                score,
                battle.Flow,
                results);
            hud = BattleHudRuntime.Create(
                config,
                hudBinding,
                this,
                playerConfig.PlayerId,
                BattleHudLanguage.ZhCN,
                root.transform);
            if (tutorial != null)
            {
                tutorialOverlay = TutorialOverlayRuntime.Create(
                    config,
                    tutorial,
                    results,
                    hud.View,
                    BattleHudLanguage.ZhCN);
            }

            feedbackRuntime = CombatFeedbackRuntime.Create(
                config,
                assets,
                battle.Flow.Time,
                battleCamera,
                referenceRoot);
            feedback = new CombatFeedbackService(feedbackRuntime.Settings, feedbackRuntime);
            feedbackRuntime.RegisterTarget(
                PlayerFeedbackTargetId,
                playerRenderer.transform,
                playerRenderer);
            player.CombatEventPublished += OnPlayerCombatEvent;

            driver = root.AddComponent<SessionDriver>();
            driver.Initialize(this);
        }

        public string LevelId { get; }

        public ResultReceipt Receipt => receipt;

        public int ActiveEnemyCount => world.ActiveCount;

        public BattleFlowState FlowState => battle.Flow.State;

        public BattleHudView HudView => hud.View;

        public TutorialOverlayView TutorialView => tutorialOverlay?.View;

        public PlayerCombatController Player => player;

        public bool IsBossSession => boss != null;

        public double GameplayTimestamp =>
            battle.Flow.Time.Current.GameplayElapsedSeconds;

        public int CompletedStrokeCount => completedStrokeCount;

        public int LastResolvedHitCount => lastResolvedHitCount;

        public void Advance(float unscaledDeltaSeconds)
        {
            if (disposed)
            {
                return;
            }

            BattleFlowAdvanceReport report;
            if (boss != null)
            {
                report = boss.Advance(unscaledDeltaSeconds);
            }
            else if (tutorial != null)
            {
                report = tutorial.Advance(unscaledDeltaSeconds, player.Current.IsDead);
            }
            else
            {
                report = battle.Advance(unscaledDeltaSeconds, player.Current.IsDead);
            }

            double gameplayTimestamp = report.Time.Current.GameplayElapsedSeconds;
            combo.AdvanceTime(gameplayTimestamp);
            world.Advance(battle.Level.ElapsedSeconds, gameplayTimestamp);
            if (boss != null)
            {
                world.AdvanceBoss(boss.BossPhases);
            }

            PublishRecurringTutorialEvents();
            feedbackRuntime.Advance(unscaledDeltaSeconds);
            hudBinding.UpdateUltimateClock(
                gameplayTimestamp,
                skills.GetCooldownUntil(battle.Flow.Settings.UltimateSkillId));
            if (report.SettledThisAdvance && receipt == null)
            {
                Settle(report.State);
            }
        }

        public void SetPlayerPaused(bool paused)
        {
            battle.SetPlayerPaused(paused);
        }

        public void SwitchStance()
        {
            if (battle.Flow.State != BattleFlowState.Playing || player.Current.IsDead)
            {
                return;
            }

            string next = string.Equals(
                player.Current.StanceId,
                ConfigIds.Stances.StanceBlade,
                StringComparison.Ordinal)
                ? ConfigIds.Stances.StanceTalisman
                : ConfigIds.Stances.StanceBlade;
            double timestamp = battle.Flow.Time.Current.GameplayElapsedSeconds;
            StanceSwitchResult switched = player.TrySwitchStance(next, timestamp);
            if (!switched.DidSwitch)
            {
                return;
            }

            if (!string.IsNullOrEmpty(switched.OnSwitchEffectGroupId))
            {
                skills.ExecuteEffectGroup(
                    switched.OnSwitchEffectGroupId,
                    switched.Current.StanceId,
                    new SkillEffectContext(world, timestamp));
            }

            NotifyTutorial(TutorialEventType.StanceChanged);
        }

        public void BeginUltimateDrawing()
        {
            battle.TryBeginUltimateDrawing();
        }

        public void Restart()
        {
            restart();
        }

        public void GoNext()
        {
            if (receipt != null && receipt.CanGoNext)
            {
                goNext();
            }
        }

        public void ReturnToMainMenu()
        {
            mainMenu();
        }

        public void SetApplicationFocus(bool hasFocus)
        {
            battle.SetApplicationFocus(hasFocus);
        }

        public void SetApplicationPaused(bool paused)
        {
            battle.SetApplicationPaused(paused);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (driver != null)
            {
                driver.Detach();
            }

            player.CombatEventPublished -= OnPlayerCombatEvent;
            world.EnemySpawned -= OnEnemySpawned;
            world.EnemyReleased -= OnEnemyReleased;
            world.AttackExecuted -= OnAttackExecuted;
            strokeCollector.StrokeCompleted -= OnStrokeCompleted;
            strokeCollector.Dispose();
            tutorialOverlay?.Dispose();
            hud?.Dispose();
            hudBinding.Dispose();
            feedbackRuntime.Dispose();
            boss?.Dispose();
            world.Dispose();
            Destroy(root);
        }

        private void OnStrokeCompleted(StrokeData stroke)
        {
            if (disposed ||
                (battle.Flow.State != BattleFlowState.Playing &&
                 battle.Flow.State != BattleFlowState.UltimateDrawing))
            {
                return;
            }

            completedStrokeCount += 1;
            lastResolvedHitCount = 0;

            StrokeRuleConfig sampling = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            StrokeGeometryData geometry = StrokeGeometry.Process(
                stroke,
                StrokeGeometrySettingsFactory.FromConfig(sampling));
            GestureMatchResult gesture = classifier.Classify(geometry);
            if (!gesture.IsMatch)
            {
                if (battle.Flow.State == BattleFlowState.UltimateDrawing)
                {
                    battle.CancelUltimateDrawing();
                }

                return;
            }

            if (battle.Flow.State == BattleFlowState.UltimateDrawing)
            {
                ResolveUltimate(stroke, gesture);
                return;
            }

            NotifyTutorial(
                TutorialEventType.ValidStroke,
                1L,
                ToTutorialGesture(gesture.GestureType));
            if (world.TryCutProjectile())
            {
                reflectedProjectileCount += 1;
                NotifyTutorial(TutorialEventType.ProjectileCut);
            }

            Physics2D.SyncTransforms();
            StrokeHitRule hitRule = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(gesture.RuleId));
            int count = resolver.Resolve(geometry, gesture, hitRule, hits);
            lastResolvedHitCount = count;
            if (count > 0)
            {
                NotifyTutorial(
                    TutorialEventType.StrokeHitCount,
                    count,
                    ToTutorialGesture(gesture.GestureType));
            }

            world.ClearStrokeSelection();
            double gameplayTimestamp = battle.Flow.Time.Current.GameplayElapsedSeconds;
            float extraMultiplier = world.ConsumeNextStrokeDamageMultiplier();
            for (int index = 0; index < count; index++)
            {
                HitRecord hit = hits[index];
                if (!world.TryGetByHitTarget(
                        hit.TargetId,
                        out long entityId,
                        out EnemyController enemy) ||
                    !enemy.IsAlive)
                {
                    continue;
                }

                world.MarkStrokeTarget(hit.TargetId, insideGesture: true);
                ComboSnapshot comboState = combo.RegisterHit(gameplayTimestamp);
                DamageRuleSet rules = DamageRuleSetFactory.CreateForEnemy(
                    config,
                    player.Current.StanceId,
                    enemy.Definition.EnemyId);
                var context = new DamageContext(
                    hit.StrokeId,
                    hit.TargetId,
                    hit.GestureType,
                    player.Current.StanceId,
                    hit.IsWeakpoint,
                    comboState.Count,
                    gameplayTimestamp);
                DamageResult damage = DamageCalculator.Calculate(context, rules, random);
                EnemyHitResolution applied = enemy.ApplyStrokeDamage(
                    damage,
                    hit.GestureType.ToString(),
                    Math.Max(Time.timeAsDouble, enemy.State.LastTimestamp),
                    $"stroke:{hit.StrokeId}");
                if (extraMultiplier > 1f && enemy.IsAlive)
                {
                    long extraDamage = checked((long)Math.Round(
                        damage.Damage * (extraMultiplier - 1f),
                        MidpointRounding.AwayFromZero));
                    enemy.ApplyDamage(
                        extraDamage,
                        $"stroke_multiplier:{hit.StrokeId}",
                        Math.Max(Time.timeAsDouble, enemy.State.LastTimestamp));
                }

                score.Record(damage);
                player.GainEnergy(damage, gameplayTimestamp);
                feedback.HandleEnemyHit(
                    damage,
                    applied,
                    $"stroke:{hit.StrokeId}",
                    gameplayTimestamp);
                if (hit.IsWeakpoint)
                {
                    NotifyTutorial(TutorialEventType.WeakpointHit);
                }

                if (applied.Damage.ArmorBroken)
                {
                    NotifyTutorial(TutorialEventType.ArmorBroken);
                }

                if (!enemy.IsAlive)
                {
                    NotifyEnemyDefeated(entityId);
                }
            }
        }

        private void ResolveUltimate(StrokeData stroke, GestureMatchResult gesture)
        {
            if (!battle.CanAcceptUltimateGestureEvent(stroke.StrokeId))
            {
                return;
            }

            double timestamp = battle.Flow.Time.Current.GameplayElapsedSeconds;
            SkillActivationResult activation = skills.TryActivate(
                new SkillActivationRequest(
                    battle.Flow.Settings.UltimateSkillId,
                    SkillTriggerTypes.Ultimate,
                    gesture.GestureType.ToString(),
                    gesture.IsMatch,
                    stroke.Duration,
                    timestamp),
                new SkillEffectContext(world, timestamp));
            if (tutorial != null)
            {
                tutorial.ResolveUltimate(stroke.StrokeId, activation);
            }
            else
            {
                battle.ResolveUltimate(stroke.StrokeId, activation);
            }
        }

        private void NotifyEnemyDefeated(long entityId)
        {
            bool accepted = boss != null
                ? boss.NotifyEnemyDefeated(entityId)
                : tutorial != null
                    ? tutorial.NotifyEnemyDefeated(entityId)
                    : battle.NotifyEnemyDefeated(entityId);
            if (accepted)
            {
                world.Release(entityId);
            }
        }

        private void OnEnemySpawned(
            long entityId,
            EnemyController controller,
            SpriteRenderer[] renderers)
        {
            feedbackRuntime.RegisterTarget(
                controller.Damage.HitTargetId,
                controller.transform,
                renderers);
            if (controller.Definition.Weakpoint.HasHitbox)
            {
                NotifyTutorial(TutorialEventType.EnemyWeakpointShown);
            }

            if (controller.Damage.MaximumArmor > 0L)
            {
                NotifyTutorial(TutorialEventType.ArmoredEnemySpawned);
            }

            if (string.Equals(
                    controller.Definition.EnemyId,
                    ConfigIds.Enemies.EnemySkeletonGhost,
                    StringComparison.Ordinal))
            {
                NotifyTutorial(TutorialEventType.GhostSpawned);
            }

            if (world.ActiveCount >= 3)
            {
                NotifyTutorial(TutorialEventType.WaveMultiTarget, world.ActiveCount);
            }
        }

        private void OnEnemyReleased(int hitTargetId)
        {
            feedbackRuntime.UnregisterTarget(hitTargetId);
        }

        private void OnAttackExecuted(EnemyAttackAction action)
        {
            if (!string.IsNullOrEmpty(action.ProjectileId))
            {
                NotifyTutorial(TutorialEventType.ProjectileSpawned);
            }
        }

        private void OnPlayerCombatEvent(PlayerCombatEvent combatEvent)
        {
            if (combatEvent.EventType == PlayerCombatEventType.HpChanged &&
                combatEvent.SignedAmount < 0L)
            {
                playerDamageTaken = checked(playerDamageTaken - combatEvent.SignedAmount);
                feedback.HandlePlayerEvent(combatEvent, PlayerFeedbackTargetId);
            }
        }

        private void PublishRecurringTutorialEvents()
        {
            if (tutorial == null || tutorial.Tutorial.State == TutorialSequenceState.Completed)
            {
                return;
            }

            SkillConfig ultimate = config.GetSkill(playerConfig.UltimateSkillId);
            if (player.Current.CurrentEnergy >= ultimate.EnergyCost)
            {
                NotifyTutorial(TutorialEventType.UltimateReady);
            }

            if (world.ActiveCount >= 3)
            {
                NotifyTutorial(TutorialEventType.WaveMultiTarget, world.ActiveCount);
            }
        }

        private void NotifyTutorial(
            TutorialEventType eventType,
            long value = 1L,
            TutorialGestureType gesture = TutorialGestureType.Any)
        {
            tutorial?.NotifyGameplayEvent(new TutorialGameplayEvent(
                eventType,
                value,
                gesture));
        }

        private void Settle(BattleFlowState state)
        {
            BattleSettlement settlement = state == BattleFlowState.Victory
                ? BattleSettlement.Victory
                : BattleSettlement.Defeat;
            receipt = results.Settle(new ResultRequest(
                $"runtime:{Guid.NewGuid():N}",
                LevelId,
                settlement,
                new BattleResultMetrics(
                    score.Current.TotalScore,
                    reflectedProjectileCount,
                    playerDamageTaken,
                    battle.Flow.Time.Current.GameplayElapsedSeconds)));
        }

        private void CreateBackground(LevelConfig level)
        {
            Sprite sprite = assets.GetSprite(level.BackgroundAssetKey);
            var background = new GameObject("Configured Background", typeof(SpriteRenderer));
            background.transform.SetParent(referenceRoot, false);
            SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            float width = ReadReference(ConfigIds.GlobalKeys.ReferenceWidth);
            float height = ReadReference(ConfigIds.GlobalKeys.ReferenceHeight);
            background.transform.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f);
            background.transform.localScale = new Vector3(
                width / sprite.bounds.size.x,
                height / sprite.bounds.size.y,
                1f);
        }

        private SpriteRenderer CreatePlayerVisual(PlayerConfig configuredPlayer)
        {
            Sprite sprite = assets.GetSprite(configuredPlayer.AssetKey);
            var playerObject = new GameObject("Configured Player", typeof(SpriteRenderer));
            playerObject.transform.SetParent(referenceRoot, false);
            SpriteRenderer renderer = playerObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;
            playerObject.transform.localScale = Vector3.one * sprite.pixelsPerUnit;
            float width = ReadReference(ConfigIds.GlobalKeys.ReferenceWidth);
            float height = ReadReference(ConfigIds.GlobalKeys.ReferenceHeight);
            playerObject.transform.localPosition = new Vector3(
                width * 0.16f,
                height * 0.34f,
                0f);
            return renderer;
        }

        private float ReadReference(string key)
        {
            GlobalConfig row = config.GetGlobal(key);
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new InvalidOperationException(
                    $"Global '{key}' must define a positive reference dimension.");
            }

            return row.IntValue.Value;
        }

        private static TutorialGestureType ToTutorialGesture(GestureType gesture)
        {
            return gesture switch
            {
                GestureType.Any => TutorialGestureType.Any,
                GestureType.Horizontal => TutorialGestureType.Horizontal,
                GestureType.Vertical => TutorialGestureType.Vertical,
                GestureType.Diagonal => TutorialGestureType.Diagonal,
                GestureType.Arc => TutorialGestureType.Arc,
                GestureType.Circle => TutorialGestureType.Circle,
                GestureType.Charged => TutorialGestureType.Charged,
                _ => TutorialGestureType.Any,
            };
        }

        private static void ConfigureReferenceSpace(
            Transform referenceSpace,
            Camera camera,
            IConfigProvider configProvider)
        {
            float width = ReadPositive(configProvider, ConfigIds.GlobalKeys.ReferenceWidth);
            float height = ReadPositive(configProvider, ConfigIds.GlobalKeys.ReferenceHeight);
            float distance = Vector3.Dot(
                Vector3.zero - camera.transform.position,
                camera.transform.forward);
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
            Vector3 bottomRight = camera.ViewportToWorldPoint(new Vector3(1f, 0f, distance));
            Vector3 topLeft = camera.ViewportToWorldPoint(new Vector3(0f, 1f, distance));
            referenceSpace.position = bottomLeft;
            referenceSpace.rotation = camera.transform.rotation;
            referenceSpace.localScale = new Vector3(
                Vector3.Distance(bottomLeft, bottomRight) / width,
                Vector3.Distance(bottomLeft, topLeft) / height,
                1f);
        }

        private static float ReadPositive(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive reference dimension.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

        private static void Destroy(GameObject gameObject)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class SystemRandomSource : IRandomSource
        {
            private readonly System.Random value = new System.Random();

            public double NextUnitInterval()
            {
                return value.NextDouble();
            }
        }

        private sealed class SessionDriver : MonoBehaviour
        {
            private ProductionBattleSession session;

            public void Initialize(ProductionBattleSession configuredSession)
            {
                session = configuredSession;
            }

            public void Detach()
            {
                session = null;
            }

            private void Update()
            {
                session?.Advance(Time.unscaledDeltaTime);
            }
        }
    }
}
