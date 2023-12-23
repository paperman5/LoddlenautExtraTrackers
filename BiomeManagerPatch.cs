using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GlobalGoopTracker
{
    [HarmonyPatch(typeof(BiomeManager))]
    public static class BiomeManager_Patch
    {
        public static BiomeManager nonBiomeManager;
        public static int waitFrame = 0;
        public static int waitFrameTrigger = 2;

        public static IEnumerator RegisterContaminantsCoroutine(BiomeManager __instance)
        {
            yield return new WaitForSeconds(0.1f);
            // Don't use __instance.UpdateBiomeTracking() because it calls the Register() methods
            // and gives things new saveID's, which messes with saving
            __instance.GetPlantsInBiome();
            __instance.GetFlatGoopInBiome();
            __instance.GetRegrowthZonesInBiome();
            __instance.GetPlasticCloudsInBiome();
            __instance.GetLitterInBiome();
            __instance.externalPollutionUpdateQueued = true;
            GlobalGoopTrackerMod.log.LogInfo("Non-biome contaminants registered");
        }

        [HarmonyPatch(nameof(BiomeManager.Start))]
        [HarmonyPrefix]
        public static bool Start_Prefix(BiomeManager __instance)
        {
            if (nonBiomeManager == null)
            {
                GlobalGoopTrackerMod.AddNonBiomeManager();
                __instance.StartCoroutine(RegisterContaminantsCoroutine(nonBiomeManager));
            }

            GlobalGoopTrackerMod.AddBiomeToDictionary(__instance);
            EngineHub.EventManager.Register<BiomePollutionUpdated>(new GameEvent.Handler(GlobalGoopTrackerMod.UpdateBiomePollution));

            return __instance.biomeIndex != GlobalGoopTrackerMod.NON_BIOME_INDEX; //Skips the original Start() if this is the NonBiomeManager
        }

        [HarmonyPatch(nameof(BiomeManager.Update))]
        [HarmonyPrefix]
        public static bool Update_Prefix(BiomeManager __instance)
        {
            return __instance.biomeIndex != GlobalGoopTrackerMod.NON_BIOME_INDEX; //Skips the original Update() if this is the NonBiomeManager
        }

        [HarmonyPatch(nameof(BiomeManager.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX && __instance.externalPollutionUpdateQueued)
            {
                __instance.UpdateOverallPollution(false, true);
                __instance.externalPollutionUpdateQueued = false;
            }
        }
        [HarmonyPatch(nameof(BiomeManager.GetPlantsInBiome))]
        [HarmonyPrefix]
        public static bool GetPlantsInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                FoodPlant[] foodPlants = UnityEngine.Object.FindObjectsByType<FoodPlant>(FindObjectsSortMode.None);
                __instance.currentGoopPollution = 0f;
                __instance.maxGoopPollution = 0f;
                foreach (FoodPlant foodPlant in foodPlants)
                {
                    if (foodPlant.gameObject.activeInHierarchy && foodPlant.parentBiome == null)
                    {
                        __instance.maxGoopPollution += (float)foodPlant.goopManager.numberOfManagedGoops;
                        __instance.AddToGoopPollution((float)foodPlant.goopManager.numberOfActiveGoops, true);
                        // Register without changing saveID
                        __instance.RegisterBiomePlant(foodPlant);
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.GetRegrowthZonesInBiome))]
        [HarmonyPrefix]
        public static bool GetRegrowthZonesInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                foreach (FloraRegrowthZone floraRegrowthZone in UnityEngine.Object.FindObjectsByType<FloraRegrowthZone>(FindObjectsSortMode.None))
                {
                    if (floraRegrowthZone.parentBiome != null)
                    {
                        continue;
                    }
                    if (floraRegrowthZone.isFlatGoopZone)
                    {
                        __instance.maxGoopPollution += (float)floraRegrowthZone.flatGoopManager.numberOfManagedFlatGoops;
                        __instance.AddToGoopPollution((float)floraRegrowthZone.flatGoopManager.numberOfActiveFlatGoops, true);
                    }
                    else
                    {
                        __instance.maxGoopPollution += (float)floraRegrowthZone.goopManager.numberOfManagedGoops;
                        __instance.AddToGoopPollution((float)floraRegrowthZone.goopManager.numberOfActiveGoops, true);
                    }
                    // Register without changing saveID
                    __instance.RegisterBiomeRegrowthZone(floraRegrowthZone);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.GetPlasticCloudsInBiome))]
        [HarmonyPrefix]
        public static bool GetPlasticCloudsInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                MicroplasticsCloud[] plasticClouds = UnityEngine.Object.FindObjectsByType<MicroplasticsCloud>(FindObjectsSortMode.None);
                __instance.currentPlasticCloudPollution = 0f;
                __instance.maxPlasticCloudPollution = 0f;
                foreach (MicroplasticsCloud microplasticsCloud in plasticClouds)
                {
                    if (microplasticsCloud.parentBiome != null)
                    {
                        continue;
                    }
                    __instance.maxPlasticCloudPollution += (float)microplasticsCloud.maxParticlesInCloud;
                    __instance.AddToPlasticCloudPollution((float)microplasticsCloud.currentParticlesInCloud, true);
                    // Register without changing saveID
                    __instance.RegisterBiomePlasticCloud(microplasticsCloud);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.GetFlatGoopInBiome))]
        [HarmonyPrefix]
        public static bool GetFlatGoopInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                foreach (FlatGoop flatGoop in UnityEngine.Object.FindObjectsByType<FlatGoop>(FindObjectsSortMode.None))
                {
                    if (!(flatGoop.flatGoopParentScript != null) && flatGoop.surroundingBiome == null)
                    {
                        __instance.maxGoopPollution += 1f;
                        if (!flatGoop.hasBeenDissolved)
                        {
                            __instance.AddToGoopPollution(1f, true);
                        }
                        // Register without changing saveID
                        __instance.RegisterBiomeFlatGoop(flatGoop);
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.GetPickupLitterInBiome))]
        [HarmonyPrefix]
        public static bool GetPickupLitterInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                foreach (Pickup pickup in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                {
                    if (pickup.gameObject.activeInHierarchy && pickup.surroundingBiome == null && pickup.CompareTag("Litter"))
                    {
                        __instance.StartTrackingPickupLitter(pickup.gameObject, true, true);
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.GetChunkLitterInBiome))]
        [HarmonyPrefix]
        public static bool GetChunkLitterInBiome_Prefix(BiomeManager __instance)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                foreach (Chunk chunk in UnityEngine.Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None))
                {
                    if (chunk.gameObject.activeInHierarchy && chunk.surroundingBiome == null)
                    {
                        __instance.StartTrackingChunkLitter(chunk.gameObject, true, true);
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.StartTrackingPickupLitter))]
        [HarmonyPrefix]
        //This could probably be a transpiler
        public static bool StartTrackingPickupLitter_Prefix(BiomeManager __instance, GameObject pickupToTrack, bool skipEffects = false, bool forceUpdate = false)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                if (__instance.litterPickupsInBiome.Contains(pickupToTrack) && !forceUpdate)
                {
                    return false;
                }
                Pickup component = pickupToTrack.GetComponent<Pickup>();
                if (component == null || component.hasBeenDestroyed || component.itemData.itemType == ItemData.ItemType.CRAFTED || component.itemStackSize > 1)
                {
                    return false;
                }
                __instance.currentLitterPollution += __instance.pollutionPerLitterPickup;
                __instance.UpdateLitterPollution(skipEffects || Time.timeSinceLevelLoad < __instance.litterTrackingStartupTime);
                if (!__instance.litterPickupsInBiome.Contains(pickupToTrack))
                {
                    __instance.litterPickupsInBiome.Add(pickupToTrack);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.StartTrackingChunkLitter))]
        [HarmonyPrefix]
        //This could probably be a transpiler
        public static bool StartTrackingChunkLitter_Prefix(BiomeManager __instance, GameObject chunkToTrack, bool skipEffects = false, bool forceUpdate = false)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                if (__instance.litterChunksInBiome.Contains(chunkToTrack) && !forceUpdate)
                {
                    return false;
                }
                Chunk component = chunkToTrack.GetComponent<Chunk>();
                if (component == null || component.hasBeenDestroyed || component.chunkData.chunkType == ChunkData.ChunkType.GOOP)
                {
                    return false;
                }
                __instance.currentLitterPollution += __instance.pollutionPerLitterChunk;
                __instance.UpdateLitterPollution(skipEffects || Time.timeSinceLevelLoad < __instance.litterTrackingStartupTime);
                if (!__instance.litterChunksInBiome.Contains(chunkToTrack))
                {
                    __instance.litterChunksInBiome.Add(chunkToTrack);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(BiomeManager.UpdateOverallPollution))]
        [HarmonyPrefix]
        public static bool UpdateOverallPollution_Prefix(BiomeManager __instance, bool skipEffects = false, bool instantTransition = false)
        {
            if (__instance.biomeIndex == GlobalGoopTrackerMod.NON_BIOME_INDEX)
            {
                float goopPollution = (__instance.maxGoopPollution > 0f) ? (__instance.currentGoopPollution / __instance.maxGoopPollution) : 0f;
                float plasticMult = (__instance.maxPlasticCloudPollution > 0f) ? __instance.plasticCloudContributionToBiomePollution : 0f;
                float weightedGoopPollution = goopPollution * (1f - (__instance.litterContributionToBiomePollution + plasticMult));
                float weightedPlasticPollution = (__instance.maxPlasticCloudPollution > 0f) ? (__instance.currentPlasticCloudPollution / __instance.maxPlasticCloudPollution * __instance.plasticCloudContributionToBiomePollution) : 0f;
                __instance.biomePollution = weightedGoopPollution + __instance.weightedLitterPollution + weightedPlasticPollution;
                __instance.biomePollution = BloopTools.SnapToZero(__instance.biomePollution, 1E-06f);
                if (skipEffects)
                {
                    __instance.externalPollutionUpdateQueued = true;
                    return false;
                }
                EngineHub.EventManager.Fire(new BiomePollutionUpdated(__instance, 0, 0, 0, __instance.biomePollution, false));
                return false;
            }
            return true;
        }
    }
}
