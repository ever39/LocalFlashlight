using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LocalFlashlight.Networking
{
    internal class NetworkingPatches
    {
        static GameObject networkPrefab;
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void InitializeNetworking()
        {
            if (networkPrefab != null) return;

            networkPrefab = (GameObject)Plugin.bundle.LoadAsset("Assets/LocalFlashlight assets/LFNetworkHandler.prefab");
            networkPrefab.AddComponent<LFNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
