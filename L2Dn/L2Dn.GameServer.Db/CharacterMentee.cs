﻿using Microsoft.EntityFrameworkCore;

namespace L2Dn.GameServer.Db;

[PrimaryKey(nameof(CharacterId), nameof(MentorId))]
public class CharacterMentee
{
    public int CharacterId { get; set; }
    public int MentorId { get; set; }
}