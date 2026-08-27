using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NpcCapSplit
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            new Harmony("NpcCapSplit").PatchAll(Assembly.GetExecutingAssembly());

            if (game.GameType is Campaign)
            {
                // 替换原生 ClanTierModel，提供拆分上限计算
                gameStarterObject.AddModel(new NpcCapSplit.CompanionCapSplit.CapSplitClanTierModel());
                // 注册拆分上限接入行为（校验点/统计）
                CampaignGameStarter campaignGameStarter = gameStarterObject as CampaignGameStarter;
                campaignGameStarter.AddBehavior(new NpcCapSplit.CompanionCapSplit.CapSplitBehavior());
            }
        }
    }
}
