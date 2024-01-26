using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GlobalGoopTracker
{
    [HarmonyPatch(typeof(BiomeManagementMenu))]
    public static class BiomeManagementMenu_Patch
    {
        public static TextMeshProUGUI currentBiomeTextMesh;

        public static void UpdateBiomeMenuInfoForNonBiome(BiomeManagementMenu __instance, bool playChangeAnimation = false)
        {
            SwitchToNonBiomeLayout(__instance);
            if (playChangeAnimation)
            {
                __instance.PlayLayoutSwapAnimation();
            }
            BiomeManager biomeManager = ExtraTrackersMod.nonBiomeManager;
            currentBiomeTextMesh.text = "Global Progress";
            float globalPollutionAmount = ExtraTrackersMod.GetGlobalPollutionAmount();
            __instance.cleanText.text = ScriptLocalization.Biome_Menu.Clean_Amount.Replace("{PERCENT}", Mathf.FloorToInt((1f - globalPollutionAmount) * 100f).ToString());
            __instance.cleanText.color = __instance.biomeHealthColors[biomeManager.currentAmbientPollutionStage];
            __instance.litterAmountText.text = (Mathf.Max(biomeManager.GetNumberOfLitterObjects(), 0).ToString() ?? "");
            __instance.goopAmountText.text = (Mathf.Max(biomeManager.GetNumberOfGoopNodes(), 0).ToString() ?? "");
            __instance.microplasticsAmountText.text = (Mathf.Max(biomeManager.GetMicroplasticsValue(), 0).ToString() ?? "");
        }

        public static void SwitchToNonBiomeLayout(BiomeManagementMenu __instance)
        {
            __instance.parentTransform.sizeDelta = __instance.homeCoveDimensions;
            __instance.currentBiomeTextTransform.anchoredPosition = __instance.homeCoveTitleTextPosition;
            __instance.cleanText.enabled = true;
            __instance.contaminantsGroup.alpha = 1f;
            __instance.nativeFruitGroup.alpha = 0f;
            GetPopulationCanvasGroup(__instance).alpha = 0f;
            __instance.populationGroupTransform.anchoredPosition = __instance.homeCovePopulationGroupPosition;
            __instance.contaminantsGroup.gameObject.GetComponent<TextMeshProUGUI>().text = "Non-Biome";
        }

        public static CanvasGroup GetPopulationCanvasGroup(BiomeManagementMenu __instance)
        {
            return __instance.populationGroupTransform.gameObject.GetComponent<CanvasGroup>();
        }

        [HarmonyPatch(nameof(BiomeManagementMenu.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(BiomeManagementMenu __instance)
        {
            currentBiomeTextMesh = __instance.currentBiomeText.gameObject.GetComponent<TextMeshProUGUI>();
        }

        [HarmonyPatch(nameof(BiomeManagementMenu.InitializeBiomeMenu))]
        [HarmonyPostfix]
        public static void InitializeBiomeMenu_Postfix(BiomeManagementMenu __instance)
        {
            if (!__instance.isInHomeCove && __instance.currentBiome == null)
            {
                UpdateBiomeMenuInfoForNonBiome(__instance);
            }
        }

        [HarmonyPatch(nameof(BiomeManagementMenu.SwitchToBiomeLayout))]
        [HarmonyPostfix]
        public static void SwitchToBiomeLayout_Postfix(BiomeManagementMenu __instance)
        {
            GetPopulationCanvasGroup(__instance).alpha = 1f;
            __instance.contaminantsGroup.gameObject.GetComponent<Localize>().OnLocalize(true);
        }

        [HarmonyPatch(nameof(BiomeManagementMenu.SwitchToHomeCoveLayout))]
        [HarmonyPostfix]
        public static void SwitchToHomeCoveLayout_Postfix(BiomeManagementMenu __instance)
        {
            GetPopulationCanvasGroup(__instance).alpha = 1f;
            __instance.contaminantsGroup.gameObject.GetComponent<Localize>().OnLocalize(true);
        }
    }

    [HarmonyPatch(typeof(CentralGameMenu))]
    public static class CentralGameMenu_Patch
    {
        [HarmonyPatch(nameof(CentralGameMenu.OpenWorldMapTab))]
        [HarmonyPostfix]
        public static void OpenWorldMapTab_Postfix(CentralGameMenu __instance)
        {
            __instance.OpenBiomeMenu(true);
        }

        [HarmonyPatch(nameof(CentralGameMenu.CloseBiomeMenu))]
        [HarmonyPrefix]
        public static bool CloseBiomeMenu_Prefix(CentralGameMenu __instance, bool instant = false, bool forceClose = false)
        {
            __instance.ClearPrimedBiome(CentralGameMenu.WorldLocation.NONE, true);
            BiomeManagementMenu_Patch.UpdateBiomeMenuInfoForNonBiome(__instance.biomeMenu, __instance.currentBiomeMenuLocation == CentralGameMenu.WorldLocation.NONE);
            __instance.ClearPrimedBiome(CentralGameMenu.WorldLocation.NONE, true);
            __instance.OpenBiomeMenu(true);
            return false;
        }
    }

    [HarmonyPatch(typeof(MainMenu))]
    public static class MainMenu_Patch
    {
        [HarmonyPatch(nameof(MainMenu.Start))]
        [HarmonyPostfix]
        public static void MainMenuStart_Postfix(MainMenu __instance)
        {
            GameObject versionLabelObject = UnityEngine.GameObject.Find("Version Text");
            TextMeshProUGUI versionLabel = versionLabelObject.GetComponent<TextMeshProUGUI>();
            string version = versionLabel.text;
            versionLabel.text = $"{version}\n{ExtraTrackersPlugin.pluginName} v{ExtraTrackersPlugin.versionString}";
            versionLabel.alignment = TextAlignmentOptions.BottomRight;
        }
    }
}
