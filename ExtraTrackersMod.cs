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
using Unity.IL2CPP.CompilerServices;
using System.Drawing.Printing;

namespace ExtraTrackers
{
    public static class ExtraTrackersMod
    {
        public const int NON_BIOME_INDEX = -1;
        public static ManualLogSource log;

        public static Dictionary<int, Dictionary<string, float>> biomePollution = new Dictionary<int, Dictionary<string, float>>();

        public static BiomeManager nonBiomeManager;

        public static Dictionary<LoddleAI.LoddleType, string> typeRemarkMapping = new Dictionary<LoddleAI.LoddleType, string>()
        {
            { LoddleAI.LoddleType.Eel,          GoogleSheetsEntryNames.SirenEvoIntro     },
            { LoddleAI.LoddleType.Betta,        GoogleSheetsEntryNames.BettaEvoIntro     },
            { LoddleAI.LoddleType.FlyingFish,   GoogleSheetsEntryNames.WingfinEvoIntro   },
            { LoddleAI.LoddleType.SeaAngel,     GoogleSheetsEntryNames.ButterflyEvoIntro },
            { LoddleAI.LoddleType.Catfish,      GoogleSheetsEntryNames.WhiskerEvoIntro   },
            { LoddleAI.LoddleType.MantaRay,     GoogleSheetsEntryNames.MantaEvoIntro     },
            { LoddleAI.LoddleType.Loach,        GoogleSheetsEntryNames.SnakeEvoIntro     },
            { LoddleAI.LoddleType.SeaBunny,     GoogleSheetsEntryNames.BunnyEvoIntro     },
            { LoddleAI.LoddleType.Pufferfish,   GoogleSheetsEntryNames.PufferEvoIntro    },
            { LoddleAI.LoddleType.Axolotl,      GoogleSheetsEntryNames.AxoEvoIntro       },
            { LoddleAI.LoddleType.Angler,       GoogleSheetsEntryNames.AnglerEvoIntro    },
            { LoddleAI.LoddleType.Dumbo,        GoogleSheetsEntryNames.OctoEvoIntro      },
            { LoddleAI.LoddleType.MegaLod,      GoogleSheetsEntryNames.JumboEvoIntro     },
        };
        public static Dictionary<LoddleAI.LoddleType, string> typeStringMapping = new Dictionary<LoddleAI.LoddleType, string>()
        {
            { LoddleAI.LoddleType.Eel,          "Siren"     },
            { LoddleAI.LoddleType.Betta,        "Betta"     },
            { LoddleAI.LoddleType.FlyingFish,   "Wingfin"   },
            { LoddleAI.LoddleType.SeaAngel,     "Butterfly" },
            { LoddleAI.LoddleType.Catfish,      "Whisker"   },
            { LoddleAI.LoddleType.MantaRay,     "Manta"     },
            { LoddleAI.LoddleType.Loach,        "Snake"     },
            { LoddleAI.LoddleType.SeaBunny,     "Bunny"     },
            { LoddleAI.LoddleType.Pufferfish,   "Puffer"    },
            { LoddleAI.LoddleType.Axolotl,      "Axo"       },
            { LoddleAI.LoddleType.Angler,       "Angler"    },
            { LoddleAI.LoddleType.Dumbo,        "Octo"      },
            { LoddleAI.LoddleType.MegaLod,      "Jumbo"     },
        };

        public static void AddBiomeToDictionary(BiomeManager bm)
        {
            //log.LogInfo(bm.biomeDisplayName);
            //log.LogInfo(bm.currentGoopPollution);
            //log.LogInfo(bm.currentPlasticCloudPollution);
            //log.LogInfo(bm.currentLitterPollution);
            int biomeIndex = bm.biomeIndex;
            if (!biomePollution.ContainsKey(biomeIndex))
            {
                biomePollution.Add(biomeIndex, new Dictionary<string, float>());
            }
            biomePollution[biomeIndex]["goopPollution"] = bm.currentGoopPollution;
            biomePollution[biomeIndex]["plasticCloudPollution"] = bm.currentPlasticCloudPollution;
            biomePollution[biomeIndex]["litterPollution"] = bm.currentLitterPollution;
            int goopyLoddles = 0;
            foreach (LoddleAI loddle in bm.loddlesInBiome)
            {
                goopyLoddles += loddle.isGoopy ? 1 : 0;
            }
            biomePollution[biomeIndex]["goopyLoddles"] = goopyLoddles;
        }

        public static void UpdateBiomePollution(GameEvent e)
        {
            BiomeManager bm = ((BiomePollutionUpdated)e).biome;
            int biomeIndex = bm.biomeIndex;
            //log.LogInfo($"Updating biome {biomeIndex}");
            if (biomePollution.ContainsKey(biomeIndex))
            {
                biomePollution[biomeIndex]["goopPollution"] = bm.currentGoopPollution;
                biomePollution[biomeIndex]["plasticCloudPollution"] = bm.currentPlasticCloudPollution;
                biomePollution[biomeIndex]["litterPollution"] = bm.currentLitterPollution;
                int goopyLoddles = 0;
                foreach (LoddleAI loddle in bm.loddlesInBiome)
                {
                    goopyLoddles += loddle.isGoopy ? 1 : 0;
                }
                biomePollution[biomeIndex]["goopyLoddles"] = goopyLoddles;
            }
        }

        public static List<LoddleAI.LoddleType> GetEncounteredLoddleTypes()
        {
            List<LoddleAI.LoddleType> encounteredLoddleTypes = new List<LoddleAI.LoddleType>();
            foreach (KeyValuePair<LoddleAI.LoddleType, string> entry in typeRemarkMapping)
            {
                if (EngineHub.GameDialogue.DaveRemarks[entry.Value].ReachedDisplayLimit())
                {
                    encounteredLoddleTypes.Add(entry.Key);
                }
            }
            return encounteredLoddleTypes;
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

        public static float GetGlobalPollutionAmount()
        {
            float totalPollution = 0.0f;
            foreach (int bi in biomePollution.Keys)
            {
                BiomeManager bm;
                bm = bi != -1 ? EngineHub.BiomeSaver.LookUpBiomeByID(bi) : nonBiomeManager;
                totalPollution += bm.biomePollution;
            }
            totalPollution = totalPollution / biomePollution.Count;
            return BloopTools.SnapToZero(totalPollution, 1E-06f);
        }

        public static int GetGoopyLoddlesCount()
        {
            int goopyLoddles = 0;
            foreach (int bi in biomePollution.Keys)
            {
                BiomeManager bm;
                bm = bi != -1 ? EngineHub.BiomeSaver.LookUpBiomeByID(bi) : nonBiomeManager;
                foreach (LoddleAI loddle in bm.loddlesInBiome)
                {
                    goopyLoddles += loddle.isGoopy ? 1 : 0;
                }
            }
            return goopyLoddles;
        }

        //[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        //[HarmonyPostfix]
        //public static void Update_Postfix()
        //{
        //    if (Input.GetKeyDown(KeyCode.F3))
        //    {
        //        foreach (int bi in biomePollution.Keys)
        //        {
        //            BiomeManager bm;
        //            bm = bi != -1 ? EngineHub.BiomeSaver.LookUpBiomeByID(bi) : nonBiomeManager;
        //            log.LogInfo(bm.biomeDisplayName);
        //            log.LogInfo($"goop: {biomePollution[bi]["goopPollution"]}");
        //            log.LogInfo($"plastic: {biomePollution[bi]["plasticCloudPollution"]}");
        //            log.LogInfo($"litter: {biomePollution[bi]["litterPollution"]}");
        //        }
        //    }
        //}
    }
}
