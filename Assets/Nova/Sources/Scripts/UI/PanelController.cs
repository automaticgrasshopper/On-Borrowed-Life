using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class PanelController : MonoBehaviour, IPanelController
    {
        [SerializeField] protected GameObject myPanel;

        protected List<UIViewTransitionBase> transitions;
        public ViewManager viewManager { get; private set; }

        protected virtual void Awake()
        {
            this.RuntimeAssert(myPanel != null, "Missing myPanel.");
            transitions = myPanel.GetComponents<UIViewTransitionBase>().ToList();
            viewManager = Utils.FindViewManager();
        }

        public bool active => myPanel.activeSelf;

        protected virtual void OnTransitionBegin() { }

        protected virtual void OnShowFinish() { }

        // this function calls before myPanel inactive
        protected virtual void OnHideComplete() { }

        // this function calls after myPanel inactive but before onFinish
        protected virtual void OnHideFinish() { }

        public virtual void Show(bool doTransition, Action onFinish)
        {
            if (active)
            {
                onFinish?.Invoke();
                return;
            }

            Action onFinishAll = () =>
            {
                OnShowFinish();
                onFinish?.Invoke();
            };
            myPanel.SetActive(true);
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (doTransition && transition != null)
            {
                OnTransitionBegin();
                transition.Enter(onFinishAll);
            }
            else
            {
                onFinishAll.Invoke();
            }
        }

        public virtual void Hide(bool doTransition, Action onFinish)
        {
            if (!active)
            {
                onFinish?.Invoke();
                return;
            }

            // 若 panel 下挂了 PanelEnterFX 覆盖层：FX 在 progress=1 时画面已被全覆盖。
            // 这里直接跳过原 transition.Exit（避免 CanvasGroup fade 出现的 "删一下" 闪烁），
            // 直接 OnHideComplete + SetActive(false)。下一个 view 入场 FX 会从全覆盖状态接力，
            // 整个切换被深色覆盖层完全挡住。
            var fx = myPanel.GetComponentInChildren<PanelEnterFX>(false);
            if (fx != null)
            {
                fx.PlayExit(() =>
                {
                    if (!active)
                    {
                        onFinish?.Invoke();
                        return;
                    }
                    OnHideComplete();
                    myPanel.SetActive(false);
                    OnHideFinish();
                    onFinish?.Invoke();
                });
            }
            else
            {
                HideAfterFX(doTransition, onFinish);
            }
        }

        private void HideAfterFX(bool doTransition, Action onFinish)
        {
            if (!active)
            {
                onFinish?.Invoke();
                return;
            }

            Action onFinishAll = () =>
            {
                OnHideFinish();
                onFinish?.Invoke();
            };
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (doTransition && transition != null)
            {
                OnTransitionBegin();
                transition.Exit(OnHideComplete, onFinishAll);
            }
            else
            {
                OnHideComplete();
                myPanel.SetActive(false);
                onFinishAll.Invoke();
            }
        }

        protected virtual void Start()
        {
            var parent = transform.parent.GetComponentInParent<PanelController>(true);
            if (parent != null)
            {
                // Let the parent init layout for this
                return;
            }

            myPanel.SetActive(true);
            ForceRebuildLayoutAndResetTransitionTarget();
            myPanel.SetActive(false);
        }

        protected virtual void ForceRebuildLayoutAndResetTransitionTarget()
        {
            // Rebuild all layouts the hard way
            foreach (var layout in GetComponentsInChildren<LayoutGroup>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }

            if (RealScreen.isScreenInitialized)
            {
                foreach (var transition in GetComponentsInChildren<UIViewTransitionBase>())
                {
                    transition.ResetTransitionTarget();
                }
            }
        }
    }
}
