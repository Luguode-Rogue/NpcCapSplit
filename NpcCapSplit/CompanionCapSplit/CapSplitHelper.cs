using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// NPC 上限拆分统计辅助。
    /// 统计口径（与原生不同，原生仅统计 Clan.Companions 同伴）：
    ///   拥有数 = 家族同伴(Companions) + 家族成员(Heroes，即家人) 的总计
    ///   跟随数 = 上述集合中当前在玩家主角队伍(MainParty)里的数量
    /// 所有计算基于原生已有引用关系，无需新增存档字段。
    /// </summary>
    public static class CapSplitHelper
    {
        /// <summary>家族所有可计入限制的 NPC（同伴 + 家人）集合。</summary>
        private static System.Collections.Generic.IEnumerable<Hero> GetAllLimitedHeroes(Clan clan)
        {
            if (clan == null)
                yield break;

            // 兜底 18，避免 AgeModel 在某些时机为 null 触发 NRE（会让整个统计回退、看起来"没生效"）
            int heroComesOfAge = Campaign.Current?.Models?.AgeModel?.HeroComesOfAge ?? 18;

            // 同伴集合（存活）。注意：Clan.Companions 是 Clan.Heroes 的子集，
            // 同伴同时出现在两个列表里，若直接并集会重复计数，故先用 HashSet 记录同伴。
            var companions = new System.Collections.Generic.HashSet<Hero>();
            foreach (Hero hero in clan.Companions)
            {
                // 同伴均为成年人，仅做存活判定
                if (hero != null && hero.IsAlive)
                    companions.Add(hero);
            }
            foreach (Hero hero in companions)
                yield return hero;

            foreach (Hero hero in clan.Heroes)
            {
                // 家人需满足：存活 且 已成年，且不能是已计入的同伴（去重，避免重复计数）
                // IsAlive 已包含死亡判定（死亡英雄 IsAlive=false），无需再比较 DeathMark
                if (hero != null && hero.IsAlive
                    && hero.Age >= (float)heroComesOfAge
                    && !companions.Contains(hero))
                    yield return hero;
            }
        }

        /// <summary>
        /// 家族当前"拥有"的 NPC 数量（同伴 + 家人，不区分是否在队）。
        /// </summary>
        public static int GetOwnedCount(Clan clan)
        {
            if (clan == null)
                return 0;
            return GetAllLimitedHeroes(clan).Count();
        }

        /// <summary>
        /// 家族当前"跟随"(在玩家主角队伍里)的 NPC 数量。
        /// 仅统计同伴(Companions)中实际在主角队伍里的部分——对应 UI 的"队伍NPC/流浪者框"。
        /// 家人(Heroes)不计入跟随：家人只受"拥有上限"约束，不会挤占"携带上限"
        /// （这也是原版 CompanionLimit 只针对同伴的语义，避免妻子/子女被误算进队伍上限）。
        /// </summary>
        public static int GetFollowingCount(Clan clan)
        {
            if (clan == null)
                return 0;
            MobileParty mainParty = MobileParty.MainParty;
            if (mainParty == null)
                return 0;
            int count = 0;
            foreach (Hero hero in clan.Companions)
            {
                if (hero != null && hero.IsAlive && hero.PartyBelongedTo == mainParty)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 是否还能招募：需同时满足"未超拥有上限"且"未超跟随上限"。
        /// </summary>
        public static bool CanRecruitMore(Clan clan, CapSplitClanTierModel model)
        {
            if (clan == null || model == null)
                return false;
            int owned = GetOwnedCount(clan);
            int following = GetFollowingCount(clan);
            return owned < model.GetCompanionLimit(clan)
                && following < model.GetFollowLimit(clan);
        }
    }
}
