using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nova
{
    public class ChoiceButtonController : MonoBehaviour
    {
        private const float MaxIdleTime = 0.5f;

        public Text text;
        public Image image;
        public Button button;

        private Dictionary<SystemLanguage, string> displayTexts;
        private ChoiceImageInformation imageInfo;
        private string imageFolder;

        private bool allowClick;
        private float idleTime;

        public void Init(Dictionary<SystemLanguage, string> displayTexts, ChoiceImageInformation imageInfo,
            string imageFolder, UnityAction onClick, bool interactable, bool wasChosen = false)
        {
            this.displayTexts = displayTexts.ToDictionary(x => x.Key, x => DialogueEntry.InterpolateText(x.Value));
            this.imageInfo = imageInfo;
            this.imageFolder = imageFolder;

            if (imageInfo != null)
            {
                var layoutElement = gameObject.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                image.type = Image.Type.Simple;

                transform.localPosition = new Vector3(imageInfo.positionX, imageInfo.positionY, 0f);
                transform.localScale = new Vector3(imageInfo.scale, imageInfo.scale, 1f);
            }

            // "已选过"视觉：把 Button.SpriteState.selectedSprite 当作"已选"基础贴图。
            // 保留 SpriteSwap transition：hover 仍换 highlightedSprite，鼠标移开会自动回到 image.sprite（即"已选"贴图）。
            if (wasChosen && imageInfo == null)
            {
                var chosenSprite = button.spriteState.selectedSprite;
                if (chosenSprite != null)
                {
                    image.sprite = chosenSprite;
                }
            }

            UpdateText();

            var fx = GetComponent<ChoiceButtonFX>();
            button.onClick.AddListener(() =>
            {
                if (CursorManager.UsingKeyboard || allowClick)
                {
                    if (fx != null)
                    {
                        fx.PlayClick();
                        StartCoroutine(InvokeAfterFX(onClick, 0.08f));
                    }
                    else
                    {
                        onClick?.Invoke();
                    }
                }

                idleTime = 0f;
            });

            button.interactable = interactable;
            allowClick = false;
            idleTime = 0f;
        }

        private void UpdateText()
        {
            if (displayTexts == null)
            {
                text.text = "";
            }
            else
            {
                text.text = I18n.__(displayTexts);
            }

            if (imageInfo != null)
            {
                // TODO: preload
                image.sprite = AssetLoader.Load<Sprite>(System.IO.Path.Combine(imageFolder, imageInfo.name));
                image.SetNativeSize();
                image.alphaHitTestMinimumThreshold = 0.5f;
            }
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }

        private void Update()
        {
            if (CursorManager.MovedLastFrame)
            {
                allowClick = true;
            }

            if (!allowClick)
            {
                idleTime += Time.deltaTime;
                if (idleTime > MaxIdleTime)
                {
                    allowClick = true;
                }
            }
        }

        private static IEnumerator InvokeAfterFX(UnityAction onClick, float delay)
        {
            yield return new WaitForSeconds(delay);
            onClick?.Invoke();
        }
    }
}
