using System.Collections.Generic;

namespace Nova
{
    /// <summary>
    /// 见闻解锁状态。存于 globalSave.data["gallery.unlock"]（跟金线一样，跨存档共享）。
    /// - unlockTimes：id → 解锁时 UTC ticks。用来给"最近解锁"排序顶置 + 触发 new 红点。
    /// - readIds：玩家点开过详情的 id 集合。已读后该条目从顶置回归默认顺序，new 红点消失。
    /// </summary>
    public class GalleryUnlockData : ISerializedData
    {
        public Dictionary<string, long> unlockTimes = new Dictionary<string, long>();
        public Dictionary<string, bool> readIds = new Dictionary<string, bool>();

        public const string SaveKey = "gallery.unlock";
    }
}
