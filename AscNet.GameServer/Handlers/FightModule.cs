﻿using AscNet.Common.Database;
using AscNet.Common.MsgPack;
using AscNet.Common.Util;
using AscNet.Table.share.fuben;
using MessagePack;

namespace AscNet.GameServer.Handlers
{
    [MessagePackObject(true)]
    public class NpcHp
    {
        public int CharacterId { get; set; }
        public int NpcId { get; set; }
        public int Type { get; set; }
        public int Level { get; set; }
        public List<int> BuffIds { get; set; }
        public dynamic AttrTable { get; set; }
    }

    [MessagePackObject(true)]
    public class NpcDps
    {
        public int Value { get; set; }
        public int MaxValue { get; set; }
        public int RoleId { get; set; }
        public int NpcId { get; set; }
        public int CharacterId { get; set; }
        public int DamageTotal { get; set; }
        public int DamageNormal { get; set; }
        public List<int> DamageMagic { get; set; }
        public int BreakEndure { get; set; }
        public int Cure { get; set; }
        public int Hurt { get; set; }
        public int Type { get; set; }
        public int Level { get; set; }
        public List<int> BuffIds { get; set; }
        public dynamic AttrTable { get; set; }
    }
    
    [MessagePackObject(true)]
    public class FightSettleResult
    {
        public bool IsWin { get; set; }
        public bool IsForceExit { get; set; }
        public int StageId { get; set; }
        public int StageLevel { get; set; }
        public int FightId { get; set; }
        public int RebootCount { get; set; }
        public int AddStars { get; set; }
        public int StartFrame { get; set; }
        public int SettleFrame { get; set; }
        public int PauseFrame { get; set; }
        public int ExSkillPauseFrame { get; set; }
        public int SettleCode { get; set; }
        public int DodgeTimes { get; set; }
        public int NormalAttackTimes { get; set; }
        public int ConsumeBallTimes { get; set; }
        public int StuntSkillTimes { get; set; }
        public int PauseTimes { get; set; }
        public int HighestCombo { get; set; }
        public int DamagedTimes { get; set; }
        public int MatrixTimes { get; set; }
        public int HighestDamage { get; set; }
        public int TotalDamage { get; set; }
        public int TotalDamaged { get; set; }
        public int TotalCure { get; set; }
        public List<int> PlayerIds { get; set; }
        public List<PlayerData> PlayerData { get; set; }
        public dynamic IntToIntRecord { get; set; }
        public dynamic StringToIntRecord { get; set; }
        public dynamic Operations { get; set; }
        public List<int> Codes { get; set; }
        public int LeftTime { get; set; }
        public Dictionary<int, NpcHp> NpcHpInfo { get; set; }
        public Dictionary<int, NpcDps> NpcDpsTable { get; set; }
        public List<dynamic> EventSet { get; set; }
        public int DeathTotalMyTeam { get; set; }
        public int DeathTotalEnemy { get; set; }
        public dynamic DeathRecord { get; set; }
        public List<dynamic> GroupDropDatas { get; set; }
        public dynamic EpisodeFightResults { get; set; }
        public dynamic CustomData { get; set; }
    }
    
    [MessagePackObject(true)]
    public class FightSettleRequest
    {
        public FightSettleResult Result { get; set; }
    }
    
    internal class FightModule
    {
        [RequestPacketHandler("PreFightRequest")]
        public static void PreFightRequestHandler(Session session, Packet.Request packet)
        {
            PreFightRequest req = MessagePackSerializer.Deserialize<PreFightRequest>(packet.Content);

            StageTable? stageTable = StageTableReader.Instance.FromStageId((int)req.PreFightData.StageId);
            if (stageTable is null)
            {
                // FubenManagerCheckPreFightStageInfoNotFound
                session.SendResponse(new PreFightResponse() { Code = 20003012 }, packet.Id);
                return;
            }

            PreFightResponse rsp = new()
            {
                Code = 0,
                FightData = new()
                {
                    Online = false,
                    FightId = req.PreFightData.StageId + (uint)Random.Shared.NextInt64(0, uint.MaxValue - req.PreFightData.StageId),
                    OnlineMode = 0,
                    Seed = (uint)Random.Shared.NextInt64(0, uint.MaxValue),
                    StageId = req.PreFightData.StageId,
                    RebootId = Miscs.ParseIntOr(stageTable.RebootId, 0),
                    PassTimeLimit = Miscs.ParseIntOr(stageTable.PassTimeLimit, 300),
                    StarsMark = 0
                }
            };

            rsp.FightData.RoleData.Add(new()
            {
                Id = (uint)session.player.PlayerData.Id,
                Camp = 1,
                Name = session.player.PlayerData.Name,
                IsRobot = false,
                NpcData = new()
            });

            for (int i = 0; i < req.PreFightData.CardIds.Count; i++)
            {
                uint cardId = req.PreFightData.CardIds[i];
                var characterData = session.character.Characters.FirstOrDefault(x => x.Id == cardId);
                if (characterData is null)
                    continue;

                rsp.FightData.RoleData.First(x => x.Id == session.player.PlayerData.Id).NpcData.Add(i, new
                {
                    Character = characterData,
                    Equips = session.character.Equips.Where(x => x.CharacterId == cardId)
                });
            }

            session.SendResponse(rsp, packet.Id);
        }

