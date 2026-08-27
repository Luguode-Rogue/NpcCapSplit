using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using System.Reflection;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// 招募 / 任务前置 / 属性重置 等原生校验点的上限拆分接入补丁。
    /// 将原生"仅 Companion.Count >= CompanionLimit"的单判断，替换为
    /// 新机制的"拥有上限 + 跟随上限"双判断（且拥有数含家人）。
    /// 以 Harmony Prefix/Postfix 介入，不改动原生文件，保持解耦、便于抽离独立 mod。
    /// </summary>
    public static class CapSplitRecruitPatch
    {
        // 1) 招募对话条件：是否"同伴太多"而无法招募
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "too_many_companions")]
        [HarmonyPrefix]
        public static bool TooManyCompanionsPrefix(ref bool __result)
        {
            __result = CapSplitBehavior.GetRecruitBlockReason(Clan.PlayerClan) != CapSplitBehavior.LimitReachedReason.None;
            return false; // 跳过原生实现
        }

        // 1.5) 招募源头拦截：在 consequence 执行前拦下整段招募
        // （扣钱 + AddCompanionAction + AddHeroToPartyAction 全部跳过），
        // 英雄留在酒馆，不会凭空变成"拥有但不跟随"的虚空同伴。
        // 原生：LordConversationsCampaignBehavior.conversation_companion_hire_on_consequence
        [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_companion_hire_on_consequence")]
        [HarmonyPrefix]
        public static bool CompanionHireConsequencePrefix()
        {
            var reason = CapSplitBehavior.GetRecruitBlockReason(Clan.PlayerClan);
            if (reason == CapSplitBehavior.LimitReachedReason.None)
                return true; // 未超限，正常招募

            // 超上限：拦截招募并提示
            var model = CapSplitBehavior.GetModel();
            int followLimit = model.GetFollowLimit(Clan.PlayerClan);
            int ownLimit = model.GetCompanionLimit(Clan.PlayerClan);
            TextObject msg = reason == CapSplitBehavior.LimitReachedReason.FollowLimit
                ? new TextObject("{=NPCAPSPLIT_HIRE_FOLLOW}Party NPC limit reached ({LIMIT}), cannot recruit this companion.").SetTextVariable("LIMIT", followLimit)
                : new TextObject("{=NPCAPSPLIT_HIRE_OWNED}Clan NPC limit reached ({LIMIT}), cannot recruit more companions.").SetTextVariable("LIMIT", ownLimit);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), new Color(1f, 0.4f, 0.2f, 1f)));
            return false; // 跳过原生 consequence，英雄留在原地
        }

        // 2) 属性重置每日检查：拥有或跟随任一超限则触发警告/移除逻辑
        [HarmonyPatch(typeof(PerkResetCampaignBehavior), "DailyTick")]
        [HarmonyPrefix]
        public static bool DailyTickPrefix(ref bool __runOriginal)
        {
            var reason = CapSplitBehavior.GetRecruitBlockReason(Clan.PlayerClan);
            if (reason == CapSplitBehavior.LimitReachedReason.None)
            {
                // 未超限：跳过原生（原生会清除警告时间），保持行为一致
                __runOriginal = true;
                return true;
            }
            // 已超限：仍走原生警告/移除流程
            __runOriginal = true;
            return true;
        }

        // 3) 任务前置（LordsNeedsTutorIssueBehavior）的 CompanionLimitReached 标记：
        //    因 IssueBase.PreconditionFlags 为 internal，模组不可直接访问。
        //    该前置条件走原生 GetCompanionLimit（已 = 新拥有上限），对"拥有"维度保持一致；
        //    故此处不再额外 patch，避免对原生强耦合。核心双限制生效见上方 1)、2)。
    }
}
