using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using LethalCompanyInputUtils.Api;

namespace localFlashlight
{
    public class ToggleButton : LcInputActions
    {
        [InputAction("<Keyboard>/f", Name = "Light Toggle")]
        public InputAction toggleKey { get; set; }
    }

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.2.2";
        public const string GUID = "command.localFlashlight";
        public const string NAME = "LocalFlashlight";

        //configs..........


        public static ConfigEntry<float> Intensity { get; private set; }
        public static ConfigEntry<float> Range { get; private set; }
        public static ConfigEntry<float> Angle { get; private set; }

        public static ConfigEntry<float> BatteryLife { get; private set; }
        public static ConfigEntry<float> RechargeMult { get; private set; }
        public static ConfigEntry<float> BurnOutCool { get; private set; }
        public static ConfigEntry<bool> BatteryBurnOut { get; private set; }
        public static ConfigEntry<float> BatteryCool { get; private set; }
        public static ConfigEntry<BatteryDisplayOptions> BatteryDisplay { get; private set; }
        public static ConfigEntry<TextDisplayOptions> TextDisplay { get; private set; }
        public static ConfigEntry<SoundOptions> soundOption { get; private set; }

        public static ConfigEntry<int> FlashColorRed { get; private set; }
        public static ConfigEntry<int> FlashColorGreen { get; private set; }
        public static ConfigEntry<int> FlashColorBlue { get; private set; }

        public static ConfigEntry<int> UIColorRed { get; private set; }
        public static ConfigEntry<int> UIColorGreen { get; private set; }
        public static ConfigEntry<int> UIColorBlue { get; private set; }

        public static ConfigEntry<bool> HideUI { get; private set; }
        public static ConfigEntry<float> HideUIDelay { get; private set; }

        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIHiddenAlpha { get; private set; }
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning { get; private set; }
        public static ConfigEntry<int> LowBatteryWarningPercentage { get; private set; }

        public static ConfigEntry<float> FlashVolume { get; private set; }

        public static ConfigEntry<float> UIPositionX { get; private set; }

        public static ConfigEntry<float> UIPositionY { get; private set; }

        public static ConfigEntry<bool> ShadowsEnabled { get; private set; }
        public static ConfigEntry<bool> flashlightToggleModSynergyquestionmark { get; private set; }

        public static AssetBundle bundle;

        public static ManualLogSource mls { get; private set; }

        internal static ToggleButton flashlightToggleInstance = new ToggleButton();

