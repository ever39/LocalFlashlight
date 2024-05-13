using GameNetcodeStuff;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace localFlashlight
{
    internal class Patches
    {
        #region Values
        private static GameObject UIContainer, frameObj, meterObj, textObj, warningObj;
        private static TextMeshProUGUI textmesh;
        private static Image frameImage, meterImage, warningImage;
        private static Sprite frame, meter;
        private static readonly Sprite warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        private static bool fadeIn, fadeOut = false;
        private static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        private static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        private static BatteryDisplayOptions selectedStyle;
        private static TextDisplayOptions selectedText;
        private static RechargeOptions selectedRecharge;
        private static float elemScale;
        public static bool isFlashlightHeld = false;
        public static bool isFlashlightPocketed = false;
        public static bool isAppTaken = false;
        public static float randomLightInterferenceMultiplier;
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
            isAppTaken = false;
            #endregion

            try
            {
                #region Making HUD elements depending on selected style
                UIContainer = new GameObject("LocalFlashlightHUDElements", typeof(RectTransform), typeof(CanvasGroup));
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
                UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
                Plugin.mls.LogDebug("made UI container");

                if (selectedStyle == BatteryDisplayOptions.Percentage)
                {
                    textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                    RectTransform transform = textmesh.rectTransform;
                    transform.SetParent(UIContainer.transform, false);
                    textmesh.font = __instance.controlTipLines[0].font;
                    textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
                    textmesh.fontSize = 23;
                    textmesh.overflowMode = TextOverflowModes.Overflow;
                    textmesh.enabled = true;
                    textmesh.text = "";
                    Plugin.mls.LogDebug("made text object (Percentage)");
                }
                if (selectedStyle == BatteryDisplayOptions.Bar)
                {
                    frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                    meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                    frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                    frameObj.transform.SetParent(UIContainer.transform, false);
                    frameObj.GetComponent<RectTransform>();
                    frameImage = frameObj.GetComponent<Image>();
                    frameImage.sprite = frame;
                    frameImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    frameObj.transform.localPosition = Vector2.zero;
                    frameObj.transform.localScale = new Vector2(1.5f, 1.25f);
                    Plugin.mls.LogDebug("made frame object (Bar)");

                    meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                    meterObj.transform.SetParent(UIContainer.transform, false);
                    meterObj.GetComponent<RectTransform>();
                    meterImage = meterObj.GetComponent<Image>();
                    meterImage.sprite = meter;
                    meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    meterImage.type = Image.Type.Filled;
                    meterImage.fillMethod = Image.FillMethod.Horizontal;
                    meterObj.transform.localPosition = Vector2.zero;
                    meterObj.transform.localScale = new Vector2(1.5f, 1.25f);
                    Plugin.mls.LogDebug("made meter object (Bar)");

                    UIContainer.SetActive(true);
                    frameObj.SetActive(true);
                    meterObj.SetActive(true);
                }
                if (selectedStyle == BatteryDisplayOptions.All)
                {
                    frame = Plugin.bundle.LoadAsset<Sprite>("meter.png");
                    meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                    textObj = new GameObject("TextHUD", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textObj.transform.SetParent(UIContainer.transform, false);
                    textObj.GetComponent<RectTransform>();
                    textmesh = textObj.GetComponent<TextMeshProUGUI>();
                    RectTransform textTransform = textmesh.rectTransform;
                    textTransform.SetParent(textObj.transform, false);
                    textTransform.localPosition = new Vector2(15, 0);
                    textmesh.font = __instance.controlTipLines[0].font;
                    textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    textmesh.fontSize = 20;
                    textmesh.overflowMode = TextOverflowModes.Overflow;
                    textmesh.enabled = true;
                    textmesh.text = "";
                    Plugin.mls.LogDebug("made text object (All)");

                    frameObj = new GameObject("FrameHUD", typeof(RectTransform), typeof(Image));
                    frameObj.transform.SetParent(UIContainer.transform, false);
                    frameObj.GetComponent<RectTransform>();
                    frameImage = frameObj.GetComponent<Image>();
                    frameImage.sprite = frame;
                    frameImage.color = new Color(0, 0, 0, 0.5f);
                    frameObj.transform.localPosition = Vector2.zero;
                    frameObj.transform.localScale = new Vector2(2.625f, 0.875f);
                    Plugin.mls.LogDebug("made frame object (All)");

                    meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                    meterObj.transform.SetParent(UIContainer.transform, false);
                    meterObj.GetComponent<RectTransform>();
                    meterImage = meterObj.GetComponent<Image>();
                    meterImage.sprite = meter;
                    meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    meterImage.type = Image.Type.Filled;
                    meterImage.fillMethod = Image.FillMethod.Horizontal;
                    meterObj.transform.localPosition = Vector2.zero;
                    meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
                    Plugin.mls.LogDebug("made meter object (All)");
                }
                if (selectedStyle == BatteryDisplayOptions.CircularBar)
                {
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
                    Plugin.mls.LogDebug("made frame object (Circular)");

                    meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                    meterObj.transform.SetParent(UIContainer.transform, false);
                    meterObj.GetComponent<RectTransform>();
                    meterImage = meterObj.GetComponent<Image>();
                    meterImage.sprite = meter;
                    meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    meterImage.type = Image.Type.Filled;
                    meterImage.fillMethod = Image.FillMethod.Radial360;
                    meterImage.fillClockwise = false;
                    meterImage.fillOrigin = 2;
                    Plugin.mls.LogDebug("made meter object (Circular)");

                    meterObj.transform.localPosition = Vector2.zero;
                    meterObj.transform.localScale = new Vector2(0.7f, 0.7f);

                    UIContainer.SetActive(true);
                    frameObj.SetActive(true);
                    meterObj.SetActive(true);
                }
                if (selectedStyle == BatteryDisplayOptions.VerticalBar)
                {
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
                    Plugin.mls.LogDebug("made frame object (Vertical)");

                    meterObj = new GameObject("MeterHUD", typeof(RectTransform), typeof(Image));
                    meterObj.transform.SetParent(UIContainer.transform, false);
                    meterObj.GetComponent<RectTransform>();
                    meterImage = meterObj.GetComponent<Image>();
                    meterImage.sprite = meter;
                    meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255); ;
                    meterImage.type = Image.Type.Filled;
                    meterImage.fillMethod = Image.FillMethod.Vertical;
                    meterObj.transform.localPosition = Vector2.zero;
                    meterObj.transform.localScale = new Vector2(1.25f, 3f);
                    Plugin.mls.LogDebug("made meter object (Vertical)");

                    UIContainer.SetActive(true);
                    frameObj.SetActive(true);
                    meterObj.SetActive(true);
                }
                if (selectedStyle == BatteryDisplayOptions.Disabled)
                {
                    if (warningEnabled)
                    {
                        warningObj = new GameObject("WarningHUD", typeof(RectTransform), typeof(Image));
                        warningObj.transform.SetParent(UIContainer.transform, false);
                        warningObj.GetComponent<RectTransform>();
                        warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        warningImage = warningObj.GetComponent<Image>();
                        warningImage.sprite = warning;
                        warningImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
                        Plugin.mls.LogDebug("made warning object (Disabled)");

                        UIContainer.SetActive(false);
                    }
                }
                #endregion

                if (selectedRecharge == RechargeOptions.Apparatice) UIContainer.SetActive(false);

                Plugin.mls.LogDebug("made hud elements");
            }
            catch (Exception e)
            {
                Plugin.mls.LogError($"Caught exception while making HUD!\n{e}");
            }
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        internal static void UpdateBatteryInfo()
        {
            //added this so the hud no longer shows up if you're stuck in a loop of NullReferenceExceptions via foggy screen
            if (selectedRecharge != RechargeOptions.Apparatice) UIContainer.SetActive(LightScript.isLightLoaded);

            if (!LightScript.isLightLoaded) return;

            float t0 = LightScript.batteryTime;
            float timeMinutes = Mathf.FloorToInt(t0 / 60);
            float timeSeconds = Mathf.FloorToInt(t0 % 60);

            if (UIContainer == null)
                return;

            #region UI hiding
            if (selectedStyle == BatteryDisplayOptions.Bar | selectedStyle == BatteryDisplayOptions.VerticalBar | selectedStyle == BatteryDisplayOptions.CircularBar | selectedStyle == BatteryDisplayOptions.All)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;
            }
            #endregion

            ///The disabled UI code, which includes the warning
            if (selectedStyle == BatteryDisplayOptions.Disabled)
            {
                UIContainer.GetComponent<CanvasGroup>().alpha = 1;

                if (warningEnabled)
                {
                    if (LightScript.truePercentBattery < warningPercent)
                    {
                        UIContainer.SetActive(true);
                    }
                    if (LightScript.truePercentBattery > warningPercent)
                    {
                        UIContainer.SetActive(false);
                    }
                }
            }

            #region Color updates
            if (frameObj != null && selectedStyle is BatteryDisplayOptions.Bar)
            {
                frameImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }

            if (meterObj != null)
            {
                meterImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }

            if (warningObj != null)
            {
                warningImage.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
                warningPercent = Plugin.LowBatteryWarningPercentage.Value;
                warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
            }

            if (textObj != null)
            {
                textmesh.color = new Color((float)Plugin.UIColorRed.Value / 255, (float)Plugin.UIColorGreen.Value / 255, (float)Plugin.UIColorBlue.Value / 255);
            }
            #endregion

            #region Text updates
            if (selectedStyle == BatteryDisplayOptions.All | selectedStyle == BatteryDisplayOptions.Percentage)
            {
                if (textmesh == null)
                    return;

                if (selectedText == TextDisplayOptions.Percent)
                {
                    if (LightScript.batteryTime > 0 || LightScript.batteryTime < LightScript.maxBatteryTime)
                    {
                        textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                    }

                    if (LightScript.batteryTime <= 0)
                    {
                        textmesh.text = "0%";
                    }
                    if (LightScript.batteryTime >= LightScript.maxBatteryTime) textmesh.text = "100%";

                }
                if (selectedText == TextDisplayOptions.AccuratePercent)
                {
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
                }

                if (selectedText == TextDisplayOptions.Time)
                {
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
                        textmesh.text = $"{Mathf.RoundToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                    }
                }

                if (selectedText == TextDisplayOptions.All)
                {
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
                        textmesh.text = "100.0%" + $" | {Mathf.RoundToInt(LightScript.maxBatteryTime / 60):0}:{Mathf.RoundToInt(LightScript.maxBatteryTime % 60):00}";
                    }
                }
            }
            #endregion

            //hide ui when needed (if the style isn't the warning one)
            if (selectedStyle != BatteryDisplayOptions.Disabled)
            {
                if (Plugin.HideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | LightScript.UIHideTime > 0) ShowUI();
                }
            }

            //updates HUD configs in-game
            UIContainer.transform.localPosition = new Vector2(Plugin.UIPositionX.Value, Plugin.UIPositionY.Value);
            elemScale = Plugin.UIScale.Value;
            UIContainer.transform.localScale = new Vector3(elemScale, elemScale, elemScale);
            if (UIContainer.GetComponent<CanvasGroup>().alpha < Plugin.UIHiddenAlpha.Value) UIContainer.GetComponent<CanvasGroup>().alpha = Plugin.UIHiddenAlpha.Value;

            #region Fading in, fading out
            if (fadeIn)
            {
                if (UIContainer.GetComponent<CanvasGroup>().alpha < 1)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha += Time.deltaTime * 6;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha >= 1)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = 1;
                        fadeIn = false;
                    }
                }
            }

            if (fadeOut)
            {
                if (UIContainer.GetComponent<CanvasGroup>().alpha > Plugin.UIHiddenAlpha.Value)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 4;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha <= Plugin.UIHiddenAlpha.Value)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = Plugin.UIHiddenAlpha.Value;
                        fadeOut = false;
                    }
                }
            }
            #endregion
        }
        #endregion


        #region Other patches
        #region Apparatice recharge method code
        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.RadiationWarningHUD))]
        [HarmonyPostfix]
        internal static void DisableHUDOnAppDisconnect()
        {
            if (selectedRecharge == RechargeOptions.Apparatice)
            {
                UIContainer.SetActive(true);
                LightScript.PlayNoise(LightScript.flashDown, 0.8f);
            }
            isAppTaken = true;
            Plugin.mls.LogDebug("HUD enabled, light is at limited battery now");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPostfix]
        internal static void ReEnableHUD()
        {
            if (selectedRecharge == RechargeOptions.Apparatice) UIContainer.SetActive(false);
            isAppTaken = false;

            if (StartOfRound.Instance.localPlayerController.isPlayerDead)
                StartOfRound.Instance.localPlayerController.pocketedFlashlight = null;

            Plugin.mls.LogDebug("ship left, redisabling hud and setting light battery back to infinite");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPostfix]
        internal static void FixStuffOnRoundStart()
        {
            isAppTaken = false;
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
            randomLightInterferenceMultiplier = UnityEngine.Random.Range(0.1f, 0.4f);
        }
        #endregion

        #region "Prioritize in-game flashlights" config code
        [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
        [HarmonyPostfix]
        internal static void CheckIfHoldingLight(ref PlayerControllerB __instance)
        {
            try
            {
                if (__instance.IsOwner && __instance.isPlayerDead)
                {
                    __instance.pocketedFlashlight = null;
                    isFlashlightHeld = false;
                    isFlashlightPocketed = false;
                }

                if (__instance.IsOwner && __instance.isPlayerControlled && !__instance.isPlayerDead && !__instance.isTestingPlayer)
                {
                    if (__instance.currentlyHeldObjectServer is FlashlightItem && __instance.currentlyHeldObjectServer != __instance.pocketedFlashlight)
                    {
                        __instance.pocketedFlashlight = __instance.currentlyHeldObjectServer;
                    }

                    if (__instance.pocketedFlashlight == null) return;

                    if (__instance.currentlyHeldObjectServer is FlashlightItem && __instance.isHoldingObject && !__instance.pocketedFlashlight.insertedBattery.empty)
                    {
                        isFlashlightHeld = true;
                    }
                    else isFlashlightHeld = false;

                    if (__instance.pocketedFlashlight is FlashlightItem && __instance.pocketedFlashlight.isHeld && !__instance.pocketedFlashlight.insertedBattery.empty)
                    {
                        isFlashlightPocketed = true;
                    }
                    else isFlashlightPocketed = false;
                }
                else return;
            }
            catch
            {
                return;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        [HarmonyPostfix]
        internal static void ClearLight(ref PlayerControllerB __instance)
        {
            Plugin.mls.LogDebug("local player died, made pocketedflashlight null");
            __instance.pocketedFlashlight = null;
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        internal static void ClearLightOnConnect()
        {
            isFlashlightHeld = false;
            isFlashlightPocketed = false;
        }
        #endregion
        #endregion

        #region Fade voids
        internal static void ShowUI()
        {
            fadeIn = true;
        }

        internal static void HideUI()
        {
            fadeOut = true;
        }
        #endregion
    }
}
