using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace localFlashlight
{
    internal class HUDPatch
    {
        private static GameObject UIContainer, frameObj, meterObj, textObj, warningObj;
        private static TextMeshProUGUI textmesh;
        private static Image frameImage;
        private static Image meterImage;
        private static Image warningImage;
        private static Sprite frame;
        private static Sprite meter;
        private static Sprite warning = Plugin.bundle.LoadAsset<Sprite>("warning");
        private static float fadeAlpha = Plugin.UIHiddenAlpha.Value;
        private static bool fadeIn, fadeOut = false;
        private static bool warningEnabled = Plugin.UIDisabledLowBatteryWarning.Value;
        private static float warningPercent = Plugin.LowBatteryWarningPercentage.Value;
        private static Color setColor = new Color((float)Plugin.colorRed.Value / 255, (float)Plugin.colorGreen.Value / 255, (float)Plugin.colorBlue.Value / 255);

        [HarmonyPatch(typeof(HUDManager), "Start")]
        [HarmonyPostfix]
        public static void GetBatteryUI(ref HUDManager __instance)
        {

            if (Plugin.batteryDisplay.Value == BatteryDisplayOptions.Percentage)
            {
                UIContainer = new GameObject("Local Flashlight UI Elements");
                UIContainer.AddComponent<RectTransform>();
                UIContainer.transform.localPosition = new Vector2(450, -215);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(Plugin.UIScale.Value, Plugin.UIScale.Value, Plugin.UIScale.Value);

                textmesh = UIContainer.AddComponent<TextMeshProUGUI>();
                RectTransform transform = textmesh.rectTransform;
                transform.SetParent(__instance.HUDContainer.transform, false);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = setColor;
                textmesh.fontSize = 25;
                textmesh.overflowMode = TextOverflowModes.Overflow;
                textmesh.enabled = true;
                textmesh.text = "";
            }
            if(Plugin.batteryDisplay.Value == BatteryDisplayOptions.Bar)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("frame.png");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter.png");

                UIContainer = new GameObject("Local Flashlight UI Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.transform.localPosition = new Vector2(375, -255);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localScale = new Vector3(Plugin.UIScale.Value, Plugin.UIScale.Value, Plugin.UIScale.Value);

                frameObj = new GameObject("Frame Object");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = setColor;
                frameObj.transform.localPosition = new Vector2(-40, 55);
                frameObj.transform.localScale = new Vector2(1.5f, 1.25f);

                meterObj = new GameObject("Meter Object");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = new Vector2(-40, 55);
                meterObj.transform.localScale = new Vector2(1.5f, 1.25f);

                UIContainer.SetActive(true);
                frameObj.SetActive(true);
                meterObj.SetActive(true);
            }
            //i fixed them
            if(Plugin.batteryDisplay.Value == BatteryDisplayOptions.All)
            {
                frame = Plugin.bundle.LoadAsset<Sprite>("meter");
                meter = Plugin.bundle.LoadAsset<Sprite>("meter");

                UIContainer = new GameObject("Local Flashlight UI Elements");
                UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                UIContainer.AddComponent<CanvasGroup>();
                UIContainer.transform.localPosition = new Vector2(375, -255);
                UIContainer.transform.localScale = new Vector3(Plugin.UIScale.Value, Plugin.UIScale.Value, Plugin.UIScale.Value);

                textObj = new GameObject("Text Object");
                textObj.transform.SetParent(UIContainer.transform, false);
                textObj.AddComponent<RectTransform>();
                textObj.transform.localPosition = Vector2.zero;
                textmesh = textObj.AddComponent<TextMeshProUGUI>();
                RectTransform textTransform = textmesh.rectTransform;
                textTransform.SetParent(textObj.transform, false);
                textTransform.localPosition = new Vector2(-37, 45);
                textmesh.font = __instance.controlTipLines[0].font;
                textmesh.color = setColor;
                textmesh.fontSize = 20;
                textmesh.overflowMode = TextOverflowModes.Overflow;
                textmesh.enabled = true;
                textmesh.text = "";

                frameObj = new GameObject("Frame Object");
                frameObj.transform.SetParent(UIContainer.transform, false);
                frameObj.AddComponent<RectTransform>();
                frameImage = frameObj.AddComponent<Image>();
                frameImage.sprite = frame;
                frameImage.color = new Color(0, 0, 0, 0.5f);
                frameObj.transform.localPosition = new Vector2(-50, 45);
                frameObj.transform.localScale = new Vector2(2.625f, 0.875f);

                meterObj = new GameObject("Meter Object");
                meterObj.transform.SetParent(UIContainer.transform, false);
                meterObj.AddComponent<RectTransform>();
                meterImage = meterObj.AddComponent<Image>();
                meterImage.sprite = meter;
                meterImage.color = setColor;
                meterImage.type = Image.Type.Filled;
                meterImage.fillMethod = Image.FillMethod.Horizontal;
                meterObj.transform.localPosition = new Vector2(-50, 45);
                meterObj.transform.localScale = new Vector2(2.625f, 0.875f);
            }
            if (Plugin.batteryDisplay.Value == BatteryDisplayOptions.Disabled)
            {
                if (warningEnabled)
                {
                    UIContainer = new GameObject("Local Flashlight UI Elements");
                    UIContainer.AddComponent<RectTransform>();
                    UIContainer.transform.localPosition = new Vector2(350, -190);
                    UIContainer.transform.SetParent(__instance.HUDContainer.transform, false);
                    UIContainer.AddComponent<CanvasGroup>();
                    UIContainer.transform.localScale = new Vector3(Plugin.UIScale.Value, Plugin.UIScale.Value, Plugin.UIScale.Value);

                    warningObj = new GameObject("Warning Object");
                    warningObj.transform.SetParent(UIContainer.transform, false);
                    warningObj.AddComponent<RectTransform>();
                    warningObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    warningImage = warningObj.AddComponent<Image>();
                    warningImage.sprite = warning;
                    warningImage.color = setColor;

                    UIContainer.SetActive(false);
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        public static void UpdateBatteryText()
        {
            if (Plugin.batteryDisplay.Value == BatteryDisplayOptions.Percentage)
            {
                textmesh.text = LightScript.BatteryPercent.ToString() + "%";

                if (Plugin.hideUI.Value) 
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if(Plugin.batteryDisplay.Value == BatteryDisplayOptions.Bar)
            {
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if(Plugin.batteryDisplay.Value == BatteryDisplayOptions.All)
            {
                textmesh.text = LightScript.BatteryPercent.ToString() + "%";
                meterImage.fillAmount = LightScript.BatteryClamped;

                if (Plugin.hideUI.Value)
                {
                    if (!LightScript.publicFlashState)
                    {
                        if (LightScript.UIHideTime < 0)
                        {
                            HideUI();
                        }
                    }
                    if (LightScript.publicFlashState | (LightScript.UIHideTime > 0 || LightScript.BatteryPercent < 100)) ShowUI();
                }
            }
            if (Plugin.batteryDisplay.Value == BatteryDisplayOptions.Disabled)
            {
                if (warningEnabled)
                {
                    if (LightScript.BatteryPercent < warningPercent)
                    {
                        UIContainer.SetActive(true);
                    }
                    if (LightScript.BatteryPercent > warningPercent)
                    {
                        UIContainer.SetActive(false);
                    }
                }
            }

            //fade things
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
                if (UIContainer.GetComponent<CanvasGroup>().alpha > fadeAlpha)
                {
                    UIContainer.GetComponent<CanvasGroup>().alpha -= Time.deltaTime * 4;
                    if (UIContainer.GetComponent<CanvasGroup>().alpha <= fadeAlpha)
                    {
                        UIContainer.GetComponent<CanvasGroup>().alpha = fadeAlpha;
                        fadeOut = false;
                    }
                }
            }

        }

        //the actual fade voids
        private static void ShowUI()
        {
            fadeIn = true;
        }

        private static void HideUI()
        {
            fadeOut = true;
        }
    }
}
