using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// 选项按钮的 shader 特效驱动：入场扫描 / Hover 细扫描线 / 点击 glitch + 闪边。
    /// 配合 Assets/Nova/UI/Shaders/ChoiceButtonFX.shader 使用。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ChoiceButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ── 可调参数 ──
        [Header("入场扫描")]
        public float revealDuration = 0.18f;

        [Header("Hover")]
        public float hoverFadeIn = 0.12f;
        public float hoverFadeOut = 0.18f;

        [Header("点击反馈")]
        public float clickDuration = 0.08f;

        // ── 内部 ──
        private static readonly int RevealID = Shader.PropertyToID("_RevealProgress");
        private static readonly int HoverID  = Shader.PropertyToID("_HoverIntensity");
        private static readonly int GlitchID = Shader.PropertyToID("_GlitchAmount");
        private static readonly int EdgeID   = Shader.PropertyToID("_EdgeFlash");

        private Image image;
        private Text textComponent;
        private Material matInstance;
        private Coroutine revealCo;
        private Coroutine hoverCo;
        private Coroutine clickCo;

        private void Awake()
        {
            image = GetComponent<Image>();
            textComponent = GetComponentInChildren<Text>(true);

            if (image.material != null && image.material.shader != null
                && image.material.shader.name == "UI/ChoiceButtonFX")
            {
                // 复制一份独立实例，避免多按钮共享参数
                matInstance = Instantiate(image.material);
                image.material = matInstance;
            }
            else
            {
                Debug.LogWarning(
                    "ChoiceButtonFX: Image.material is not UI/ChoiceButtonFX. FX disabled.", this);
            }

            // 初始隐藏（等 PlayReveal 触发）
            if (matInstance != null)
            {
                matInstance.SetFloat(RevealID, 0f);
                matInstance.SetFloat(HoverID, 0f);
                matInstance.SetFloat(GlitchID, 0f);
                matInstance.SetFloat(EdgeID, 0f);
            }

            if (textComponent != null)
            {
                var c = textComponent.color;
                c.a = 0f;
                textComponent.color = c;
            }
        }

        // ── 入场 ──
        public void PlayReveal(float delay)
        {
            if (matInstance == null) return;
            if (revealCo != null) StopCoroutine(revealCo);
            revealCo = StartCoroutine(CoReveal(delay));
        }

        private IEnumerator CoReveal(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float t = 0f;
            while (t < revealDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / revealDuration);
                // ease-out 三次方
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                matInstance.SetFloat(RevealID, eased);

                // 文字 alpha：扫描过半后 0.06s 内淡入
                if (textComponent != null)
                {
                    float textP = Mathf.Clamp01((eased - 0.5f) / 0.5f);
                    var c = textComponent.color;
                    c.a = textP;
                    textComponent.color = c;
                }
                yield return null;
            }
            matInstance.SetFloat(RevealID, 1f);
            if (textComponent != null)
            {
                var c = textComponent.color;
                c.a = 1f;
                textComponent.color = c;
            }
        }

        // ── Hover ──
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (matInstance == null) return;
            if (hoverCo != null) StopCoroutine(hoverCo);
            hoverCo = StartCoroutine(CoHover(1f, hoverFadeIn));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (matInstance == null) return;
            if (hoverCo != null) StopCoroutine(hoverCo);
            hoverCo = StartCoroutine(CoHover(0f, hoverFadeOut));
        }

        private IEnumerator CoHover(float target, float duration)
        {
            float start = matInstance.GetFloat(HoverID);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                matInstance.SetFloat(HoverID, Mathf.Lerp(start, target, p));
                yield return null;
            }
            matInstance.SetFloat(HoverID, target);
        }

        // ── 点击 ──
        public void PlayClick()
        {
            if (matInstance == null) return;
            if (clickCo != null) StopCoroutine(clickCo);
            clickCo = StartCoroutine(CoClick());
        }

        private IEnumerator CoClick()
        {
            float t = 0f;
            while (t < clickDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / clickDuration);
                // 半正弦：0→1→0
                float v = Mathf.Sin(p * Mathf.PI);
                matInstance.SetFloat(GlitchID, v);
                matInstance.SetFloat(EdgeID, v);
                yield return null;
            }
            matInstance.SetFloat(GlitchID, 0f);
            matInstance.SetFloat(EdgeID, 0f);
        }

        private void OnDestroy()
        {
            if (matInstance != null)
            {
                Destroy(matInstance);
            }
        }
    }
}
