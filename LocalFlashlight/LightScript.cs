using GameNetcodeStuff;
using LocalFlashlight.Networking;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

//some of the things in here that look like code are commented because i am still thinking if i should keep them in or not, besides my actual comments related to my own silly code which sucks
//i also dont recommend using this mod as a reference to make one of your mods, but if you do, thank you
namespace LocalFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        #region Values
        // base values (so the mod works properly)
        private PlayerControllerB? player_controller;
        private GameObject? player, cameraObject, dynamoLightObj;
        private static GameObject? lightObject;
        public static AudioClip[] flashlightClips = new AudioClip[25];
        public static int[] activeClips = new int[15];
        static AudioSource? flashSource, dynamoAudioSource;
        private Light? localLight;
        private bool flashState = false;
        public static bool publicFlashState = false;
        private bool canToggle = true;
        private SoundOptions selectedSoundOption;
        private RechargeOptions selectedRechargeOption;
        private float initialPlayerMoveSpeed;
        public static float UIHideTime;

        // battery values
        public static float batteryTime, maxBatteryTime, BatteryPercent, truePercentBattery, BatteryClamped;
        private float batteryRegen, regenCool, batteryCooldown, burnOutCooldown;

        // other values
        private float shakeCool, soundCool;
        private float lastShakeTime = 0;
        private bool rechargeKeyHeld = false;
        private float targetVolume = 0f;
        private float targetPitch = 0f;
        private float windRechargeMult = 0f;
        public static Color flashColor;
        private float flashlightIntensityMult;
        #endregion

        private void Start()
        {
            Plugin.mls.LogInfo("Mod script started, setting mod values and making light...");

            FindLocalPlayer();

            if (player == null | cameraObject == null) return;

            SetModValues();
            SetFlashlightSounds();
            MakeLocalLight();

            if (Plugin.enableNetworking.Value)
            {
                LFNetworkHandler.Instance.RequestAllLightsUpdateServerRpc();
            }
            localLight.enabled = false;
        }

        #region Update void
        public void Update()
        {
            if (player == null | player_controller == null | cameraObject == null | lightObject == null)
                return;

            try
            {
                UpdateModValues();

                ///handles consuming battery and turning the light off when under 0%
                if (flashState)
                {
                    batteryTime -= Time.deltaTime;

                    if (batteryTime < 0)
                    {
                        if (Plugin.flickerOnBatteryBurn.Value)
                            StartCoroutine(FlickerAndStop());
                        else
                            Toggle();
                    }
                }

                // handles hiding UI when the flashlight is not in use
                if (BatteryPercent <= 99.75 | flashState)
                {
                    UIHideTime = Plugin.HideUIDelay.Value;
                }
                if (BatteryPercent > 99.75 && !flashState)
                {
                    UIHideTime -= Time.deltaTime;
                }

                UpdateBatteryValues();

                #region Toggling and changing light position
                //calling toggle void, added some checks in case you do have the flashlight toggle "synergy" config enabled
                if (Plugin.flashlightToggleInstance.toggleKey.triggered)
                {
                    if (player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat | player_controller.inSpecialInteractAnimation | localLight == null)
                        return;

                    if (batteryTime > 0)
                    {
                        if (flashState) Toggle();
                        else if (!flashState && canToggle) Toggle();
                        else
                        {
                            if (Patches.isFlashlightHeld)
                            {
                                PlayNoise(22, 0.3f, false);
                            }
                            //Plugin.mls.LogDebug("a flashlight is already in the inventory, and the synergy config is enabled! not toggling on.");
                        }
                    }
                    else
                    {
                        PlayNoise(22, 0.7f, false);
                    }
                }

                //handles changing the light's position from left to right and vice-versa (used in the past in case the light was covered by scrap, but i'm keeping it in)
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
                    LFNetworkHandler.Instance.ToggleLightServerRpc(player_controller.playerClientId, flashState);
                }

                if (flashState)
                {
                    PlayNoise(activeClips[0], 0.3f, true);
                    //Plugin.mls.LogDebug("toggled light on");
                }
                else
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
                    }
                    if (batteryTime <= 0) PlayNoise(activeClips[2], 0.5f, true);
                    else PlayNoise(activeClips[1], 0.6f, true);

                    //Plugin.mls.LogDebug("toggled light off");
                }
            }
        }

        void ChangeLightPosition()
        {
            var lightPosX = lightObject.transform.localPosition.x;
            var lightRotY = lightObject.transform.localRotation.y;
            lightObject.transform.localPosition = new Vector3(-lightPosX, -0.55f, 0.5f);
            lightObject.transform.localRotation = Quaternion.Euler(-12, -lightRotY, 0);
            PlayNoise(21, 0.3f, false);
            //Plugin.mls.LogDebug("changed light position");
        }

        public static void PlayNoise(int clipIndex, float volume, bool playForWorld)
        {
            flashSource?.PlayOneShot(flashlightClips[clipIndex]);
            if (playForWorld)
            {
                FindObjectOfType<RoundManager>().PlayAudibleNoise(lightObject.transform.position, 8, volume, 0, StartOfRound.Instance.localPlayerController.isInHangarShipRoom, 0);
                LFNetworkHandler.Instance.PlayNetworkedSoundServerRpc(StartOfRound.Instance.localPlayerController.playerClientId, clipIndex);
            }
            //Plugin.mls.LogDebug("played sound");
        }

        void holdCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo | player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat) return;

            targetVolume = (float)Plugin.FlashVolume.Value / 200;
            targetPitch = 1;
            dynamoAudioSource.clip = flashlightClips[activeClips[5]];
            dynamoAudioSource.loop = true;
            dynamoAudioSource.Play();
            rechargeKeyHeld = true;
            //Plugin.mls.LogDebug("input press performed, starting dynamo use key hold");
        }
        void releaseCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            targetVolume = 0;
            targetPitch = 0;
            dynamoAudioSource.loop = false;
            soundCool = 0.25f;
            rechargeKeyHeld = false;
            //Plugin.mls.LogDebug("input cancel performed, stopping dynamo use key hold");
        }

        void SetFlashlightSounds()
        {
            try
            {
                selectedSoundOption = Plugin.soundOption.Value;
                //okay so for the audioclips, ive gone ahead and made some changes so it's okay if someone ever makes a fork of this mod, the arrays are in order of the original sound's appearance
                //in terms of active clips: 0 is lighton, 1 is lightoff, 2 is lowtoggle, 3 is recharged, 4 is reloadlight, 5 is dynamo, 6 is flashdown, flashlight clip 21 is change position, clip 22 is deny toggle

                flashlightClips[0] = Plugin.bundle.LoadAsset<AudioClip>("lighton");
                flashlightClips[1] = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                flashlightClips[2] = Plugin.bundle.LoadAsset<AudioClip>("lighton2");

                flashlightClips[3] = Plugin.bundle.LoadAsset<AudioClip>("lightoff");

                flashlightClips[4] = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle");
                flashlightClips[5] = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle2");
                flashlightClips[6] = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");

                flashlightClips[7] = Plugin.bundle.LoadAsset<AudioClip>("recharged");
                flashlightClips[8] = Plugin.bundle.LoadAsset<AudioClip>("recharged1");
                flashlightClips[9] = Plugin.bundle.LoadAsset<AudioClip>("recharged2");

                flashlightClips[10] = Plugin.bundle.LoadAsset<AudioClip>("reloadlight");
                //flashlightClips[-] = Plugin.bundle.LoadAsset<AudioClip>("reloadlight"); //change?
                //flashlightClips[-] = Plugin.bundle.LoadAsset<AudioClip>("reloadlight"); //change?

                flashlightClips[11] = Plugin.bundle.LoadAsset<AudioClip>("dynamo");
                flashlightClips[12] = Plugin.bundle.LoadAsset<AudioClip>("dynamo1");
                flashlightClips[13] = Plugin.bundle.LoadAsset<AudioClip>("dynamo2");

                flashlightClips[14] = Plugin.bundle.LoadAsset<AudioClip>("flashDown");
                flashlightClips[15] = Plugin.bundle.LoadAsset<AudioClip>("flashDown1");
                flashlightClips[16] = Plugin.bundle.LoadAsset<AudioClip>("flashDown2");

                flashlightClips[21] = Plugin.bundle.LoadAsset<AudioClip>("changepos.ogg");
                flashlightClips[22] = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                Plugin.mls.LogDebug("loaded assets...");


                switch (selectedSoundOption)
                {
                    case SoundOptions.OriginalLightSounds:
                        activeClips[0] = 0;
                        activeClips[1] = 3;
                        activeClips[2] = 4;
                        activeClips[3] = 7;
                        activeClips[4] = 10;
                        activeClips[5] = 12;
                        activeClips[6] = 15;
                        return;

                    case SoundOptions.OtherwordlyLight:
                        activeClips[0] = 1;
                        activeClips[1] = 1;
                        activeClips[2] = 5;
                        activeClips[3] = 9;
                        activeClips[4] = 10;
                        activeClips[5] = 13;
                        activeClips[6] = 14;
                        return;

                    case SoundOptions.InGameFlashlight:
                        activeClips[0] = 2;
                        activeClips[1] = 2;
                        activeClips[2] = 6;
                        activeClips[3] = 8;
                        activeClips[4] = 10;
                        activeClips[5] = 11;
                        activeClips[6] = 16;
                        return;
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while setting localflashlight mod sounds:\n{e}");
                return;
            }
        }

        //Handles setting the mod's configs after finding the local player's object
        void SetModValues()
        {
            try
            {
                flashState = false;
                publicFlashState = false;
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

                initialPlayerMoveSpeed = player_controller.movementSpeed;
                flashlightIntensityMult = selectedRechargeOption == RechargeOptions.FacilityPowered ? Plugin.apparaticeFlashlightIntensityMult.Value : 1;

                ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out flashColor);
                if(!ColorUtility.TryParseHtmlString(Plugin.flashlightColorHex.Value, out flashColor))
                {
                    Plugin.mls.LogWarning("Flashlight hex code was invalid! did you type it properly?");
                }

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
                player = GameNetworkManager.Instance.localPlayerController.gameObject;

                if (player != null)
                {
                    player_controller = player.GetComponent<PlayerControllerB>();
                    cameraObject = player_controller.gameplayCamera.gameObject;
                    //Plugin.mls.LogDebug("found player gameobject, setting values and sounds");
                    return;
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while finding the local player controller! it may be null or the mod just can't find it for whatever reason\n{e}");
            }
        }
        void MakeLocalLight()
        {
            if (player == null)
            {
                Plugin.mls.LogError("no player to add the light to!");
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
                localLight.intensity = Plugin.Intensity.Value;
                localLight.range = Plugin.Range.Value;
                localLight.shadows = LightShadows.Hard;
                localLight.spotAngle = Plugin.Angle.Value;

                flashSource = lightObject.AddComponent<AudioSource>();
                flashSource.loop = false;
                flashSource.playOnAwake = false;
                flashSource.volume = (float)Plugin.FlashVolume.Value / 100;
                flashSource.priority = 0;
                flashSource.spatialize = true;
                flashSource.outputAudioMixerGroup = mixerGroup;

                lightObject.transform.localPosition = new Vector3(-0.15f, -0.55f, 0.5f);
                lightObject.transform.Rotate(new Vector3(-12, 3, 0));
                //Plugin.mls.LogDebug("finished positioning light");

                if (selectedRechargeOption == RechargeOptions.Dynamo)
                {
                    dynamoLightObj = new GameObject("DynamoAudioSource (ONLY USED FOR DYNAMO RECHARGE)");
                    dynamoLightObj.transform.SetParent(lightObject.transform, false);
                    dynamoAudioSource = dynamoLightObj.AddComponent<AudioSource>();
                    dynamoAudioSource.name = "yeah no idea why this happens but every time its a nullreferenceexception per frame if i dont keep this, nevermind i fixed it";
                    dynamoAudioSource.loop = true;
                    dynamoAudioSource.priority = 0;
                    dynamoAudioSource.spatialize = true;
                    dynamoAudioSource.outputAudioMixerGroup = mixerGroup;
                }

                if (Plugin.ShadowsEnabled.Value)
                {
                    var HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                    HDRPLight.EnableShadows(true);
                    HDRPLight.SetShadowNearPlane(0.35f);
                }
                else Plugin.mls.LogDebug("shadows not enabled, skipped creating them");

                lightObject.SetActive(true);

                Plugin.mls.LogInfo("light up and working!");
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

            localLight.range = Plugin.Range.Value;
            localLight.spotAngle = Plugin.Angle.Value;
            flashSource.volume = (float)Plugin.FlashVolume.Value / 100;

            if (selectedRechargeOption == RechargeOptions.Dynamo && dynamoAudioSource != null)
            {
                dynamoAudioSource.volume = Mathf.Lerp(dynamoAudioSource.volume, targetVolume, Time.deltaTime * 6);
                dynamoAudioSource.pitch = Mathf.Lerp(dynamoAudioSource.pitch, targetPitch, Time.deltaTime * 6);
            }

            canToggle = Plugin.flashlightToggleModSynergyquestionmark.Value ? (!player_controller.helmetLight.enabled && !Patches.isFlashlightPocketed && !Patches.isFlashlightHeld) : true;

            if (player_controller.isPlayerDead)
            {
                flashState = false;
                localLight.enabled = false;
                batteryTime = maxBatteryTime;
                regenCool = 0;
                canToggle = true;
                rechargeKeyHeld = false;
            }

            float val = Mathf.Lerp(0, 1, BatteryClamped);
            val = Math.Max(val, (float)Plugin.flashlightStopDimBatteryValue.Value / 100);

            //the globalFlashlightInterferenceLevel integer is thankfully static, so we can use that to change the light's intensity when all of the lights flicker
            if (FlashlightItem.globalFlashlightInterferenceLevel >= 1)
            {
                localLight.intensity = Plugin.Intensity.Value * flashlightIntensityMult * Patches.randomLightInterferenceMultiplier;
                return;
            }
            localLight.intensity = Plugin.dimEnabled.Value ? Plugin.Intensity.Value * val : Plugin.Intensity.Value;

        }
        void UpdateBatteryValues()
        {
            //didn't mention this in setflashlightsounds but switching is way better
            switch (selectedRechargeOption)
            {
                case RechargeOptions.Time:
                    if (!flashState)
                    {
                        if (batteryTime <= maxBatteryTime - 0.001)
                        {
                            if (regenCool < 0) batteryTime += batteryRegen * Time.deltaTime;
                        }
                    }

                    if (batteryTime > maxBatteryTime)
                    {
                        batteryTime = maxBatteryTime;
                        if (selectedRechargeOption == RechargeOptions.Time) PlayNoise(activeClips[3], .7f, true);
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
                        else PlayNoise(22, .7f, false);
                    }
                    return;

                case RechargeOptions.Dynamo:
                    if (!(player_controller.quickMenuManager.isMenuOpen && player_controller.isPlayerDead && player_controller.isTypingChat && player_controller.inSpecialInteractAnimation))
                    {
                        Plugin.flashlightToggleInstance.rechargeKey.performed += holdCallback;
                        Plugin.flashlightToggleInstance.rechargeKey.canceled += releaseCallback;
                    }
                    else
                    {
                        Plugin.flashlightToggleInstance.rechargeKey.performed -= holdCallback;
                    }

                    if (rechargeKeyHeld)
                    {
                        WindUpFlashlight();
                        windRechargeMult += Time.deltaTime * 3;
                    }
                    else if (windRechargeMult > 0)
                    {
                        windRechargeMult -= Time.deltaTime * 5;
                    }

                    batteryTime += (Time.deltaTime * batteryRegen) * windRechargeMult;
                    windRechargeMult = Mathf.Clamp(windRechargeMult, 0, 1.5f);
                    player_controller.movementSpeed = rechargeKeyHeld ? initialPlayerMoveSpeed * Plugin.dynamoUseMoveMult.Value : initialPlayerMoveSpeed;
                    return;

                case RechargeOptions.FacilityPowered:
                    if (Patches.isFacilityPowered)
                    {
                        batteryTime = maxBatteryTime;
                    }
                    return;

                case RechargeOptions.ShipRecharge:
                    batteryTime = (player_controller.isInHangarShipRoom) ? maxBatteryTime : batteryTime;
                    return;
            }
        }

        void WindUpFlashlight()
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            player_controller.sprintMeter -= Time.deltaTime * 0.03f;
            if (soundCool < 0)
            {
                FindObjectOfType<RoundManager>().PlayAudibleNoise(lightObject.transform.position, 8, 0.5f, 0, player_controller.isInHangarShipRoom, 0);
                soundCool = 0.4f;
            }
            soundCool -= Time.deltaTime;
        }

        private IEnumerator FlickerAndStop()
        {
            //Plugin.mls.LogDebug("Flickering local flashlight");
            regenCool = 0.3f + (Plugin.BatteryBurnOut.Value ? burnOutCooldown : batteryCooldown);
            flashState = false;
            publicFlashState = flashState;
            PlayNoise(6, 0.6f, true);

            if (Plugin.enableNetworking.Value)
                LFNetworkHandler.Instance.FlickerLightOutServerRpc(player_controller.playerClientId);

            localLight.enabled = false;
            yield return new WaitForSeconds(0.1f);
            localLight.enabled = true;
            yield return new WaitForSeconds(0.1f);
            localLight.enabled = false;
        }

        //i WAS actually planning on making an upgrade system with terminal api but i'll leave that for next update since i don't know how to make mod saves yet
        /*private string onCommandParse()
        {
            string terminalNode = null;
            terminalNode += "the command isn't fully implemented, nor is the upgrade system, so here's some stats from your entire game session up to this point\n\n";
            terminalNode += "Flashlight toggle count: " + toggleAmount.ToString() + "\n";
            terminalNode += "Time spent using the light: " + String.Format("{0:.00}", flashOnTime) + " seconds\n";
            terminalNode += "Flashlight recharge method: " + selectedRechargeOption.ToString() + "\n";
            timesCommandUsed++;
            terminalNode += "Times you used this command: " + timesCommandUsed.ToString() + "\n";
            if (timesCommandUsed <= 1)
                terminalNode += "\nsadly these stats will get reset when starting up the game again but i'll try making it an actual mini-save file if it turns out to be a good feature to add in";

            if (Plugin.enableNetworking.Value)
                TerminalApi.TerminalApi.AddCommand("Localflashlight sayhi", new CommandInfo()
                {
                    DisplayTextSupplier = terminalSayHi,
                    Category = null
                });

            return terminalNode;
        }*/

        /*
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
        //will be included when all the terminal commands get added
        /*private string terminalSayHi()
        {
            LFNetworkHandler.Instance.SayHiServerRpc(player_controller.playerClientId);
            return "Said hello to all the people in the server!\n\nhow'd you find this anyway?\n";
        }*/
        #endregion
    }
}
