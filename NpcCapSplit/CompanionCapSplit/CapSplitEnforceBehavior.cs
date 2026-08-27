using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// 上限拆分硬拦截：超上限时直接在"英雄加入主角队伍"动作执行前拦下，
    /// 不招募、不进队、英雄留在原地，并弹出中文提示。
    /// 这是对 too_many_companions 显示层拦截的兜底，确保任何入口都无法突破携带上限。
    /// </summary>
    [HarmonyPatch(typeof(AddHeroToPartyAction))]
    public static class CapSplitEnforceBehavior
    {
        /// <summary>
        /// 在所有"英雄加入队伍"动作执行【之前】介入。
        /// 原生签名：AddHeroToPartyAction.Apply(Hero hero, MobileParty party, bool showNotification)
        /// 返回 false 即可跳过原方法 —— 英雄根本不会进队（实现"直接不招募"）。
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("Apply")]
        public static bool AddHeroToPartyPrefix(Hero hero, MobileParty party, bool showNotification)
        {
            if (hero == null || party == null)
                return true; // 异常情况不拦，交给原逻辑
            // 只处理"加入主角队伍"的情况
            MobileParty mainParty = MobileParty.MainParty;
            if (mainParty == null || party != mainParty)
                return true; // 不是主角队伍，放行

            Clan clan = Clan.PlayerClan;
            if (clan == null)
                return true;

            var model = CapSplitBehavior.GetModel();
            int followLimit = model.GetFollowLimit(clan);

            // 未超上限，正常放行（召唤同伴、任务随从等正常进队不受阻）
            if (CapSplitHelper.GetFollowingCount(clan) < followLimit)
                return true;

            // 超上限：直接拦截本次招募，英雄不进队，弹提示
            var msg = new TextObject("{=NPCAPSPLIT_ENFORCE}Party NPC limit reached ({LIMIT}), cannot recruit {HERO}.")
                .SetTextVariable("LIMIT", followLimit)
                .SetTextVariable("HERO", hero.Name);
            InformationManager.DisplayMessage(new InformationMessage(
                msg.ToString(),
                new Color(1f, 0.4f, 0.2f, 1f)));
            return false; // 短路原 Apply，英雄保持原状（留在酒馆/城镇），不进队、不消失
        }
    }
}
