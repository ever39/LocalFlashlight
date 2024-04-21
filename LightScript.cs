using BepInEx;
using GameNetcodeStuff;
using System;
using UnityEngine;

namespace localFlashlight
{
    internal class LightScript : MonoBehaviour
    {
        //get player controller
        private PlayerControllerB player_controller;

        //the gameobjects used
        private GameObject player, cameraObject, lightContainer, lightObject, soundObject;

        //audioclips and audiosource
        public static AudioClip toggleon = Plugin.bundle.LoadAsset<AudioClip>("lighton");
        public static AudioClip toggleoff = Plugin.bundle.LoadAsset<AudioClip>("lightoff");
        public static AudioClip denytoggle = Plugin.bundle.LoadAsset<AudioClip>("denytoggle");
        public static AudioClip nochargetoggle = Plugin.bundle.LoadAsset<AudioClip>("lowtoggle");
        public AudioSource flashSource;

        //THE LIGHT.
        private Light locallight;

        //inputs!
        private IInputSystem input;

        //bool to check if light is positioned correctly so you avoid having a circle of holy light as your crosshair instead :)
        public static bool positioned = false;

        //flashlight state
        private bool flashState;
        public static bool publicFlashState;

        //battery stuff
        public static float batteryTime = Plugin.batteryLife.Value;
        public static float maxBatteryTime = Plugin.batteryLife.Value;
        public static float BatteryPercent;
        public static float BatteryClamped;
        private float batteryRegen = Plugin.rechargeMult.Value;
        private float regenCool;
        public static float UIHideTime = Plugin.hideUIDelay.Value;
        private Color setColor = new Color((float)Plugin.colorRed.Value / 255, (float)Plugin.colorGreen.Value / 255, (float)Plugin.colorBlue.Value / 255);
        private float lightIntensity = Plugin.intensity.Value;
        private float lightRange = Plugin.range.Value;
        private float lightSpotAngle = Plugin.angle.Value;
        private float flashVolume = Plugin.flashVolume.Value;

        //only used to set the inputs
        public void Awake()
        {
            input = UnityInput.Current;
        }

        public void Update()
        {
            try
            {
                if (player == null)
                {
                    ///try to find local player gameobject, and if its found then proceed to the very important code
                    player = GameNetworkManager.Instance.localPlayerController.gameObject;
                    positioned = false;
                    batteryTime = maxBatteryTime;
                    flashState = false;
                    UIHideTime = 3f + Plugin.hideUIDelay.Value;

                    if (player != null)
                    {
                        ///very important code, base of the entire flashlight

                        player_controller = player.GetComponent<PlayerControllerB>();

                        cameraObject = player.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera").gameObject;

                        lightContainer = new GameObject();
                        lightContainer.transform.SetParent(cameraObject.transform, false);

                        lightObject = new GameObject();
                        soundObject = new GameObject();

                        lightObject.transform.SetParent(lightContainer.transform, false);
                        soundObject.transform.SetParent(lightContainer.transform, false);

                        lightContainer.name = "Light Container";
                        lightObject.name = "Local Light Object";
                        soundObject.name = "Local Sound Object";

                        locallight = lightObject.AddComponent<Light>();
                        locallight.type = LightType.Spot;
                        locallight.shape = LightShape.Cone;
                        locallight.color = setColor;
                        locallight.intensity = lightIntensity;
                        locallight.range = lightRange;
                        locallight.spotAngle = lightSpotAngle;

                        flashSource = soundObject.AddComponent<AudioSource>();
                        flashSource.loop = false;
                        flashSource.playOnAwake = false;
                        flashSource.volume = flashVolume;
                        flashSource.priority = 0;

                        if (!positioned)
                        {
                            lightObject.transform.position = cameraObject.transform.position - new Vector3(0, 0.6f, 0);
                            lightObject.transform.Rotate(new Vector3(-10, 0, 0));
                            positioned = true;
                        }

                        lightObject.SetActive(false);
                        soundObject.SetActive(true);

                        publicFlashState = flashState;
                    }
                }
            }
            //(nevermind its used now) catching errors :)
            catch (Exception e)
            {
                Plugin.mls.LogError($"if you see this, please report it to me on github with a screenshot of the error shown thank you\n{e}");
                return;
            }

            if (input.GetKeyDown(Plugin.toggleKey.Value) && !(player_controller.isPlayerDead || player_controller.inTerminalMenu || player_controller.isTypingChat))
            {
                Plugin.mls.LogInfo("attempted toggle!");
                if (batteryTime > 0)
                {
                    Toggle();
                }
                else flashSource.PlayOneShot(denytoggle);
            }
        }

        //late update, used in its entirety for battery updates
        public void LateUpdate()
        {
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

            if (flashState)
            {
                batteryTime -= Time.deltaTime;
                if (batteryTime < 0)
                {
                    Toggle();
                }
            }


            if (BatteryPercent != 100)
            {
                UIHideTime = Plugin.hideUIDelay.Value;
            }
            if (BatteryPercent == 100)
            {
                UIHideTime -= Time.deltaTime;
            }

            regenCool -= Time.deltaTime;

            BatteryPercent = (int)(Math.Ceiling(batteryTime / maxBatteryTime * 100));

            BatteryClamped = batteryTime / maxBatteryTime;
        }

        //the toggle void!
        public void Toggle()
        {
            if (!(player == null || cameraObject == null))
            {
                flashState = !flashState;
                lightObject.SetActive(flashState);
                if (flashState)
                {
                    flashSource.PlayOneShot(toggleon);
                    Plugin.mls.LogInfo("toggled light on!");
                }
                else
                {
                    Plugin.mls.LogInfo("toggled light off!");
                    regenCool = 1;
                    if (batteryTime <= 0) flashSource.PlayOneShot(nochargetoggle);
                    else flashSource.PlayOneShot(toggleoff);
                }
            }
        }
    }
}
