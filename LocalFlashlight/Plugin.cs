using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalCompanyInputUtils.Api;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LocalFlashlight.Networking;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LocalFlashlight
{
    #region Keybinds and inputs
    public class ToggleButton : LcInputActions
    {
        [InputAction("<Keyboard>/f", Name = "Toggle key")]
        public InputAction toggleKey { get; set; }

        [InputAction("<Keyboard>/h", Name = "Position switch key")]
        public InputAction switchLightPosKey { get; set; }

        [InputAction("<Keyboard>/q", Name = "Recharge key")]
        public InputAction rechargeKey { get; set; }
    }
    #endregion

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("atomic.terminalapi", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("FlipMods.ReservedItemSlotCore", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("LCSoundTool", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.5.1";
        public const string GUID = "command.localFlashlight";
        public const string NAME = "LocalFlashlight";

        #region Config list
        public static ConfigEntry<float> lightIntensity { get; private set; }

        public static ConfigEntry<float> lightRange { get; private set; }
        public static ConfigEntry<float> lightAngle { get; private set; }

        public static ConfigEntry<float> BatteryLife { get; private set; }
        public static ConfigEntry<float> RechargeMult { get; private set; }
        public static ConfigEntry<float> BurnOutCool { get; private set; }
        public static ConfigEntry<bool> BatteryBurnOut { get; private set; }
        public static ConfigEntry<float> BatteryCool { get; private set; }

        public static ConfigEntry<BatteryDisplayOptions> BatteryDisplay { get; private set; }
        public static ConfigEntry<TextDisplayOptions> TextDisplay { get; private set; }
        public static ConfigEntry<SoundOptions> soundOption { get; private set; }
        public static ConfigEntry<RechargeOptions> rechargeOption { get; private set; }

        public static ConfigEntry<bool> HideUI { get; private set; }
        public static ConfigEntry<float> HideUIDelay { get; private set; }

        public static ConfigEntry<float> UIScale { get; private set; }
        public static ConfigEntry<float> UIHiddenAlpha { get; private set; }
        public static ConfigEntry<bool> UIDisabledLowBatteryWarning { get; private set; }
        public static ConfigEntry<int> LowBatteryWarningPercentage { get; private set; }

        public static ConfigEntry<int> FlashVolume { get; private set; }

        public static ConfigEntry<float> UIPositionX { get; private set; }

        public static ConfigEntry<float> UIPositionY { get; private set; }

        public static ConfigEntry<bool> ShadowsEnabled { get; private set; }
        public static ConfigEntry<bool> soundAggros { get; private set; }
        public static ConfigEntry<bool> flashlightToggleModSynergyquestionmark { get; private set; } //WHAT IS THIS CONFIG NAME??
        public static ConfigEntry<int> shakeStaminaConsume { get; private set; }
        public static ConfigEntry<float> shakeActionCooldown { get; private set; }
        public static ConfigEntry<float> apparaticeFlashlightIntensityMult { get; private set; }
        public static ConfigEntry<float> dynamoUseMoveMult { get; private set; }
        public static ConfigEntry<bool> dimEnabled { get; private set; }
        public static ConfigEntry<int> flashlightStopDimBatteryValue { get; private set; }
        public static ConfigEntry<bool> flickerOnBatteryBurn { get; private set; }
        public static ConfigEntry<bool> enableNetworking { get; private set; }
        public static ConfigEntry<int> networkedPlayersVol { get; private set; }
        public static ConfigEntry<string> flashlightColorHex { get; private set; }
        public static ConfigEntry<string> HUDColorHex { get; private set; }
        public static ConfigEntry<int> DarkVisionMult { get; private set; }
        public static ConfigEntry<bool> rechargeInOrbit { get; private set; }
        public static ConfigEntry<bool> soundOnRecharge { get; private set; }
        public static ConfigEntry<bool> rechargeInShip { get; private set; }

        public static ConfigEntry<float> lightPosX1 { get; private set; }
        public static ConfigEntry<float> lightPosY1 { get; private set; }
        public static ConfigEntry<float> lightPosZ1 { get; private set; }

        public static ConfigEntry<float> lightRotX1 { get; private set; }
        public static ConfigEntry<float> lightRotY1 { get; private set; }
        public static ConfigEntry<float> lightRotZ1 { get; private set; }

        public static ConfigEntry<float> lightPosX2 { get; private set; }
        public static ConfigEntry<float> lightPosY2 { get; private set; }
        public static ConfigEntry<float> lightPosZ2 { get; private set; }

        public static ConfigEntry<float> lightRotX2 { get; private set; }
        public static ConfigEntry<float> lightRotY2 { get; private set; }
        public static ConfigEntry<float> lightRotZ2 { get; private set; }
        #endregion

        #region Other values
        public static AssetBundle bundle;
        public static ManualLogSource mls { get; private set; }
        internal static ToggleButton flashlightToggleInstance = new();
        private readonly Harmony har = new(GUID);

        static string sceneName;
        #endregion

        #region Awake void
        private void Awake()
        {
            mls = Logger;

            #region Setting configs
            lightIntensity = Config.Bind<float>("Flashlight", "Light intensity", 350, new ConfigDescription("The intensity of the light in lumens", new AcceptableValueRange<float>(1, 5000)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightIntensity, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 1,
                Max = 5000
            }));

            lightRange = Config.Bind<float>("Flashlight", "Light range", 17, new ConfigDescription("The range of the light in units"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRange, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 1,
                Max = float.PositiveInfinity
            }));

            lightAngle = Config.Bind<float>("Flashlight", "Light angle", 55, new ConfigDescription("The size of the light's area", new AcceptableValueRange<float>(1, 120)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightAngle, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 1,
                Max = 120
            }));

            BatteryLife = Config.Bind<float>("Battery", "Battery life", 12f, new ConfigDescription("The battery life of the flashlight in seconds"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BatteryLife, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 1f,
                Max = float.PositiveInfinity
            }));

            RechargeMult = Config.Bind<float>("Battery", "Recharge Multiplier", 0.8f, new ConfigDescription("The flashlight's battery recharge multiplier (For instance, setting it to 1 will make it recharge at about the same rate that it is depleted when using the Time recharge method, however this config also applies to other recharge methods)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(RechargeMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            BatteryDisplay = Config.Bind<BatteryDisplayOptions>("Indicator", "Battery Details", BatteryDisplayOptions.Bar, "How the indicator displays the flashlight's remaining battery time");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<BatteryDisplayOptions>(BatteryDisplay, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
            }));

            TextDisplay = Config.Bind<TextDisplayOptions>("Indicator", "Battery text display", TextDisplayOptions.Percent, "(Only applies to \"Text\" and \"All\" indicator display options) Wherether the mod should display the battery information text in percents, accurate percents or time left");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<TextDisplayOptions>(TextDisplay, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            HideUI = Config.Bind<bool>("Indicator", "Indicator fading", true, "Fades out the battery indicator when the flashlight is inactive for a while");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(HideUI, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            HideUIDelay = Config.Bind<float>("Indicator", "Battery indicator fade delay", 2f, new ConfigDescription("The delay before fading out the indicator in seconds"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(HideUIDelay, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            UIHiddenAlpha = Config.Bind<float>("Indicator", "Indicator faded out opacity", 0.2f, new ConfigDescription("The opacity of the indicator when the indicator is faded out", new AcceptableValueRange<float>(0f, 1f)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(UIHiddenAlpha, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 1
            }));

            UIScale = Config.Bind<float>("Indicator", "Indicator scale", 1, new ConfigDescription("Battery indicator scale", new AcceptableValueRange<float>(0.01f, 1f)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(UIScale, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0.01f,
                Max = float.PositiveInfinity
            }));

            UIPositionX = Config.Bind<float>("Indicator", "IndicatorPositionX", 350, new ConfigDescription("The position of the battery indicator on the X axis", new AcceptableValueRange<float>(-450f, 450f)));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(UIPositionX, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = -450,
                Max = 450
            }));

            UIPositionY = Config.Bind<float>("Indicator", "IndicatorPositionY", -150, new ConfigDescription("The position of the battery indicator on the Y axis", new AcceptableValueRange<float>(-280f, 280f)));
            LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(UIPositionY, new FloatSliderOptions
            {
                RequiresRestart = false,
                Min = -280,
                Max = 280
            }));

            UIDisabledLowBatteryWarning = Config.Bind<bool>("Indicator", "Low Battery Warning toggle", true, "(Only applies when the indicator is disabled) Shows a warning on the HUD when the battery charge reaches a certain percentage");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(UIDisabledLowBatteryWarning, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
            }));

            LowBatteryWarningPercentage = Config.Bind("Indicator", "Low Battery Warning %", 30, new ConfigDescription("(Only applies when the indicator is disabled) The percentage at which the low battery warning shows up", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(LowBatteryWarningPercentage, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            flashlightColorHex = Config.Bind<string>("Colors", "Flashlight Color (Hex)", "#FFFFFF", new ConfigDescription("very important, add the # before the hex! if it breaks or doesn't work, try resetting the config and typing the code in manually"));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(flashlightColorHex, new TextInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig
            }));

            HUDColorHex = Config.Bind<string>("Colors", "Indicator Color (Hex)", "#FFFFFF", new ConfigDescription("very important, add the # before the hex! if it breaks or doesn't work, try resetting the config and typing the code in manually"));
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(HUDColorHex, new TextInputFieldOptions
            {
                RequiresRestart = false
            }));

            flashlightToggleModSynergyquestionmark = Config.Bind<bool>("Other", "Prioritize flashlights in the inventory", true, "Prevents the localflashlight from turning on while there already is a flashlight in your inventory (doesn't take into account modded flashlights)");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(flashlightToggleModSynergyquestionmark, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            FlashVolume = Config.Bind<int>("Other", "Flashlight volume", 50, new ConfigDescription("The volume of the flashlight's sounds. Does not apply to how well others can hear it.", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(FlashVolume, new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            soundOption = Config.Bind<SoundOptions>("Other", "Sound options", SoundOptions.InGameFlashlight, "Switch between default sounds and custom sounds, with only the former being heard by other players when networking is enabled. To apply custom sounds you need to have LCSoundTool installed and enabled, as well as having the custom sounds in the customsounds folder of this mod (refer to readme if you don't know how to do that).");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<SoundOptions>(soundOption, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            ShadowsEnabled = Config.Bind<bool>("Other", "Enable shadows", true, "The light also emits shadows when enabled. Good for when modded items are too large and block the light.");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(ShadowsEnabled, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            rechargeOption = Config.Bind<RechargeOptions>("Other", "Recharge method", RechargeOptions.Time, "How the flashlight is recharged. Different options result in different playstyles and battery management.");
            LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<RechargeOptions>(rechargeOption, new EnumDropDownOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            rechargeInOrbit = Config.Bind("Other", "Fully recharge in orbit", true, "When set to true, fully recharges the flashlight when reaching orbit (when the round ends). Good for slow recharge times or longer battery times.");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(rechargeInOrbit, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            rechargeInShip = Config.Bind("Other", "Recharge in ship", true, "When set to true, recharges the flashlight at a fast rate while in the ship when the flashlight is turned off. Good for longer battery times or slow recharge multipliers.");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(rechargeInShip, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            }));

            DarkVisionMult = Config.Bind("Other", "Ambient light intensity multiplier", 100, new ConfigDescription("Determines the multiplier of the indoor ambient light. Useful if you want to use the flashlight more.", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(DarkVisionMult, new IntSliderOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 0,
                Max = 100
            }));

            flickerOnBatteryBurn = Config.Bind<bool>("Other", "Enable light flickering", true, "When true, the local flashlight replicates the in-game flashlights' \"flickers\", and also flickers when the battery runs out");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(flickerOnBatteryBurn, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                //CanModifyCallback = modifyConfig modifying it in-game works just fine, no need to prevent config modifications when they clearly update
            }));

            dimEnabled = Config.Bind<bool>("Other", "Enable light dimming", false, "The intensity of the flashlight dims down to a certain point");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(dimEnabled, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                //CanModifyCallback = modifyConfig same thing
            }));

            flashlightStopDimBatteryValue = Config.Bind<int>("Other", "Minimum battery amount dim", 15, new ConfigDescription("The battery charge percentage at which the light stops dimming", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(flashlightStopDimBatteryValue, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));


            //POSITIONS AND ROTATIONS 1

            lightPosX1 = Config.Bind("Light Positions and Rotations", "Light Position X 1", -0.15f, new ConfigDescription("Light's position on the X axis (default value: -0.15)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosX1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightPosY1 = Config.Bind("Light Positions and Rotations", "Light Position Y 1", -0.55f, new ConfigDescription("Light's position on the Y axis (default value: -0.55)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosY1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightPosZ1 = Config.Bind("Light Positions and Rotations", "Light Position Z 1", 0.5f, new ConfigDescription("Light's position on the Z axis (default value: 0.5)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosZ1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightRotX1 = Config.Bind("Light Positions and Rotations", "Light Rotation X 1", -10f, new ConfigDescription("Light's rotation on the X axis (default value: -10)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotX1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            lightRotY1 = Config.Bind("Light Positions and Rotations", "Light Rotation Y 1", 3f, new ConfigDescription("Light's rotation on the Y axis (default value: 3)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotY1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            lightRotZ1 = Config.Bind("Light Positions and Rotations", "Light Rotation Z 1", 0f, new ConfigDescription("Light's rotation on the Z axis (default value: 0)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotZ1, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            //POSITIONS AND ROTATIONS 2

            lightPosX2 = Config.Bind("Light Positions and Rotations", "Light Position X 2", 0.15f, new ConfigDescription("Light's position on the X axis (default value: 0.15)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosX2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightPosY2 = Config.Bind("Light Positions and Rotations", "Light Position Y 2", -0.55f, new ConfigDescription("Light's position on the Y axis (default value: -0.55)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosY2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightPosZ2 = Config.Bind("Light Positions and Rotations", "Light Position Z 2", 0.5f, new ConfigDescription("Light's position on the Z axis (default value: 0)", new AcceptableValueRange<float>(-5, 5)));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightPosZ2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = -5,
                Max = 5
            }));

            lightRotX2 = Config.Bind("Light Positions and Rotations", "Light Rotation X 2", -10f, new ConfigDescription("Light's rotation on the X axis (default value: -10)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotX2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            lightRotY2 = Config.Bind("Light Positions and Rotations", "Light Rotation Y 2", -3f, new ConfigDescription("Light's rotation on the Y axis (default value: -3)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotY2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            lightRotZ2 = Config.Bind("Light Positions and Rotations", "Light Rotation Z 2", 0f, new ConfigDescription("Light's rotation on the Z axis (default value: 0)"));
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(lightRotZ2, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = float.NegativeInfinity,
                Max = float.PositiveInfinity
            }));

            BatteryCool = Config.Bind<float>("Time battery recharge configs", "Battery recharge cooldown", 1, "The cooldown after turning the flashlight off for the battery to start recharging");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BatteryCool, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            BatteryBurnOut = Config.Bind<bool>("Time battery recharge configs", "Battery burnout", true, "When the flashlight is turned off because of no battery charge remaining, the cooldown before starting to recharge lasts longer than usual if this option is enabled");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(BatteryBurnOut, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig
            }));

            BurnOutCool = Config.Bind<float>("Time battery recharge configs", "Battery recharge cooldown (no battery)", 3, "The cooldown before the battery starts recharging when burnt out");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(BurnOutCool, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            soundOnRecharge = Config.Bind<bool>("Time battery recharge configs", "Full battery recharge sound toggle", true, "Makes a noise when the battery fully recharges (heard by enemies if they're in range, and other players if networking is also enabled)");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(soundOnRecharge, new BoolCheckBoxOptions
            {
                RequiresRestart = false,
            }));

            shakeActionCooldown = Config.Bind<float>("Shake battery recharge configs", "Shake cooldown", 0.2f, "The time needed to wait between \"shaking\" the flashlight again");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(shakeActionCooldown, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifyConfig,
                Min = 0,
                Max = float.PositiveInfinity
            }));

            shakeStaminaConsume = Config.Bind<int>("Shake battery recharge configs", "Consumed stamina amount", 2, new ConfigDescription("The amount of stamina that shaking the flashlight consumes"));
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(shakeStaminaConsume, new IntInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));

            dynamoUseMoveMult = Config.Bind<float>("Dynamo battery recharge configs", "Recharge move speed multiplier", 0.75f, "Player movement speed multiplier while recharging the flashlight");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(dynamoUseMoveMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 1
            }));

            apparaticeFlashlightIntensityMult = Config.Bind<float>("Facility powered battery recharge configs", "Light intensity multiplier", 0.65f, "Light intensity multiplier when using the Facility Powered recharge option");
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(apparaticeFlashlightIntensityMult, new FloatInputFieldOptions
            {
                RequiresRestart = false,
                CanModifyCallback = modifySpecialConfig,
                Min = 0,
                Max = 1
            }));

            enableNetworking = Config.Bind("Networking", "(EXPERIMENTAL) Enable networking", false, "Enables networking. While this is enabled, you cannot join other lobbies if the host doesn't have this mod enabled and its networking option also enabled. However, you can see other players' lights (alongside the intensity, range, spot angle and color they set) and hear their flashlight sounds (unless they're custom sounds). Can only be changed from the config file directly or from a mod manager");
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(enableNetworking, new BoolCheckBoxOptions
            {
                RequiresRestart = true,
                CanModifyCallback = denyNetworkConfigModify
            }));

            networkedPlayersVol = Config.Bind<int>("Networking", "Other player flashlight volume", 75, new ConfigDescription("The volume of other players' flashlights.", new AcceptableValueRange<int>(0, 100)));
            LethalConfigManager.AddConfigItem(new IntSliderConfigItem(networkedPlayersVol, new IntSliderOptions
            {
                RequiresRestart = false,
                Min = 0,
                Max = 100
            }));
            #endregion

            // Loading asset bundles from the mod folder (instead of loading the embedded asset bundle, as it doesn't waste memory this way)
            var modPath = System.IO.Path.GetDirectoryName(Info.Location);
            var bundlePath = System.IO.Path.Combine(modPath, "localbundle");
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                mls.LogError("failed to get mod assets!");

            //normally there would be a scene check in here, however i changed it so the light script gets loaded when the player controller is linked to the client to prevent useless errors
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            //now the base of the entire mod, since it adds the script to the player object
            har.PatchAll(typeof(Patches));

            //networking!! very buggy :)
            if (enableNetworking.Value)
            {
                mls.LogWarning("NETWORKING ENABLED!!!! buggy, and won't let you join other servers because of how it's set up, so be wary");
                har.PatchAll(typeof(NetworkingPatches));
                NetcodePatcher();
            }
            else
            {
                mls.LogWarning("networking disabled");
            }

            //running thru compatibilities
            RunThroughCompatibilities();
        }
        #endregion

        #region Methods used for locking configs
        ///scenes now used for locking configs when in-game or FOREVER (while in-game)
        private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            sceneName = currentScene.name;
        }

        private static CanModifyResult modifyConfig()
        {
            return (sceneName == "MainMenu", "This setting cannot be changed mid-game.");
        }

        private static CanModifyResult denyNetworkConfigModify()
        {
            return (false, "Enabling or disabling networking cannot be done while the game is open.\nEdit it from your mod manager of choice instead.");
        }

        private static CanModifyResult modifySpecialConfig()
        {
            if (Plugin.enableNetworking.Value)
            {
                return (sceneName == "MainMenu", "Setting cannot be changed in a lobby when networking is enabled.");
            }
            else return (true);
        }
        #endregion

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        public static bool hasReservedSlots = false;
        public static bool hasLCSoundTool = false;
        private void RunThroughCompatibilities()
        {
            if (Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                var compatibilityRegister = AccessTools.Method("LobbyCompatibility.Features.PluginHelper:RegisterPlugin");
                Plugin.mls.LogInfo($"Found LobbyCompatibility, trying to register plugin depending on configs... (Version {VERSION})");

                try
                {
                    if (enableNetworking.Value)
                    {
                        compatibilityRegister.Invoke(null, new object[] { GUID, new Version(VERSION), 2, 1 });
                    }
                    else
                    {
                        compatibilityRegister.Invoke(null, new object[] { GUID, new Version(VERSION), 0, 0 });
                    }
                }
                catch (Exception)
                {
                    mls.LogError("Couldn't register mod compatibility.");
                }
            }
            if (Chainloader.PluginInfos.ContainsKey("FlipMods.ReservedItemSlotCore"))
            {
                Plugin.mls.LogDebug("found reserved item slots. this only affects that one config that i've been trying to fix with the past updates, hopefully it fixes all the issues with the reserved flashlight slots");
                hasReservedSlots = true;
            }
            if (Chainloader.PluginInfos.ContainsKey("LCSoundTool"))
            {
                Plugin.mls.LogDebug("found LCSoundTool! custom sounds, yippee.");
                hasLCSoundTool = true;
            }
        }
    }
}
