using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.Library;

namespace NpcCapSplit.CompanionCapSplit
{
    /// <summary>
    /// 家族界面（ClanMembersVM）UI 微调 Harmony 补丁。
    /// 在原生 RefreshMembersList 之后，直接替换原生标题文本为拆分上限口径说明
    /// （隐藏原生文本，不再追加，避免与原生口径混淆）：
    ///   家人框  -> 「家族NPC 当前 拥有数 / 拥有上限」
    ///   同伴框  -> 「队伍NPC 当前 跟随数 / 携带上限」
    /// 以 Postfix 方式介入，不复制/改写原生 VM，保持与本功能解耦。
    /// </summary>
    [HarmonyPatch(typeof(ClanMembersVM), "RefreshMembersList")]
    public static class CapSplitUIPatch
    {
        public static void Postfix(ClanMembersVM __instance)
        {
            if (__instance == null)
                return;
            // 直接替换：家人框显示"拥有"口径
            __instance.FamilyText = CapSplitBehavior.GetOwnedText();
            // 同伴框显示"跟随"口径
            __instance.CompanionsText = CapSplitBehavior.GetFollowText();
        }
    }
}
