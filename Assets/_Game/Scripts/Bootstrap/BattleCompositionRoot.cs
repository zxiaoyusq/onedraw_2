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
    // 定义 BattleCompositionRoot 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    public sealed class BattleCompositionRoot : MonoBehaviour
    {
        private ResultService results;
        private BattleResultNavigation navigation;
        private SceneFlowService sceneFlow;

        public ProductionBattleSession CurrentSession =>
            navigation?.Current as ProductionBattleSession;

        public uint SessionGeneration => navigation?.Generation ?? 0U;

        // 启动 Start 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void Start()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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

        // 响应 OnDestroy 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnDestroy()
        {
            navigation?.Dispose();
            navigation = null;
        }

        // 响应 OnApplicationFocus 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnApplicationFocus(bool hasFocus)
        {
            CurrentSession?.SetApplicationFocus(hasFocus);
        }

        // 响应 OnApplicationPause 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnApplicationPause(bool paused)
        {
            CurrentSession?.SetApplicationPaused(paused);
        }

        // 处理 Restart 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void Restart()
        {
            navigation.Restart();
        }

        // 处理 GoNext 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void GoNext()
        {
            ProductionBattleSession current = CurrentSession;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (current?.Receipt != null && current.Receipt.CanGoNext)
            {
                navigation.GoNext(current.Receipt);
            }
        }

        // 处理 ReturnToMainMenu 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void ReturnToMainMenu()
        {
            BattleLaunchContext.Clear();
            sceneFlow.LoadMainMenu();
        }

        // 处理 ResolveInitialLevel 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static string ResolveInitialLevel(
            IConfigProvider config,
            ProgressSnapshot progress)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (BattleLaunchContext.HasSelection)
            {
                string selected = config.GetLevel(
                    BattleLaunchContext.SelectedLevelId).LevelId;
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (progress.IsLevelUnlocked(selected))
                {
                    return selected;
                }
            }

            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < config.GetLevels().Count; index++)
            {
                LevelConfig level = config.GetLevels()[index];
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (progress.IsLevelUnlocked(level.LevelId))
                {
                    return level.LevelId;
                }
            }

            throw new InvalidOperationException(
                "Configured progress must expose at least one playable root level.");
        }
    }

    // 定义 ProductionBattleSessionFactory 的入口装配契约，集中管理场景、服务与战斗会话所有权。
    internal sealed class ProductionBattleSessionFactory : IBattleSessionFactory
    {
        private readonly IConfigProvider config;
        private readonly IAssetRegistry assets;
        private readonly ResultService results;
        private readonly Transform parent;
        private readonly Action restart;
        private readonly Action goNext;
        private readonly Action mainMenu;

        // 初始化 ProductionBattleSessionFactory，并建立生产入口或战斗会话的依赖关系。
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

        // 创建 Create 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

    // 定义 ProductionBattleSession 的入口装配契约，集中管理场景、服务与战斗会话所有权。
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
        private readonly StrokeTrailPool trailPool;
        private readonly Material trailMaterial;
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
        private readonly Animator playerAnimator;
        private readonly SessionDriver driver;
        private ResultReceipt receipt;
        private long playerDamageTaken;
        private int reflectedProjectileCount;
        private int completedStrokeCount;
        private int lastResolvedHitCount;
        private bool disposed;

        // 初始化 ProductionBattleSession，并建立生产入口或战斗会话的依赖关系。
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
            GameObject playerObject = CreatePlayerVisual(playerConfig);
            playerRenderer = playerObject.GetComponentInChildren<SpriteRenderer>(true);
            if (playerRenderer == null || playerRenderer.sprite == null)
            {
                throw new InvalidOperationException(
                    $"Player asset '{playerConfig.AssetKey}' must expose a configured SpriteRenderer.");
            }

            playerAnimator = playerObject.GetComponent<Animator>();
            player = playerObject.AddComponent<PlayerCombatController>();
            player.Initialize(config, playerConfig.PlayerId);
            world = new ProductionBattleWorld(config, assets, player, referenceRoot);

            LevelConfig level = config.GetLevel(LevelId);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            world.EnemyDefeatedByProjectile += OnEnemyDefeatedByProjectile;
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
            strokeCollector.StrokeStarted += OnStrokeStarted;
            strokeCollector.StrokePointAdded += OnStrokePointAdded;
            strokeCollector.StrokeCompleted += OnStrokeCompleted;
            strokeCollector.StrokeCanceled += OnStrokeCanceled;
            trailMaterial = CreateTrailMaterial();
            VfxCueConfig trailCue = config.GetVfxCue(ConfigIds.VfxCues.VfxSlash);
            GameObject trailViewPrefab = assets.GetPrefab(trailCue.AssetKey);
            var trailRoot = new GameObject("Stroke Trail Pool");
            trailRoot.transform.SetParent(root.transform, false);
            trailPool = trailRoot.AddComponent<StrokeTrailPool>();
            trailPool.Initialize(
                StrokeTrailSettingsFactory.CreatePoolSettings(
                    config,
                    ConfigIds.VfxCues.VfxSlash),
                trailMaterial,
                referenceRoot,
                trailViewPrefab);

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
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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

        public int ActiveProjectileCount => world.PendingProjectileCount;

        // 获取 GetActiveProjectile 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public ProjectileController GetActiveProjectile(int index) =>
            world.GetActiveProjectile(index);

        public BattleFlowState FlowState => battle.Flow.State;

        public BattleHudView HudView => hud.View;

        public TutorialOverlayView TutorialView => tutorialOverlay?.View;

        public PlayerCombatController Player => player;

        public bool IsBossSession => boss != null;

        public double GameplayTimestamp =>
            battle.Flow.Time.Current.GameplayElapsedSeconds;

        public int CompletedStrokeCount => completedStrokeCount;

        public int LastResolvedHitCount => lastResolvedHitCount;

        // 处理 Advance 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Advance(float unscaledDeltaSeconds)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed)
            {
                return;
            }

            BattleFlowAdvanceReport report;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (boss != null)
            {
                report = boss.Advance(unscaledDeltaSeconds);
            }
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (boss != null)
            {
                world.AdvanceBoss(boss.BossPhases);
            }

            PublishRecurringTutorialEvents();
            feedbackRuntime.Advance(unscaledDeltaSeconds);
            hudBinding.UpdateUltimateClock(
                gameplayTimestamp,
                skills.GetCooldownUntil(battle.Flow.Settings.UltimateSkillId));
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (report.SettledThisAdvance && receipt == null)
            {
                Settle(report.State);
            }
        }

        // 设置 SetPlayerPaused 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void SetPlayerPaused(bool paused)
        {
            battle.SetPlayerPaused(paused);
        }

        // 处理 SwitchStance 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void SwitchStance()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!switched.DidSwitch)
            {
                return;
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.IsNullOrEmpty(switched.OnSwitchEffectGroupId))
            {
                skills.ExecuteEffectGroup(
                    switched.OnSwitchEffectGroupId,
                    switched.Current.StanceId,
                    new SkillEffectContext(world, timestamp));
            }

            NotifyTutorial(TutorialEventType.StanceChanged);
        }

        // 处理 BeginUltimateDrawing 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void BeginUltimateDrawing()
        {
            battle.TryBeginUltimateDrawing();
        }

        // 处理 Restart 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Restart()
        {
            restart();
        }

        // 处理 GoNext 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void GoNext()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (receipt != null && receipt.CanGoNext)
            {
                goNext();
            }
        }

        // 处理 ReturnToMainMenu 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void ReturnToMainMenu()
        {
            mainMenu();
        }

        // 设置 SetApplicationFocus 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void SetApplicationFocus(bool hasFocus)
        {
            battle.SetApplicationFocus(hasFocus);
        }

        // 设置 SetApplicationPaused 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void SetApplicationPaused(bool paused)
        {
            battle.SetApplicationPaused(paused);
        }

        // 释放 Dispose 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        public void Dispose()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed)
            {
                return;
            }

            disposed = true;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (driver != null)
            {
                driver.Detach();
            }

            player.CombatEventPublished -= OnPlayerCombatEvent;
            world.EnemySpawned -= OnEnemySpawned;
            world.EnemyReleased -= OnEnemyReleased;
            world.EnemyDefeatedByProjectile -= OnEnemyDefeatedByProjectile;
            world.AttackExecuted -= OnAttackExecuted;
            strokeCollector.StrokeStarted -= OnStrokeStarted;
            strokeCollector.StrokePointAdded -= OnStrokePointAdded;
            strokeCollector.StrokeCompleted -= OnStrokeCompleted;
            strokeCollector.StrokeCanceled -= OnStrokeCanceled;
            strokeCollector.Dispose();
            trailPool.Clear();
            tutorialOverlay?.Dispose();
            hud?.Dispose();
            hudBinding.Dispose();
            feedbackRuntime.Dispose();
            boss?.Dispose();
            world.Dispose();
            Destroy(root);
            Destroy(trailMaterial);
        }

        // 响应 OnStrokeCompleted 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnStrokeCompleted(StrokeData stroke)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed ||
                (battle.Flow.State != BattleFlowState.Playing &&
                 battle.Flow.State != BattleFlowState.UltimateDrawing))
            {
                trailPool.CancelPreview(stroke.StrokeId);
                return;
            }

            completedStrokeCount += 1;
            lastResolvedHitCount = 0;

            StrokeRuleConfig sampling = config.GetStrokeRule(ConfigIds.StrokeRules.StrokeAny);
            StrokeGeometryData geometry = StrokeGeometry.Process(
                stroke,
                StrokeGeometrySettingsFactory.FromConfig(sampling));
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (geometry.PointCount >= 2)
            {
                trailPool.CompletePreview(
                    StrokeTrailPath.FromGeometry(geometry),
                    StrokeTrailSettingsFactory.CreateStyle(
                        config,
                        player.Current.StanceId,
                        ConfigIds.VfxCues.VfxSlash));
            }
            else
            {
                trailPool.CancelPreview(stroke.StrokeId);
            }
            GestureMatchResult gesture = classifier.Classify(geometry);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!gesture.IsMatch)
            {
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (battle.Flow.State == BattleFlowState.UltimateDrawing)
                {
                    battle.CancelUltimateDrawing();
                }

                return;
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (battle.Flow.State == BattleFlowState.UltimateDrawing)
            {
                ResolveUltimate(stroke, gesture);
                return;
            }

            // 攻击动画只消费已确认的普通有效笔势，不通过动画事件驱动命中或伤害。
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(T694PlayerAnimationContract.AttackTriggerHash);
            }

            NotifyTutorial(
                TutorialEventType.ValidStroke,
                1L,
                ToTutorialGesture(gesture.GestureType));

            Physics2D.SyncTransforms();
            StrokeHitRule hitRule = StrokeHitSettingsFactory.CreateRule(
                config.GetStrokeRule(gesture.RuleId));
            int count = resolver.Resolve(geometry, gesture, hitRule, hits);
            lastResolvedHitCount = count;
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            // 逐项装配或释放会话资源，保持创建与回收顺序一致。
            for (int index = 0; index < count; index++)
            {
                HitRecord hit = hits[index];
                // 投射物必须由真实笔迹路径命中；规则决定切断、反弹、架势不符或不可切断。
                if (hit.Target is ProjectileHitTarget)
                {
                    if (world.TryResolveProjectileStroke(
                            hit,
                            player.Current.StanceId,
                            out ProjectileStrokeResult projectileResult) &&
                        (projectileResult.Outcome == ProjectileStrokeOutcome.Cut ||
                         projectileResult.Outcome == ProjectileStrokeOutcome.Reflected))
                    {
                        reflectedProjectileCount += 1;
                        NotifyTutorial(TutorialEventType.ProjectileCut);
                    }

                    continue;
                }

                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (hit.IsWeakpoint)
                {
                    NotifyTutorial(TutorialEventType.WeakpointHit);
                }

                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (applied.Damage.ArmorBroken)
                {
                    NotifyTutorial(TutorialEventType.ArmorBroken);
                }

                // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
                if (!enemy.IsAlive)
                {
                    NotifyEnemyDefeated(entityId);
                }
            }
        }

        // 响应 OnStrokeStarted 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnStrokeStarted(StrokePreviewPointEvent preview)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (disposed ||
                (battle.Flow.State != BattleFlowState.Playing &&
                 battle.Flow.State != BattleFlowState.UltimateDrawing))
            {
                return;
            }

            trailPool.BeginPreview(
                preview.StrokeId,
                preview.ReferencePosition,
                StrokeTrailSettingsFactory.CreateStyle(
                    config,
                    player.Current.StanceId,
                    ConfigIds.VfxCues.VfxSlash));
        }

        // 响应 OnStrokePointAdded 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnStrokePointAdded(StrokePreviewPointEvent preview)
        {
            trailPool.TryAppendPreviewPoint(
                preview.StrokeId,
                preview.ReferencePosition);
        }

        // 响应 OnStrokeCanceled 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnStrokeCanceled(StrokeCanceledEvent canceled)
        {
            trailPool.CancelPreview(canceled.StrokeId);
        }

        // 处理 ResolveUltimate 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void ResolveUltimate(StrokeData stroke, GestureMatchResult gesture)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
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
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (tutorial != null)
            {
                tutorial.ResolveUltimate(stroke.StrokeId, activation);
            }
            else
            {
                battle.ResolveUltimate(stroke.StrokeId, activation);
            }
        }

        // 处理 NotifyEnemyDefeated 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void NotifyEnemyDefeated(long entityId)
        {
            bool accepted = boss != null
                ? boss.NotifyEnemyDefeated(entityId)
                : tutorial != null
                    ? tutorial.NotifyEnemyDefeated(entityId)
                    : battle.NotifyEnemyDefeated(entityId);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (accepted)
            {
                // 先在目标仍注册且位置有效时发布纯表现死亡事件，再回收怪物实体。
                if (world.TryGetEnemyController(entityId, out EnemyController defeatedEnemy))
                {
                    feedback.HandleEnemyDeath(
                        defeatedEnemy.Damage.HitTargetId,
                        $"enemy_death:{entityId}",
                        battle.Flow.Time.Current.GameplayElapsedSeconds);
                }

                world.Release(entityId);
            }
        }

        // 响应 OnEnemySpawned 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnEnemySpawned(
            long entityId,
            EnemyController controller,
            SpriteRenderer[] renderers)
        {
            feedbackRuntime.RegisterTarget(
                controller.Damage.HitTargetId,
                controller.transform,
                renderers);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (controller.Definition.Weakpoint.HasHitbox)
            {
                NotifyTutorial(TutorialEventType.EnemyWeakpointShown);
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (controller.Damage.MaximumArmor > 0L)
            {
                NotifyTutorial(TutorialEventType.ArmoredEnemySpawned);
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (string.Equals(
                    controller.Definition.EnemyId,
                    ConfigIds.Enemies.EnemySkeletonGhost,
                    StringComparison.Ordinal))
            {
                NotifyTutorial(TutorialEventType.GhostSpawned);
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (world.ActiveCount >= 3)
            {
                NotifyTutorial(TutorialEventType.WaveMultiTarget, world.ActiveCount);
            }
        }

        // 响应 OnEnemyReleased 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnEnemyReleased(int hitTargetId)
        {
            feedbackRuntime.UnregisterTarget(hitTargetId);
        }

        // 响应反弹投射物造成的敌人死亡，并复用既有关卡记账、死亡反馈和实体回收链。
        private void OnEnemyDefeatedByProjectile(long entityId)
        {
            NotifyEnemyDefeated(entityId);
        }

        // 响应 OnAttackExecuted 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnAttackExecuted(EnemyAttackAction action)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!string.IsNullOrEmpty(action.ProjectileId))
            {
                NotifyTutorial(TutorialEventType.ProjectileSpawned);
            }
        }

        // 响应 OnPlayerCombatEvent 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void OnPlayerCombatEvent(PlayerCombatEvent combatEvent)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (combatEvent.EventType == PlayerCombatEventType.HpChanged &&
                combatEvent.SignedAmount < 0L)
            {
                playerDamageTaken = checked(playerDamageTaken - combatEvent.SignedAmount);
                feedback.HandlePlayerEvent(combatEvent, PlayerFeedbackTargetId);
            }
        }

        // 处理 PublishRecurringTutorialEvents 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private void PublishRecurringTutorialEvents()
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (tutorial == null || tutorial.Tutorial.State == TutorialSequenceState.Completed)
            {
                return;
            }

            SkillConfig ultimate = config.GetSkill(playerConfig.UltimateSkillId);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (player.Current.CurrentEnergy >= ultimate.EnergyCost)
            {
                NotifyTutorial(TutorialEventType.UltimateReady);
            }

            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (world.ActiveCount >= 3)
            {
                NotifyTutorial(TutorialEventType.WaveMultiTarget, world.ActiveCount);
            }
        }

        // 处理 NotifyTutorial 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 设置 Settle 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 创建 CreateBackground 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 创建 CreatePlayerVisual 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private GameObject CreatePlayerVisual(PlayerConfig configuredPlayer)
        {
            AssetManifestConfig asset = config.GetAsset(configuredPlayer.AssetKey);
            GameObject playerObject;
            if (string.Equals(asset.AssetType, "Sprite", StringComparison.Ordinal))
            {
                Sprite sprite = assets.GetSprite(configuredPlayer.AssetKey);
                playerObject = new GameObject("Configured Player", typeof(SpriteRenderer));
                playerObject.GetComponent<SpriteRenderer>().sprite = sprite;
            }
            else if (string.Equals(asset.AssetType, "Prefab", StringComparison.Ordinal))
            {
                playerObject = UnityEngine.Object.Instantiate(
                    assets.GetPrefab(configuredPlayer.AssetKey));
                playerObject.name = "Configured Player";
            }
            else
            {
                throw new InvalidOperationException(
                    $"Player asset '{configuredPlayer.AssetKey}' must be a Sprite or Prefab.");
            }

            playerObject.transform.SetParent(referenceRoot, false);
            SpriteRenderer renderer = playerObject.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null || renderer.sprite == null)
            {
                Destroy(playerObject);
                throw new InvalidOperationException(
                    $"Player asset '{configuredPlayer.AssetKey}' must expose a configured SpriteRenderer.");
            }

            playerObject.transform.localScale = Vector3.one * renderer.sprite.pixelsPerUnit;
            float width = ReadReference(ConfigIds.GlobalKeys.ReferenceWidth);
            float height = ReadReference(ConfigIds.GlobalKeys.ReferenceHeight);
            playerObject.transform.localPosition = new Vector3(
                width * 0.16f,
                height * 0.34f,
                0f);
            BoxCollider2D body = playerObject.GetComponent<BoxCollider2D>();
            if (body == null)
            {
                body = playerObject.AddComponent<BoxCollider2D>();
            }

            // 身体碰撞体只承担生产战斗的接触判定，不参与笔迹命中查询语义。
            body.isTrigger = true;
            body.offset = playerObject.transform.InverseTransformPoint(
                renderer.transform.TransformPoint(renderer.sprite.bounds.center));
            body.size = renderer.sprite.bounds.size;
            return playerObject;
        }

        // 处理 ReadReference 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private float ReadReference(string key)
        {
            GlobalConfig row = config.GetGlobal(key);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new InvalidOperationException(
                    $"Global '{key}' must define a positive reference dimension.");
            }

            return row.IntValue.Value;
        }

        // 处理 ToTutorialGesture 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 创建 CreateTrailMaterial 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static Material CreateTrailMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Production stroke trails require the built-in Sprites/Default shader.");
            }

            return new Material(shader)
            {
                name = "Production Stroke Trail Material",
            };
        }

        // 处理 ConfigureReferenceSpace 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
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

        // 处理 ReadPositive 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static float ReadPositive(IConfigProvider configProvider, string key)
        {
            GlobalConfig row = configProvider.GetGlobal(key);
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (!row.IntValue.HasValue || row.IntValue.Value <= 0L)
            {
                throw new ArgumentException(
                    $"Global '{key}' must define a positive reference dimension.",
                    nameof(configProvider));
            }

            return row.IntValue.Value;
        }

        // 处理 Destroy 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void Destroy(GameObject gameObject)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        // 处理 Destroy 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
        private static void Destroy(UnityEngine.Object value)
        {
            // 检查入口状态、依赖或生命周期边界，避免重复装配和悬空引用。
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        // 定义 SystemRandomSource 的入口装配契约，集中管理场景、服务与战斗会话所有权。
        private sealed class SystemRandomSource : IRandomSource
        {
            private readonly System.Random value = new System.Random();

            // 处理 NextUnitInterval 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
            public double NextUnitInterval()
            {
                return value.NextDouble();
            }
        }

        // 定义 SessionDriver 的入口装配契约，集中管理场景、服务与战斗会话所有权。
        private sealed class SessionDriver : MonoBehaviour
        {
            private ProductionBattleSession session;

            // 处理 Initialize 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
            public void Initialize(ProductionBattleSession configuredSession)
            {
                session = configuredSession;
            }

            // 处理 Detach 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
            public void Detach()
            {
                session = null;
            }

            // 更新 Update 对应的入口装配逻辑，并维护会话所有权和跨场景边界。
            private void Update()
            {
                session?.Advance(Time.unscaledDeltaTime);
            }
        }
    }
}
