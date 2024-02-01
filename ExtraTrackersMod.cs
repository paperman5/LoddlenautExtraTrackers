using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using Unity.IL2CPP.CompilerServices;
using System.Drawing.Printing;
using SCPE;
using Cinemachine;
using System.Collections;

namespace ExtraTrackers
{
    public static class ExtraTrackersMod
    {
        public const int NON_BIOME_INDEX = -1;
        public static int screenshotIndex = 0;
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
            nonBiomeManager.litterContributionToBiomePollution = 0.3f;
            nonBiomeManager.plasticCloudContributionToBiomePollution = 0.15f;
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
            LoddleAI[] loddles = UnityEngine.Object.FindObjectsByType<LoddleAI>(FindObjectsSortMode.None);
            foreach (LoddleAI loddle in loddles)
            {
                goopyLoddles += loddle.isGoopy ? 1 : 0;
            }
            return goopyLoddles;
        }

        public static void SetupForScreenshots()
        {
            float viewDist = 2000f;
            float orthoSize = 200f;

            EngineHub.GameManager.globalShaderVariables.GlobalDepthDimDistance = 100000f;
            EngineHub.GameManager.globalShaderVariables.CullShrinkDistance = 100000f;

            EngineHub.SkyManager.defaultWorldSMD.intensity = 0f;
            EngineHub.SkyManager.defaultWorldSMD.noiseStrength= 0f;
            EngineHub.SkyManager.defaultWorldSMD.topExponent = 0f;
            EngineHub.SkyManager.defaultWorldSMD.bottomExponent = 0f;
            EngineHub.SkyManager.sunShaftEnableHeight = 10000f;

            EngineHub.PlayerTransforms.controllerRootTransform.parent.Find("Cameras/Main Camera/AmbientFloatyBits").gameObject.SetActive(false);
            EngineHub.PlayerTransforms.controllerRootTransform.parent.Find("PostProcessingVolume").gameObject.GetComponent<PostProcessVolume>().profile.RemoveSettings<SCPE.Fog>();
            EngineHub.PlayerTransforms.controllerRootTransform.gameObject.GetComponent<PlayerMeterManager>().enabled = false;
            EngineHub.PlayerTransforms.controllerRootTransform.parent.Find("Cameras/CM FreeLook").gameObject.GetComponent<CinemachineFreeLook>().m_Lens = new LensSettings(70f, orthoSize, 0.2f, viewDist, 0f);

            GameObject.Find("/HomeCove/PortalVolume_HomeCove").SetActive(false);
            foreach (BiomeManager bm in EngineHub.BiomeSaver.allBiomes.Values)
            {
                bm.pollutedPostVolume.profile.RemoveSettings<SCPE.Fog>();
                bm.cleanPostVolume.profile.RemoveSettings<SCPE.Fog>();
                bm.pollutedHazeSphereParticles.gameObject.SetActive(false);
                bm.cleanSMD.intensity = 0f;
                bm.cleanSMD.noiseStrength = 0f;
                bm.cleanSMD.topExponent = 0f;
                bm.cleanSMD.bottomExponent = 0f;
                bm.pollutedSMD.intensity = 0f;
                bm.pollutedSMD.noiseStrength = 0f;
                bm.pollutedSMD.topExponent = 0f;
                bm.pollutedSMD.bottomExponent = 0f;
            }
            GameObject.Find("/GrimyGulf/Buildings/RefineryTower/PortalVolume_GrimyGulfRefinery").SetActive(false);
            GameObject.Find("/FlotsamFlats/Boats/GUPPITanker/PortalVolume_FlotsamFlatsShipwreck").SetActive(false);
            GameObject.Find("/HomeCove/HomeCoveSunShafts").SetActive(false);

            EngineHub.GPUPrefabManager.SetLODBias(viewDist, null);
            foreach (GPUInstancer.GPUInstancerPrototype prototype in EngineHub.GPUPrefabManager.prototypeList)
            {
                prototype.maxDistance = viewDist;
            }

            ScannerObject[] scannerObjects = UnityEngine.Object.FindObjectsByType<ScannerObject>(FindObjectsSortMode.None);
            foreach (ScannerObject so in scannerObjects)
            {
                if (so.gameObject.name.Contains("Goop") || so.gameObject.name.Contains("Crate") || so.gameObject.name.Contains("Spaceship") || so.gameObject.name.Contains("RechargeRing"))
                {
                    so.canGetScanned = false;
                }
            }
            LitterScanner ls = UnityEngine.Object.FindObjectsByType<LitterScanner>(FindObjectsSortMode.None)[0];
            ls.currentScanRange = viewDist;
        }

        public static void SaveRenderTextureToPNG(RenderTexture toSave, string filepath)
        {
            RenderTexture oldActive = RenderTexture.active;
            Texture2D image = new Texture2D(toSave.width, toSave.height, TextureFormat.RGB24, true);
            RenderTexture.active = toSave;
            image.ReadPixels(new Rect(0, 0, toSave.width, toSave.height), 0, 0);
            image.Apply();
            RenderTexture.active = oldActive;
            toSave.Release();
            System.IO.File.WriteAllBytes(filepath, image.EncodeToPNG());
            log.LogInfo("Saved screenshot to " + filepath);
        }

        public static string GetScreenshotFilepath()
        {
            string screenshotDir = Application.persistentDataPath + "/screenshots";
            var baseDir = new System.IO.DirectoryInfo(screenshotDir);
            string prefix = DateTime.Now.ToString("yyyy-MM-dd_");
            string name = baseDir.FullName + "/" + prefix + screenshotIndex.ToString() + ".png";
            screenshotIndex += 1;
            return name;
        }

        public static IEnumerator TakeScreenshot()
        {
            RenderTexture camPreviousRT = Camera.main.targetTexture;
            RenderTexture scaledRenderTexture = new RenderTexture(7680, 4320, 32, RenderTextureFormat.ARGB32);
            Camera.main.targetTexture = scaledRenderTexture;
            yield return null;
            yield return new WaitForEndOfFrame();
            string filepath = GetScreenshotFilepath();
            SaveRenderTextureToPNG(scaledRenderTexture, filepath);
            Camera.main.targetTexture = camPreviousRT;
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(GameManager __instance)
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SetupForScreenshots();
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                __instance.StartCoroutine(TakeScreenshot());
            }
        }
    }
}
