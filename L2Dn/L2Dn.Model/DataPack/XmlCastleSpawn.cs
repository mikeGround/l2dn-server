﻿using System.Xml.Serialization;
using L2Dn.Model.Enums;

namespace L2Dn.Model.DataPack;

public class XmlCastleSpawn: XmlLocationWithHeading
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("castleSide")]
    public CastleSide CastleSide { get; set; } = CastleSide.NEUTRAL;
}