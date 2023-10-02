using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LogisticsCoreClusters.Patches;
using UnityEngine;
using EquinoxsModUtils;

namespace LogisticsCoreClusters
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class LogisticsCoreClustersPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.LogisticsCoreClusters";
        private const string PluginName = "LogisticsCoreClusters";
        private const string VersionString = "1.0.1";

        public static string SpeedMultiplierKey = "SpeedMultiplier";
        public static ConfigEntry<float> SpeedMultiplier;

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private void Awake() {
            SpeedMultiplier = Config.Bind("General", SpeedMultiplierKey, 0.001f, new ConfigDescription("How much faster each core cluster makes your belts and inserters. 0.001 = 0.1% per cluster.", new AcceptableValueRange<float>(0.0f, 1.0f)));
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            NewUnlockDetails details = new NewUnlockDetails() {
                category = Unlock.TechCategory.Science,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 1500,
                dependencyNames = new List<string>() { UnlockNames.CoreBoostAssembly },
                description = $"Boosts logistic speed by {SpeedMultiplier.Value * 100}% per core cluster.",
                displayName = "Core Boost (Logistics)",
                numScansNeeded = 0,
                requiredTier = TechTreeState.ResearchTier.Tier11,
                treePosition = 0
            };
            ModUtils.AddNewUnlock(details, true);

            ModUtils.TechTreeStateLoaded += OnTechTreeLoaded;

            Harmony.CreateAndPatchAll(typeof(ConveyorMachineListPatch));
            Harmony.CreateAndPatchAll(typeof(InserterInstancePatch));

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void OnTechTreeLoaded(object sender, EventArgs e) {
            Unlock conveyorMK2Unlock = ModUtils.GetUnlockByName(UnlockNames.ConveyorBeltMKII);
            if (conveyorMK2Unlock != null) {
                ModUtils.UpdateUnlockSprite("Core Boost (Logistics)", conveyorMK2Unlock.sprite);
            }

            Unlock coreBoostAssembly = ModUtils.GetUnlockByName(UnlockNames.CoreBoostAssembly);
            if (coreBoostAssembly != null) {
                ModUtils.UpdateUnlockTreePosition("Core Boost (Logistics)", coreBoostAssembly.treePosition, true);
                Debug.Log("Core Boost (Assembly) treePosition: " + coreBoostAssembly.treePosition.ToString());
            }
        }
    }
}
