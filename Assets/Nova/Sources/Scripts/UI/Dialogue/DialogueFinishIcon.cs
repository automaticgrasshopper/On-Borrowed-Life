using DG.Tweening;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// 对话框"等待点击"提示图标：
    /// - 用 DOTween 在世界 Y 上做 jump 动画（参数语义对齐 Assets/Resources/CScripts/DiaCursorTik.cs）
    /// - 可被 DialogueBoxController 重新 parent + 重定位到最新对话条尾
    /// </summary>
    public class DialogueFinishIcon : MonoBehaviour
    {
        [Tooltip("跳跃高度（世界单位，同 DiaCursorTik）")]
        public float jumpHeight = 0.1f;

        [Tooltip("单程时长（秒）。完整起落周期 = 2 × jumpDuration")]
        public float jumpDuration = 1f;

        [Tooltip("循环次数，-1 = 无限")]
        public int jumpCount = -1;

        [Tooltip("Append 模式下，相对末字右下角的额外偏移（x = 离末字右边距离，y = 垂直微调）")]
        public Vector2 tailOffset = new Vector2(8f, 0f);

        private RectTransform rt;
        private Transform originalParent;
        private Vector3 originalLocalPos;
        private Tween upTween;
        private Tween loopTween;
        private bool inited;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            originalParent = rt.parent;
            originalLocalPos = rt.localPosition;
            inited = true;
        }

        /// <summary>把图标 reparent 到指定 transform 下，并以 parent 的 local 坐标定位。</summary>
        public void SetParentAndLocalPosition(Transform parent, Vector3 localPos)
        {
            if (!inited) return;
            if (rt.parent != parent)
            {
                rt.SetParent(parent, false);
            }
            rt.localPosition = localPos;
            RestartJump();
        }

        /// <summary>回到 Awake 时缓存的原始 parent 与 localPosition。</summary>
        public void RestoreToOriginal()
        {
            if (!inited) return;
            if (rt.parent != originalParent)
            {
                rt.SetParent(originalParent, false);
            }
            rt.localPosition = originalLocalPos;
            RestartJump();
        }

        private void OnEnable()
        {
            if (inited) RestartJump();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        private void KillTweens()
        {
            upTween?.Kill();
            loopTween?.Kill();
            upTween = null;
            loopTween = null;
        }

        private void RestartJump()
        {
            KillTweens();
            // 第一段：从当前位置上跳一次到峰值
            float baseY = transform.position.y;
            upTween = transform.DOMoveY(baseY + jumpHeight, jumpDuration)
                .OnComplete(() =>
                {
                    // 第二段：在 [baseY, baseY+jumpHeight] 之间 Yoyo 循环
                    loopTween = transform.DOMoveY(baseY, jumpDuration)
                        .SetLoops(jumpCount, LoopType.Yoyo);
                });
        }
    }
}
