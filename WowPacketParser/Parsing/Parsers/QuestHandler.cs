using System;
using System.Collections.Generic;
using WowPacketParser.Enums;
using WowPacketParser.Enums.Version;
using WowPacketParser.Misc;
using WowPacketParser.SQL;


namespace WowPacketParser.Parsing.Parsers
{
    public static class QuestHandler
    {
        [Parser(Opcode.CMSG_QUEST_QUERY)]
        [Parser(Opcode.CMSG_PUSHQUESTTOPARTY)]
        public static void HandleQuestQuery(Packet packet)
        {
            packet.ReadInt32("Entry");
        }

        [Parser(Opcode.SMSG_QUEST_QUERY_RESPONSE)]
        public static void HandleQuestQueryResponse(Packet packet)
        {
            var id = packet.ReadEntry("Quest ID");
            if (id.Value) // entry is masked
                return;

            var method = packet.ReadEnum<QuestMethod>("Method", TypeCode.Int32);

            var level = packet.ReadInt32("Level");

            var minLevel = 0;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                minLevel = packet.ReadInt32("Min Level");

            var sort = packet.ReadEnum<QuestSort>("Sort", TypeCode.Int32);

            var type = packet.ReadEnum<QuestType>("Type", TypeCode.Int32);

            var players = packet.ReadInt32("Suggested Players");

            var factId = new int[2];
            var factRep = new int[2];
            for (var i = 0; i < 2; i++)
            {
                factId[i] = packet.ReadInt32("Required Faction ID", i);

                factRep[i] = packet.ReadInt32("Required Faction Rep", i);
            }

            var nextQuest = packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Next Chain Quest");

            var xpId = 0;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                xpId = packet.ReadInt32("Quest XP ID");

            var rewReqMoney = packet.ReadInt32("Reward/Required Money");

            var rewMoneyMaxLvl = packet.ReadInt32("Reward Money Max Level");

            var rewSpell = packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Reward Spell");

            var rewSpellCast = packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Reward Spell Cast");

            var rewHonor = packet.ReadInt32("Reward Honor");

            var rewHonorBonus = 0f;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                rewHonorBonus = packet.ReadSingle("Reward Honor Multiplier");

            var srcItemId = packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Source Item ID");

            var flags = (QuestFlag)(packet.ReadInt32() | 0xFFFF);
            packet.Writer.WriteLine("Flags: " + flags);

            var titleId = 0;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V2_4_0_8089))
                titleId = packet.ReadInt32("Title ID");

            var reqPlayerKills = 0;
            var bonusTalents = 0;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
            {
                reqPlayerKills = packet.ReadInt32("Required Player Kills");
                bonusTalents = packet.ReadInt32("Bonus Talents");
            }

