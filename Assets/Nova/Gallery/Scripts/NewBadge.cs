using UnityEngine;

namespace Nova
{
    /// <summary>
    /// 挂在 NewShowTitle.prefab 根节点上。仅一个职责：
    ///   让外部脚本通过 SetVisible 切换显隐，并保证 Image 的 raycast 关闭（NEW 红点不拦截点击）。
    ///
    /// 这个组件本身不订阅 GalleryService，订阅由外层（SideMenu 的见闻按钮 / Tab / EntryItem）做。
    /// 这样同一个 prefab 复用到三个位置时各自有不同的显隐逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public class NewBadge : MonoBehaviour
    {
        private void Awake()
        {
            // 防止 NEW 图标拦截父按钮的点击/hover
            var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var g in graphics) g.raycastTarget = false;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
