using GameNetcodeStuff;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LocalFlashlight
{
    internal class Patches : MonoBehaviour
    {
        #region Values
        static GameObject? UIContainer, frameObj, meterObj, textObj, warningObj;
        static TextMeshProUGUI? textmesh;
        static Image? frameImage, meterImage, warningImage;
        static Sprite? frame, meter;
        static readonly Sprite? warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        public static Color UIColorHex;
        static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        static BatteryDisplayOptions selectedStyle;
        static TextDisplayOptions selectedText;
        static RechargeOptions selectedRecharge;
        static float elemScale;
        public static bool isFlashlightHeld = false;
        public static bool isFlashlightPocketed = false;
        public static bool isFacilityPowered = true;
        public static float randomLightInterferenceMultiplier;
        static float targetAlpha;
        static float soundCd = 3;
        static float lastSoundTime = 0;
        #endregion

        #region Adding and removing the LightController via patches (a lot easier than with scenes, as i don't have to get the player controller in Update anymore)
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        internal static void MakeLightController()
        {
            //turns out! i did not need to change scripts :)
            ///i also changed light controller method so its found at the local player controller script to make dissecting the mod with unityexplorer a bit easier
            GameObject gameObject = new("LightController");
            gameObject.transform.SetParent(StartOfRound.Instance.localPlayerController.transform, false);
            gameObject.AddComponent<LightScript>();

            StartOfRound.Instance.localPlayerController.nightVision.intensity = StartOfRound.Instance.localPlayerController.nightVision.intensity * ((float)Plugin.DarkVisionMult.Value / 100);

            //also includes ClearLightB now
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
            ////Plugin.mls.LogDebug("player joined lobby, made lightcontroller and parented it to the local player controller");
        }

        //is this even needed if i have the light script attached to the player? i'm only keeping it in case something ever goes wrong, but i think it automatically gets removed from the local player controller on disconnect
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        internal static void DestroyLightController()
        {
            GameObject lightController = GameObject.Find("LightController");
            if (lightController != null)
                Destroy(lightController);
            ////Plugin.mls.LogDebug("player disconnected, removing light controller");
        }
        #endregion

        #region HUDManager patches
        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPostfix]
        internal static void GetBatteryUI(ref HUDManager __instance)
        {
            #region Value setting
            selectedStyle = Plugin.BatteryDisplay.Value;
            selectedText = Plugin.TextDisplay.Value;
            warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            warningPercent = Plugin.LowBatteryWarningPercentage.Value;
            elemScale = Plugin.UIScale.Value;
            selectedRecharge = Plugin.rechargeOption.Value;
            isFacilityPowered = true;
            ColorUtility.TryParseHtmlString(Plugin.HUDColorHex.Value, out UIColorHex);
            #endregion

            MakeIndicator(__instance);
        }

        //don't use playercontrollerb's update, worst mistake of my life!! lateupdate works fine i think
        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        internal static void UpdateBatteryInfo(ref HUDManager __instance)
        {
            if (UIContainer == null)
            {
                MakeIndicator(__instance);
                return;
            }

            try
            {
                float t0 = LightScript.batteryTime;
                float timeMinutes = Mathf.FloorToInt(t0 / 60);
                float timeSeconds = Mathf.FloorToInt(t0 % 60);

                //update some HUD configs in-game
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                elemScale = Plugin.UIScale.Value;
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
                UIContainer.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(UIContainer.GetComponent<CanvasGroup>().alpha, targetAlpha, Time.deltaTime * 6);

                if (selectedStyle == BatteryDisplayOptions.Text | selectedStyle == BatteryDisplayOptions.Bar | selectedStyle == BatteryDisplayOptions.VerticalBar | selectedStyle == BatteryDisplayOptions.CircularBar | selectedStyle == BatteryDisplayOptions.All)
                {
                    targetAlpha = Plugin.HideUI.Value ? (LightScript.UIHideTime < 0 ? Plugin.UIHiddenAlpha.Value : 1) : 1;

                    if (selectedStyle != BatteryDisplayOptions.Text) meterImage.fillAmount = LightScript.BatteryClamped;
                }

                //hides the hud when the UI is disabled
                if (selectedStyle == BatteryDisplayOptions.Disabled)
                {
                    UIContainer.SetActive(warningEnabled && LightScript.truePercentBattery <= warningPercent);
                }

                #region Color updates
                ColorUtility.TryParseHtmlString(Plugin.HUDColorHex.Value, out UIColorHex);
                if (frameObj != null && selectedStyle is BatteryDisplayOptions.Bar)
                {
                    frameImage.color = UIColorHex;
                }
                if (meterObj != null)
                {
                    meterImage.color = UIColorHex;
                }
                if (warningObj != null)
                {
                    warningImage.color = UIColorHex;
                    warningPercent = Plugin.LowBatteryWarningPercentage.Value;
                    warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
                }
                if (textObj != null)
                {
                    textmesh.color = UIColorHex;
                }
                #endregion

                #region Text updates
                if (selectedStyle == BatteryDisplayOptions.All | selectedStyle == BatteryDisplayOptions.Text)
                {
                    if (textmesh == null)
                        return;

                    switch (selectedText)
                    {
                        case TextDisplayOptions.Percent:
                            if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                            {
                                textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                            }
                            if (LightScript.batteryTime <= 0)
                            {
                                textmesh.text = "0%";
                            }
                            if (LightScript.batteryTime >= LightScript.maxBatteryTime) textmesh.text = "100%";
                            return;
                        case TextDisplayOptions.AccuratePercent:
                            if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                            {
                                textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%";
                            }
                            if (LightScript.batteryTime <= 0)
                            {
                                textmesh.text = "0.0%";
                            }
                            if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                            {
                                textmesh.text = "100.0%";
                            }
                            return;
                        case TextDisplayOptions.Time:
                            if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                            {
                                textmesh.text = $"{timeMinutes:0}:{timeSeconds:00}";
                            }
                            if (LightScript.batteryTime <= 0)
                            {
                                textmesh.text = "0:00";
                            }
                            if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                            {
                                textmesh.text = $"{Mathf.FloorToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                            }
                            return;
                        case TextDisplayOptions.All:
                            if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                            {
                                textmesh.text = LightScript.truePercentBattery.ToString("0.0") + "%" + $" | {timeMinutes:0}:{timeSeconds:00}";
                            }
                            if (LightScript.batteryTime <= 0)
                            {
                                textmesh.text = "0.0% | 0:00";

                            }
                            if (LightScript.batteryTime >= LightScript.maxBatteryTime)
                            {
                                textmesh.text = "100.0%" + $" | {Mathf.FloorToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                            }
                            return;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while updating hud!! is the light script there?? is the hud there????\n{e}");
                return;
            }
            #endregion
        }
        #endregion

        #region Other patches
        #region Apparatice recharge method code
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPostfix]
        internal static void ReDisableHUD()
        {
            if (selectedRecharge == RechargeOptions.FacilityPowered) UIContainer.SetActive(false);
            isFacilityPowered = true;

            if (StartOfRound.Instance.localPlayerController.isPlayerDead)
                StartOfRound.Instance.localPlayerController.pocketedFlashlight = null;

            if (Plugin.rechargeInOrbit.Value)
                LightScript.batteryTime = LightScript.maxBatteryTime;

            //Plugin.mls.LogDebug("ship left, redisabling hud and setting light battery back to infinite");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPostfix]
        internal static void FixValues()
        {
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
            randomLightInterferenceMultiplier = UnityEngine.Random.Range(0.1f, 0.4f);

            //Plugin.mls.LogDebug("fixing some values...");
        }
        #endregion

        #region "Prioritize in-game flashlights" config code
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        internal static void PocketedFlashlightChecks()
        {
            try
            {
                var player = StartOfRound.Instance.localPlayerController;

                if ((!player.IsOwner || !player.isPlayerControlled || player.IsServer && !player.isHostPlayerObject) && !player.isTestingPlayer)
                    return;

                if (!player.isPlayerDead)
                {
                    if (player.currentlyHeldObjectServer is FlashlightItem && player.currentlyHeldObjectServer != player.pocketedFlashlight)
                        player.pocketedFlashlight = player.currentlyHeldObjectServer;

                    if (player.pocketedFlashlight == null) return;

                    if (player.currentlyHeldObjectServer is FlashlightItem && player.isHoldingObject && !player.pocketedFlashlight.insertedBattery.empty)
                    {
                        isFlashlightHeld = true;
                    }
                    else isFlashlightHeld = false;

                    if (player.pocketedFlashlight is FlashlightItem && player.pocketedFlashlight.isHeld && !player.pocketedFlashlight.insertedBattery.empty)
                    {
                        isFlashlightPocketed = true;
                    }
                    else isFlashlightPocketed = false;

                }
                else
                {
                    player.pocketedFlashlight = null;
                    isFlashlightHeld = false;
                    isFlashlightPocketed = false;
                }
            }
            catch
            {
                return;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        internal static void ClearLightA(ref PlayerControllerB __instance)
        {
            ////Plugin.mls.LogDebug("local player died, made pocketedflashlight null");
            __instance.pocketedFlashlight = null;
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
        }
        #endregion

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOnClientRpc))]
        [HarmonyPostfix]
        static void TurnOnFlashlightPower()
        {
            ////Plugin.mls.LogDebug("facility power was switched on, hiding hud and giving flashlight infinite battery");
            isFacilityPowered = true;
            if (selectedRecharge == RechargeOptions.FacilityPowered)
                UIContainer.SetActive(!isFacilityPowered);
            lastSoundTime = Time.time;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void TurnOffFlashlightPower()
        {
            ////Plugin.mls.LogDebug("facility power was switched off, enabling hud, playing the lights off sound, flashlight is back to normal battery");
            isFacilityPowered = false;
            if (selectedRecharge == RechargeOptions.FacilityPowered)
            {
                UIContainer.SetActive(!isFacilityPowered);
                if (Time.time - lastSoundTime > soundCd) LightScript.PlayNoise(LightScript.activeClips[6], 0.2f, false);
            }
        }
        #endregion

        #region making HUD
        //why am i still not using prefabs for these anyway?
        static void MakeIndicator(HUDManager hudManager)
        {
            try
            {
                UIContainer = new GameObject("LocalFlashlightHUDElements", typeof(RectTransform), typeof(CanvasGroup));
                UIContainer.transform.SetParent(hudManager.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
                //Plugin.mls.LogDebug("made UI container");

                switch (selectedStyle)
                {
                    case BatteryDisplayOptions.Bar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = UIColorHex;
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(1.5f, 1.25f);
                        //Plugin.mls.LogDebug("made frame object (Bar)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Horizontal;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(1.5f, 1.25f);
                        //Plugin.mls.LogDebug("made meter object (Bar)");

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.Text:
                        textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                        RectTransform transform = textmesh.rectTransform;
                        transform.SetParent(UIContainer.transform, false);
                        textmesh.font = hudManager.controlTipLines[0].font;
                        textmesh.color = UIColorHex;
                        textmesh.fontSize = 23;
                        textmesh.overflowMode = TextOverflowModes.Overflow;
                        textmesh.enabled = true;
                        textmesh.text = "";
                        //Plugin.mls.LogDebug("made text object (Percentage)");
                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.All:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                        textObj = new GameObject("TextHUD", typeof(RectTransform), typeof(TextMeshProUGUI));
                        textObj.transform.SetParent(UIContainer.transform, false);
                        textObj.GetComponent<RectTransform>();
                        textmesh = textObj.GetComponent<TextMeshProUGUI>();
                        RectTransform textTransform = textmesh.rectTransform;
                        textTransform.SetParent(textObj.transform, false);
                        textTransform.localPosition = new Vector2(15, 0);
                        textmesh.font = hudManager.controlTipLines[0].font;
                        textmesh.color = UIColorHex;
                        textmesh.fontSize = 20;
                        textmesh.overflowMode = TextOverflowModes.Overflow;
                        textmesh.enabled = true;
                        textmesh.text = "";
                        //Plugin.mls.LogDebug("made text object (All)");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.5f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(2.625f, 0.875f);
                        //Plugin.mls.LogDebug("made frame object (All)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Horizontal;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
                        //Plugin.mls.LogDebug("made meter object (All)");
                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.CircularBar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter2.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter2.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.3f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(0.7f, 0.7f);
                        //Plugin.mls.LogDebug("made frame object (Circular)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Radial360;
                        meterImage.fillClockwise = false;
                        meterImage.fillOrigin = 2;
                        //Plugin.mls.LogDebug("made meter object (Circular)");

                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(0.7f, 0.7f);

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.VerticalBar:
                        frame = Plugin.bundle.LoadAsset<Sprite>("meter3.png");
                        meter = Plugin.bundle.LoadAsset<Sprite>("meter3.png");

                        frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                        frameObj.transform.SetParent(UIContainer.transform, false);
                        frameObj.GetComponent<RectTransform>();
                        frameImage = frameObj.GetComponent<Image>();
                        frameImage.sprite = frame;
                        frameImage.color = new Color(0, 0, 0, 0.5f);
                        frameObj.transform.localPosition = Vector2.zero;
                        frameObj.transform.localScale = new Vector2(1.25f, 3f);
                        //Plugin.mls.LogDebug("made frame object (Vertical)");

                        meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                        meterObj.transform.SetParent(UIContainer.transform, false);
                        meterObj.GetComponent<RectTransform>();
                        meterImage = meterObj.GetComponent<Image>();
                        meterImage.sprite = meter;
                        meterImage.color = UIColorHex;
                        meterImage.type = Image.Type.Filled;
                        meterImage.fillMethod = Image.FillMethod.Vertical;
                        meterObj.transform.localPosition = Vector2.zero;
                        meterObj.transform.localScale = new Vector2(1.25f, 3f);
                        //Plugin.mls.LogDebug("made meter object (Vertical)");

                        UIContainer.SetActive(!(selectedRecharge == RechargeOptions.FacilityPowered) || !isFacilityPowered);
                        frameObj.SetActive(true);
                        meterObj.SetActive(true);

                        //Plugin.mls.LogDebug("made hud elements");
                        return;

                    case BatteryDisplayOptions.Disabled:
                        if (warningEnabled)
                        {
                            warningObj = new GameObject("WarningHUD", typeof(RectTransform), typeof(Image));
                            warningObj.transform.SetParent(UIContainer.transform, false);
                            warningObj.GetComponent<RectTransform>();
                            warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                            warningImage = warningObj.GetComponent<Image>();
                            warningImage.sprite = warning;
                            warningImage.color = UIColorHex;
                            ////Plugin.mls.LogDebug("made warning object (Disabled)");
                        }
                        UIContainer.SetActive(false);

                        ////Plugin.mls.LogDebug("made hud elements");
                        return;
                }
                #endregion
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"error while making the mod's indicator:\n{e}");
            }
        }
    }
}
