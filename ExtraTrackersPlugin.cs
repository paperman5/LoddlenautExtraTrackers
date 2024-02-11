using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Diagnostics;
using System.Reflection;
using static HarmonyLib.Tools.Logger;
using UnityEngine;
using System;

namespace ExtraTrackers
{
    [BepInPlugin(myGUID, pluginName, versionString)]
    public class ExtraTrackersPlugin : BaseUnityPlugin
    {
        public const string myGUID = "com.paperish.extratrackers";
        public const string pluginName = "Extra Trackers";
        public const string versionString = "1.1.2";

        private static readonly Harmony harmony = new Harmony(myGUID);
        private void Awake()
        {
            // Plugin startup logic //
            ExtraTrackersMod.log = Logger;
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
