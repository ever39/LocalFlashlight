using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

//keeping it like this, doesn't need a lot of stuff, no networkobjects needed: this just sends messages from client to server and vice-versa, it works fine for now
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

        //whatever's below this comment is fine, whatever's above this comment has to stay since it's the base of the entire networking system alongside NetworkingPatches

        [ServerRpc(RequireOwnership = false)]
        public void RequestAllLightsUpdateServerRpc()
        {
            Plugin.mls.LogInfo("Recieved ServerRpc to update lights on all clients");
            AcceptAllLightUpdateRequestClientRpc();
        }

        [ClientRpc]
        public void AcceptAllLightUpdateRequestClientRpc()
        {
            Plugin.mls.LogInfo("Updating local light for all clients!");
            ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out Color tempColor);
            MakeLightServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, tempColor, Plugin.lightIntensity.Value, Plugin.lightRange.Value, Plugin.lightAngle.Value, LightScript.publicFlashState);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MakeLightServerRpc(ulong clientId, UnityEngine.Color color, float intensity, float range, float angle, bool currentState)
        {
            Plugin.mls.LogDebug($"Recieved ServerRpc to create light from player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            if (color == Color.white)
            {
                Plugin.mls.LogInfo($"small warning: player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername} does not have a valid light color, or is set to the default color. If they didn't intend that, they might have inputted the HEX code wrong when setting the light's color");
            }
            MakeLightClientRpc(clientId, color, intensity, range, angle, currentState);
        }

        [ClientRpc]
        public void MakeLightClientRpc(ulong clientId, UnityEngine.Color lightColor, float lightIntensity, float lightRange, float lightAngle, bool currentState)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId)
            {
                return;
            }

            Plugin.mls.LogDebug($"Creating light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");

            if (StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})") != null)
            {
                try
                {
                    GameObject tempObject2 = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").gameObject;
                    Light tempLight = tempObject2.GetComponent<Light>();

                    tempLight.color = lightColor;
                    Plugin.mls.LogDebug($"Set flashlightcolor to {lightColor} for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
                    tempLight.intensity = lightIntensity;
                    tempLight.range = lightRange;
                    tempLight.spotAngle = lightAngle;
                    Plugin.mls.LogDebug($"Light already existed locally for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}, updated its values instead");
                    return;
                }
                catch (Exception)
                {
                    Plugin.mls.LogError($"error while updating light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
                }
            }

            try
            {
                var tempObject1 = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera;
                var mixerGroup = GameNetworkManager.Instance.localPlayerController.movementAudio.GetComponent<AudioSource>().outputAudioMixerGroup;

                var lightObject = new GameObject($"lightObject ({clientId})");
                lightObject.transform.SetParent(tempObject1.transform, false);
                lightObject.transform.localPosition = new Vector3(-0f + (Plugin.applyCustomPosToOthers.Value ? Plugin.lightPosX1.Value : 0), -0.55f + (Plugin.applyCustomPosToOthers.Value ? (Plugin.lightPosY1.Value + 0.55f) : 0), -0.1f + (Plugin.applyCustomPosToOthers.Value ? Plugin.lightPosZ1.Value : 0));
                lightObject.transform.Rotate(new Vector3(-12, 3, 0));

                Light realLight = lightObject.AddComponent<Light>();
                realLight.type = LightType.Spot;
                realLight.shape = LightShape.Cone;
                realLight.color = lightColor;
                realLight.intensity = lightIntensity;
                realLight.range = lightRange;
                realLight.shadows = LightShadows.Soft;
                realLight.spotAngle = lightAngle;
                HDAdditionalLightData HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                HDRPLight.EnableShadows(true);
                HDRPLight.SetShadowNearPlane(0.45f);

                AudioSource audioSource1 = lightObject.AddComponent<AudioSource>();
                audioSource1.loop = false;
                audioSource1.playOnAwake = false;
                audioSource1.volume = 1;
                audioSource1.spatialBlend = 1;
                audioSource1.spatialize = true;
                audioSource1.priority = 0;
                audioSource1.dopplerLevel = 0.5f;
                audioSource1.outputAudioMixerGroup = mixerGroup;
                audioSource1.rolloffMode = AudioRolloffMode.Linear;
                audioSource1.maxDistance = 40;
                lightObject.AddComponent<OccludeAudio>();

                //will work on dynamo stuff later later
                var dynamoSource = new GameObject($"DynamoAudioSource ({clientId})", typeof(AudioSource));
                dynamoSource.transform.SetParent(lightObject.transform, false);
                AudioSource audioSource2 = dynamoSource.GetComponent<AudioSource>();
                audioSource2.loop = true;
                audioSource2.priority = 0;
                audioSource2.spatialBlend = 1;
                audioSource2.spatialize = true;
                audioSource2.outputAudioMixerGroup = mixerGroup;
                audioSource2.rolloffMode = AudioRolloffMode.Linear;
                audioSource2.maxDistance = 30;
                dynamoSource.AddComponent<OccludeAudio>();

                lightObject.SetActive(true);
                dynamoSource.SetActive(true);
                realLight.enabled = currentState;

                Plugin.mls.LogDebug($"Created light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
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
            Plugin.mls.LogDebug($"Recieved ServerRpc to toggle light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            ToggleLightClientRpc(clientId, enabled);
        }

        [ClientRpc]
        void ToggleLightClientRpc(ulong clientId, bool enabled)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            Plugin.mls.LogDebug($"Toggling light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            Light light = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponent<Light>();
            light.enabled = enabled;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayNetworkedSoundServerRpc(ulong clientId, int flashlightClip)
        {
            ///if using backup sound just use the mod's default sounds. what's this gonna do, select another sound option? :)
            Plugin.mls.LogDebug($"Recieved ServerRpc to play networked sound for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            PlayNetworkedSoundClientRpc(clientId, flashlightClip);
        }

        [ClientRpc]
        void PlayNetworkedSoundClientRpc(ulong clientId, int flashlightClip)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            Plugin.mls.LogDebug($"Playing flashlight clip #{flashlightClip} at position of player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            AudioSource tempSource = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").GetComponent<AudioSource>();
            tempSource.PlayOneShot(LightScript.flashlightClips[flashlightClip], (float)Plugin.networkedPlayersVol.Value / 120);
        }

        [ServerRpc(RequireOwnership = false)]
        public void FlickerOutServerRpc(ulong clientId)
        {
            Plugin.mls.LogDebug($"Recieved ServerRpc to flicker light for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            FlickerOutClientRpc(clientId);
        }

        [ClientRpc]
        void FlickerOutClientRpc(ulong clientId)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;
            Plugin.mls.LogDebug($"Flickering light out for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
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

        //add bool in voids to check if backup sound is used instead (used for custom sounds)
        [ServerRpc(RequireOwnership = false)]
        public void PlayDynamoAudioServerRpc(ulong clientId, int flashlightClip)
        {
            Plugin.mls.LogDebug($"Recieved ServerRpc to play dynamo audio for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            PlayDynamoAudioClientRpc(clientId, flashlightClip);
        }

        [ClientRpc]
        public void PlayDynamoAudioClientRpc(ulong clientId, int flashlightClip)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            Plugin.mls.LogDebug($"Playing and looping dynamo audio for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            AudioSource tempSource = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").FindChild($"DynamoAudioSource ({clientId})").GetComponent<AudioSource>();
            tempSource.clip = LightScript.flashlightClips[flashlightClip];
            tempSource.time = 0;
            tempSource.volume = (float)Plugin.networkedPlayersVol.Value / 240;
            tempSource.loop = true;
            tempSource.Play();
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopDynamoAudioServerRpc(ulong clientId)
        {
            Plugin.mls.LogDebug($"Recieved serverRpc to stop dynamo audio for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            StopDynamoAudioClientRpc(clientId);
        }

        [ClientRpc]
        public void StopDynamoAudioClientRpc(ulong clientId)
        {
            if (clientId == StartOfRound.Instance.localPlayerController.playerClientId) return;

            Plugin.mls.LogDebug($"Stopping dynamo audio for player {StartOfRound.Instance.allPlayerScripts[clientId].playerUsername}");
            AudioSource tempSource = StartOfRound.Instance.allPlayerScripts[clientId].gameplayCamera.transform.FindChild($"lightObject ({clientId})").FindChild($"DynamoAudioSource ({clientId})").GetComponent<AudioSource>();
            tempSource.Stop();
            tempSource.time = 0;
            tempSource.volume = 0;
            tempSource.loop = false;
        }

        ///UNUSED - terminalAPI related code
        /*[ServerRpc(RequireOwnership = false)]
        public void SayHiServerRpc(ulong clientId)
        {
            Plugin.mls.LogDebug("Recieved ServerRpc to say hi to the entire server :)");
            SayHiClientRpc(clientId);
        }

        [ClientRpc]
        public void SayHiClientRpc(ulong clientId)
        {
            Plugin.mls.LogDebug("displaying hello message to client");
            HUDManager.Instance.DisplayTip("hello!!", $"{StartOfRound.Instance.allPlayerScripts[clientId].playerUsername} said hi through the terminal!");
        }*/
    }
}
