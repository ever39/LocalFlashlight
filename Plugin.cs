using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace localFlashlight
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.1.0";
        public const string GUID = "command.localFlashlight";
        public const string NAME = "Local Flashlight";

        //configs...........
        public static ConfigEntry<KeyCode> toggleKey { get; private set; }
        public static ConfigEntry<float> intensity { get; private set; }
        public static ConfigEntry<float> range { get; private set; }
        public static ConfigEntry<float> angle { get; private set; }
        public static ConfigEntry<float> batteryLife { get; private set; }
        public static ConfigEntry<float> rechargeMult { get; private set; }
        public static ConfigEntry<BatteryDisplayOptions> batteryDisplay { get; private set; }
        public static ConfigEntry<bool> hideUI { get; private set; }
        public static ConfigEntry<float> hideUIDelay { get; private set; }
        public static ConfigEntry<byte> colorRed { get; private set; }
        public static ConfigEntry<byte> colorGreen { get; private set; }
        public static ConfigEntry<byte> colorBlue { get; private set; }
        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIHiddenAlpha { get; private set; }
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning { get; private set; }
        public static ConfigEntry<float> LowBatteryWarningPercentage { get; private set; }
        public static ConfigEntry<float> flashVolume { get; private set; }

        public static AssetBundle bundle;

        public static ManualLogSource mls { get; private set; }

        private readonly Harmony har = new Harmony("command.localFlashlight");
        private void Awake()
        {
            mls = Logger;
            mls.LogInfo("Patching stuff and adding the light.....");

            //configs
            toggleKey = Config.Bind<KeyCode>("Keybinds", "Toggle Key", KeyCode.F);
            intensity = Config.Bind<float>("Flashlight", "Light Intensity", 225);
            range = Config.Bind<float>("Flashlight", "Light Range", 17);
            angle = Config.Bind<float>("Flashlight", "Light Angle", 55, "How much the light lets you see");
            batteryLife = Config.Bind<float>("Battery", "Battery Life", 8f, "The battery of the flashlight, only used if you have the battery toggle on");
            rechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.75f, "The rate at which the flashlight battery recharges");
            batteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Percentage, "How the mod should display the battery details on the indicator");
            hideUI = Config.Bind<bool>("Indicator", "Hide Battery Indicator?", true, "Should the mod hide the indicator when the flashlight is not used?");
            hideUIDelay = Config.Bind<float>("Indicator", "Battery Indicator Hide Delay", 1.5f);
            colorRed = Config.Bind<byte>("Colors (Flashlight and Indicator)", "Red Color Value", 255);
            colorGreen = Config.Bind<byte>("Colors (Flashlight and Indicator)", "Green Color Value", 255);
            colorBlue = Config.Bind<byte>("Colors (Flashlight and Indicator)", "Blue Color Value", 255);
            UIScale = Config.Bind<float>("Indicator", "Indicator Scale", 1);
            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator Hidden Opacity", 0.2f, "The opacity of the indicator when the flashlight is not in use for a while");
            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "Should the mod display a low battery warning when the indicator is disabled?");
            LowBatteryWarningPercentage = Config.Bind<float>("Indicator", "Low Battery Warning Percentage", 30, "The percentage at which the low battery warning shows up");
            flashVolume = Config.Bind<float>("Other", "Volume", 0.5f, "Volume of all the flashlight sounds");

            //assetbundle usage
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("localFlashlight.bundle");
            bundle = AssetBundle.LoadFromStream(stream);
            if (bundle == null)
            {
                mls.LogWarning("failed to get assets, as assetbundle is null :(");
            }

            ///detetcting scene, and adding the gameobject
            SceneManager.sceneLoaded += OnPlayingSceneLoaded;

            //old scripts
            /*GameObject gameObject = new GameObject("LightController");
            gameObject.AddComponent<LightScript>();
            Object.DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;*/


            ///hud patching
            har.PatchAll(typeof(HUDPatch));

            mls.LogInfo("Patched! you'll see it work in game");
        }

        private void OnPlayingSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            //detect if in-game (MAY NOT WORK WITH LATECOMPANY IF IT IS SET TO ALLOW MID-ROUND JOINING, AS IT SKIPS THE SHIP SCENE!)
            if(scene.name == "SampleSceneRelay")
            {
                GameObject gameObject = new GameObject("LightController");
                gameObject.AddComponent<LightScript>();
                Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                mls.LogInfo("joined game, made lightcontroller!");
            }

            //detect if in main menu
            if(scene.name == "MainMenu")
            {
                GameObject lightController = GameObject.Find("LightController");
                GameObject.Destroy(lightController);
                mls.LogInfo("in main menu, destroyed lightcontroller!");
            }
        }
    }
}
