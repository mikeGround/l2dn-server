﻿using L2Dn.Packages.DatDefinitions.Annotations;
using L2Dn.Packages.DatDefinitions.Definitions.Shared;

namespace L2Dn.Packages.DatDefinitions.Definitions;

[ChronicleRange(Chronicles.Fafurion, Chronicles.Latest)]
public sealed class QuestNameV8
{
    [ArrayLengthType(ArrayLengthType.Int32)]
    public QuestNameRecord[] Records { get; set; } = Array.Empty<QuestNameRecord>();
    
    public sealed class QuestNameRecord
    {
        public uint Tag { get; set; }
        public uint Id { get; set; }
        public int Level { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public uint[] GoalIds { get; set; } = Array.Empty<uint>();
        public uint[] GoalTypes { get; set; } = Array.Empty<uint>();
        public uint[] GoalNums { get; set; } = Array.Empty<uint>();
        public Location TargetLoc { get; set; } = new Location();
        public Location[] AddTargetLocs { get; set; } = Array.Empty<Location>();
        public uint[] Levels { get; set; } = Array.Empty<uint>();
        public uint LvlMin { get; set; }
        public uint LvlMax { get; set; }
        public uint JournalDisp { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public uint GetItemInQuest { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint[] StartNpcIds { get; set; } = Array.Empty<uint>();
        public Location StartNpcLoc { get; set; } = new Location();
        public string Requirements { get; set; } = string.Empty;
        public string Intro { get; set; } = string.Empty;
        public uint[] ClassLimits { get; set; } = Array.Empty<uint>();
        public uint[] HaveItems { get; set; } = Array.Empty<uint>();
        public uint ClanPetQuest { get; set; }
        public uint ClearedQuest { get; set; }
        public int MarkType { get; set; }
        public uint CategoryId { get; set; }
        public uint PriorityLevel { get; set; }
        public uint SearchZoneId { get; set; }
        public uint IsCategory { get; set; }
        public uint[] RewardIds { get; set; } = Array.Empty<uint>();
        public long[] RewardNums { get; set; } = Array.Empty<long>();
        public uint[] PreLevels { get; set; } = Array.Empty<uint>();
        public uint FactionId { get; set; }
        public uint FactionLevelMin { get; set; }
        public uint FactionLevelMax { get; set; }
    }
}