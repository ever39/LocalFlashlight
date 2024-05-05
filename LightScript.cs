using GameNetcodeStuff;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace localFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        #region Values
        //get player controller
        private PlayerControllerB player_controller;

        //the gameobjects used
        private GameObject player, cameraObject;
        private static GameObject lightObject;

        //audioclips and audiosource
        private AudioClip toggleon, toggleoff, denytoggle, nochargetoggle, changepos, reloadLight, useDynamo;
        public static AudioClip fullCharge;
        public static AudioClip flashDown;
        private static AudioSource flashSource;

        //THE LIGHT.
        private Light locallight;

        //flashlight state
        private bool flashState;
        public static bool publicFlashState;

        //battery configs
        public static float maxBatteryTime;
        public static float batteryTime = maxBatteryTime;

        private float batteryRegen;
        private float regenCool;
        private float batteryCooldown;
        private float burnOutCooldown;

        //battery values
        public static float BatteryPercent;
        public static float truePercentBattery;
        public static float BatteryClamped;

        //other wacky values
        private float shakeCool;
        private float lastShakeTime = 0;
        private bool rechargeKeyHeld;
        private float soundCool;
        public static bool isLightLoaded = false;
        private bool hasShownError = false;

        //ui hide delay
        public static float UIHideTime;

        //shadow toggle
        private bool enabledShadows;

        //flashlight checks
        private bool canToggle = true;

        //selected sound, selected recharge, maybe selected HUD too sooner or later lmao
        private SoundOptions selectedSoundOption;
        private RechargeOptions selectedRechargeOption;

        //for dynamo flashlight
        private float playerMovementSpeed;
        #endregion

        private void Start()
        {
            Plugin.mls.LogInfo("LocalFlashlight mod script started!");
            isLightLoaded = false;
            hasShownError = false;
        }

        #region Update void
        public void Update()
        {
            #region Making gameobjects.
            try
            {
                if (player == null)
                {
                    //try to find local player gameobject, and if its found then proceed to the base flashlight code
                    player = GameNetworkManager.Instance.localPlayerController.gameObject;

                    if (player != null)
                    {
                        player_controller = player.GetComponent<PlayerControllerB>();

                        cameraObject = player_controller.gameplayCamera.gameObject;

                        //getting the SFX audiomixergroup
                        var mixerGroup = GameNetworkManager.Instance.localPlayerController.itemAudio.GetComponent<AudioSource>().outputAudioMixerGroup;

                        Plugin.mls.LogDebug("found player gameobject, setting values and sounds");

                        SetFlashlightSounds();
                        SetModValues();

                        lightObject = new GameObject();
                        lightObject.transform.SetParent(cameraObject.transform, false);
                        lightObject.name = "lightObject";

                        locallight = lightObject.AddComponent<Light>();
                        locallight.type = LightType.Spot;
                        locallight.shape = LightShape.Cone;
                        locallight.color = new Color((float)Plugin.FlashColorRed.Value / 255, (float)Plugin.FlashColorGreen.Value / 255, (float)Plugin.FlashColorBlue.Value / 255);
                        locallight.intensity = Plugin.Intensity.Value;
                        locallight.range = Plugin.Range.Value;
                        locallight.shadows = LightShadows.Hard;
                        locallight.spotAngle = Plugin.Angle.Value;

                        //flashlight sounds
                        flashSource = lightObject.AddComponent<AudioSource>();
                        flashSource.loop = false;
                        flashSource.playOnAwake = false;
                        flashSource.volume = Plugin.FlashVolume.Value;
                        flashSource.priority = 0;
                        flashSource.spatialize = true;
                        flashSource.outputAudioMixerGroup = mixerGroup;
                        flashSource.spatialize = true;

                        lightObject.transform.localPosition = new Vector3(-0.15f, -0.55f, 0.5f);
                        lightObject.transform.Rotate(new Vector3(-12, 3, 0));
                        Plugin.mls.LogDebug("finished positioning light");

                        //handles shadows, they're pretty buggy though
                        if (enabledShadows && lightObject != null)
                        {
                            var HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                            HDRPLight.EnableShadows(true);
                        }
                        else Plugin.mls.LogDebug("shadows not enabled, skipped creating them");

                        locallight.enabled = false;
                        lightObject.SetActive(true);

                        Plugin.mls.LogInfo("light up and working! right from the company");
                        isLightLoaded = true;
                    }
                }
            }
            //used for catching errors and logging them :)
            catch (Exception e)
            {
                if (!hasShownError)
                {
                    Plugin.mls.LogError($"big horrible error popped up in the mod, so i'm going to make the text as big as possible so it's eyecatching as it only shows up once\n as far as i'm concerned, this only ever pops up if you have the \"foggy screen\"\n alternatively, there could be some other mods changing how the local player works, which may lead to this mod not working at all\n HOWEVER if it isn't from another mod, then do report the issue on the mod's github page (alongside a screenshot and probably the modlist you're using)\n thanks, and sorry for the big error text\n{e}");
                    hasShownError = true;
                }
                return;
            }
            #endregion

            if (player == null | player_controller == null | cameraObject == null | lightObject == null)
                return;
            try
            {
                #region "Prioritize in-game flashlights" config
                if (Plugin.flashlightToggleModSynergyquestionmark.Value)
                {
                    if (player_controller.helmetLight.enabled | Patches.isFlashlightPocketed | Patches.isFlashlightHeld)
                    {
                        canToggle = false;
                    }
                    else canToggle = true;
                }
                else
                {
                    canToggle = true;
                }
                #endregion

                #region Battery management.
                ///handling battery regen (TIME RECHARGE CONFIG)
                if (!flashState)
                {
                    if (selectedRechargeOption == RechargeOptions.Time)
                    {
                        if (batteryTime <= maxBatteryTime - 0.001)
                        {
                            if (regenCool < 0) batteryTime += batteryRegen * Time.deltaTime;
                        }
                    }
                }

                ///this works the same
                if (batteryTime > maxBatteryTime)
                {
                    batteryTime = maxBatteryTime;
                    if (selectedRechargeOption == RechargeOptions.Time) PlayNoise(fullCharge, .9f);
                }

                ///handles consuming battery and turning the light off when under 0%
                if (flashState)
                {
                    batteryTime -= Time.deltaTime;

                    if (batteryTime < 0)
                    {
                        Toggle();
                    }
                }

                ///handles battery regen (SHAKE RECHARGE CONFIG)
                if (selectedRechargeOption == RechargeOptions.Shake)
                {
                    if (Plugin.flashlightToggleInstance.rechargeKey.triggered && Time.time - lastShakeTime > shakeCool && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
                    {
                        if (player_controller.sprintMeter > 0.25f)
                        {
                            batteryTime += (maxBatteryTime * 0.07f) * batteryRegen;
                            PlayNoise(reloadLight, .5f);
                            player_controller.sprintMeter -= ((float)Plugin.shakeStaminaConsume.Value) / 100;
                            lastShakeTime = Time.time;
                        }
                        else PlayNoise(denytoggle, .7f);
                    }
                }

                ///handles battery regen (DYNAMO RECHARGE CONFIG)
                if (selectedRechargeOption == RechargeOptions.Dynamo)
                {
                    if (!(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
                    {
                        Plugin.flashlightToggleInstance.rechargeKey.performed += holdCallback;
                        Plugin.flashlightToggleInstance.rechargeKey.canceled += releaseCallback;
                    }
                    else
                    {
                        Plugin.flashlightToggleInstance.rechargeKey.performed -= holdCallback;
                        Plugin.flashlightToggleInstance.rechargeKey.canceled -= releaseCallback;
                    }

                    if (rechargeKeyHeld)
                    {
                        WindUpFlashlight();
                    }
                    else
                    {
                        player_controller.movementSpeed = playerMovementSpeed;
                    }
                }
                else
                {
                    Plugin.flashlightToggleInstance.rechargeKey.performed -= holdCallback;
                    Plugin.flashlightToggleInstance.rechargeKey.canceled -= releaseCallback;
                }

                ///handles THE ENTIRE battery system, at the cost of the flashlight's precious light intensity!! (APPARATUS RECHARGE CONFIG)
                if (selectedRechargeOption == RechargeOptions.Apparatice)
                {
                    locallight.intensity = Plugin.Intensity.Value * Plugin.apparaticeFlashlightIntensityMult.Value;

                    if (!Patches.isAppTaken)
                    {
                        batteryTime = maxBatteryTime;
                    }
                    else if (Patches.isAppTaken)
                    {
                        if (flashState)
                        {
                            batteryTime -= Time.deltaTime * 0.7f;
                        }
                    }
                }

                if (selectedRechargeOption == RechargeOptions.OnShipEnter)
                {
                    if (player_controller.isInHangarShipRoom)
                    {
                        batteryTime = maxBatteryTime;
                    }
                }
                #endregion

                //handles hiding UI when the flashlight is not in use
                if (BatteryPercent <= 99.5 | flashState)
                {
                    UIHideTime = Plugin.HideUIDelay.Value;
                }
                if (BatteryPercent > 99.5 && !flashState)
                {
                    UIHideTime -= Time.deltaTime;
                }

                UpdateModValues();

                #region Toggling and changing light position
                //calling toggle void, added some checks in case you do have the flashlight toggle "synergy" config enabled
                if (Plugin.flashlightToggleInstance.toggleKey.triggered && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
                {
                    if (batteryTime > 0)
                    {
                        if (flashState) Toggle();
                        else if (!flashState && canToggle) Toggle();
                        else
                        {
                            if (Patches.isFlashlightHeld)
                            {
                                PlayNoise(denytoggle, 0.7f);
                            }
                            Plugin.mls.LogDebug("a flashlight is already in the inventory, and the config is enabled! not toggling on.");
                        }
                    }
                    else
                    {
                        PlayNoise(denytoggle, 0.7f);
                    }
                }

                //handles changing flash position
                if (Plugin.flashlightToggleInstance.switchLightPosKey.triggered && flashState && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
                {
                    if (player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat)
                        return;

                    ChangeLightPosition();
                }
                #endregion
            }
            catch(Exception e)
            {
                Plugin.mls.LogError($"this is an actual bad error, please report it on github with a screenshot and the modlist you're using, thanks\n{e}");
                return;
            }
        }
        #endregion

        #region Other methods
        private void Toggle()
        {
            if (!(player == null || cameraObject == null))
            {
                flashState = !flashState;
                locallight.enabled = flashState;
                if (flashState)
                {
                    PlayNoise(toggleon, 0.6f);
                    Plugin.mls.LogDebug("toggled light on");
                }
                else
                {
                    if (batteryTime <= 0 && Plugin.BatteryBurnOut.Value)
                    {
                        regenCool = burnOutCooldown;
                        Plugin.mls.LogDebug("No battery, setting cooldown to the burnt out value");
                    }
                    else
                    {
                        regenCool = batteryCooldown;
                        Plugin.mls.LogDebug($"Battery at " + truePercentBattery.ToString("0.0") + "%" + ", setting cooldown to normal");
                    }
                    if (batteryTime <= 0) PlayNoise(nochargetoggle, 1);
                    else PlayNoise(toggleoff, 0.6f);
                    Plugin.mls.LogDebug("toggled light off");
                }
            }
        }

        private void ChangeLightPosition()
        {
            var lightPosX = lightObject.transform.localPosition.x;
            var lightRotY = lightObject.transform.localRotation.y;
            lightObject.transform.localPosition = new Vector3(-lightPosX, -0.55f, 0.5f);
            lightObject.transform.localRotation = Quaternion.Euler(-10, -lightRotY, 0);
            PlayNoise(changepos, 0.3f);
            Plugin.mls.LogDebug("changed light position");
        }

        public static void PlayNoise(AudioClip audioClip, float volume)
        {
            flashSource.PlayOneShot(audioClip);
            if (Plugin.soundAggros.Value) FindObjectOfType<RoundManager>().PlayAudibleNoise(lightObject.transform.position, 8, volume, 0, false, 0);
            Plugin.mls.LogDebug("played sound");
        }

        private void holdCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;
            if (player_controller.quickMenuManager.isMenuOpen | player_controller.isPlayerDead | player_controller.inTerminalMenu | player_controller.isTypingChat) return;

            flashSource.loop = true;
            flashSource.clip = useDynamo;
            flashSource.Play();
            rechargeKeyHeld = true;
            Plugin.mls.LogDebug("input press performed, starting dynamo use key hold");
        }
        private void releaseCallback(InputAction.CallbackContext context)
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            flashSource.loop = false;
            flashSource.clip = null;
            flashSource.Stop();
            soundCool = 0.25f;
            rechargeKeyHeld = false;
            Plugin.mls.LogDebug("input cancel performed, stopping dynamo use key hold");
        }


        private void SetFlashlightSounds()
        {
            selectedSoundOption = Plugin.soundOption.Value;
            changepos = Plugin.bundle.LoadAsset<AudioClip>("changepos.ogg");

            if (selectedSoundOption == SoundOptions.OriginalLightSounds)
            {
                toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton");
                toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lightoff");
                denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle");
                fullCharge = Plugin.bundle.LoadAsset<AudioClip>("recharged");

                reloadLight = Plugin.bundle.LoadAsset<AudioClip>("reloadlight");
                useDynamo = Plugin.bundle.LoadAsset<AudioClip>("dynamo1");
                flashDown = Plugin.bundle.LoadAsset<AudioClip>("flashDown1");
            }

            if (selectedSoundOption == SoundOptions.OtherwordlyLight)
            {
                toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                fullCharge = Plugin.bundle.LoadAsset<AudioClip>("recharged2");

                reloadLight = Plugin.bundle.LoadAsset<AudioClip>("reloadlight");
                useDynamo = Plugin.bundle.LoadAsset<AudioClip>("dynamo2");
                flashDown = Plugin.bundle.LoadAsset<AudioClip>("flashDown");
            }

            if (selectedSoundOption == SoundOptions.InGameFlashlight)
            {
                toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                fullCharge = Plugin.bundle.LoadAsset<AudioClip>("recharged1");

                reloadLight = Plugin.bundle.LoadAsset<AudioClip>("reloadlight");
                useDynamo = Plugin.bundle.LoadAsset<AudioClip>("dynamo");
                flashDown = Plugin.bundle.LoadAsset<AudioClip>("flashDown2");
            }

            Plugin.mls.LogDebug("mod sounds set");
        }

        private void SetModValues()
        {
            flashState = false;
            canToggle = true;
            rechargeKeyHeld = false;

            maxBatteryTime = Plugin.BatteryLife.Value;
            batteryTime = maxBatteryTime;
            batteryRegen = Plugin.RechargeMult.Value;

            burnOutCooldown = Plugin.BurnOutCool.Value;
            batteryCooldown = Plugin.BatteryCool.Value;

            UIHideTime = 2f + Plugin.HideUIDelay.Value;

            enabledShadows = Plugin.ShadowsEnabled.Value;
            selectedRechargeOption = Plugin.rechargeOption.Value;

            playerMovementSpeed = player_controller.movementSpeed;

            Plugin.mls.LogDebug("mod values set");
        }

        private void UpdateModValues()
        {
            publicFlashState = flashState;
            regenCool -= Time.deltaTime;

            BatteryPercent = (int)(Math.Ceiling(batteryTime / maxBatteryTime * 100));
            BatteryClamped = batteryTime / maxBatteryTime;
            truePercentBattery = BatteryClamped * 100;

            locallight.color = new Color((float)Plugin.FlashColorRed.Value / 255, (float)Plugin.FlashColorGreen.Value / 255, (float)Plugin.FlashColorBlue.Value / 255);
            locallight.range = Plugin.Range.Value;
            locallight.spotAngle = Plugin.Angle.Value;
            flashSource.volume = Plugin.FlashVolume.Value;

            shakeCool = Plugin.shakeActionCooldown.Value;

            //handles what to do when the player is dead
            if (player_controller.isPlayerDead)
            {
                flashState = false;
                locallight.enabled = false;
                batteryTime = maxBatteryTime;
                regenCool = 0;
                canToggle = true;
                rechargeKeyHeld = false;
            }
        }

        private void WindUpFlashlight()
        {
            if (selectedRechargeOption != RechargeOptions.Dynamo) return;

            batteryTime += (Time.deltaTime * 1.25f) * batteryRegen;
            player_controller.movementSpeed = playerMovementSpeed * Plugin.dynamoUseMoveMult.Value;
            player_controller.sprintMeter -= Time.deltaTime * 0.05f;
            if (soundCool < 0)
            {
                FindObjectOfType<RoundManager>().PlayAudibleNoise(lightObject.transform.position, 8, 0.5f, 0, player_controller.isInHangarShipRoom, 0);
                soundCool = 0.3f;
            }
            soundCool -= Time.deltaTime;
        }
        #endregion
    }
}
