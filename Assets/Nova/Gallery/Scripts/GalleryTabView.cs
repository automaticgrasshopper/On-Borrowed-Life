using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// 见闻面板顶部的单个页签按钮。
    /// 拼装方式：Image (背景) + Button + 子物体 NewBadgeAnchor（运行时 Instantiate NewShowTitle.prefab）。
    /// 点击行为：通知 controller 选中此 tab；hover 时换 material（科技感闪烁）并播音效。
    /// </summary>
    public class GalleryTabView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Button button;
        [Tooltip("NewShowTitle.prefab 的挂载点。运行时若该 tab 有未读，会激活子物体里的 badge。")]
        [SerializeField] private RectTransform newBadgeAnchor;

        private StoryGalleryViewController controller;
        private GalleryCategory category;
        private Sprite normalSprite;
        private Sprite selectedSprite;
        private Material hoverMaterial;
        private Material originalMaterial;
        private bool isSelected;
        private NewBadge badgeInstance;

        public GalleryCategory Category => category;

        public void Init(StoryGalleryViewController controller, GalleryCategory category,
            Sprite normal, Sprite selected, Material hoverMat, NewBadge badgePrefab)
        {
            this.controller = controller;
            this.category = category;
            this.normalSprite = normal;
            this.selectedSprite = selected;
            this.hoverMaterial = hoverMat;

            if (background != null)
            {
                originalMaterial = background.material;
                background.sprite = normalSprite != null ? normalSprite : background.sprite;
            }

            if (button != null)
            {
                // 不走 Unity 自带 transition，外观由我们的 sprite swap 主导
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }

            if (newBadgeAnchor != null && badgePrefab != null && badgeInstance == null)
            {
                var inst = Instantiate(badgePrefab, newBadgeAnchor, false);
                badgeInstance = inst;
                var rt = inst.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                }
                badgeInstance.SetVisible(false);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (background != null)
            {
                var target = selected ? selectedSprite : normalSprite;
                if (target != null) background.sprite = target;
            }
        }

        public void SetHasUnread(bool hasUnread)
        {
            if (badgeInstance != null) badgeInstance.SetVisible(hasUnread);
        }

        private void OnClicked()
        {
            if (controller != null) controller.OnTabClicked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isSelected) return;
            if (background != null && hoverMaterial != null)
            {
                background.material = hoverMaterial;
            }
            if (controller != null) controller.PlayTabHoverSound();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null && originalMaterial != null)
            {
                background.material = originalMaterial;
            }
        }
    }
}
