using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// 面板入场覆盖层 FX。挂在每个 view 根节点下的"全屏 overlay Image"上，
    /// Image.Material 指向 UI/PanelEnterFX。每次 OnEnable 自动播放一遍。
    ///
    /// 用法：
    /// - 在 FlowChart / Log / Gallery / Config 各自的 ViewRoot 下加一个子 GameObject：
    ///   - RectTransform：stretch all（anchor 0,0 → 1,1，offset 全 0）
    ///   - Image：source image 留空（白），material 选 PanelEnterFX.mat
    ///   - Raycast Target = false
    ///   - 添加本组件
    /// - view 显示时该子物体随之 enable，FX 自动播放并在结尾隐藏自身。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PanelEnterFX : MonoBehaviour
    {
        [Header("总时长")]
        [Tooltip("从中线 hairline 到完全隐藏的总时长（秒）。0.15 = 横向切开 0.10s + 尾淡 0.05s")]
        public float duration = 0.15f;

        [Header("尾段淡出")]
        [Tooltip("最后这段时间用于把整层 alpha 拉到 0（包含在 duration 内）")]
        public float tailFadeDuration = 0.05f;

        private static readonly int ProgressID   = Shader.PropertyToID("_Progress");
        private static readonly int LayerAlphaID = Shader.PropertyToID("_LayerAlpha");
        private static readonly int DirectionID  = Shader.PropertyToID("_Direction");

        private Image image;
        private Material matInstance;
        private Coroutine playCo;

        private void Awake()
        {
            image = GetComponent<Image>();

            if (image.material != null && image.material.shader != null
                && image.material.shader.name == "UI/PanelEnterFX")
            {
                // 独立实例：多个 view 同时存在时不串扰
                matInstance = Instantiate(image.material);
                image.material = matInstance;
            }
            else
            {
                Debug.LogWarning(
                    "PanelEnterFX: Image.material is not UI/PanelEnterFX. FX disabled.", this);
            }

            // Image 不应阻挡点击（哪怕 alpha 已 0 也不挡）
            image.raycastTarget = false;
        }

        private void OnEnable()
        {
            if (matInstance == null) return;
            if (playCo != null) StopCoroutine(playCo);
            playCo = StartCoroutine(CoPlay());
        }

        private void OnDisable()
        {
            if (playCo != null) StopCoroutine(playCo);
            playCo = null;
        }

        private IEnumerator CoPlay()
        {
            // 初始：完全覆盖、入场方向（L→R）
            matInstance.SetFloat(DirectionID, 0f);
            matInstance.SetFloat(ProgressID, 0f);
            matInstance.SetFloat(LayerAlphaID, 1f);

            float t = 0f;
            float coreDuration = Mathf.Max(0.01f, duration - tailFadeDuration);

            // 主阶段：progress 0→1
            while (t < coreDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / coreDuration);
                // ease-out 三次方：和 ChoiceButtonFX 同款手感
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                matInstance.SetFloat(ProgressID, eased);
                yield return null;
            }
            matInstance.SetFloat(ProgressID, 1f);

            // 尾段：整层 alpha 1→0
            float ft = 0f;
            float fadeDur = Mathf.Max(0.01f, tailFadeDuration);
            while (ft < fadeDur)
            {
                ft += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(ft / fadeDur);
                matInstance.SetFloat(LayerAlphaID, 1f - p);
                yield return null;
            }
            matInstance.SetFloat(LayerAlphaID, 0f);

            playCo = null;
            // 不要 SetActive(false)：GameObject 保持激活，下次 view 的 myPanel.SetActive(true)
            // 会让本物体 activeInHierarchy 重新 false→true，OnEnable 再触发，特效重放。
        }

        // ── 退场（R→L 离子消散）──
        // 由 PanelController.Hide 在隐藏前调用，播完触发 onComplete 后才走实际的 hide 流程。
        public void PlayExit(Action onComplete)
        {
            if (matInstance == null)
            {
                onComplete?.Invoke();
                return;
            }
            if (playCo != null) StopCoroutine(playCo);
            playCo = StartCoroutine(CoPlayExit(onComplete));
        }

        private IEnumerator CoPlayExit(Action onComplete)
        {
            // 初始：完全露出、退场方向（R→L）
            matInstance.SetFloat(DirectionID, 1f);
            matInstance.SetFloat(ProgressID, 0f);
            matInstance.SetFloat(LayerAlphaID, 1f);

            // 退场只走核心阶段：progress 0→1，结束时整个画面被覆盖层全覆盖。
            // 不做尾段 alpha 淡出 —— 我们要让覆盖层"扛着"，
            // 上层 PanelController 接到 onComplete 后会跳过原 transition.Exit
            // 直接 SetActive(false)，下一个 view 入场时再从全覆盖状态接力 L→R 扫开。
            float t = 0f;
            float coreDuration = Mathf.Max(0.01f, duration);

            while (t < coreDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / coreDuration);
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                matInstance.SetFloat(ProgressID, eased);
                yield return null;
            }
            matInstance.SetFloat(ProgressID, 1f);
            matInstance.SetFloat(LayerAlphaID, 1f);

            playCo = null;
            onComplete?.Invoke();
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
