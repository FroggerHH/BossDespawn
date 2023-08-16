using System;
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
        private static List<Vector3> bossesOkayToDestroyAndWaiting_spawnPositions = new();
        private static List<Vector3> bossesReadyToDestroy_spawnPositions = new();

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CustomFixedUpdate)), HarmonyPostfix]
        public static void CheckAndCreateTimerIfNeeded(Humanoid __instance)
        {
            if (SceneManager.GetActiveScene().name != "main" ||
                !ZNetScene.instance ||
                !Player.m_localPlayer ||
                !__instance) return;

            var baseAI = __instance.GetBaseAI();
            if (!baseAI) return;
            var spawnPoint = baseAI.m_spawnPoint;

            if (!IsOkayToDestroy(__instance))
            {
                if (bossesOkayToDestroyAndWaiting_spawnPositions.Contains(spawnPoint))
                    bossesOkayToDestroyAndWaiting_spawnPositions.Remove(spawnPoint);
                return;
            }

            if (bossesReadyToDestroy_spawnPositions.Contains(spawnPoint))
            {
                var localizedBossName = __instance.m_name.Localize();
                Debug($"Destroing {localizedBossName}...");
                ZNetScene.instance.Destroy(__instance.gameObject);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                    $"Destroing {localizedBossName}...");

                bossesReadyToDestroy_spawnPositions.Remove(spawnPoint);
                bossesOkayToDestroyAndWaiting_spawnPositions.Remove(spawnPoint);
                return;
            }

            if (bossesOkayToDestroyAndWaiting_spawnPositions.Contains(spawnPoint)) return;

            bossesOkayToDestroyAndWaiting_spawnPositions.Add(spawnPoint);
            var milliseconds = TimeSpan.FromMinutes(despawnDelay).TotalMilliseconds;
            Debug($"Starting timers for {milliseconds} milliseconds");
            if (milliseconds > 0)
            {
                var timer = new System.Timers.Timer(milliseconds);
                timer.Elapsed += (_, _) => OnTimerElapsed(timer, spawnPoint);
                timer.Start();
            }
            else OnTimerElapsed(null, spawnPoint);
        }

        private static void OnTimerElapsed(System.Timers.Timer timer, Vector3 spawnPoint)
        {
            Debug("Timer elapsed");
            bossesReadyToDestroy_spawnPositions.Add(spawnPoint);
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
        }

        private static bool IsOkayToDestroy(Humanoid humanoid)
        {
            if (!humanoid) return false;
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