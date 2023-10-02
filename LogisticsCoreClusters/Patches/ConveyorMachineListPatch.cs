using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EquinoxsModUtils;
using HarmonyLib;
using JetBrains.Annotations;
using MessagePack.Formatters;
using UnityEngine;

namespace LogisticsCoreClusters.Patches
{
    public class ConveyorMachineListPatch
    {
        private static bool haveSetDescriptions = false;

        [HarmonyPatch(typeof(ConveyorMachineList), "TickMovementOnBelt")]
        [HarmonyPrefix]
        private static void setBeltSpeed(ref ConveyorInstance belt, float dt) {
            if (!ModUtils.hasTechTreeStateLoaded) return;
            if (TechTreeState.instance.freeCores == 0) return;
            if (belt.beltSpeed > 2) return;

            Unlock unlock = ModUtils.GetUnlockByName("Core Boost (Logistics)");
            if (!TechTreeState.instance.IsUnlockActive(unlock.uniqueId)) return;

            float boost = TechTreeState.instance.freeCores * LogisticsCoreClustersPlugin.SpeedMultiplier.Value;
            belt.beltSpeed = belt.beltSpeed * boost;

            if (haveSetDescriptions) return;

            ResourceInfo conveyorInfo = ModUtils.GetResourceInfoByName(ResourceNames.ConveyorBelt);
            conveyorInfo.description += $"\n\nCurrent speed: {0.88 * 3 * 60 * boost:#} items / minute";

            ResourceInfo conveyor2Info = ModUtils.GetResourceInfoByName(ResourceNames.ConveyorBeltMKII);
            conveyor2Info.description += $"\n\nCurrent speed: {1.89 * 3 * 60 * boost:#} items / minute";

            haveSetDescriptions = true;
        }
    }
}
