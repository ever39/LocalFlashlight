using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

//ooo its the big thing of this update!!!!!
namespace LocalFlashlight.Networking
{
    public class LFNetworkHandler : NetworkBehaviour
    {
        public static LFNetworkHandler Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();

            Instance = this;

            base.OnNetworkSpawn();
        }

        //i mean, is making the object for all clients a bad idea instead of using networked objects?
        //which also means transmitting messages from server to client so it doesn't do much for the server to handle
        [ServerRpc(RequireOwnership = false)]
        public void RequestAllLightsUpdateServerRpc()
        {
            Plugin.mls.LogDebug("Recieved request to update lights on all clients from joining client!! sending it further to other clients for them to also update their lights...");
            AcceptAllLightUpdateRequestClientRpc();
        }

        [ClientRpc]
        public void AcceptAllLightUpdateRequestClientRpc()
        {
            Plugin.mls.LogDebug("Sending request to update this client's light for all current clients...");
            Color tempColor;
            ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out tempColor);
            MakeLightServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, tempColor, Plugin.Intensity.Value, Plugin.Range.Value, Plugin.Angle.Value, LightScript.publicFlashState);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MakeLightServerRpc(ulong clientId, UnityEngine.Color color, float intensity, float range, float angle, bool currentFlashState)
        {
            Plugin.mls.LogDebug($"RECIEVED REQUEST TO MAKE LIGHT FOR PLAYER {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}!!! sending it over to all clients..");
            if (color == null)
            {
                Plugin.mls.LogWarning($"WARNING!!! player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}'s light does not have a valid color!! changing it to default instead...");
                color = Color.white;
            }
            MakeLightClientRpc(clientId, color, intensity, range, angle, currentFlashState);
        }

        [ClientRpc]
        public void MakeLightClientRpc(ulong clientId, UnityEngine.Color lightColor, float lightIntensity, float lightRange, float lightAngle, bool currentFlashState)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                return;
            }

            GameObject clientCam = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.gameObject;
            if (clientCam.transform.FindChild($"lightObject ({clientId})").gameObject != null)
            {
                GameObject tempObject2 = clientCam.transform.FindChild($"lightObject ({clientId})").gameObject;
                Light tempLight = tempObject2.GetComponent<Light>();

                tempLight.color = lightColor;
                Plugin.mls.LogDebug($"Set light color to {lightColor} for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");

                tempLight.intensity = lightIntensity;
                tempLight.range = lightRange;
                tempLight.spotAngle = lightAngle;

                Plugin.mls.LogDebug($"Client #{clientId}'s light already existed for this client, updated some of its values instead");
                return;
            }

            try
            {
                var tempObject1 = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera;
                var mixerGroup = GameNetworkManager.Instance.localPlayerController.movementAudio.GetComponent<AudioSource>().outputAudioMixerGroup;

                var lightObject = new GameObject($"lightObject ({clientId})");
                lightObject.transform.SetParent(tempObject1.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, -0.55f, 0.5f);
                lightObject.transform.Rotate(new Vector3(-12, 3, 0));

                Light realLight = lightObject.AddComponent<Light>();
                realLight.type = LightType.Spot;
                realLight.shape = LightShape.Cone;
                realLight.color = lightColor;
                realLight.intensity = lightIntensity;
                realLight.range = lightRange;
                realLight.spotAngle = lightAngle;
                HDAdditionalLightData HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                realLight.shadows = LightShadows.Hard;
                HDRPLight.EnableShadows(true);
                HDRPLight.SetShadowNearPlane(0.35f);

                AudioSource audioSource1 = lightObject.AddComponent<AudioSource>();
                audioSource1.loop = false;
                audioSource1.playOnAwake = false;
                audioSource1.volume = 0.7f;
                audioSource1.spatialBlend = 1;
                audioSource1.spatialize = true;
                audioSource1.priority = 0;
                audioSource1.dopplerLevel = 0.3f;
                audioSource1.outputAudioMixerGroup = mixerGroup;
                audioSource1.rolloffMode = AudioRolloffMode.Linear;
                audioSource1.maxDistance = 20;

                //will work on dynamo stuff later (or never)
                /*var dynamoSource = new GameObject($"DynamoAudioSource ({clientId})", typeof(AudioSource));
                dynamoSource.transform.SetParent(lightObject.transform, false);
                AudioSource audioSource2 = dynamoSource.GetComponent<AudioSource>();
                audioSource2.loop = true;
                audioSource2.priority = 0;
                audioSource2.spatialBlend = 1;
                audioSource2.spatialize = true;
                audioSource2.clip = LightScript.flashlightClips[7];
                audioSource2.outputAudioMixerGroup = mixerGroup;
                audioSource2.rolloffMode = AudioRolloffMode.Linear;
                audioSource2.maxDistance = 20;*/

                lightObject.SetActive(true);
                //dynamoSource.SetActive(true);
                realLight.enabled = currentFlashState;

                Plugin.mls.LogDebug($"Made player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}'s light locally!");
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"NETWORKING ERROR WHILE MAKING LIGHT OBJECT FOR PLAYER {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}!\n{e}");
                return;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleLightServerRpc(ulong clientId, bool enabled)
        {
            Plugin.mls.LogDebug($"Recieved request to toggle light to {enabled} for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}, sending to clients...");
            ToggleLightClientRpc(clientId, enabled);
        }

        [ClientRpc]
        void ToggleLightClientRpc(ulong clientId, bool enabled)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            Light light = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponent<Light>();
            light.enabled = enabled;
            Plugin.mls.LogDebug($"Toggled light to {enabled} via client rpc from player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}!!");
        }

        [ServerRpc(RequireOwnership = false)]
        public void FlickerLightOutServerRpc(ulong clientId)
        {
            Plugin.mls.LogDebug($"Recieved request to flicker light for client #{clientId}, sending to clients...");
            FlickerLightOutClientRpc(clientId);
        }

        [ClientRpc]
        void FlickerLightOutClientRpc(ulong clientId)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;
            Plugin.mls.LogDebug($"Flickering light for client #{clientId}");
            StartCoroutine(FlickerAndStopNetworked(clientId));
        }

        private IEnumerator FlickerAndStopNetworked(ulong clientId)
        {
            Light tempObject = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponent<Light>();
            tempObject.enabled = false;
            yield return new WaitForSeconds(0.1f);
            tempObject.enabled = true;
            yield return new WaitForSeconds(0.1f);
            tempObject.enabled = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayNetworkedSoundServerRpc(ulong clientId, int flashlightClip)
        {
            Plugin.mls.LogDebug($"Recieved request to play networked sound at player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}'s position");
            PlayNetworkedSoundClientRpc(clientId, flashlightClip);
        }

        [ClientRpc]
        void PlayNetworkedSoundClientRpc(ulong clientId, int flashlightClip)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            AudioSource tempSource = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponent<AudioSource>();
            tempSource.PlayOneShot(LightScript.flashlightClips[flashlightClip], 1);
            Plugin.mls.LogDebug($"Played flashlight clip #{flashlightClip} at position of player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
        }

        /*
        [ServerRpc(RequireOwnership = false)]
        public void StartDynamoServerRpc(ulong clientId, int flashlightClip)
        {
            StartDynamoClientRpc(clientId, flashlightClip);
        }

        [ServerRpc(RequireOwnership =false)]
        public void StopDynamoServerRpc(ulong clientId)
        {
            StopDynamoClientRpc(clientId);
        }

        [ClientRpc]
        void StartDynamoClientRpc(ulong clientId, int flashlightClip)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;
            AudioSource tempSource2 = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponentInChildren<AudioSource>();
            tempSource2.clip = LightScript.flashlightClips[flashlightClip];
            tempSource2.Play();
        }

        [ClientRpc]
        void StopDynamoClientRpc(ulong clientId)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;
            AudioSource tempSource2 = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponentInChildren<AudioSource>();
            tempSource2.Stop();
        }*/

        //list of rpcs:
        ///(done, WORKS) rpc that sends info to the server about the light info (color, intensity, range, angle) and stuff and then spawns the light, might make some configs unchangeable in-game when using networking? most of them maybe
        ///(done, WORKS) rpc for toggling light
        ///(done, WORKS) rpc for playing different sounds
        //rpc for dynamo recharge (not implemented yet)
        ///(not for now) rpc for buying upgrade
        ///(also not for now) rpc for updating states, and upgrade price based on host's settings


        ///ooooo secret terminal command oooooo (that isnt implemented but can still be called from unity explorer i guess)
        [ServerRpc(RequireOwnership = false)]
        public void SayHiServerRpc(ulong clientId)
        {
            Plugin.mls.LogDebug("SERVER RPC TRIGGERED!!!");
            SayHiClientRpc(clientId);
        }

        [ClientRpc]
        public void SayHiClientRpc(ulong clientId)
        {
            Plugin.mls.LogDebug("oh hey someone used the secret command that i left in the mod");
            HUDManager.Instance.DisplayTip("hello!!", $"{StartOfRound.Instance.allPlayerScripts[clientId].playerUsername} said hi through the terminal! :)");
        }
    }
}
