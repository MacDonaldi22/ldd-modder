﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LDDModder.LDD.Palettes
{
    public partial class Palette
    {
        [XmlRoot("Brick")]
        public class Brick : PaletteItem
        {
            [XmlAttribute("materialID")]
            public int MaterialID { get; set; }

            [XmlElement("SubMaterial")]
            public List<SubMaterial> SubMaterials { get; set; }

            [XmlElement("Decoration")]
            public List<Decoration> Decorations { get; set; }

            public override bool HasDecorations => Decorations.Any();
        }
    }
}