            var bonusArenaPoints = 0;
            var bonusUnk = 0;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
            {
                bonusArenaPoints = packet.ReadInt32("Bonus Arena Points");
                bonusUnk = packet.ReadInt32("Unk Int32");
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_2_14545)) // Probably added earlier
            {
                packet.ReadUInt32("RewSkillPoints");
                packet.ReadUInt32("RewRepMask");
                packet.ReadUInt32("QuestGiverPortrait");
                packet.ReadUInt32("QuestTurnInPortrait");
                packet.ReadUInt32("UnknownUInt32"); // This was added on 422
            }

            var rewItemId = new int[4];
            var rewItemCnt = new int[4];
            for (var i = 0; i < 4; i++)
            {
                rewItemId[i] = packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Reward Item ID", i);
                rewItemCnt[i] = packet.ReadInt32("Reward Item Count", i);
            }

            var rewChoiceItemId = new int[6];
            var rewChoiceItemCnt = new int[6];
            for (var i = 0; i < 6; i++)
            {
                rewChoiceItemId[i] = packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Reward Choice Item ID", i);
                rewChoiceItemCnt[i] = packet.ReadInt32("Reward Choice Item Count", i);
            }

            var rewFactionId = new int[5];
            var rewRepIdx = new int[5];
            var rewRepOverride = new int[5];
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
            {
                for (var i = 0; i < 5; i++)
                    rewFactionId[i] = packet.ReadInt32("Reward Faction ID", i);

                for (var i = 0; i < 5; i++)
                    rewRepIdx[i] = packet.ReadInt32("Reward Reputation ID", i);

                for (var i = 0; i < 5; i++)
                    rewRepOverride[i] = packet.ReadInt32("Reward Reputation ID", i);
            }

            var pointMap = packet.ReadInt32("Point Map ID");

            var pointX = packet.ReadSingle("Point X");

            var pointY = packet.ReadSingle("Point Y");

            var pointOpt = packet.ReadInt32("Point Opt");

            var title = packet.ReadCString("Title");

            var objectives = packet.ReadCString("Objectives");

            var details = packet.ReadCString("Details");

            var endText = packet.ReadCString("End Text");

            var returnText = string.Empty;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                returnText = packet.ReadCString("Return Text");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_2_14545)) // Probably earlier
                packet.ReadCString("Completed Text");

            var reqId = new KeyValuePair<int, bool>[4];
            var reqCnt = new int[4];
            var srcId = new int[4];
            var srcCnt = new int[4];

            var reqItemFieldCount = ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_8_9464) ? 6 : 4;
            var reqItemId = new int[reqItemFieldCount];
            var reqItemCnt = new int[reqItemFieldCount];

            for (var i = 0; i < 4; i++)
            {
                reqId[i] = packet.ReadEntry();
                var isGO = reqId[i].Value;

                packet.Writer.WriteLine("[" + i + "] Required " + (isGO ? "GO" : "NPC") +
                    " ID: " + StoreGetters.GetName(isGO ? StoreNameType.GameObject : StoreNameType.Unit, (int)reqId[i].Key));

                reqCnt[i] = packet.ReadInt32("Required Count", i);

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                    srcId[i] = packet.ReadInt32("Source ID", i);

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                    srcCnt[i] = packet.ReadInt32("Source Count", i);

                if (ClientVersion.RemovedInVersion(ClientVersionBuild.V3_0_8_9464))
                {
                    reqItemId[i] = packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Required Item ID", i);
                    reqItemCnt[i] = packet.ReadInt32("Required Item Count", i);
                }
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_8_9464))
            {
                for (var i = 0; i < reqItemFieldCount; i++)
                {
                    reqItemId[i] = packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Required Item ID", i);
                    reqItemCnt[i] = packet.ReadInt32("Required Item Count", i);
                }
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_0_14333)) // Probably earlier
                packet.ReadEntryWithName<UInt32>(StoreNameType.Spell, "Required Spell");

            var objectiveText = new string[4];
            for (var i = 0; i < 4; i++)
                objectiveText[i] = packet.ReadCString("Objective Text", i);

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_0_14333))
            {
                for (int i = 0; i < 4; ++i)
                {
                    packet.ReadUInt32("Reward Currency ID", i);
                    packet.ReadUInt32("Reward Currency Count", i);
                }
                for (int i = 0; i < 4; ++i)
                {
                    packet.ReadUInt32("Required Currency ID", i);
                    packet.ReadUInt32("Required Currency Count", i);
                }
                packet.ReadCString("QuestGiver Text Window");
                packet.ReadCString("QuestGiver Target Name");
                packet.ReadCString("QuestTurn Text Window");
                packet.ReadCString("QuestTurn Target Name");

                packet.ReadUInt32("Sound Accept");

                packet.ReadUInt32("Sound TurnIn");
            }

            SQLStore.WriteData(SQLStore.Quests.GetCommand(id.Key, method, level, minLevel, sort, type,
                players, factId, factRep, nextQuest, xpId, rewReqMoney, rewMoneyMaxLvl,
                rewSpell, rewSpellCast, rewHonor, rewHonorBonus, srcItemId, flags, titleId,
                reqPlayerKills, bonusTalents, bonusArenaPoints, bonusUnk, rewItemId, rewItemCnt,
                rewChoiceItemId, rewChoiceItemCnt, rewFactionId, rewRepIdx, rewRepOverride,
                pointMap, pointX, pointY, pointOpt, title, objectives, details, endText,
                returnText, reqId, reqCnt, srcId, srcCnt, reqItemId, reqItemCnt, objectiveText));
        }

        [Parser(Opcode.CMSG_QUEST_POI_QUERY)]
        public static void HandleQuestPoiQuery(Packet packet)
        {
            var count = packet.ReadInt32("Count");

            for (var i = 0; i < count; i++)
                packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
        }

        [Parser(Opcode.SMSG_QUEST_POI_QUERY_RESPONSE)]
        public static void HandleQuestPoiQueryResponse(Packet packet)
        {
            var count = packet.ReadInt32("Count");

            for (var i = 0; i < count; i++)
            {
                var questId = packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");

                var counter = packet.ReadInt32("[" + i + "] POI Counter");
                for (var j = 0; j < counter; j++)
                {
                    var idx = packet.ReadInt32("POI Index", i, j);
                    var objIndex = packet.ReadInt32("Objective Index", i, j);

                    var mapId = packet.ReadEntryWithName<Int32>(StoreNameType.Map, "Map ID");
                    var wmaId = packet.ReadInt32("World Map Area", i, j);
                    var floorId = packet.ReadInt32("Floor Id", i, j);
                    var unk2 = packet.ReadInt32("Unk Int32 2", i, j);
                    var unk3 = packet.ReadInt32("Unk Int32 3", i, j);

                    SQLStore.WriteData(SQLStore.QuestPois.GetCommand(questId, idx, objIndex, mapId, wmaId,
                        floorId, unk2, unk3));

                    var pointsSize = packet.ReadInt32("Points counter", i, j);
                    for (var k = 0; k < pointsSize; k++)
                    {
                        var pointX = packet.ReadInt32("Point X", i, j, k);
                        var pointY = packet.ReadInt32("Point Y", i, j, k);
                        SQLStore.WriteData(SQLStore.QuestPoiPoints.GetCommand(questId, idx, objIndex, pointX,
                            pointY));
                    }
                }
            }
        }

        [Parser(Opcode.SMSG_QUEST_FORCE_REMOVE)]
        [Parser(Opcode.CMSG_QUEST_CONFIRM_ACCEPT)]
        [Parser(Opcode.SMSG_QUESTUPDATE_FAILED)]
        [Parser(Opcode.SMSG_QUESTUPDATE_FAILEDTIMER)]
        [Parser(Opcode.SMSG_QUESTUPDATE_COMPLETE)]
        public static void HandleQuestForceRemoved(Packet packet)
        {
            packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
        }

        [Parser(Opcode.SMSG_QUERY_QUESTS_COMPLETED_RESPONSE)]
        public static void HandleQuestCompletedResponse(Packet packet)
        {
            packet.ReadInt32("Count");
            // Prints ~4k lines of quest IDs, should be DEBUG only or something...
            /*
            for (var i = 0; i < count; i++)
                packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Rewarded Quest");
            */
            packet.Writer.WriteLine("Packet is currently not printed");
            packet.ReadBytes((int)packet.GetLength());
        }

        [Parser(Opcode.CMSG_QUESTGIVER_STATUS_QUERY)]
        [Parser(Opcode.CMSG_QUESTGIVER_HELLO)]
        [Parser(Opcode.CMSG_QUESTGIVER_QUEST_AUTOLAUNCH)]
        public static void HandleQuestgiverStatusQuery(Packet packet)
        {
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.SMSG_QUESTGIVER_QUEST_LIST)]
        public static void HandleQuestgiverQuestList(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadCString("Title");
            packet.ReadUInt32("Delay");
            packet.ReadUInt32("Emote");

            var count = packet.ReadByte("Count");
            for (var i = 0; i < count; i++)
            {
                packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
                packet.ReadUInt32("Quest Icon", i);
                packet.ReadInt32("Quest Level", i);
                packet.ReadEnum<QuestFlag>("Quest Flags", TypeCode.UInt32, i);
                packet.ReadBoolean("Change icon", i);
                packet.ReadCString("Title", i);
            }

        }

        [Parser(Opcode.CMSG_QUESTGIVER_QUERY_QUEST)]
        public static void HandleQuestgiverQueryQuest(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
        }

        [Parser(Opcode.CMSG_QUESTGIVER_ACCEPT_QUEST)]
        public static void HandleQuestgiverAcceptQuest(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_1_2_9901))
                packet.ReadUInt32("Unk UInt32");
        }

        [Parser(Opcode.SMSG_QUESTGIVER_QUEST_DETAILS)]
        public static void HandleQuestgiverDetails(Packet packet)
        {
            packet.ReadGuid("GUID1");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                packet.ReadGuid("GUID2");

            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
            packet.ReadCString("Title");
            packet.ReadCString("Details");
            packet.ReadCString("Objectives");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
            {
                packet.ReadCString("Target Text Window");
                packet.ReadCString("Target Name");
                packet.ReadUInt16("Unknown UInt16");
                packet.ReadUInt32("Quest Giver Portrait Id");
                packet.ReadUInt32("Unknown UInt32");
            }

            var flags = QuestFlag.None;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
            {
                packet.ReadByte("AutoAccept");
                flags = packet.ReadEnum<QuestFlag>("Quest Flags", TypeCode.UInt32);
            }
            else
                packet.ReadInt32("AutoAccept");

            packet.ReadUInt32("Suggested Players");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                packet.ReadByte("Unknown byte");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
            {
                packet.ReadByte("Start Type");
                packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Required Spell");
            }

            if ((flags & QuestFlag.HiddenRewards) > 0)
            {
                packet.ReadUInt32("Hidden Chosen Items");
                packet.ReadUInt32("Hidden Items");
                packet.ReadUInt32("Hidden Money");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_2_10482))
                    packet.ReadUInt32("Hidden XP");
            }
            else
            {
                var choiceCount = packet.ReadUInt32("Choice Item Count");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
                {
                    for (var i = 0; i < choiceCount; i++)
                        packet.ReadUInt32("Choice Item Id", i);
                    for (var i = 0; i < choiceCount; i++)
                        packet.ReadUInt32("Choice Item Count", i);
                    for (var i = 0; i < choiceCount; i++)
                        packet.ReadUInt32("Choice Item Display Id", i);

                    var rewardCount = packet.ReadUInt32("Reward Item Count");
                    for (var i = 0; i < rewardCount; i++)
                        packet.ReadUInt32("Reward Item Id", i);
                    for (var i = 0; i < rewardCount; i++)
                        packet.ReadUInt32("Reward Item Count", i);
                    for (var i = 0; i < rewardCount; i++)
                        packet.ReadUInt32("Reward Item Display Id", i);
                }
                else
                {
                    for (var i = 0; i < choiceCount; i++)
                    {
                        packet.ReadUInt32("Choice Item Id", i);
                        packet.ReadUInt32("Choice Item Count", i);
                        packet.ReadUInt32("Choice Item Display Id", i);
                    }

                    var rewardCount = packet.ReadUInt32("Reward Item Count");
                    for (var i = 0; i < rewardCount; i++)
                    {
                        packet.ReadUInt32("Reward Item Id", i);
                        packet.ReadUInt32("Reward Item Count", i);
                        packet.ReadUInt32("Reward Item Display Id", i);
                    }
                }

                packet.ReadUInt32("Money");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_2_10482))
                    packet.ReadUInt32("XP");
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
            {
                packet.ReadUInt32("Title Id");
                packet.ReadUInt32("Unknown UInt32");
                packet.ReadUInt32("Unknown UInt32");
                packet.ReadUInt32("Bonus Talents");
                packet.ReadUInt32("Unknown UInt32");
                packet.ReadUInt32("Unknown UInt32");
            }
            else
            {
                packet.ReadUInt32("Honor Points");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                    packet.ReadSingle("Honor Multiplier");

                packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Id");
                packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Cast Id");
                packet.ReadUInt32("Title Id");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                    packet.ReadUInt32("Bonus Talents");

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
                {
                    packet.ReadUInt32("Arena Points");
                    packet.ReadUInt32("Unk UInt32");
                }
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_0_10958))
            {
                for (var i = 0; i < 5; i++)
                    packet.ReadUInt32("[" + i + "] Reputation Faction");

                for (var i = 0; i < 5; i++)
                    packet.ReadUInt32("[" + i + "] Reputation Value Id");

                for (var i = 0; i < 5; i++)
                    packet.ReadInt32("[" + i + "] Reputation Value");
            }

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_1_13164))
            {
                packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Id");
                packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Cast Id");

                for (var i = 0; i < 4; i++)
                    packet.ReadUInt32("[" + i + "] " + "Unknown UInt32 1");
                for (var i = 0; i < 4; i++)
                    packet.ReadUInt32("[" + i + "] " + "Unknown UInt32 2");

                packet.ReadUInt32("Unknown UInt32");
                packet.ReadUInt32("Unknown UInt32");
            }

            var emoteCount = packet.ReadUInt32("Quest Emote Count");
            for (var i = 0; i < emoteCount; i++)
            {
                packet.ReadUInt32("[" + i + "] Emote Id");
                packet.ReadUInt32("[" + i + "] Emote Delay (ms)");
            }
        }

        [Parser(Opcode.CMSG_QUESTGIVER_COMPLETE_QUEST)]
        [Parser(Opcode.CMSG_QUESTGIVER_REQUEST_REWARD)]
        public static void HandleQuestcompleteQuest(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
        }

        [Parser(Opcode.SMSG_QUESTGIVER_REQUEST_ITEMS)]
        public static void HandleQuestRequestItems(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
            packet.ReadCString("Title");
            packet.ReadCString("Text");
            packet.ReadUInt32("Unk UInt32 1");
            packet.ReadUInt32("Emote");
            packet.ReadUInt32("Close Window on Cancel");
            packet.ReadEnum<QuestFlag>("Quest Flags", TypeCode.UInt32);
            packet.ReadUInt32("Suggested Players");
            packet.ReadUInt32("Money");
            var count = packet.ReadUInt32("Required Item Count");
            for (var i = 0; i < count; i++)
            {
                packet.ReadUInt32("[" + i + "] Required Item Id");
                packet.ReadUInt32("[" + i + "] Required Item Count");
                packet.ReadUInt32("[" + i + "] Required Item Display Id");
            }
            packet.ReadUInt32("Unk UInt32 2");
            packet.ReadUInt32("Unk UInt32 3");
            packet.ReadUInt32("Unk UInt32 4");
            packet.ReadUInt32("Unk UInt32 5");
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_2_14545))
            {
                packet.ReadUInt32("Unk UInt32 6");
                packet.ReadUInt32("Unk UInt32 7");
            }
        }

        [Parser(Opcode.SMSG_QUESTGIVER_OFFER_REWARD)]
        public static void HandleQuestOfferReward(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
            packet.ReadCString("Title");
            packet.ReadCString("Text");
            packet.ReadByte("Auto Finish");
            packet.ReadEnum<QuestFlag>("Quest Flags", TypeCode.UInt32);
            packet.ReadUInt32("Suggested Players");
            var count1 = packet.ReadUInt32("Emote Count");
            for (var i = 0; i < count1; i++)
            {
                packet.ReadUInt32("[" + i + "] Emote Delay");
                packet.ReadUInt32("[" + i + "] Emote Id");
            }

            var count2 = packet.ReadUInt32("Choice Item Count");
            for (var i = 0; i < count2; i++)
            {
                packet.ReadUInt32("[" + i + "] Choice Item Id");
                packet.ReadUInt32("[" + i + "] Choice Item Count");
                packet.ReadUInt32("[" + i + "] Choice Item Display Id");
            }

            var count3 = packet.ReadUInt32("Reward Item Count");
            for (var i = 0; i < count3; i++)
            {
                packet.ReadUInt32("[" + i + "] Reward Item Id");
                packet.ReadUInt32("[" + i + "] Reward Item Count");
                packet.ReadUInt32("[" + i + "] Reward Item Display Id");
            }
            packet.ReadUInt32("Money");
            packet.ReadUInt32("XP");

            packet.ReadUInt32("Honor Points");
            packet.ReadSingle("Honor Multiplier");
            packet.ReadUInt32("Unk UInt32 1");
            packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Id");
            packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell Cast Id");
            packet.ReadUInt32("Title Id");
            packet.ReadUInt32("Bonus Talent");
            packet.ReadUInt32("Arena Points");
            packet.ReadUInt32("Unk Uint32");

            for (var i = 0; i < 5; i++)
                packet.ReadUInt32("[" + i + "] Reputation Faction");

            for (var i = 0; i < 5; i++)
                packet.ReadUInt32("[" + i + "] Reputation Value Id");

            for (var i = 0; i < 5; i++)
                packet.ReadInt32("[" + i + "] Reputation Value");
        }

        [Parser(Opcode.CMSG_QUESTGIVER_CHOOSE_REWARD)]
        public static void HandleQuestChooseReward(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
            packet.ReadUInt32("Reward");
        }

        [Parser(Opcode.SMSG_QUESTGIVER_QUEST_INVALID)]
        public static void HandleQuestInvalid(Packet packet)
        {
            packet.ReadEnum<QuestReasonType>("Reason", TypeCode.UInt32);
        }

        [Parser(Opcode.SMSG_QUESTGIVER_QUEST_FAILED)]
        public static void HandleQuestFailed(Packet packet)
        {
            packet.ReadEntryWithName<UInt32>(StoreNameType.Quest, "Quest ID");
            packet.ReadEnum<QuestReasonType>("Reason", TypeCode.UInt32);
        }

        [Parser(Opcode.SMSG_QUESTGIVER_QUEST_COMPLETE)]
        public static void HandleQuestCompleted(Packet packet)
        {
            packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
            packet.ReadInt32("Reward");
            packet.ReadInt32("Money");
            var honor = packet.ReadInt32();
            if (honor < 0)
                packet.Writer.WriteLine("Honor: " + honor);

            var talentpoints = packet.ReadInt32();
            if (talentpoints < 0)
                packet.Writer.WriteLine("Talentpoints: " + talentpoints);

            var arenapoints = packet.ReadInt32();
            if (arenapoints < 0)
                packet.Writer.WriteLine("Arenapoints: " + arenapoints);
        }

        [Parser(Opcode.CMSG_QUESTLOG_SWAP_QUEST)]
        public static void HandleQuestSwapQuest(Packet packet)
        {
            packet.ReadByte("Slot 1");
            packet.ReadByte("Slot 2");
        }

        [Parser(Opcode.CMSG_QUESTLOG_REMOVE_QUEST)]
        public static void HandleQuestRemoveQuest(Packet packet)
        {
            packet.ReadByte("Slot");
        }

        [Parser(Opcode.SMSG_QUESTUPDATE_ADD_KILL)]
        [Parser(Opcode.SMSG_QUESTUPDATE_ADD_ITEM)]
        public static void HandleQuestUpdateAdd(Packet packet)
        {
            packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
            packet.ReadInt32("Entry");
            packet.ReadInt32("Count");
            packet.ReadInt32("Required Count");
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.SMSG_QUESTGIVER_STATUS)]
        [Parser(Opcode.SMSG_QUESTGIVER_STATUS_MULTIPLE)]
        public static void HandleQuestgiverStatus(Packet packet)
        {
            uint count = 1;
            if (packet.Opcode == Opcodes.GetOpcode(Opcode.SMSG_QUESTGIVER_STATUS_MULTIPLE))
                count = packet.ReadUInt32("Count");

            var typeCode = ClientVersion.Build >= ClientVersionBuild.V4_2_2_14545 ? TypeCode.Int32 : TypeCode.Byte;

            for (int i = 0; i < count; i++)
            {
                packet.ReadGuid("GUID", i);
                packet.ReadEnum<QuestGiverStatus>("Status", typeCode, i);
            }
        }

        
        public static void HandleQuestgiverStatusMultiple(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            for (var i = 0; i < count; i++)
            {
                packet.ReadGuid("GUID");
                packet.ReadEnum<QuestGiverStatus>("Status", TypeCode.Byte);
            }
        }

        [Parser(Opcode.SMSG_QUESTUPDATE_ADD_PVP_KILL)]
        public static void HandleQuestupdateAddPvpKill(Packet packet)
        {
            packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
            packet.ReadInt32("Count");
            packet.ReadInt32("Required Count");
        }

        [Parser(Opcode.SMSG_QUEST_CONFIRM_ACCEPT)]
        public static void HandleQuestConfirAccept(Packet packet)
        {
            packet.ReadEntryWithName<Int32>(StoreNameType.Quest, "Quest ID");
            packet.ReadCString("Title");
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.MSG_QUEST_PUSH_RESULT)]
        public static void HandleQuestPushResult(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadEnum<QuestPartyResult>("Result", TypeCode.Byte);
        }

        [Parser(Opcode.CMSG_QUERY_QUESTS_COMPLETED)]
        [Parser(Opcode.SMSG_QUESTLOG_FULL)]
        [Parser(Opcode.CMSG_QUESTGIVER_CANCEL)]
        [Parser(Opcode.CMSG_QUESTGIVER_STATUS_MULTIPLE_QUERY)]
        public static void HandleQuestZeroLengthPackets(Packet packet)
        {
        }

        //[Parser(Opcode.CMSG_START_QUEST)]
        //[Parser(Opcode.CMSG_FLAG_QUEST)]
        //[Parser(Opcode.CMSG_FLAG_QUEST_FINISH)]
        //[Parser(Opcode.CMSG_CLEAR_QUEST)]
    }
}
