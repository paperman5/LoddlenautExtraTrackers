using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LoddleAI;

namespace ExtraTrackers
{
    [HarmonyPatch(typeof(BiomeManagementMenu))]
    public static class BiomeManagementMenu_Patch
    {
        public static TextMeshProUGUI currentBiomeTextMesh;
        public static Vector2 nativeFoodGroupOrigPos;
        public static Vector2 nativeFoodGroupOffset = new Vector2(0.0f, 110.0f);
        public static Vector2 populationGroupOffset = new Vector2(0.0f, 15.0f);
        public static GridLayoutGroup loddleListLayoutGroup;
        public static List<TextMeshProUGUI> loddleListLabels = new List<TextMeshProUGUI>();

        public static void UpdateBiomeMenuInfoForNonBiome(BiomeManagementMenu __instance, bool playChangeAnimation = false)
        {
            // Updates the modified "Global Progress" window on the biome management menu.
            SwitchToNonBiomeLayout(__instance);
            if (playChangeAnimation)
            {
                __instance.PlayLayoutSwapAnimation();
            }
            BiomeManager biomeManager = ExtraTrackersMod.nonBiomeManager;
            // Update the global cleaned progress
            currentBiomeTextMesh.text = "Global Progress";
            float globalPollutionAmount = ExtraTrackersMod.GetGlobalPollutionAmount();
            __instance.cleanText.text = ScriptLocalization.Biome_Menu.Clean_Amount.Replace("{PERCENT}", Mathf.FloorToInt((1f - globalPollutionAmount) * 100f).ToString());
            int pollutionStage = (int)Mathf.Ceil(globalPollutionAmount * (__instance.biomeHealthColors.Count() - 1));
            __instance.cleanText.color = __instance.biomeHealthColors[pollutionStage];
            // Update the goop numbers
            __instance.litterAmountText.text = (Mathf.Max(biomeManager.GetNumberOfLitterObjects(), 0).ToString() ?? "");
            __instance.goopAmountText.text = (Mathf.Max(biomeManager.GetNumberOfGoopNodes(), 0).ToString() ?? "");
            __instance.microplasticsAmountText.text = (Mathf.Max(biomeManager.GetMicroplasticsValue(), 0).ToString() ?? "");
            __instance.inhabitantsText.text = ExtraTrackersMod.GetGoopyLoddlesCount().ToString();
            // Update the encountered loddle types
            UpdateLoddleListLabels();
        }

        public static void SwitchToNonBiomeLayout(BiomeManagementMenu __instance)
        {
            CanvasGroup populationCanvasGroup = GetPopulationCanvasGroup(__instance);
            // Set the size to the standard biome info size
            __instance.parentTransform.sizeDelta = __instance.defaultDimensions;
            __instance.currentBiomeTextTransform.anchoredPosition = __instance.defaultTitleTextPosition;
            __instance.cleanText.enabled = true;
            __instance.contaminantsGroup.alpha = 1f;
            __instance.nativeFruitGroup.alpha = 1f;
            populationCanvasGroup.alpha = 1f;

            // Update the text and positioning of categories
            __instance.contaminantsGroup.gameObject.GetComponent<TextMeshProUGUI>().text = "Non-Biome";
            populationCanvasGroup.gameObject.GetComponent<TextMeshProUGUI>().text = "Misc";
            __instance.nativeFruitGroup.gameObject.GetComponent<TextMeshProUGUI>().text = "Loddles";
            loddleListLayoutGroup.gameObject.SetActive(true);
            __instance.nativeFruitGroup.transform.Find("Native Fruit Arranger").gameObject.SetActive(false);
            populationCanvasGroup.transform.Find("Inhabitants Label").gameObject.GetComponent<TextMeshProUGUI>().text = "Goopy Loddles";
            populationCanvasGroup.transform.Find("Eggs Label").gameObject.SetActive(false);
            populationCanvasGroup.transform.Find("Eggs Amount").gameObject.SetActive(false);
            populationCanvasGroup.transform.Find("Cocoons Label").gameObject.SetActive(false);
            populationCanvasGroup.transform.Find("Cocoons Amount").gameObject.SetActive(false);
            __instance.nativeFruitGroup.GetComponent<RectTransform>().anchoredPosition = nativeFoodGroupOrigPos + nativeFoodGroupOffset;
            __instance.populationGroupTransform.anchoredPosition = __instance.defaultPopulationGroupPosition + populationGroupOffset;
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
            nativeFoodGroupOrigPos = __instance.nativeFruitGroup.gameObject.GetComponent<RectTransform>().anchoredPosition;
            InitializeLoddleListLayoutGroup(__instance);
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
            ResetBiomeInfo(__instance, false);
        }

        [HarmonyPatch(nameof(BiomeManagementMenu.SwitchToHomeCoveLayout))]
        [HarmonyPostfix]
        public static void SwitchToHomeCoveLayout_Postfix(BiomeManagementMenu __instance)
        {
            ResetBiomeInfo(__instance, true);
        }

