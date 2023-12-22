using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;

namespace GlobalGoopTracker
{
    public static class GlobalGoopTrackerMod
    {
        public const int NON_BIOME_INDEX = -1;
        public static ManualLogSource log;

        public static Dictionary<int, Dictionary<string, float>> biomePollution = new Dictionary<int, Dictionary<string, float>>();

        public static BiomeManager nonBiomeManager;

        public static void AddBiomeToDictionary(BiomeManager bm)
        {
            log.LogInfo(bm.biomeDisplayName);
            log.LogInfo(bm.currentGoopPollution);
            log.LogInfo(bm.currentPlasticCloudPollution);
            log.LogInfo(bm.currentLitterPollution);
            int biomeIndex = bm.biomeIndex;
            if (!biomePollution.ContainsKey(biomeIndex))
            {
                biomePollution.Add(biomeIndex, new Dictionary<string, float>());
            }
            biomePollution[biomeIndex]["goopPollution"] = bm.currentGoopPollution;
            biomePollution[biomeIndex]["plasticCloudPollution"] = bm.currentPlasticCloudPollution;
            biomePollution[biomeIndex]["litterPollution"] = bm.currentLitterPollution;
        }

        public static void UpdateBiomePollution(GameEvent e)
        {
            BiomeManager bm = ((BiomePollutionUpdated)e).biome;
            int biomeIndex = bm.biomeIndex;
            log.LogInfo($"Updating biome {biomeIndex}");
            if (biomePollution.ContainsKey(biomeIndex))
            {
                biomePollution[biomeIndex]["goopPollution"] = bm.currentGoopPollution;
                biomePollution[biomeIndex]["plasticCloudPollution"] = bm.currentPlasticCloudPollution;
                biomePollution[biomeIndex]["litterPollution"] = bm.currentLitterPollution;
            }
        }

        public static void AddNonBiomeManager()
        {
            // Create a new BiomeManager just for keeping track of the non-biome pollution
            GameObject go = new GameObject("NonBiomeManager");
            go.AddComponent<BiomeManager>();
            nonBiomeManager = go.GetComponent<BiomeManager>();
            GoopPatches.nonBiomeManager = nonBiomeManager;
            BiomeManager_Patch.nonBiomeManager = nonBiomeManager;

            nonBiomeManager.biomeName = "NonBiome";
            //nonBiomeManager.biomeDisplayName = "Non-Biome"; //Can't use a regular string here
            nonBiomeManager.biomeIndex = NON_BIOME_INDEX;
            nonBiomeManager.pollutionPerLitterPickup = 1.0f;
            nonBiomeManager.pollutionPerLitterChunk = 1.0f;
        }

        public static void FixNonBiomeGoop()
        {
            // Some plants/litter/goop might have a reference to an incorrect biome, so fix it
            GameObject nonBiomeParent = GameObject.Find("NonBiome_Decor");
            foreach (FoodPlant foodPlant in nonBiomeParent.GetComponentsInChildren<FoodPlant>())
            {
                if (foodPlant.parentBiome != null)
                {
                    log.LogInfo($"Found non-biome plant {foodPlant.saveID} with reference to {foodPlant.parentBiome}, fixing");
                    foodPlant.parentBiome = null;
                }
            }
            foreach (Pickup pickup in nonBiomeParent.GetComponentsInChildren<Pickup>())
            {
                if (pickup.surroundingBiome != null)
                {
                    log.LogInfo($"Found non-biome pickup {pickup.saveID} with reference to {pickup.surroundingBiome}, fixing");
                    pickup.surroundingBiome = null;
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                foreach (int bi in biomePollution.Keys)
                {
                    BiomeManager bm;
                    bm = bi != -1 ? EngineHub.BiomeSaver.LookUpBiomeByID(bi) : nonBiomeManager;
                    log.LogInfo(bm.biomeDisplayName);
                    log.LogInfo($"goop: {biomePollution[bi]["goopPollution"]}");
                    log.LogInfo($"plastic: {biomePollution[bi]["plasticCloudPollution"]}");
                    log.LogInfo($"litter: {biomePollution[bi]["litterPollution"]}");
                }
            }
        }

        [HarmonyPatch(typeof(GameProgressTracker), nameof(GameProgressTracker.HandleBiomePollutionShift))]
        [HarmonyPrefix]
        public static bool HandleBiomePollutionShift_Prefix(GameEvent e)
        {
            // Don't handle biome pollution shifting for non-biome pollution
            if (((BiomePollutionUpdated)e).biome.biomeIndex == NON_BIOME_INDEX)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Start))]
        [HarmonyPostfix]
        public static void MainMenuStart_Postfix(MainMenu __instance)
        {
            GameObject versionLabelObject = __instance.gameObject.transform.Find("Version Text").gameObject;
            TextMeshProUGUI versionLabel = versionLabelObject.GetComponent<TextMeshProUGUI>();
            string version = versionLabel.text;
            versionLabel.text = $"{version}\n{GlobalGoopTrackerPlugin.pluginName} v{GlobalGoopTrackerPlugin.versionString}";
            //versionLabel.ForceMeshUpdate();
        }
    }
}
