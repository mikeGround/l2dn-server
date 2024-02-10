﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2Dn.GameServer.Db;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(ClanId))]
[Index(nameof(Created))]
public class Character
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int AccountId { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountRef Account { get; set; } = null!;

    public int AccessLevel { get; set; }
    
    public byte SlotIndex { get; set; }
    
    [MaxLength(70)]
    public string Name { get; set; } = string.Empty;

    public CharacterClass Class { get; set; } // Race and base class are calculated from the class
    public byte Level { get; set; } = 1;
    public long Exp { get; set; }
    public long ExpBeforeDeath { get; set; }
    public long Sp { get; set; }
    
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxCp { get; set; }
    public int CurrentCp { get; set; }
    public int MaxMp { get; set; }
    public int CurrentMp { get; set; }
    public int VitalityPoints { get; set; }
    public int PcCafePoints { get; set; }

    // Appearance
    public byte Face { get; set; }
    public byte HairStyle { get; set; }
    public byte HairColor { get; set; }
    public Sex Sex { get; set; }
    public int? TransformId { get; set; }
    
    [MaxLength(40)]
    public string? Title { get; set; }
    
    public int NameColor { get; set; }
    public int TitleColor { get; set; }

    // Coordinates
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int Heading { get; set; }

    // Scores
    public int Reputation { get; set; }
    public int Fame { get; set; }
    public int RaidBossPoints { get; set; }
    public int PvpKills { get; set; }
    public int PkKills { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }

    
    // Clan
    public int? ClanId { get; set; }
    
    [ForeignKey(nameof(ClanId))]
    public Clan? Clan { get; set; }
    
    public int ClanPrivileges { get; set; }
    public int SubPledge { get; set; }
    
    public int? SponsorId { get; set; }
    
    [ForeignKey(nameof(SponsorId))]
    public Character? Sponsor { get; set; }

    public byte LevelJoinedAcademy { get; set; } = 1;

    // Some data
    public DateTime Created { get; set; }
    public DateTime? LastLogin { get; set; }
    public TimeSpan OnlineTime { get; set; }
    public DateTime? DeleteTime { get; set; }
    public DateTime? ClanCreateExpiryTime { get; set; }
    public DateTime? ClanJoinExpiryTime { get; set; }
    
    [MaxLength(2)]
    public string? Language { get; set; }
}