        private readonly Harmony har = new Harmony(GUID);
        private void Awake()
        {
            mls = Logger;

            mls.LogInfo("preparing mod");

            #region Configs
            Intensity = Config.Bind<float>("Flashlight", "Light Intensity", 350, new ConfigDescription("Intensity of the light", new AcceptableValueRange<float>(0, 5000)));
            Range = Config.Bind<float>("Flashlight", "Light Range", 17);
            Angle = Config.Bind<float>("Flashlight", "Light Angle", 55);

            BatteryLife = Config.Bind<float>("Battery", "Battery Life", 12f);
            RechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.8f, "The rate at which the flashlight battery recharges");
            BatteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Bar, "How the mod should display the battery details on the indicator");

            HideUI = Config.Bind<bool>("Indicator", "Hide Battery Indicator?", true, "Should the mod hide the battery indicator if the light is at full battery?");
            HideUIDelay = Config.Bind<float>("Indicator", "Battery Indicator Hide Delay", 1.5f);

            FlashColorRed = Config.Bind<int>("Flashlight Colors", "RedColorValue", 255, new ConfigDescription("Light red color value", new AcceptableValueRange<int>(0, 255)));
            FlashColorGreen = Config.Bind<int>("Flashlight Colors", "Green Color Value", 255, new ConfigDescription("Light green color value", new AcceptableValueRange<int>(0, 255)));
            FlashColorBlue = Config.Bind<int>("Flashlight Colors", "Blue Color Value", 255, new ConfigDescription("Light blue color value", new AcceptableValueRange<int>(0, 255)));

            UIColorRed = Config.Bind<int>("HUD Colors", "RedColorValue", 255, new ConfigDescription("HUD red color value", new AcceptableValueRange<int>(0, 255)));
            UIColorGreen = Config.Bind<int>("HUD Colors", "GreenColorValue", 255, new ConfigDescription("HUD green color value", new AcceptableValueRange<int>(0, 255)));
            UIColorBlue = Config.Bind<int>("HUD Colors", "BlueColorValue", 255, new ConfigDescription("HUD blue color value", new AcceptableValueRange<int>(0, 255)));

            UIScale = Config.Bind<float>("Indicator", "Indicator Scale", 1);
            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator Hidden Opacity", 0.2f, new ConfigDescription("The opacity of the indicator when it is hidden", new AcceptableValueRange<float>(0, 1)));
            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning Toggle", true, "When true, shows a warning on the HUD when the battery reaches a certain percentage");
            LowBatteryWarningPercentage = Config.Bind<int>("Indicator", "Low Battery Warning Percentage", 30, new ConfigDescription("The percentage at which the low battery warning shows up", new AcceptableValueRange<int>(0, 100)));
            TextDisplay = Config.Bind<TextDisplayOptions>("Indicator", "Text display", TextDisplayOptions.Percent);

            UIPositionX = Config.Bind<float>("Indicator", "IndicatorPositionX", 350, new ConfigDescription("The position of the UI on the X axis", new AcceptableValueRange<float>(-450, 450)));
            UIPositionY = Config.Bind<float>("Indicator", "IndicatorPositionY", -150, new ConfigDescription("The position of the UI on the Y axis", new AcceptableValueRange<float>(-280, 280)));

            FlashVolume = Config.Bind<float>("Other", "Volume", 0.5f, "Volume of all flashlight sounds");
            soundOption = Config.Bind<SoundOptions>("Other", "Sound Options", SoundOptions.Default, "different flashlight sounds (Light on, off, or out of battery)");

            BatteryCool = Config.Bind<float>("Battery", "Battery Recharge Cooldown", 1, "The cooldown before the battery starts recharging");
            BatteryBurnOut = Config.Bind<bool>("Battery", "Battery Burnout", true, "When true, if the flashlight turns off with no more battery, it goes on cooldown for a longer time");
            BurnOutCool = Config.Bind<float>("Battery", "Battery Recharge Cooldown (burnt out)", 3);

            ShadowsEnabled = Config.Bind<bool>("Other", "(EXPERIMENTAL) Enable shadows", true, "set this as an experimental feature because shadows are inconsistent, especially when they come from lights that are inside the player. this is an issue that the vanilla game also has with its helmet lights");
            flashlightToggleModSynergyquestionmark = Config.Bind<bool>("Other", "Prioritize flashlights in inventory", true, "Setting this to true will prevent the light turning on while you have a flashlight in your inventory");
            #endregion

            mls.LogInfo("set configs");

            //assetbundle usage
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("localFlashlight.bundle");
            bundle = AssetBundle.LoadFromStream(stream);
            if (bundle == null)
            {
                mls.LogWarning("failed to get assets, as assetbundle is null :(");
            }

            mls.LogInfo("loaded assetbundle");

            ///detetcting scene, and adding the gameobject
            SceneManager.sceneLoaded += OnPlayingSceneLoaded;

            ///hud patching
            har.PatchAll(typeof(HUDPatch));

            mls.LogInfo("applied HUD patches");
        }

        private void OnPlayingSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            //detect if in-game (MAY NOT WORK WITH LATECOMPANY IF IT IS SET TO ALLOW MID-ROUND JOINING, AS IT SKIPS THE SHIP SCENE!)
            if (scene.name == "SampleSceneRelay")
            {
                GameObject gameObject = new GameObject("LightController");
                gameObject.AddComponent<LightScript>();
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                mls.LogInfo("joined game, made lightcontroller");
            }

            //detect if in main menu
            if (scene.name == "MainMenu")
            {
                GameObject lightController = GameObject.Find("LightController");
                GameObject.Destroy(lightController);
                mls.LogInfo("in main menu, destroyed lightcontroller to prevent duplicates of it");
            }
        }
    }
}
