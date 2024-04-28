using BepInEx;
using GameNetcodeStuff;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace localFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        #region Values
        //get player controller
        private PlayerControllerB player_controller;

        //the gameobjects used
        private GameObject player, cameraObject, lightObject;

        //audioclips and audiosource
        public static AudioClip toggleon;
        public static AudioClip toggleoff;
        public static AudioClip denytoggle;
        public static AudioClip nochargetoggle;
        public static AudioClip changepos;
        public AudioSource flashSource;

        //THE LIGHT.
        private Light locallight;

        //bool to check if light is positioned correctly so you avoid having a circle of holy light as your crosshair instead :)
        public static bool positioned = false;

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

        //ui hide delay
        public static float UIHideTime;

        //shadow toggle
        public static bool enabledShadows;

        private static bool canToggle = true;
        private static bool isHoldingFlashlight = false;
        private static bool isAFlashlightPocketed = false;

        private static SoundOptions selectedSoundOption;

        #endregion

        public void Update()
        {
            try
            {
                if (player == null)
                {

                    ///try to find local player gameobject, and if its found then proceed to the very important code
                    player = GameNetworkManager.Instance.localPlayerController.gameObject;

                    //getting the SFX audiomixergroup
                    var mixerGroup = GameNetworkManager.Instance.localPlayerController.itemAudio.GetComponent<AudioSource>().outputAudioMixerGroup;

                    Plugin.mls.LogInfo("Found player gameobject, setting values...");

                    //there's one frame where the mod tries to find the player gameobject, so that's the frame where every value gets set back to config values / default values
                    #region Setting values

                    positioned = false;

                    flashState = false;

                    isHoldingFlashlight = false;
                    canToggle = true;
                    isAFlashlightPocketed = false;

                    maxBatteryTime = Plugin.BatteryLife.Value;
                    batteryTime = maxBatteryTime;
                    burnOutCooldown = Plugin.BurnOutCool.Value;
                    batteryCooldown = Plugin.BatteryCool.Value;
                    batteryRegen = Plugin.RechargeMult.Value;

                    UIHideTime = 3f + Plugin.HideUIDelay.Value;

                    enabledShadows = Plugin.ShadowsEnabled.Value;

                    selectedSoundOption = Plugin.soundOption.Value;

                    //handles changing sounds depending on the soundoption
                    changepos = Plugin.bundle.LoadAsset<AudioClip>("changepos.ogg");
                    if(selectedSoundOption == SoundOptions.Default)
                    {
                        toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton");
                        toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lightoff");
                        denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                        nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle");
                    }

                    if(selectedSoundOption == SoundOptions.ActualFlashlight)
                    {
                        toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                        toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lighton1");
                        denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                        nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                    }
                    
                    if(selectedSoundOption == SoundOptions.InGameFlashlight)
                    {
                        toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                        toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lighton2");
                        denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
                        nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle1");
                    }

                    #endregion

                    if (player != null)
                    {
                        ///very important code, base of the entire flashlight

                        player_controller = player.GetComponent<PlayerControllerB>();

                        cameraObject = player.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera").gameObject;

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

                        //handles shadows, they're pretty buggy though

                        if (enabledShadows && lightObject != null)
                        {
                            var HDRPLight = lightObject.AddComponent<HDAdditionalLightData>();
                            HDRPLight.EnableShadows(true);
                        }
                        else Plugin.mls.LogInfo("shadows not enabled, skipped creating them");


                        //flashlight sounds (now added to the lightobject instead of a separate soundobject, also works with the master audiomixergroup now!)

                        flashSource = lightObject.AddComponent<AudioSource>();
                        flashSource.loop = false;
                        flashSource.playOnAwake = false;
                        flashSource.volume = Plugin.FlashVolume.Value;
                        flashSource.priority = 0;
                        flashSource.outputAudioMixerGroup = mixerGroup;

                        if (!positioned)
                        {
                            lightObject.transform.localPosition = new Vector3(0f, -0.55f, 0.5f);
                            lightObject.transform.Rotate(new Vector3(-10, 0, 0));
                            positioned = true;
                            Plugin.mls.LogInfo("finished positioning light");
                        }

                        locallight.enabled = false;
                        lightObject.SetActive(true);

                        Plugin.mls.LogInfo("light built, note that some values can't be updated in-game");
                    }
                }
            }
            //used for catching errors and logging them :)
            catch (Exception e)
            {
                Plugin.mls.LogError($"if you see this, please report it to me on github with a screenshot of the error shown thank you\n{e}");
                return;
            }

            //handles death
            if (player_controller.isPlayerDead)
            {
                flashState = false;
                locallight.enabled = false;
                batteryTime = maxBatteryTime;
                regenCool = 0;
                canToggle = true;
            }

            //handles the "prioritize in-game flashlights" config
            if (Plugin.flashlightToggleModSynergyquestionmark.Value)
            {
                if (player_controller.helmetLight.enabled)
                {
                    canToggle = false;
                }
                if (!player_controller.helmetLight.enabled)
                {
                    canToggle = true;
                }

                if (HUDPatch.isFlashlightPocketed)
                {
                    isAFlashlightPocketed = true;
                }
                if (!HUDPatch.isFlashlightPocketed)
                {
                    isAFlashlightPocketed = false;
                }

                if(!HUDPatch.isFlashlightHeld)
                { 
                    isHoldingFlashlight = false;
                }
                if (HUDPatch.isFlashlightHeld)
                {
                    isHoldingFlashlight = true;
                }
            }
            else
            {
                canToggle = true;
                isHoldingFlashlight = false;
                isAFlashlightPocketed = false;
            }

            #region Resetting values to update alongside config changes

            locallight.color = new Color((float)Plugin.FlashColorRed.Value / 255, (float)Plugin.FlashColorGreen.Value / 255, (float)Plugin.FlashColorBlue.Value / 255);

            locallight.range = Plugin.Range.Value;
            locallight.spotAngle = Plugin.Angle.Value;

            flashSource.volume = Plugin.FlashVolume.Value;

            #endregion

            #region Previously in LateUpdate, now in Update. Still used for battery management.

            ///handling battery regen
            if (!flashState)
            {
                if (batteryTime <= maxBatteryTime - 0.001)
                {
                    if (regenCool < 0) batteryTime += batteryRegen * Time.deltaTime;
                }

                if (batteryTime >= maxBatteryTime)
                {
                    batteryTime = maxBatteryTime;
                }
            }

            ///handles toggling the flashlight back off when it is under 0
            if (flashState)
            {
                batteryTime -= Time.deltaTime;
                if (batteryTime < 0)
                {
                    Toggle();
                }
            }

            //handles hiding UI when the flashlight is not in use

            if (BatteryPercent <= 99 | flashState)
            {
                UIHideTime = Plugin.HideUIDelay.Value;
            }
            if (BatteryPercent > 99 && !flashState)
            {
                UIHideTime -= Time.deltaTime;
            }

            //updating values

            publicFlashState = flashState;

            regenCool -= Time.deltaTime;

            BatteryPercent = (int)(Math.Ceiling(batteryTime / maxBatteryTime * 100));

            BatteryClamped = batteryTime / maxBatteryTime;

            truePercentBattery = BatteryClamped * 100;
            #endregion

            //calling toggle void, added some checks in case you do have the other experimental option enabled

            if (Plugin.flashlightToggleInstance.toggleKey.triggered && !(player_controller.quickMenuManager.isMenuOpen || player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
            {
                if (batteryTime > 0)
                {
                    if (flashState) Toggle();
                    else if (!flashState && canToggle && !(isHoldingFlashlight || isAFlashlightPocketed)) Toggle();
                    else
                    {
                        if (isHoldingFlashlight)
                        {
                            flashSource.PlayOneShot(denytoggle);
                        }
                        Plugin.mls.LogInfo("a flashlight is already held, and the config is enabled! not toggling on.");
                    }
                }
                else 
                {
                    flashSource.PlayOneShot(denytoggle);
                }
            }
        }

        //the toggle void!
        public void Toggle()
        {
            if (!(player == null || cameraObject == null))
            {
                flashState = !flashState;
                locallight.enabled = flashState;
                if (flashState)
                {
                    flashSource.PlayOneShot(toggleon);
                    Plugin.mls.LogInfo("toggled light on");
                }
                else
                {
                    if (batteryTime <= 0 && Plugin.BatteryBurnOut.Value)
                    {
                        regenCool = burnOutCooldown;
                        Plugin.mls.LogInfo("No battery, setting cooldown to the burnt out value");
                    }
                    else
                    {
                        regenCool = batteryCooldown;
                        Plugin.mls.LogInfo($"Battery at " + truePercentBattery.ToString("0.0") + "%" + ", setting cooldown to normal");
                    }
                    if (batteryTime <= 0) flashSource.PlayOneShot(nochargetoggle);
                    else flashSource.PlayOneShot(toggleoff);
                    Plugin.mls.LogInfo("toggled light off");
                }
            }
        }
    }
}
