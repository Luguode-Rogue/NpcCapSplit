using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// NPC 上限拆分机制核心模型。
    /// 将原生的单一"家族拥有同伴上限"拆分为两个独立上限：
    ///   1) 跟随上限（携带数量）：3 + perk数 + 家族等级
    ///   2) 拥有上限（家族总数量）：(3 + perk数 + 家族等级) * (3 + perk数)
    /// 两个相关 perk（统御 WePledgeOurSwords、魅力 Camaraderie）每个同时
    /// +1 携带，并 +1 倍率（让拥有上限的乘数 +1）。
    /// 上限均随家族等级提升。
    /// </summary>
    public class CapSplitClanTierModel : DefaultClanTierModel
    {
        /// <summary>基础常量 3（与原生 tier+3 的基准保持一致）</summary>
        private const int BaseValue = 3;

        /// <summary>
        /// 计算家族拥有的、与目标 perk 相关的英雄数量。
        /// 沿用原生 DefaultClanTierModel 的判定方式：家族任意英雄拥有该 perk 即计 1。
        /// </summary>
        private int GetPerkHeroCount(Clan clan, PerkObject perk)
        {
            int count = 0;
            if (clan == null || perk == null)
                return 0;
            foreach (Hero hero in clan.Heroes)
            {
                if (hero != null && hero.GetPerkValue(perk))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 与目标上限相关的 perk 总数（每个 +1 携带、+1 倍率）。
        /// 统御 WePledgeOurSwords、魅力 Camaraderie。
        /// </summary>
        private int GetRelatedPerkCount(Clan clan)
        {
            int count = 0;
            count += GetPerkHeroCount(clan, DefaultPerks.Leadership.WePledgeOurSwords);
            count += GetPerkHeroCount(clan, DefaultPerks.Charm.Camaraderie);
            return count;
        }

        /// <summary>
        /// 携带上限（跟随数量）= 3 + perk数 + 家族等级。
        /// </summary>
        public int GetFollowLimit(Clan clan)
        {
            int perk = GetRelatedPerkCount(clan);
            int tier = (clan != null) ? clan.Tier : 0;
            return BaseValue + perk + tier;
        }

        /// <summary>
        /// 拥有上限（家族总数量）= (3 + perk数 + 家族等级) * (3 + perk数)。
        /// 注意：原生 GetCompanionLimit 只限制"同伴"，这里语义改为"同伴+家人"的总上限，
        /// 具体统计口径见 CapSplitHelper。
        /// </summary>
        public override int GetCompanionLimit(Clan clan)
        {
            int perk = GetRelatedPerkCount(clan);
            int tier = (clan != null) ? clan.Tier : 0;
            int carry = BaseValue + perk + tier;
            return carry * (BaseValue + perk);
        }
    }
}
