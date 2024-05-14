﻿using System.Xml.Serialization;

namespace L2Dn.Model.DataPack;

public class XmlEnchantScrollGroupRate
{
    [XmlElement("item")]
    public List<XmlEnchantScrollGroupRateItem> Items { get; set; } = [];

    [XmlAttribute("group")]
    public string Group { get; set; } = string.Empty;
}