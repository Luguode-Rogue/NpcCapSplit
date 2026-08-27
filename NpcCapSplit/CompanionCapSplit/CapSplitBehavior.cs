using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// NPC 上限拆分接入行为。
    /// 提供统一的"是否可招募/超限原因"判断，供招募对话、任务前置、属性重置等
    /// 校验点（通过 Harmony 或原生 Behavior）调用。
    /// 本类自身不动原生逻辑，便于整体抽离为独立 mod。
    /// </summary>
    public class CapSplitBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            // 无需事件监听：上限模型通过 Campaign.Current.Models.ClanTierModel 实时获取
        }

        public override void SyncData(IDataStore dataStore)
        {
            // 无状态，无需同步
        }

        /// <summary>获取当前生效的拆分上限模型（回退到基类以防未注册）。</summary>
        internal static CapSplitClanTierModel GetModel()
        {
            if (Campaign.Current?.Models?.ClanTierModel is CapSplitClanTierModel m)
                return m;
            return new CapSplitClanTierModel();
        }

        /// <summary>超限原因枚举。</summary>
        public enum LimitReachedReason
        {
            None,
            OwnedLimit,   // 拥有上限已满
            FollowLimit,  // 跟随上限已满
        }

        /// <summary>
        /// 判断当前玩家家族是否还能招募更多 NPC，并返回超限原因。
        /// </summary>
        public static LimitReachedReason GetRecruitBlockReason(Clan clan = null)
        {
            clan = clan ?? Clan.PlayerClan;
            var model = GetModel();
            int owned = CapSplitHelper.GetOwnedCount(clan);
            int following = CapSplitHelper.GetFollowingCount(clan);
            int ownLimit = model.GetCompanionLimit(clan);
            int followLimit = model.GetFollowLimit(clan);

            if (owned >= ownLimit)
                return LimitReachedReason.OwnedLimit;
            if (following >= followLimit)
                return LimitReachedReason.FollowLimit;
            return LimitReachedReason.None;
        }

        /// <summary>当前拥有数量 / 拥有上限（带中文含义说明）。</summary>
        public static string GetOwnedText(Clan clan = null)
        {
            clan = clan ?? Clan.PlayerClan;
            var model = Campaign.Current?.Models?.ClanTierModel as CapSplitClanTierModel ?? new CapSplitClanTierModel();
            return new TextObject("{=NPCAPSPLIT_OWNED_UI}Clan NPC: {COUNT} / Limit {LIMIT}")
                .SetTextVariable("COUNT", CapSplitHelper.GetOwnedCount(clan))
                .SetTextVariable("LIMIT", model.GetCompanionLimit(clan))
                .ToString();
        }

        /// <summary>当前跟随数量 / 跟随上限（带中文含义说明）。</summary>
        public static string GetFollowText(Clan clan = null)
        {
            clan = clan ?? Clan.PlayerClan;
            var model = Campaign.Current?.Models?.ClanTierModel as CapSplitClanTierModel ?? new CapSplitClanTierModel();
            return new TextObject("{=NPCAPSPLIT_FOLLOW_UI}Party NPC: {COUNT} / Limit {LIMIT}")
                .SetTextVariable("COUNT", CapSplitHelper.GetFollowingCount(clan))
                .SetTextVariable("LIMIT", model.GetFollowLimit(clan))
                .ToString();
        }
    }
}