        [RequestPacketHandler("TeamSetTeamRequest")]
        public static void HandleTeamSetTeamRequestHandler(Session session, Packet.Request packet)
        {
            session.SendResponse(new TeamSetTeamResponse(), packet.Id);
        }

        [RequestPacketHandler("FightSettleRequest")]
        public static void FightSettleRequestHandler(Session session, Packet.Request packet)
        {
            FightSettleRequest req = MessagePackSerializer.Deserialize<FightSettleRequest>(packet.Content);

            session.SendPush("NotifyStageData", MessagePackSerializer.ConvertFromJson($"{{\"StageList\": [{{\"StageId\": {req.Result.StageId}, \"StarsMark\": 7, \"Passed\": true, \"PassTimesToday\": 1, \"PassTimesTotal\": 2, \"BuyCount\": 0, \"Score\": 0, \"LastPassTime\": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, \"RefreshTime\": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}, \"CreateTime\": 1626546618, \"BestRecordTime\": 17, \"LastRecordTime\": 17, \"BestCardIds\": [1021001, 1031003, 1051001], \"LastCardIds\": [1021001, 1031003, 1051001]}}]}}"));
            session.SendResponse(MessagePackSerializer.Deserialize<PreFightResponse>(MessagePackSerializer.ConvertFromJson($"{{\"Code\": 0, \"Settle\": {{\"IsWin\": true, \"StageId\": {req.Result.StageId}, \"StarsMark\": 7, \"RewardGoodsList\": [{{\"RewardType\": 1, \"TemplateId\": 1, \"Count\": 1947, \"Level\": 0, \"Quality\": 0, \"Grade\": 0, \"Breakthrough\": 0, \"ConvertFrom\": 0, \"Id\": 0}}, {{\"RewardType\": 1, \"TemplateId\": 30011, \"Count\": 1, \"Level\": 0, \"Quality\": 0, \"Grade\": 0, \"Breakthrough\": 0, \"ConvertFrom\": 0, \"Id\": 0}}, {{\"RewardType\": 1, \"TemplateId\": 30011, \"Count\": 1, \"Level\": 0, \"Quality\": 0, \"Grade\": 0, \"Breakthrough\": 0, \"ConvertFrom\": 0, \"Id\": 0}}, {{\"RewardType\": 3, \"TemplateId\": 3052001, \"Count\": 1, \"Level\": 1, \"Quality\": 0, \"Grade\": 0, \"Breakthrough\": 0, \"ConvertFrom\": 0, \"Id\": 0}}, {{\"RewardType\": 3, \"TemplateId\": 3034002, \"Count\": 1, \"Level\": 1, \"Quality\": 0, \"Grade\": 0, \"Breakthrough\": 0, \"ConvertFrom\": 0, \"Id\": 0}}], \"LeftTime\": 282, \"NpcHpInfo\": {{\"9\": {{\"CharacterId\": 0, \"NpcId\": 91030, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9003, 900007, 900050, 701022, 900011, 900051, 910300, 715531, 700053, 715075, 700007, 900080, 700045, 100062, 700030, 700029, 710076, 700027, 700215, 700028], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 751}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 84, \"MaxValue\": 84}}, \"4\": {{\"Value\": 84, \"MaxValue\": 84}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"10\": {{\"CharacterId\": 0, \"NpcId\": 92090, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9009, 900007, 900050, 900055, 900011, 900051, 900072, 715531, 700053, 715075, 700007, 900080, 700028, 700045, 100062, 700030, 700029, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 301}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 84, \"MaxValue\": 84}}, \"4\": {{\"Value\": 84, \"MaxValue\": 84}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"11\": {{\"CharacterId\": 0, \"NpcId\": 92090, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9009, 900007, 900050, 900072, 900011, 900051, 700028, 715531, 700053, 715075, 700007, 900080, 100063, 100068, 700030, 700045, 700029, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 301}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 84, \"MaxValue\": 84}}, \"4\": {{\"Value\": 84, \"MaxValue\": 84}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"12\": {{\"CharacterId\": 0, \"NpcId\": 91100, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9010, 900007, 900050, 900055, 900011, 900051, 900082, 715531, 700053, 715075, 700007, 900080, 700030, 700029, 700045, 700028, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 844}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 199, \"MaxValue\": 199}}, \"4\": {{\"Value\": 99, \"MaxValue\": 99}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 38, \"MaxValue\": 38}}, \"12\": {{\"Value\": 38, \"MaxValue\": 38}}, \"13\": {{\"Value\": 38, \"MaxValue\": 38}}, \"14\": {{\"Value\": 38, \"MaxValue\": 38}}, \"15\": {{\"Value\": 38, \"MaxValue\": 38}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 18, \"MaxValue\": 18}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"14\": {{\"CharacterId\": 0, \"NpcId\": 91090, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9009, 900007, 900050, 900055, 900011, 900051, 900082, 715531, 700053, 715075, 700007, 900080, 100062, 100063, 100068, 700030, 700029, 700045, 700028, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 751}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 84, \"MaxValue\": 84}}, \"4\": {{\"Value\": 84, \"MaxValue\": 84}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"13\": {{\"CharacterId\": 0, \"NpcId\": 91090, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9009, 900007, 900050, 900082, 900011, 900051, 700030, 715531, 700053, 715075, 700007, 900080, 100063, 100068, 700029, 700045, 700028, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 751}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 84, \"MaxValue\": 84}}, \"4\": {{\"Value\": 84, \"MaxValue\": 84}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"16\": {{\"CharacterId\": 0, \"NpcId\": 91120, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9012, 900007, 900050, 700030, 900011, 900051, 700029, 715531, 700933, 715075, 700053, 700007, 900080, 700045, 700028, 701022, 710076, 700027, 700215, 700008, 900052], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 751}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 99, \"MaxValue\": 99}}, \"4\": {{\"Value\": 99, \"MaxValue\": 99}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"15\": {{\"CharacterId\": 0, \"NpcId\": 91120, \"Type\": 4, \"Level\": 7, \"BuffIds\": [9012, 900007, 900050, 900055, 900011, 900051, 900082, 715531, 700030, 715075, 700053, 700007, 900080, 700029, 700045, 700028, 701022, 710076, 700027, 700215], \"AttrTable\": {{\"1\": {{\"Value\": 1, \"MaxValue\": 751}}, \"2\": {{\"Value\": 0, \"MaxValue\": 100}}, \"3\": {{\"Value\": 99, \"MaxValue\": 99}}, \"4\": {{\"Value\": 99, \"MaxValue\": 99}}, \"5\": {{\"Value\": 418, \"MaxValue\": 418}}, \"11\": {{\"Value\": 50, \"MaxValue\": 50}}, \"12\": {{\"Value\": 50, \"MaxValue\": 50}}, \"13\": {{\"Value\": 50, \"MaxValue\": 50}}, \"14\": {{\"Value\": 50, \"MaxValue\": 50}}, \"15\": {{\"Value\": 50, \"MaxValue\": 50}}, \"23\": {{\"Value\": 31, \"MaxValue\": 31}}, \"24\": {{\"Value\": 37, \"MaxValue\": 37}}, \"32\": {{\"Value\": 30, \"MaxValue\": 30}}, \"34\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"35\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"36\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"37\": {{\"Value\": 2030, \"MaxValue\": 2030}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 1, \"MaxValue\": 1}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}}}}}, \"1\": {{\"CharacterId\": 1021001, \"NpcId\": 10210012, \"Type\": 1, \"Level\": 50, \"BuffIds\": [100054, 1002, 100050, 100051, 100074, 100085, 100201, 715587, 100219, 100243, 502101, 620701, 620705, 620801, 100055, 100056, 100057, 100058, 900081, 199931, 100052, 100053, 100204, 100294, 100295, 100296, 100297, 100001, 105535, 410011, 410012, 731103, 100016, 100007, 100093, 100091, 100000, 100021, 100076], \"AttrTable\": {{\"1\": {{\"Value\": 5211, \"MaxValue\": 5211}}, \"2\": {{\"Value\": 42, \"MaxValue\": 120}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 610, \"MaxValue\": 610}}, \"6\": {{\"Value\": 20019980, \"MaxValue\": 20019980}}, \"11\": {{\"Value\": 1076, \"MaxValue\": 1076}}, \"12\": {{\"Value\": 1076, \"MaxValue\": 1076}}, \"13\": {{\"Value\": 1076, \"MaxValue\": 1076}}, \"14\": {{\"Value\": 1076, \"MaxValue\": 1076}}, \"15\": {{\"Value\": 1076, \"MaxValue\": 1076}}, \"23\": {{\"Value\": 676, \"MaxValue\": 676}}, \"25\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 527, \"MaxValue\": 527}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 0, \"MaxValue\": 1000}}, \"56\": {{\"Value\": -96, \"MaxValue\": -96}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 199, \"MaxValue\": 199}}, \"74\": {{\"Value\": 699, \"MaxValue\": 699}}}}}}, \"2\": {{\"CharacterId\": 1031003, \"NpcId\": 10310033, \"Type\": 1, \"Level\": 45, \"BuffIds\": [100054, 102389, 1023, 100050, 100051, 100074, 100088, 102339, 102340, 102348, 410011, 503101, 620201, 620202, 620217, 620301, 620302, 620304, 620307, 100055, 100056, 100057, 100058, 199931, 100052, 100053, 105535, 410012], \"AttrTable\": {{\"1\": {{\"Value\": 2271, \"MaxValue\": 2271}}, \"2\": {{\"Value\": 0, \"MaxValue\": 120}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 610, \"MaxValue\": 610}}, \"6\": {{\"Value\": 20019980, \"MaxValue\": 20019980}}, \"11\": {{\"Value\": 470, \"MaxValue\": 470}}, \"12\": {{\"Value\": 470, \"MaxValue\": 470}}, \"13\": {{\"Value\": 470, \"MaxValue\": 470}}, \"14\": {{\"Value\": 470, \"MaxValue\": 470}}, \"15\": {{\"Value\": 470, \"MaxValue\": 470}}, \"23\": {{\"Value\": 302, \"MaxValue\": 302}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 263, \"MaxValue\": 263}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"51\": {{\"Value\": 2471, \"MaxValue\": 2471}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 800, \"MaxValue\": 800}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 4}}, \"68\": {{\"Value\": 199, \"MaxValue\": 199}}, \"74\": {{\"Value\": 699, \"MaxValue\": 699}}}}}}, \"3\": {{\"CharacterId\": 1051001, \"NpcId\": 10510012, \"Type\": 1, \"Level\": 45, \"BuffIds\": [100054, 1005, 100050, 100051, 100074, 100086, 100541, 410011, 505101, 620401, 620413, 620501, 620503, 620506, 100055, 100056, 100057, 100058, 199931, 100052, 100053, 105535, 410012], \"AttrTable\": {{\"1\": {{\"Value\": 2678, \"MaxValue\": 2678}}, \"2\": {{\"Value\": 0, \"MaxValue\": 120}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 610, \"MaxValue\": 610}}, \"6\": {{\"Value\": 20019980, \"MaxValue\": 20019980}}, \"11\": {{\"Value\": 544, \"MaxValue\": 544}}, \"12\": {{\"Value\": 544, \"MaxValue\": 544}}, \"13\": {{\"Value\": 544, \"MaxValue\": 544}}, \"14\": {{\"Value\": 544, \"MaxValue\": 544}}, \"15\": {{\"Value\": 544, \"MaxValue\": 544}}, \"23\": {{\"Value\": 303, \"MaxValue\": 303}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 314, \"MaxValue\": 314}}, \"46\": {{\"Value\": 20000, \"MaxValue\": 20000}}, \"54\": {{\"Value\": 7500, \"MaxValue\": 7500}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 199, \"MaxValue\": 199}}, \"74\": {{\"Value\": 699, \"MaxValue\": 699}}}}}}, \"4\": {{\"CharacterId\": 0, \"NpcId\": 2006, \"Type\": 5, \"Level\": 99999, \"BuffIds\": [900064, 900065, 900056, 905068, 710193, 710195, 715587, 710301, 700230, 700007, 701022, 710076, 700017, 715707, 715678, 700501, 700053], \"AttrTable\": {{\"1\": {{\"Value\": 499990010, \"MaxValue\": 499990010}}, \"2\": {{\"Value\": 0, \"MaxValue\": 500}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 209, \"MaxValue\": 209}}, \"6\": {{\"Value\": 19999980, \"MaxValue\": 19999980}}, \"11\": {{\"Value\": 15, \"MaxValue\": 15}}, \"12\": {{\"Value\": 499990, \"MaxValue\": 499990}}, \"13\": {{\"Value\": 499990, \"MaxValue\": 499990}}, \"14\": {{\"Value\": 499990, \"MaxValue\": 499990}}, \"15\": {{\"Value\": 499990, \"MaxValue\": 499990}}, \"24\": {{\"Value\": 249994, \"MaxValue\": 249994}}, \"25\": {{\"Value\": 149996, \"MaxValue\": 149996}}, \"34\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"35\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"36\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"37\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 249994, \"MaxValue\": 249994}}, \"53\": {{\"Value\": 3300034, \"MaxValue\": 3300034}}, \"54\": {{\"Value\": -19000, \"MaxValue\": -19000}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}, \"75\": {{\"Value\": 0, \"MaxValue\": 100}}, \"76\": {{\"Value\": 0, \"MaxValue\": 100}}, \"77\": {{\"Value\": 0, \"MaxValue\": 100}}, \"78\": {{\"Value\": 0, \"MaxValue\": 100}}}}}}, \"5\": {{\"CharacterId\": 0, \"NpcId\": 2006, \"Type\": 5, \"Level\": 21, \"BuffIds\": [900064, 900065, 900056, 905068, 710193, 710195], \"AttrTable\": {{\"1\": {{\"Value\": 100010, \"MaxValue\": 100010}}, \"2\": {{\"Value\": 0, \"MaxValue\": 500}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 209, \"MaxValue\": 209}}, \"6\": {{\"Value\": 19999980, \"MaxValue\": 19999980}}, \"11\": {{\"Value\": 15, \"MaxValue\": 15}}, \"12\": {{\"Value\": 100, \"MaxValue\": 100}}, \"13\": {{\"Value\": 100, \"MaxValue\": 100}}, \"14\": {{\"Value\": 100, \"MaxValue\": 100}}, \"15\": {{\"Value\": 100, \"MaxValue\": 100}}, \"24\": {{\"Value\": 49, \"MaxValue\": 49}}, \"25\": {{\"Value\": 29, \"MaxValue\": 29}}, \"34\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"35\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"36\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"37\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"44\": {{\"Value\": 49, \"MaxValue\": 49}}, \"53\": {{\"Value\": 760, \"MaxValue\": 760}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}, \"75\": {{\"Value\": 0, \"MaxValue\": 100}}, \"76\": {{\"Value\": 0, \"MaxValue\": 100}}, \"77\": {{\"Value\": 0, \"MaxValue\": 100}}, \"78\": {{\"Value\": 0, \"MaxValue\": 100}}}}}}, \"6\": {{\"CharacterId\": 0, \"NpcId\": 2006, \"Type\": 5, \"Level\": 1, \"BuffIds\": [900064, 900065, 900056, 905068, 710193, 710195], \"AttrTable\": {{\"1\": {{\"Value\": 10, \"MaxValue\": 10}}, \"2\": {{\"Value\": 0, \"MaxValue\": 500}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 209, \"MaxValue\": 209}}, \"6\": {{\"Value\": 19999980, \"MaxValue\": 19999980}}, \"11\": {{\"Value\": 15, \"MaxValue\": 15}}, \"34\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"35\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"36\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"37\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"53\": {{\"Value\": 100, \"MaxValue\": 100}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}, \"75\": {{\"Value\": 0, \"MaxValue\": 100}}, \"76\": {{\"Value\": 0, \"MaxValue\": 100}}, \"77\": {{\"Value\": 0, \"MaxValue\": 100}}, \"78\": {{\"Value\": 0, \"MaxValue\": 100}}}}}}, \"7\": {{\"CharacterId\": 0, \"NpcId\": 2006, \"Type\": 5, \"Level\": 1, \"BuffIds\": [900064, 900065, 900056, 905068, 710193, 710195, 709001], \"AttrTable\": {{\"1\": {{\"Value\": 10, \"MaxValue\": 10}}, \"2\": {{\"Value\": 0, \"MaxValue\": 500}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 209, \"MaxValue\": 209}}, \"6\": {{\"Value\": 19999980, \"MaxValue\": 19999980}}, \"11\": {{\"Value\": 15, \"MaxValue\": 15}}, \"34\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"35\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"36\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"37\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"53\": {{\"Value\": 100, \"MaxValue\": 100}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}, \"75\": {{\"Value\": 0, \"MaxValue\": 100}}, \"76\": {{\"Value\": 0, \"MaxValue\": 100}}, \"77\": {{\"Value\": 0, \"MaxValue\": 100}}, \"78\": {{\"Value\": 0, \"MaxValue\": 100}}}}}}, \"8\": {{\"CharacterId\": 0, \"NpcId\": 2006, \"Type\": 5, \"Level\": 1, \"BuffIds\": [900064, 900065, 900056, 905068, 710193, 710195, 709003], \"AttrTable\": {{\"1\": {{\"Value\": 10, \"MaxValue\": 10}}, \"2\": {{\"Value\": 0, \"MaxValue\": 500}}, \"3\": {{\"Value\": 249, \"MaxValue\": 249}}, \"4\": {{\"Value\": 249, \"MaxValue\": 249}}, \"5\": {{\"Value\": 209, \"MaxValue\": 209}}, \"6\": {{\"Value\": 19999980, \"MaxValue\": 19999980}}, \"11\": {{\"Value\": 15, \"MaxValue\": 15}}, \"34\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"35\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"36\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"37\": {{\"Value\": 2000, \"MaxValue\": 2000}}, \"40\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"41\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"42\": {{\"Value\": 10000, \"MaxValue\": 10000}}, \"53\": {{\"Value\": 100, \"MaxValue\": 100}}, \"54\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"55\": {{\"Value\": 1000, \"MaxValue\": 1000}}, \"56\": {{\"Value\": 4, \"MaxValue\": 4}}, \"57\": {{\"Value\": 0, \"MaxValue\": 100}}, \"68\": {{\"Value\": 99, \"MaxValue\": 99}}, \"74\": {{\"Value\": 349, \"MaxValue\": 349}}, \"75\": {{\"Value\": 0, \"MaxValue\": 100}}, \"76\": {{\"Value\": 0, \"MaxValue\": 100}}, \"77\": {{\"Value\": 0, \"MaxValue\": 100}}, \"78\": {{\"Value\": 0, \"MaxValue\": 100}}}}}}}}, \"UrgentEnventId\": 0, \"ClientAssistInfo\": null, \"FlopRewardList\": [], \"ArenaResult\": null, \"MultiRewardGoodsList\": [], \"ChallengeCount\": 0, \"UnionKillResult\": null, \"InfestorBossFightResult\": null, \"GuildBossFightResult\": null, \"WorldBossFightResult\": null, \"BossSingleFightResult\": null, \"NieRBossFightResult\": null, \"TRPGBossFightResult\": null, \"ExpeditionFightResult\": null, \"ChessPursuitResult\": [], \"StrongholdFightResult\": null, \"AreaWarFightResult\": null, \"GuildWarFightResult\": null, \"ReformFightResult\": null, \"KillZoneStageResult\": null, \"EpisodeFightResult\": {{}}, \"StTargetStageFightResult\": null, \"StMapTierDataOperation\": null, \"SimulateTrainFightResult\": null, \"SuperSmashBrosBattleResult\": null, \"SpecialTrainRankFightResult\": null, \"DoubleTowerFightResult\": null, \"TaikoMasterSettleResult\": null, \"MultiDimFightResult\": null, \"MoewarParkourSettleResult\": null, \"SpecialTrainBreakthroughResult\": null}}}}")), packet.Id);
        }
    }
}
