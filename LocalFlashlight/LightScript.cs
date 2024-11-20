using GameNetcodeStuff;
using LocalFlashlight.Compatibilities;
using LocalFlashlight.Networking;
using System;
using System.Collections;
using TerminalApi.Classes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

//i kept in some unused code as comments in case i do want to use them in the future (if i ever update this mod again, schedule has been horrible, haven't updated in like half a year)
//i also dont recommend using this mod as a reference to make one of your mods
namespace LocalFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        #region Variables
        private PlayerControllerB player_controller;
        public ulong clientId;
        private GameObject player, cameraObject, dynamoLightObj;
        public static GameObject lightObject;

        //explanation as to why theyre arrays now in the noise assigning void
        public static AudioClip[] flashlightClips = new AudioClip[25];
        public static int[] activeClips = new int[20];

        static AudioSource flashSource, dynamoAudioSource;
        private Light localLight;
        private bool flashState, posState;
        public static bool publicFlashState;
        private bool canToggle = true;
        private static SoundOptions selectedSoundOption;
        private RechargeOptions selectedRechargeOption;
        private float playerMovementSpeed;
        public static float UIHideTime;

        // battery values
        public static float maxBatteryTime, BatteryPercent, truePercentBattery, BatteryClamped;
        public static float batteryTime = maxBatteryTime;
        private float batteryRegen, regenCool, batteryCooldown, burnOutCooldown;

        // other values
        private float shakeCool, soundCool;
        private float lastShakeTime = 0;
        private bool rechargeKeyHeld = false;
        private float targetVolume = 0f;
        private float targetPitch = 0f;
        private float windRechargeMult = 0f;
        public static Color flashColor;

        //terminal stuff
        private int toggleAmount = 0;
        private float flashOnTime = 0;
        private int timesUsedCommand = 0;

        public bool canUse = true;
        #endregion

        private void Start()
        {
            Plugin.mls.LogDebug("localflashlight script started, setting mod values and creating light...");

            FindLocalPlayer();

            if (player == null | cameraObject == null)
            {
                Plugin.mls.LogError("either the player is null, or the camera object is null, so the mod won't work at all since it needs those two things");
                return;
            }

            SetModValues();
            SetFlashlightSounds();
            MakeLocalLight();

            if (Plugin.enableNetworking.Value)
                LFNetworkHandler.Instance?.RequestAllLightsUpdateServerRpc();

            localLight.enabled = false;

            TerminalApi.TerminalApi.AddCommand("LocalFlashlight", new CommandInfo()
            {
                Category = "other",
                Description = Plugin.enableNetworking.Value ? "Displays statistics about the flashlight's usage" : "Displays statistics about the flashlight's usage",
                DisplayTextSupplier = onCommandParse
            });
        }

        #region Update void
        public void Update()
        {

            if (player == null | player_controller == null | cameraObject == null | lightObject == null)
                return;

            try
            {
                UpdateModValues();
                UpdateBatteryValues();

                /// handles consuming battery and turning the light off when under 0%
                if (flashState)
                {
                    batteryTime -= Time.deltaTime;
                    flashOnTime += Time.deltaTime;

                    if (batteryTime < 0)
                    {
                        if (Plugin.flickerOnBatteryBurn.Value)
                            StartCoroutine(FlickerAndStop());
                        else
                            Toggle();
                    }
                }

                // handles hiding UI when the flashlight is not in use
                if (BatteryPercent <= 99.8 | flashState)
                {
                    UIHideTime = Plugin.HideUIDelay.Value;
                }
                else if (BatteryPercent > 99.8 && !flashState)
                {
                    UIHideTime -= Time.deltaTime;
                }

                if (Plugin.rechargeInShip.Value && !flashState && player_controller.isInHangarShipRoom && batteryTime < maxBatteryTime - 0.01)
                {
                    batteryTime += Time.deltaTime * 3;
                }

                #region Toggling and changing light position
                //calling toggle void, added some checks in case you do have the flashlight toggle "synergy" config enabled
                if (Plugin.flashlightToggleInstance.toggleKey.triggered)
                {
                    if (player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat | player_controller.inSpecialInteractAnimation | localLight == null)
                        return;

                    if (batteryTime > 0)
                    {
                        //flash beacons later on?
                        if (flashState) Toggle();
                        else if (!flashState && canToggle) Toggle();
                        else
                        {
                            if (Patches.isFlashlightHeld)
                            {
                                PlayNoise(activeClips[8], 0.3f, false);
                            }
                            //Plugin.mls.LogDebug("a flashlight is already in the inventory, and the synergy config is enabled! not toggling on.");
                        }
                    }
                    else
                    {
                        PlayNoise(activeClips[8], 0.7f, false);
                    }
                }

                //handles changing the light's position from left to right and vice-versa
                if (Plugin.flashlightToggleInstance.switchLightPosKey.triggered)
                {
                    if (player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat | player_controller.inSpecialInteractAnimation | localLight == null)
                        return;

                    ChangeLightPosition();
                }
                #endregion
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"something went wrong in the update script!! might be either from battery update, mod value update, or whatever else there is in this script:\n{e}");
                return;
            }
        }
        #endregion

        #region Other methods
        void Toggle()
        {
            if (!(player == null || cameraObject == null))
            {
                flashState = !flashState;
                publicFlashState = flashState;
                localLight.enabled = flashState;

                if (Plugin.enableNetworking.Value)
                {
                    LFNetworkHandler.Instance?.ToggleLightServerRpc(player_controller.playerClientId, flashState);
                }

                if (flashState)
                {
                    PlayNoise(activeClips[0], 0.3f, true);
                    //Plugin.mls.LogDebug("toggled light on");
                    toggleAmount++;
                }
                else if (!flashState)
                {
                    if (batteryTime <= 0 && Plugin.BatteryBurnOut.Value)
                    {
                        regenCool = burnOutCooldown;
                        //Plugin.mls.LogDebug("No battery, setting cooldown to the burnt out value");
                    }
                    else
                    {
                        regenCool = batteryCooldown;
                        //Plugin.mls.LogDebug($"Battery at " + truePercentBattery.ToString("0.0") + "%" + ", setting cooldown to normal");
                        toggleAmount++;
                    }
                    if (batteryTime <= 0) PlayNoise(activeClips[2], 0.5f, true);
                    else PlayNoise(activeClips[1], 0.6f, true);

                    //Plugin.mls.LogDebug("toggled light off");
                }
            }
        }

        void ChangeLightPosition()
        {
            posState = !posState;

            lightObject.transform.localPosition = new Vector3(posState ? Plugin.lightPosX2.Value : Plugin.lightPosX1.Value, posState ? Plugin.lightPosY2.Value : Plugin.lightPosY1.Value, posState ? Plugin.lightPosZ2.Value : Plugin.lightPosZ1.Value); //make a boolean that's positionToggle and switch between true and false, and depending on that use either config for 1 or config for 2 as positions and angles
            lightObject.transform.localRotation = Quaternion.Euler(posState ? Plugin.lightRotX2.Value : Plugin.lightRotX1.Value, posState ? Plugin.lightRotY2.Value : Plugin.lightRotY1.Value, posState ? Plugin.lightRotZ2.Value : Plugin.lightRotZ1.Value);
            PlayNoise(activeClips[7], 0.3f, false);
            //Plugin.mls.LogDebug("changed light position");
        }

        public static void PlayNoise(int clipIndex, float volume, bool playForWorld = false)
        {
            flashSource?.PlayOneShot(flashlightClips[clipIndex]);
            if (playForWorld)
            {
                FindObjectOfType<RoundManager>().PlayAudibleNoise(lightObject.transform.position, 8, volume, 0, StartOfRound.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed, 0);

                if (Plugin.enableNetworking.Value)
                    LFNetworkHandler.Instance?.PlayNetworkedSoundServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, selectedSoundOption == SoundOptions.CustomSounds ? clipIndex - 1 : clipIndex);
            }
            //Plugin.mls.LogDebug("played sound");
        }

        void holdCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo | player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat) return;

            targetVolume = (float)Plugin.FlashVolume.Value / 200;
            targetPitch = 1;
            rechargeKeyHeld = true;
            if (dynamoAudioSource != null)
            {
                dynamoAudioSource.loop = true;
                dynamoAudioSource.clip = flashlightClips[activeClips[5]];
                dynamoAudioSource.Play();

                if (Plugin.enableNetworking.Value)
                    LFNetworkHandler.Instance.PlayDynamoAudioServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, activeClips[5]);
            }
            //Plugin.mls.LogDebug("input press performed, starting dynamo use key hold");
        }
        void releaseCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            targetVolume = 0;
            targetPitch = 0;
            soundCool = 0.25f;
            rechargeKeyHeld = false;
            if (dynamoAudioSource != null)
            {
                dynamoAudioSource.loop = false;

                if (Plugin.enableNetworking.Value)
                    LFNetworkHandler.Instance.StopDynamoAudioServerRpc(StartOfRound.Instance.localPlayerController.playerClientId);
            }
            //Plugin.mls.LogDebug("input cancel performed, stopping dynamo use key hold");
        }

        void SetFlashlightSounds()
        {
            try
            {
                selectedSoundOption = Plugin.soundOption.Value;
                #region loading flashlight audio clips
                flashlightClips[0] = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                flashlightClips[2] = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                flashlightClips[4] = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                flashlightClips[6] = Plugin.bundle.LoadAsset<AudioClip>("recharged1");
                flashlightClips[8] = Plugin.bundle.LoadAsset<AudioClip>("reloadlight");
                flashlightClips[10] = Plugin.bundle.LoadAsset<AudioClip>("dynamo");
                flashlightClips[12] = Plugin.bundle.LoadAsset<AudioClip>("flashDown2");
                flashlightClips[14] = Plugin.bundle.LoadAsset<AudioClip>("changepos.ogg");
                flashlightClips[16] = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");

                if (Plugin.hasLCSoundTool)
                {
                    flashlightClips[1] = LCSoundToolCompatibility.LoadCustomSound("lighton_custom");
                    flashlightClips[3] = LCSoundToolCompatibility.LoadCustomSound("lightoff_custom");
                    flashlightClips[5] = LCSoundToolCompatibility.LoadCustomSound("lowtoggle_custom");
                    flashlightClips[7] = LCSoundToolCompatibility.LoadCustomSound("recharged_custom");
                    flashlightClips[9] = LCSoundToolCompatibility.LoadCustomSound("reloadlight_custom");
                    flashlightClips[11] = LCSoundToolCompatibility.LoadCustomSound("dynamo_custom");
                    flashlightClips[13] = LCSoundToolCompatibility.LoadCustomSound("flashDown_custom");
                    flashlightClips[15] = LCSoundToolCompatibility.LoadCustomSound("changepos_custom");
                    flashlightClips[17] = LCSoundToolCompatibility.LoadCustomSound("denytoggle_custom");
                }
                else
                {
                    Plugin.mls.LogWarning("LCSoundTool not installed/enabled, this will lead to an error if you're using custom sounds for the flashlight.");
                    #region setting clips to backup clips
                    activeClips[0] = 0;
                    activeClips[1] = 2;
                    activeClips[2] = 4;
                    activeClips[3] = 6;
                    activeClips[4] = 8;
                    activeClips[5] = 10;
                    activeClips[6] = 12;
                    activeClips[7] = 14;
                    activeClips[8] = 16;
                    #endregion
                }
                Plugin.mls.LogDebug("loaded sound assets...");
                #endregion

                switch (selectedSoundOption)
                {
                    case SoundOptions.InGameFlashlight:
                        activeClips[0] = 0;
                        activeClips[1] = 2;
                        activeClips[2] = 4;
                        activeClips[3] = 6;
                        activeClips[4] = 8;
                        activeClips[5] = 10;
                        activeClips[6] = 12;
                        activeClips[7] = 14;
                        activeClips[8] = 16;
                        return;

                    case SoundOptions.CustomSounds:
                        activeClips[0] = 1;
                        activeClips[1] = 3;
                        activeClips[2] = 5;
                        activeClips[3] = 7;
                        activeClips[4] = 9;
                        activeClips[5] = 11;
                        activeClips[6] = 13;
                        activeClips[7] = 15;
                        activeClips[8] = 17;
                        return;
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while setting localflashlight mod sounds:\n{e}");
                return;
            }
        }

        void SetModValues()
        {
            try
            {
                flashState = false;
                posState = false;
                canToggle = true;
                rechargeKeyHeld = false;

                maxBatteryTime = Plugin.BatteryLife.Value;
                batteryTime = maxBatteryTime;
                batteryRegen = Plugin.RechargeMult.Value;

                burnOutCooldown = Plugin.BurnOutCool.Value;
                batteryCooldown = Plugin.BatteryCool.Value;

                UIHideTime = 2f + Plugin.HideUIDelay.Value;

                selectedRechargeOption = Plugin.rechargeOption.Value;
                shakeCool = Plugin.shakeActionCooldown.Value;

                playerMovementSpeed = player_controller.movementSpeed;

                ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out flashColor);

                //Plugin.mls.LogDebug("mod values set");
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while setting initial localflashlight mod values:\n{e}");
                return;
            }
        }

        void FindLocalPlayer()
        {
            if (player != null) return;

            try
            {
                Plugin.mls.LogDebug("finding local player controller");
                player = GameNetworkManager.Instance.localPlayerController.gameObject;

                if (player != null)
                {
                    player_controller = player.GetComponent<PlayerControllerB>();
                    cameraObject = player_controller.gameplayCamera.gameObject;
                    clientId = GameNetworkManager.Instance.localPlayerController.playerClientId;
                    //Plugin.mls.LogDebug("found player gameobject, setting values and sounds");
                    return;
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while finding the local player controller, it may be null or the mod just can't find it\n{e}");
            }
        }
        void MakeLocalLight()
        {
            if (player == null)
            {
                Plugin.mls.LogError("no player to attach the light to (somehow)");
                return;
            }

            try
            {
                var mixerGroup = GameNetworkManager.Instance.localPlayerController.itemAudio.GetComponent<AudioSource>().outputAudioMixerGroup;

                lightObject = new GameObject();
                lightObject.transform.SetParent(cameraObject.transform, false);
                lightObject.name = "lightObject (LOCAL)";

                localLight = lightObject.AddComponent<Light>();
                localLight.type = LightType.Spot;
                localLight.shape = LightShape.Cone;
                localLight.color = flashColor;
                localLight.intensity = Plugin.lightIntensity.Value;
                localLight.range = Plugin.lightRange.Value;
                localLight.shadows = LightShadows.Hard;
                localLight.spotAngle = Plugin.lightAngle.Value;

                flashSource = lightObject.AddComponent<AudioSource>();
                flashSource.loop = false;
                flashSource.playOnAwake = false;
                flashSource.volume = (float)Plugin.FlashVolume.Value / 100;
                flashSource.priority = 0;
                flashSource.spatialize = true;
                flashSource.outputAudioMixerGroup = mixerGroup;

                lightObject.transform.localPosition = new Vector3(Plugin.lightPosX1.Value, Plugin.lightPosY1.Value, Plugin.lightPosZ1.Value);
                lightObject.transform.Rotate(new Vector3(Plugin.lightRotX1.Value, Plugin.lightRotY1.Value, Plugin.lightRotZ1.Value));
                //Plugin.mls.LogDebug("finished positioning light");

                if (selectedRechargeOption == RechargeOptions.Dynamo)
                {
                    dynamoLightObj = new GameObject("DynamoAudioSource (ONLY USED FOR DYNAMO RECHARGE)");
                    dynamoLightObj.transform.SetParent(lightObject.transform, false);
                    dynamoAudioSource = dynamoLightObj.AddComponent<AudioSource>();
                    dynamoAudioSource.name = "dynamo audio source";
                    dynamoAudioSource.loop = true;
                    dynamoAudioSource.priority = 0;
                    dynamoAudioSource.spatialize = true;
                    dynamoAudioSource.outputAudioMixerGroup = mixerGroup;
                }

                //handles shadows, no longer as buggy, might delete its config if all works well since the vanilla flashlights have shadows
                if (Plugin.ShadowsEnabled.Value)
                {
                    var HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                    HDRPLight.EnableShadows(true);
                    HDRPLight.SetShadowNearPlane(0.35f);
                }

                lightObject.SetActive(true);

                Plugin.mls.LogInfo("created local light object!");
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while creating the light object:\n{e}");
                return;
            }
        }

        //Handles updating the mod's values, including some configs
        void UpdateModValues()
        {
            regenCool -= Time.deltaTime;

            BatteryPercent = (int)(Math.Ceiling(batteryTime / maxBatteryTime * 100));
            BatteryClamped = batteryTime / maxBatteryTime;
            truePercentBattery = BatteryClamped * 100;

            ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out flashColor);
            localLight.color = flashColor;

            localLight.range = Plugin.lightRange.Value;
            localLight.spotAngle = Plugin.lightAngle.Value;
            flashSource.volume = (float)Plugin.FlashVolume.Value / 100;

            if (selectedRechargeOption == RechargeOptions.Dynamo && dynamoAudioSource != null)
            {
                dynamoAudioSource.volume = Mathf.Lerp(dynamoAudioSource.volume, targetVolume, Time.deltaTime * 6);
                dynamoAudioSource.pitch = Mathf.Lerp(dynamoAudioSource.pitch, targetPitch, Time.deltaTime * 6);
            }

            canToggle = !Plugin.flashlightToggleModSynergyquestionmark.Value || (!player_controller.allHelmetLights[0].enabled && !player_controller.allHelmetLights[1].enabled && !Patches.isFlashlightPocketed && !Patches.isFlashlightHeld && !ReservedItemSlotCompatibility.flashlightInReservedSlot);

            if (player_controller.isPlayerDead)
            {
                flashState = false;
                localLight.enabled = false;
                batteryTime = maxBatteryTime;
                regenCool = 0;
                canToggle = true;
                rechargeKeyHeld = false;
            }

            //light dimming
            float val = Mathf.Lerp(0, 1, BatteryClamped);
            val = Math.Max(val, (float)Plugin.flashlightStopDimBatteryValue.Value / 100);

            //the game has a globalFlashlightInterferenceLevel integer, which is thankfully static, so we can use that to change the light's intensity randomly when all of the lights flicker
            if (FlashlightItem.globalFlashlightInterferenceLevel >= 1)
            {
                localLight.intensity = Plugin.lightIntensity.Value * (selectedRechargeOption == RechargeOptions.FacilityPowered ? Plugin.apparaticeFlashlightIntensityMult.Value : 1) * Patches.randomLightInterferenceMultiplier;
                return;
            }
            localLight.intensity = Plugin.dimEnabled.Value ? Plugin.lightIntensity.Value * val : Plugin.lightIntensity.Value;
        }
        void UpdateBatteryValues()
        {
            switch (selectedRechargeOption)
            {
                case RechargeOptions.Time:
                    if (!flashState)
                        if (batteryTime <= maxBatteryTime - 0.001)
                            if (regenCool < 0)
                                batteryTime += batteryRegen * Time.deltaTime;

                    if (batteryTime > maxBatteryTime)
                    {
                        batteryTime = maxBatteryTime;
                        if (selectedRechargeOption == RechargeOptions.Time) 
                            PlayNoise(activeClips[3], .7f, true);
                    }
                    return;

                case RechargeOptions.Shake:
                    if (Plugin.flashlightToggleInstance.rechargeKey.triggered && Time.time - lastShakeTime > shakeCool && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
                    {
                        if (player_controller.sprintMeter > 0.25f)
                        {
                            batteryTime += (maxBatteryTime * 0.07f) * batteryRegen;
                            PlayNoise(activeClips[4], .6f, true);
                            player_controller.sprintMeter -= ((float)Plugin.shakeStaminaConsume.Value) / 100;
                            lastShakeTime = Time.time;
                        }
                        else PlayNoise(activeClips[8], .7f, false);
                    }
                    return;

                case RechargeOptions.Dynamo:
                    if (!(player_controller.quickMenuManager.isMenuOpen && player_controller.isPlayerDead && player_controller.isTypingChat && player_controller.inSpecialInteractAnimation))
                    {
                        Plugin.flashlightToggleInstance.rechargeKey.performed += holdCallback;
                        Plugin.flashlightToggleInstance.rechargeKey.canceled += releaseCallback;
                    }
                    else
                        Plugin.flashlightToggleInstance.rechargeKey.performed -= holdCallback;

                    if (rechargeKeyHeld)
                    {
                        WindUpFlashlight();
                        windRechargeMult += Time.deltaTime * 3;
                    }
                    else if (windRechargeMult > 0)
                        windRechargeMult -= Time.deltaTime * 5;

                    batteryTime += (Time.deltaTime * batteryRegen) * windRechargeMult;
                    windRechargeMult = Mathf.Clamp(windRechargeMult, 0, 1.5f);
                    player_controller.movementSpeed = rechargeKeyHeld ? playerMovementSpeed * Plugin.dynamoUseMoveMult.Value : playerMovementSpeed;
                    return;

                case RechargeOptions.FacilityPowered:
                    if (Patches.isFacilityPowered)
                        batteryTime = maxBatteryTime;

                    return;
            }
        }

        void WindUpFlashlight()
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            player_controller.sprintMeter -= Time.deltaTime * 0.03f;
            if (soundCool < 0)
            {
                FindObjectOfType<RoundManager>().PlayAudibleNoise(player_controller.transform.position, 8, 0.5f, 0, player_controller.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed, 0);
                soundCool = 0.4f;
            }
            soundCool -= Time.deltaTime;
        }
        public static void FullyChargeBattery()
        {
            batteryTime = maxBatteryTime;
        }

        private IEnumerator FlickerAndStop()
        {
            //Plugin.mls.LogDebug("Flickering local flashlight");
            regenCool = 0.3f + (Plugin.BatteryBurnOut.Value ? burnOutCooldown : batteryCooldown);
            flashState = false;
            publicFlashState = flashState;
            PlayNoise(activeClips[2], 0.6f, true);

            if (Plugin.enableNetworking.Value)
                LFNetworkHandler.Instance?.FlickerOutServerRpc(player_controller.playerClientId);

            localLight.enabled = false;
            yield return new WaitForSeconds(0.1f);
            localLight.enabled = true;
            yield return new WaitForSeconds(0.1f);
            localLight.enabled = false;
        }

        private string onCommandParse()
        {
            string terminalNode = null;
            terminalNode += "the command isn't fully implemented, nor is the upgrade system, so here's some stats from your entire game session up to this point\n\n";
            terminalNode += "Flashlight toggle count: " + toggleAmount.ToString() + "\n";
            terminalNode += "Time spent using the light: " + String.Format("{0:.00}", flashOnTime) + " seconds\n";
            terminalNode += "Flashlight recharge method: " + selectedRechargeOption.ToString() + "\n";
            timesUsedCommand++;
            terminalNode += "Times you used this command: " + timesUsedCommand.ToString() + "\n";
            if (timesUsedCommand <= 1)
                terminalNode += "\nthese stats are ONLY for this play session, so they will get reset if you close the game\n";

            if (Plugin.enableNetworking.Value)
                TerminalApi.TerminalApi.AddCommand("Localflashlight sayhi", new CommandInfo()
                {
                    DisplayTextSupplier = terminalSayHi
                });

            return terminalNode;
        }

        /*
        turns out, i did not take into account reloading files. Awesome! guess i'll finish main networking part, release update and then fix most stuff when i get more info on saves of mods
        WON'T remove terminalAPI because i do want to keep the current playsession thing up, but i do have to learn saves to add the upgrade system in
        private string onNetworkCommandParse()
        {
            string buyNode = null;
            if(Plugin.globalFlashlightOptions.Value == GlobalFlashlightOptions.Unrestricted)
            {
                buyNode = $"LocalFlashlight access is unrestricted, with host being {hostPlayer.playerUsername}";
            }
            else if(Plugin.globalFlashlightOptions.Value == GlobalFlashlightOptions.CrewUpgrade && flashlight isnt enabled)
            {
                var items = terminal.numberOfItemsInDropship;
                if (terminal.groupCredits >= 200)
                {
                    //fully send it as a server rpc, then handle client rpc when you figure out networking
                    terminal.SyncGroupCreditsServerRpc(terminal.groupCredits - 200, items);
                    terminal.PlayTerminalAudioServerRpc(0);
                    LFNetworkHandler.Instance.ChangeFlashlightUpgradeStateServerRpc(true);
                    buyNode = "Bought LocalFlashlight crew upgrade!";
                }
                else
                {
                    //same thing here
                    terminal.PlayTerminalAudioServerRpc(1);
                    buyNode = $"You could not afford this upgrade!\nYour balance is {terminal.groupCredits}$. The cost of this upgrade is {Plugin.upgradeCost.Value}$";
                }
                    
            }
            return buyNode;
        }
        */

        //first command implemented in this mod that also has networking, gets unlocked after typing in the other command and also has a separate noun for it, just like LocalFlashlight buy will :)
        private string terminalSayHi()
        {
            LFNetworkHandler.Instance?.SayHiServerRpc(player_controller.playerClientId);
            return "Said hello to all the people in the server!\n\nhow'd you find this anyway?\n";
        }
        #endregion
    }
}
