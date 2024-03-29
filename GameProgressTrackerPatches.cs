﻿using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ExtraTrackers
{
    [HarmonyPatch(typeof(GameProgressTracker))]
    public static class GameProgressTrackerPatches
    {
        [HarmonyPatch(nameof(GameProgressTracker.HandleBiomePollutionShift))]
        [HarmonyPrefix]
        public static bool HandleBiomePollutionShift_Prefix(GameEvent e)
        {
            // Don't handle biome pollution shifting for non-biome pollution
            if (((BiomePollutionUpdated)e).biome.biomeIndex == ExtraTrackersMod.NON_BIOME_INDEX)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(GameProgressTracker.RegisterForEvents))]
        [HarmonyPostfix]
        public static void RegisterForEvents_Postfix(GameProgressTracker __instance)
        {
            EngineHub.EventManager.Register<LoddleGotClean>(new GameEvent.Handler(ExtraTrackersMod.GetGoopyLoddlesCount));
            EngineHub.EventManager.Register<HoloBadgeCollected>(new GameEvent.Handler(ExtraTrackersMod.OnHoloBadgeCollected));
            EngineHub.EventManager.Register<LoddleMetPlayer>(new GameEvent.Handler(ExtraTrackersMod.OnLoddleMetPlayer));
        }
    }
}
