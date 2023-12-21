﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalQuantities.Objects
{
    public class RoundState : MonoBehaviour
    {
        public Plugin plugin { get; internal set; }
        public Scene scene { get; internal set; }
        public LevelConfiguration levelConfiguration { get; set; }

        public List<SpawnableEnemyWithRarity> enemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> daytimeEnemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> outsideEnemies { get; } = new List<SpawnableEnemyWithRarity>();

        public List<GameObject> modifiedEnemyTypes { get; } = new List<GameObject>();
        public void initialize()
        {
            Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable enemy options");
            if (levelConfiguration.enemies.enabled.Value)
            {
                populate(enemies, levelConfiguration.enemies.enemyTypes);
            }
            if (levelConfiguration.daytimeEnemies.enabled.Value)
            {
                populate(daytimeEnemies, levelConfiguration.daytimeEnemies.enemyTypes, true, true);
            }
            if (levelConfiguration.outsideEnemies.enabled.Value)
            {
                populate(outsideEnemies, levelConfiguration.outsideEnemies.enemyTypes, true);
            }
        }

        public void OnDestroy()
        {
            foreach (GameObject obj in modifiedEnemyTypes)
            {
                Destroy(obj);
            }
            modifiedEnemyTypes.Clear();
        }

        public void Update()
        {
            foreach (var item in RoundManager.Instance.SpawnedEnemies)
            {
                if (item.hideFlags != HideFlags.None)
                {
                    item.hideFlags = HideFlags.None;
                }
            }
        }

        private void populate(List<SpawnableEnemyWithRarity> enemiesList, List<EnemyTypeConfiguration> configs, bool isOutside = false, bool isDaytimeEnemy = false)
        {
            foreach (var item in configs)
            {
                int rarity = item.rarity.Value;
                int maxEnemyCount = item.maxEnemyCount.Value;
                if (rarity > 0 && maxEnemyCount > 0)
                {
                    EnemyType type = Instantiate(item.type);
                    type.MaxCount = item.maxEnemyCount.Value;
                    type.PowerLevel = item.powerLevel.Value;
                    type.probabilityCurve = item.spawnCurve.Value;
                    type.numberSpawnedFalloff = item.spawnFalloffCurve.Value;
                    type.useNumberSpawnedFalloff = item.useSpawnFalloff.Value;
                    type.isOutsideEnemy = isOutside;
                    type.isDaytimeEnemy = isDaytimeEnemy;

                    instantiateEnemyTypeObject(type);

                    SpawnableEnemyWithRarity spawnable = new SpawnableEnemyWithRarity();
                    spawnable.enemyType = type;
                    spawnable.rarity = rarity;
                    enemiesList.Add(spawnable);
                }
            }
        }

        private void instantiateEnemyTypeObject(EnemyType type)
        {
            GameObject obj = type.enemyPrefab;
            bool isActive = obj.activeSelf;
            obj.SetActive(false);

            GameObject copy = Instantiate(obj, new Vector3(0, -50, 0), Quaternion.identity);
            modifiedEnemyTypes.Add(copy);
            SceneManager.MoveGameObjectToScene(copy, SceneManager.GetSceneByName("SampleSceneRelay"));
            type.enemyPrefab = copy;
            copy.GetComponent<EnemyAI>().enemyType = type;
            obj.SetActive(isActive);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.SetActive(isActive);

            DontDestroyOnLoad(copy);
        }
    }
}
