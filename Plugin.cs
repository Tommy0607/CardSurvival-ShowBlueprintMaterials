using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MyPlugin
{
    [BepInPlugin(
        "xyz.tommy0607.csti.show_blueprint_materials",
        "ShowBlueprintMaterials",
        "1.0.0"
    )]
    public class Plugin : BaseUnityPlugin
    {

        private static ManualLogSource logger;
        private static GameObject showElementsIcon;
        private const string TOOLTIP_TITLE = "剩余材料";
        
        private void Start()
        {
            Logger.LogInfo("ShowBlueprintMaterials Start");
            logger = Logger;
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MainMenu), "Update")]
        public static void MainMenu_Update(MainMenu __instance)
        {
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                logger.LogInfo("You pressed " + KeyCode.Alpha9);
                __instance.MenuNavigation.SetGroupActive(1);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BlueprintConstructionPopup), "SetupInventory")]
        public static void BlueprintConstructionPopup_SetupInventory(BlueprintConstructionPopup __instance, InGameCardBase _Card)
        {
            var cardStages = _Card.CardModel.BlueprintStages;
            var currentStageIndex = _Card.BlueprintData.CurrentStage;
            if (currentStageIndex < 0 || currentStageIndex >= cardStages.Length)
            {
                return;
            }
            
            var elementSizeDict = new Dictionary<string, int>();
            for (int i = currentStageIndex; i < cardStages.Length; i++)
            {
                foreach (var blueprintElement in cardStages[i].RequiredElements)
                {
                    var elementName = blueprintElement.GetName;
                    var elementCount = blueprintElement.GetQuantity;
                    if (!elementSizeDict.ContainsKey(elementName))
                    {
                        elementSizeDict[elementName] = elementCount;
                    }
                    else
                    {
                        elementSizeDict[elementName] += elementCount;
                    }
                }
            }
            var content = "";
            foreach (var nameSize in elementSizeDict)
            {
                content += nameSize.Value + "x" + nameSize.Key + "\n";
            }
            content = content.Substring(0, content.Length - 1);
            if (showElementsIcon == null)
            {
                GameObject gameObject = new GameObject();
                gameObject.name = "Show Elements Icon";
                var assembly = Assembly.GetExecutingAssembly();
                Texture2D texture = new Texture2D(0, 0);
                var stream = assembly.GetManifestResourceStream("MyPlugin.Info.png");
                texture.LoadImage(ReadBytesFromStream(stream));
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
                Image image = gameObject.AddComponent<Image>();
                image.sprite = sprite;
                var tooltip = gameObject.AddComponent<TooltipProvider>();
                tooltip.SetTooltip(TOOLTIP_TITLE, content, "");
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.pivot = Vector2.one;
                rectTransform.sizeDelta = new Vector2(64, 64);
                rectTransform.SetParent(__instance.transform);
                rectTransform.localScale = new Vector3(1, 1, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(64, -62);
                gameObject.SetActive(true);
                showElementsIcon = gameObject;
            }
            else
            {
                showElementsIcon.GetComponent<TooltipProvider>().SetTooltip(TOOLTIP_TITLE, content, "");
            }

        }

        public static byte[] ReadBytesFromStream(Stream input)
        {
            byte[] buffer = new byte[16*1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }
}
