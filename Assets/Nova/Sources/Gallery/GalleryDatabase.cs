using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// 见闻总数据库。挂在 ScriptableObject 资产上（Assets/Nova/Sources/Gallery/Database/GalleryDatabase.asset）。
    /// 4 个分类各一个 List；列表内顺序 = 默认显示顺序。
    /// 新解锁的条目运行时浮到顶，已读后回归 List 顺序。
    /// </summary>
    [CreateAssetMenu(fileName = "GalleryDatabase", menuName = "Nova/Gallery/Database")]
    public class GalleryDatabase : ScriptableObject
    {
        [Header("条目（按分类）")]
        public List<GalleryEntry> heroes = new List<GalleryEntry>();    // 人物
        public List<GalleryEntry> news = new List<GalleryEntry>();      // 奇闻
        public List<GalleryEntry> messages = new List<GalleryEntry>();  // 情报
        public List<GalleryEntry> items = new List<GalleryEntry>();     // 道具

        [Header("缺省资源")]
        [Tooltip("详情面板无图时的占位")]
        public Sprite defaultDetailImage;

        [Header("Tab 视觉")]
        public Sprite tabNormalSprite;
        public Sprite tabSelectedSprite;
        [Tooltip("hover 时 Image 替换的材质（科技感闪一下），可空")]
        public Material tabHoverMaterial;

        [Header("条目行视觉")]
        [Tooltip("列表行被选中时的背景；不填则不切换视觉")]
        public Sprite entrySelectedSprite;

        [Header("音效（走 UI 音量）")]
        public AudioClip tabHoverSound;
        public AudioClip tabClickSound;
        public AudioClip entryClickSound;

        public List<GalleryEntry> GetEntriesOf(GalleryCategory category)
        {
            switch (category)
            {
                case GalleryCategory.Hero:    return heroes;
                case GalleryCategory.News:    return news;
                case GalleryCategory.Message: return messages;
                case GalleryCategory.Item:    return items;
                default:                      return null;
            }
        }

        /// <summary>
        /// 按 id 反查条目 + 所属分类。找不到返回 false（这样写允许策划临时撤掉一条 entry 而不会让旧存档 NRE）。
        /// </summary>
        public bool TryGetById(string id, out GalleryEntry entry, out GalleryCategory category)
        {
            for (int c = 0; c < 4; ++c)
            {
                var cat = (GalleryCategory)c;
                var list = GetEntriesOf(cat);
                if (list == null) continue;
                for (int i = 0; i < list.Count; ++i)
                {
                    var e = list[i];
                    if (e != null && e.id == id)
                    {
                        entry = e;
                        category = cat;
                        return true;
                    }
                }
            }
            entry = null;
            category = GalleryCategory.Hero;
            return false;
        }
    }
}
