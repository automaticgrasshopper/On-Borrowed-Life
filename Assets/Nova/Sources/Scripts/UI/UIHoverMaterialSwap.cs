using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// 通用 hover material swap：鼠标进入时把同物体 Image.material 切换到 hoverMaterial，
    /// 离开/失活时还原。复用 GalleryTabView 那套"hover 时走科技扫光 shader"的视觉语言，
    /// 但解耦于具体业务，可挂在任意 UI Image 上（开始菜单按钮、SideMenu icon 等）。
    /// </summary>
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("UI/Nova/UI Hover Material Swap")]
    public class UIHoverMaterialSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("hover 时切换到的 material（如 FlowChart/Shaders/FlowNewMat.mat）。留空则不生效。")]
        [SerializeField] private Material hoverMaterial;

        private Image image;
        private Material originalMaterial;
        private bool isHovering;

        private void Awake()
        {
            image = GetComponent<Image>();
            if (image != null) originalMaterial = image.material;
        }

        private void OnDisable()
        {
            // 失活时强制还原，避免下次启用还停留在 hover 态
            if (image != null) image.material = originalMaterial;
            isHovering = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (image == null || hoverMaterial == null) return;
            isHovering = true;
            image.material = hoverMaterial;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (image == null) return;
            isHovering = false;
            image.material = originalMaterial;
        }
    }
}
