using HarmonyLib;
using static BossDespawn.Plugin;

namespace BossDespawn
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CustomFixedUpdate)), HarmonyPostfix]
        public static void BaseAICanSenseTarget(Humanoid __instance)
        {
            if (__instance.IsBoss() && !Player.IsPlayerInRange(__instance.transform.position, radius))
            {
                Debug($"Destroing {__instance.GetHoverName()}...");
                ZNetScene.instance?.Destroy(__instance.gameObject);
            }
        }
    }
}