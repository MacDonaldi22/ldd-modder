﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace LDDModder.LDD.Palettes
{
    public class Assembly : PaletteItem
    {
        [XmlElement("Part")]
        public List<Part> Parts { get; set; }

        public override bool HasDecorations
        {
            get
            {
                return Parts.Any(p => p.Decorations.Count > 0);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Assembly"/> class.
        /// </summary>
        public Assembly()
            : base()
        {
            Parts = new List<Part>();
        }

        public Assembly(int designID, string elementID, int quantity)
            : base(designID, quantity, elementID)
        {
            Parts = new List<Part>();
        }

        public Assembly(int designID, string elementID, int quantity, IEnumerable<Part> parts)
            : base(designID, quantity, elementID)
        {
            Parts = new List<Part>(parts);
        }

        public override object Clone()
        {
            return new Assembly(DesignID, ElementID, Quantity, Parts.Clone());
        }
    }
}
