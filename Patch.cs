using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BossDespawn.Plugin;

namespace BossDespawn
{
    [HarmonyPatch]
    internal static class Patch
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CustomFixedUpdate)), HarmonyPostfix]
        public static void Check(Humanoid __instance)
        {
            if (SceneManager.GetActiveScene().name != "main") return;
            if (!IsOkayToDestroy(__instance)) return;

            Debug($"Destroing {__instance.GetHoverName()}...");
            ZNetScene.instance?.Destroy(__instance.gameObject);
        }

        private static bool IsOkayToDestroy(Humanoid humanoid)
        {
            var prefabName = humanoid.GetPrefabName();
            var havePlayerInRange = Player.IsPlayerInRange(humanoid.transform.position, radius);
            var isAllowed = filterMode == BossFilterMode.WhiteList
                ? whiteList.Contains(prefabName)
                : !blackList.Contains(prefabName);
            var isOkay = humanoid.IsBoss() && !havePlayerInRange && isAllowed;

            return isOkay;
        }
    }
}