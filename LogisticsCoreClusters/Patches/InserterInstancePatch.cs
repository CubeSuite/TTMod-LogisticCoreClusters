using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using EquinoxsModUtils;
using FIMSpace.GroundFitter;
using HarmonyLib;
using UnityEngine;

namespace LogisticsCoreClusters.Patches
{
    public class InserterInstancePatch
    {
        delegate void foo(ref float cyclesPerMinute);
        [HarmonyPatch()]
        internal class test
        {
            private static bool haveSetDescriptions = false;

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(InserterInstance), "SimUpdate")]
            public static IEnumerable<CodeInstruction> testPatch(IEnumerable<CodeInstruction> instructions) {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false,                                                                                          // search for line 18 and insert right before
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldarg_1),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "ConsumeFuel"))
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),                                                                     // this
                        new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(InserterInstance), "cyclesPerMinute")),      // cyclesPerMinute's address pushed to stack and consumed by following delegate
                        Transpilers.EmitDelegate<foo>((ref float cyclesPerMinute) => {
                            if(shouldSetSpeed(cyclesPerMinute)) {                                                                 // access to the address so setting the variable here will preserve the change when leaving scope
                                cyclesPerMinute *= TechTreeState.instance.freeCores * LogisticsCoreClustersPlugin.SpeedMultiplier.Value;
                            }
                        }));
                return matcher.InstructionEnumeration();                                                                          // return modified IL instructions
            }

            [HarmonyPatch(typeof(InserterInstance), "SimUpdate")]
            [HarmonyPostfix]
            private static void updateDescriptions(InserterInstance __instance) {
                if (haveSetDescriptions) return;
                double boost = TechTreeState.instance.freeCores * LogisticsCoreClustersPlugin.SpeedMultiplier.Value;

                ResourceInfo inserterInfo = ModUtils.GetResourceInfoByName(ResourceNames.Inserter);
                inserterInfo.description += $"\n\nCurrent speed: {17 * boost:#} items / minute";

                ResourceInfo fastInserterInfo = ModUtils.GetResourceInfoByName(ResourceNames.FastInserter);
                fastInserterInfo.description += $"\n\nCurrent speed: {39 * boost:#} items / minute";

                ResourceInfo longInserterInfo = ModUtils.GetResourceInfoByName(ResourceNames.LongInserter);
                longInserterInfo.description += $"\n\nCurrent speed: {15 * boost:#} items / minute";

                ResourceInfo filterInserterInfo = ModUtils.GetResourceInfoByName(ResourceNames.FilterInserter);
                filterInserterInfo.description += $"\n\nCurrent speed: {30 * boost:#} items / minute";

                ResourceInfo stackInserterInfo = ModUtils.GetResourceInfoByName(ResourceNames.StackInserter);
                stackInserterInfo.description += $"\n\nCurrent speed: {33 * GameState.instance.stackInserterSize * boost:#} items / minute";

                haveSetDescriptions = true;
            }
        }

        private static bool shouldSetSpeed(float cyclesPerMinue) {
            if (!ModUtils.hasTechTreeStateLoaded) return false;
            if (TechTreeState.instance.freeCores == 0) return false;
            if (cyclesPerMinue > 40) return false;

            Unlock unlock = ModUtils.GetUnlockByName("Core Boost (Logistics)");
            if (!TechTreeState.instance.IsUnlockActive(unlock.uniqueId)) return false;
            
            return true;
        }
    }
}
