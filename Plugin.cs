﻿using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using LethalQuantities.Objects;
using System.IO;
using LethalQuantities.Patches;

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        private Dictionary<string, LevelConfiguration> levelConfigs = new Dictionary<string, LevelConfiguration>();

        private Harmony _harmony;

        private void Awake()
        {
            if (INSTANCE != null && INSTANCE != this)
            {
                Destroy(this);
            }
            else
            {
                INSTANCE = this;
            }

            LETHAL_LOGGER = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

            TomlTypeConverter.AddConverter(typeof(AnimationCurve), new AnimationCurveTypeConverter());

            _harmony = new Harmony("LethalQuantities");
            _harmony.PatchAll(typeof(RoundManagerPatch));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SampleSceneRelay")
            {
                StartOfRound instance = StartOfRound.Instance;
                HashSet<EnemyType> enemies = new HashSet<EnemyType>();

                // Get all enemy types
                foreach (SelectableLevel level in instance.levels)
                {
                    AddAllTo(enemies, level.Enemies);
                    AddAllTo(enemies, level.DaytimeEnemies);
                    AddAllTo(enemies, level.OutsideEnemies);
                }

                foreach (SelectableLevel level in instance.levels)
                {
                    string levelSaveDir = Path.Combine(LEVEL_SAVE_DIR, level.sceneName);
                    levelConfigs.Add(level.sceneName, new LevelConfiguration(levelSaveDir, level, enemies));
                }

                LETHAL_LOGGER.LogInfo("Printing out default moon info");
                foreach (var level in instance.levels)
                {
                    LETHAL_LOGGER.LogInfo("\tName: " + level.sceneName);
                    LETHAL_LOGGER.LogInfo("\tPlanet Name: " + level.PlanetName);
                    LETHAL_LOGGER.LogInfo("\tMax enemy power count: " + level.maxEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tMax daytime enemy power count: " + level.maxDaytimeEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tMax outside enemy power count: " + level.maxOutsideEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tSpawn probability range: " + level.spawnProbabilityRange);
                    LETHAL_LOGGER.LogInfo("\tDaytime spawn probability range: " + level.daytimeEnemiesProbabilityRange);
                    LETHAL_LOGGER.LogInfo("\tEnemy spawn curve:");
                    PrintAnimationCurve(level.enemySpawnChanceThroughoutDay);
                    LETHAL_LOGGER.LogInfo("\tDaytime enemy spawn curve:");
                    PrintAnimationCurve(level.daytimeEnemySpawnChanceThroughDay);
                    LETHAL_LOGGER.LogInfo("\tOutside enemy spawn curve:");
                    PrintAnimationCurve(level.outsideEnemySpawnChanceThroughDay);
                    LETHAL_LOGGER.LogInfo("\tEnemy spawn info:");
                    PrintEnemySpawnTypes(level.Enemies);
                    LETHAL_LOGGER.LogInfo("\tDaytime enemy spawn info:");
                    PrintEnemySpawnTypes(level.DaytimeEnemies);
                    LETHAL_LOGGER.LogInfo("\tOutside nemy spawn info:");
                    PrintEnemySpawnTypes(level.OutsideEnemies);
                    LETHAL_LOGGER.LogInfo("\tLevel size multiplier: " + level.factorySizeMultiplier);
                }

                LETHAL_LOGGER.LogInfo("Printing out default enemy info:");
                foreach (var enemyType in enemies)
                {
                    LETHAL_LOGGER.LogInfo("\tEnemy: " + enemyType.enemyName);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy max count: " + enemyType.MaxCount);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy power level: " + enemyType.PowerLevel);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy spawn curve:");
                    PrintAnimationCurve(enemyType.probabilityCurve);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy spawn falloff curve: " + enemyType.useNumberSpawnedFalloff);
                    PrintAnimationCurve(enemyType.numberSpawnedFalloff);
                }
            }
            else if (levelConfigs.ContainsKey(scene.name))
            {
                LETHAL_LOGGER.LogInfo($"Found scene {scene.name}");

                if (RoundManager.Instance.IsServer)
                {
                    // Add a manager to keep track of all objects
                    GameObject levelModifier = new GameObject("LevelModifier");
                    SceneManager.MoveGameObjectToScene(levelModifier, scene);

                    RoundState state = levelModifier.AddComponent<RoundState>();
                    state.plugin = this;
                    state.levelConfiguration = levelConfigs[scene.name];
                    state.scene = scene;
                    state.initialize();
                }
            }
        }

        private static void AddAllTo(HashSet<EnemyType> enemies, List<SpawnableEnemyWithRarity> spawnables)
        {
            foreach (var enemy in spawnables)
            {
                enemies.Add(enemy.enemyType);
            }
        }

        static void PrintAnimationCurve(AnimationCurve curve)
        {
            int i = 0;
            foreach (var frame in curve.GetKeys())
            {
                LETHAL_LOGGER.LogInfo("\t\tFrame " + i++ + ": " + frame.m_Time + "\t" + frame.m_Value);
            }
        }

        static void PrintEnemySpawnTypes(List<SpawnableEnemyWithRarity> enemies)
        {
            foreach (var enemy in enemies)
            {
                EnemyType enemyType = enemy.enemyType;
                LETHAL_LOGGER.LogInfo("\t\tEnemy: " + enemyType.enemyName + ": " + enemy.rarity);
            }
        }
    }
}