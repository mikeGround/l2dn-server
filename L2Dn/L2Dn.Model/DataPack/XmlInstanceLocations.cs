﻿using System.Xml.Serialization;

namespace L2Dn.Model.DataPack;

public class XmlInstanceLocations
{
    [XmlElement("enter")]
    public XmlInstanceEnterLocations? EnterLocations { get; set; }

    [XmlElement("exit")]
    public XmlInstanceExitLocations? ExitLocations { get; set; }
}