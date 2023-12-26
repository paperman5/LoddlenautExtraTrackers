using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Diagnostics;
using System.Reflection;
using static HarmonyLib.Tools.Logger;
using UnityEngine;
using System;

namespace GlobalGoopTracker
{
    [BepInPlugin(myGUID, pluginName, versionString)]
    public class GlobalGoopTrackerPlugin : BaseUnityPlugin
    {
        public const string myGUID = "com.paperish.globalgooptracker";
        public const string pluginName = "Global Goop Tracker";
        public const string versionString = "1.0.0";
        //TODO: Check FlatGoop loading logic
        //      Fix global percentage counter colors

        private static readonly Harmony harmony = new Harmony(myGUID);
        private void Awake()
        {
            // Plugin startup logic //
            GlobalGoopTrackerMod.log = Logger;
            //ChannelFilter = LogChannel.All;
            //HarmonyFileLog.Enabled = true;

            // Patching doesn't work the normal way for this game for whatever reason, manual override!
            AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do(delegate (Type type)
            {
                harmony.PatchAll(type);
            });

            Logger.LogInfo($"Plugin {myGUID} {versionString} is loaded");
        }
    }
}
