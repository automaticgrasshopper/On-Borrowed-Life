-- 见闻系统 lua API。
-- C# 端：GalleryService 在 Awake 时 BindObject("gallery", this)，挂到 __Nova.gallery
-- 用法：
--   gallery_unlock('yingbinmansion')                       -- 解锁见闻条目 + 右上角通知
--   gallery_revoke('talisman_1stroke')                     -- 撤销（用于条目替换的清理阶段）
--   gallery_replace('talisman_1stroke', 'talisman_2stroke')-- 一步替换（revoke + unlock）

function gallery_unlock(id)
    if __Nova.gallery == nil then
        warn('gallery_unlock: GalleryService 未绑定（场景里缺挂载组件？）')
        return
    end
    __Nova.gallery:Unlock(id)
end

function gallery_revoke(id)
    if __Nova.gallery == nil then
        warn('gallery_revoke: GalleryService 未绑定')
        return
    end
    __Nova.gallery:Revoke(id)
end

function gallery_replace(old_id, new_id)
    if __Nova.gallery == nil then
        warn('gallery_replace: GalleryService 未绑定')
        return
    end
    __Nova.gallery:Replace(old_id, new_id)
end
