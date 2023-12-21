using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GlobalGoopTracker
{
    public static class GlobalGoopTrackerMod
    {
        public static KeyCode testKey = KeyCode.F3;
        public static bool toggled = false;

        public static ManualLogSource log;

        public static Dictionary<int, Dictionary<string, float>> biomePollution = new Dictionary<int, Dictionary<string, float>>();

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
            if (biomePollution.ContainsKey(biomeIndex))
            {
                biomePollution[biomeIndex]["goopPollution"] = bm.currentGoopPollution;
                biomePollution[biomeIndex]["plasticCloudPollution"] = bm.currentPlasticCloudPollution;
                biomePollution[biomeIndex]["litterPollution"] = bm.currentLitterPollution;
            }
        }

        [HarmonyPatch(typeof(BiomeManager), nameof(BiomeManager.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(BiomeManager __instance)
        {
            AddBiomeToDictionary(__instance);
            EngineHub.EventManager.Register<BiomePollutionUpdated>(new GameEvent.Handler(UpdateBiomePollution));
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix()
        {
            if (Input.GetKeyDown(testKey))
            {
                foreach (int bi in biomePollution.Keys)
                {
                    BiomeManager bm = EngineHub.BiomeSaver.LookUpBiomeByID(bi);
                    log.LogInfo(bm.biomeDisplayName);
                    log.LogInfo($"goop: {biomePollution[bi]["goopPollution"]}");
                    log.LogInfo($"plastic: {biomePollution[bi]["plasticCloudPollution"]}");
                    log.LogInfo($"litter: {biomePollution[bi]["litterPollution"]}");
                }
            }
        }
    }
}
