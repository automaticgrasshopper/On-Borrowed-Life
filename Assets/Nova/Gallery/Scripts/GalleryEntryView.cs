using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// 见闻列表里的一行条目（即 StoryItem.prefab 上挂的脚本）。
    /// 预制体结构（用户已搭）：
    ///   StoryItem (Image 整体背景)              → bgImage（不填则自动取根节点 Image）
    ///   ├─ NewShowTitle (运行时 SetActive 显隐)  → newBadge 引用
    ///   └─ TextItem (TMP_Text)                  → titleText 引用
    /// 选中态：bgImage.sprite 在 Init 捕获的"普通"贴图 与 controller 传入的"选中"贴图之间切换。
    /// hover：与 Tab 共用 tabHoverMaterial（控制器从 database 拿来传入），鼠标进入时替换材质；
    ///        选中态优先级最高，hover 期间也不变。
    /// </summary>
    public class GalleryEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private NewBadge newBadge;
        [SerializeField] private Button button;
        [Tooltip("条目的背景 Image；留空自动取根节点 Image")]
        [SerializeField] private Image bgImage;

        private StoryGalleryViewController controller;
        private GalleryEntry entry;
        private Sprite normalSprite;
        private Sprite selectedSprite;
        private Material hoverMaterial;
        private Material originalMaterial;
        private bool isSelected;

        public GalleryEntry Entry => entry;

        public void Init(StoryGalleryViewController controller, GalleryEntry entry, bool isUnread,
            Sprite selectedSprite, Material hoverMaterial)
        {
            this.controller = controller;
            this.entry = entry;
            this.selectedSprite = selectedSprite;
            this.hoverMaterial = hoverMaterial;

            if (bgImage == null) bgImage = GetComponent<Image>();
            if (bgImage != null)
            {
                this.normalSprite = bgImage.sprite;
                this.originalMaterial = bgImage.material;
            }

            if (titleText != null && entry != null)
            {
                titleText.text = I18n.C(entry.titleKey);
            }

            if (newBadge != null) newBadge.SetVisible(isUnread);

            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (bgImage == null) return;
            if (selected && selectedSprite != null) bgImage.sprite = selectedSprite;
            else                                    bgImage.sprite = normalSprite;
            // 切到选中时，复位材质（避免选中态还残留 hover 材质）
            if (selected && originalMaterial != null) bgImage.material = originalMaterial;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isSelected) return;
            if (bgImage != null && hoverMaterial != null)
            {
                bgImage.material = hoverMaterial;
            }
            if (controller != null) controller.PlayTabHoverSound();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (bgImage != null && originalMaterial != null)
            {
                bgImage.material = originalMaterial;
            }
        }

        private void OnClicked()
        {
            if (controller != null && entry != null) controller.OnEntryClicked(this);
        }
    }
}