        public static void ResetBiomeInfo(BiomeManagementMenu __instance, bool homecove)
        {
            CanvasGroup populationCanvasGroup = GetPopulationCanvasGroup(__instance);
            populationCanvasGroup.alpha = 1f;
            __instance.contaminantsGroup.gameObject.GetComponent<Localize>().OnLocalize(true);
            populationCanvasGroup.gameObject.GetComponent<Localize>().OnLocalize(true);
            __instance.nativeFruitGroup.gameObject.GetComponent<Localize>().OnLocalize(true);
            __instance.nativeFruitGroup.transform.Find("Native Fruit Arranger").gameObject.SetActive(true);
            populationCanvasGroup.transform.Find("Inhabitants Label").gameObject.GetComponent<Localize>().OnLocalize(true);
            populationCanvasGroup.transform.Find("Eggs Label").gameObject.SetActive(true);
            populationCanvasGroup.transform.Find("Eggs Amount").gameObject.SetActive(true);
            populationCanvasGroup.transform.Find("Cocoons Label").gameObject.SetActive(true);
            populationCanvasGroup.transform.Find("Cocoons Amount").gameObject.SetActive(true);
            loddleListLayoutGroup.gameObject.SetActive(false);
            __instance.nativeFruitGroup.GetComponent<RectTransform>().anchoredPosition = nativeFoodGroupOrigPos;
            __instance.populationGroupTransform.anchoredPosition = homecove ? __instance.homeCovePopulationGroupPosition : __instance.defaultPopulationGroupPosition;
        }

        public static void InitializeLoddleListLayoutGroup(BiomeManagementMenu __instance)
        {
            // This mod adds a GridLayoutGroup to display the encountered loddle types.
            loddleListLabels = new List<TextMeshProUGUI>(); // Reset in case of save/load
            GameObject go = new GameObject();
            go.name = "Loddle List Grid";
            go.transform.parent = __instance.nativeFruitGroup.transform;
            go.AddComponent<RectTransform>();
            go.GetComponent<RectTransform>().anchoredPosition = __instance.nativeFruitGroup.transform.Find("Native Fruit Arranger").GetComponent<RectTransform>().anchoredPosition;
            go.GetComponent<RectTransform>().sizeDelta = __instance.nativeFruitGroup.transform.Find("Native Fruit Arranger").GetComponent<RectTransform>().sizeDelta;
            go.AddComponent<GridLayoutGroup>();
            // Set the properties of the GridLayoutGroup
            loddleListLayoutGroup = go.GetComponent<GridLayoutGroup>();
            loddleListLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            loddleListLayoutGroup.constraintCount = 3;
            loddleListLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            loddleListLayoutGroup.cellSize = new Vector2(100f, 30f);
            go.transform.position = __instance.nativeFruitGroup.transform.position;
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            go.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -100f);

            // Create a label with similar properties to the other labels on this screen, to copy for each loddle type
            GameObject prototypeLabel = GameObject.Instantiate(__instance.contaminantsGroup.transform.Find("Litter Amount Label").gameObject);
            prototypeLabel.name = "Loddle Type Label";
            prototypeLabel.GetComponent<TextMeshProUGUI>().enableAutoSizing = false;
            prototypeLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            prototypeLabel.GetComponent<TextMeshProUGUI>().fontSize = 18f;
            prototypeLabel.GetComponent<TextMeshProUGUI>().text = "";
            GameObject.Destroy(prototypeLabel.GetComponent<Localize>());

            for (int i = 0; i < (int)Mathf.Ceil(ExtraTrackersMod.typeRemarkMapping.Count / 3f)*3; i++)
            {
                GameObject label = GameObject.Instantiate(prototypeLabel, loddleListLayoutGroup.transform);
                loddleListLabels.Add(label.GetComponent<TextMeshProUGUI>());
            }
            GameObject.Destroy(prototypeLabel);
        }

        public static void UpdateLoddleListLabels()
        {
            List<LoddleAI.LoddleType> encounteredLoddleTypes = ExtraTrackersMod.GetEncounteredLoddleTypes();
            List<string> loddleStrings = new List<string>();
            foreach (LoddleAI.LoddleType loddleType in encounteredLoddleTypes)
            {
                loddleStrings.Add(ExtraTrackersMod.typeStringMapping[loddleType]);
            }
            loddleStrings.Sort();

            foreach (TextMeshProUGUI tm in loddleListLabels)
            {
                tm.text = "";
            }
            for (int i = 0; i < loddleStrings.Count; i++)
            {
                loddleListLabels[i].text = loddleStrings[i];
                // Make the last entry centered
                if (i == 12)
                {
                    loddleListLabels[12].text = "";
                    loddleListLabels[13].text = loddleStrings[12];
                }
            }
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
            // Hook into the method to close the individual biome menu, to display the global progress instead of closing the menu.
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
            // Add a mod version label on the title screen
            GameObject versionLabelObject = UnityEngine.GameObject.Find("Version Text");
            TextMeshProUGUI versionLabel = versionLabelObject.GetComponent<TextMeshProUGUI>();
            string version = versionLabel.text;
            versionLabel.text = $"{version}\n{ExtraTrackersPlugin.pluginName} v{ExtraTrackersPlugin.versionString}";
            versionLabel.alignment = TextAlignmentOptions.BottomRight;
        }
    }
}
