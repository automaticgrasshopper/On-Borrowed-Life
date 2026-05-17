using System;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// 单条见闻数据。Database 里按 4 个分类 List 分别存。
    /// 列表中的索引 = 默认显示顺序（玩家"已读"后回归该顺序）。
    /// </summary>
    [Serializable]
    public class GalleryEntry
    {
        [Tooltip("全局唯一 id。lua 用 gallery_unlock('id') 解锁。建议拼音/英文短串，例如 yingbinmansion")]
        public string id;

        [Tooltip("LocalizedContent key，标题。例如 gallery.message.yingbinmansion.title")]
        public string titleKey;

        [Tooltip("LocalizedContent key，正文描述。例如 gallery.message.yingbinmansion.desc")]
        public string descKey;

        [Tooltip("可选，详情面板右侧的图片")]
        public Sprite image;
    }
}
