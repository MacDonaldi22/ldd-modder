﻿using LDDModder.LDD;
using LDDModder.LDD.Files;
using LDDModder.LDD.Palettes;
using LDDModder.PaletteMaker.Rebrickable;
using LDDModder.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LDDModder.PaletteMaker
{
    public static class PaletteBuilder
    {
        private static bool _Initialized;
        static Palette LddPalette;
        static List<string> LddBricks;
        static List<string> LddAssemblies;
        static Dictionary<int, int> RbColorToLDD;

        public static bool Initialized
        {
            get { return _Initialized; }
        }

        static PaletteBuilder()
        {
            LddBricks = new List<string>();
            LddAssemblies = new List<string>();
            RbColorToLDD = new Dictionary<int, int>();
            _Initialized = false;
            LddPalette = null;
        }

        public static void Initialize()
        {
            if (Initialized)
                return;

            InitColorTable();

            LddPalette = XSerializable.LoadFrom<Palette>("LDD.PAXML");

            string[] brickFileNames;
            string[] assemblyFileNames;

            if (LDDManager.IsLifExtracted(LifInstance.Database))
            {
                brickFileNames = Directory.GetFiles(LDDManager.GetDirectory(LDDManager.DbDirectories.Primitives), "*.xml");
                assemblyFileNames = Directory.GetFiles(LDDManager.GetDirectory(LDDManager.DbDirectories.Assemblies), "*.lxfml");
            }
            else
            {
                using (var lifFile = LDDManager.OpenLif(LifInstance.Database))
                {
                    brickFileNames = lifFile.Entries.OfType<LifFile.FileEntry>().Where(f => f.FullPath.Contains("Primitives") && f.Name.EndsWith(".xml")).Select(x => x.Name).ToArray();
                    assemblyFileNames = lifFile.Entries.OfType<LifFile.FileEntry>().Where(f => f.FullPath.Contains("Assemblies") && f.Name.EndsWith(".lxfml")).Select(x => x.Name).ToArray();
                    lifFile.Close();
                }
            }

            for (int i = 0; i < brickFileNames.Length; i++)
                LddBricks.Add(Path.GetFileNameWithoutExtension(brickFileNames[i]));
            for (int i = 0; i < assemblyFileNames.Length; i++)
                LddAssemblies.Add(Path.GetFileNameWithoutExtension(assemblyFileNames[i]));

            _Initialized = true;
        }

        private static void InitColorTable()
        {
            RbColorToLDD.Add(0, 26);
            RbColorToLDD.Add(1, 23);
            RbColorToLDD.Add(2, 28);
            RbColorToLDD.Add(3, 107);
            RbColorToLDD.Add(4, 21);
            RbColorToLDD.Add(5, 221);
            RbColorToLDD.Add(6, 25);
            RbColorToLDD.Add(7, 2);
            RbColorToLDD.Add(8, 27);
            RbColorToLDD.Add(9, 45);
            RbColorToLDD.Add(10, 37);
            RbColorToLDD.Add(11, 116);
            RbColorToLDD.Add(12, 4);
            RbColorToLDD.Add(13, 1007);
            RbColorToLDD.Add(14, 24);
            RbColorToLDD.Add(15, 1);
            RbColorToLDD.Add(17, 6);
            RbColorToLDD.Add(18, 3);
            RbColorToLDD.Add(19, 5);
            RbColorToLDD.Add(20, 39);
            RbColorToLDD.Add(21, 294);
            RbColorToLDD.Add(22, 104);
            RbColorToLDD.Add(23, 196);
            RbColorToLDD.Add(25, 106);
            RbColorToLDD.Add(26, 124);
            RbColorToLDD.Add(27, 119);
            RbColorToLDD.Add(28, 138);
            RbColorToLDD.Add(29, 222);
            RbColorToLDD.Add(30, 324);
            RbColorToLDD.Add(31, 325);
            RbColorToLDD.Add(33, 43);
            RbColorToLDD.Add(34, 48);
            RbColorToLDD.Add(35, 227);
            RbColorToLDD.Add(36, 41);
            RbColorToLDD.Add(40, 111);
            RbColorToLDD.Add(41, 42);
            RbColorToLDD.Add(42, 49);
            RbColorToLDD.Add(43, 229);
            RbColorToLDD.Add(45, 113);
            RbColorToLDD.Add(46, 44);
            RbColorToLDD.Add(47, 40);
            RbColorToLDD.Add(52, 126);
            RbColorToLDD.Add(54, 157);
            RbColorToLDD.Add(57, 47);
            RbColorToLDD.Add(68, 36);
            RbColorToLDD.Add(69, 198);
            RbColorToLDD.Add(70, 192);
            RbColorToLDD.Add(71, 194);
            RbColorToLDD.Add(72, 199);
            RbColorToLDD.Add(73, 102);
            RbColorToLDD.Add(74, 29);
            RbColorToLDD.Add(75, 75);
            RbColorToLDD.Add(77, 223);
            RbColorToLDD.Add(78, 283);
            RbColorToLDD.Add(79, 20);
            RbColorToLDD.Add(80, 1002);
            RbColorToLDD.Add(81, 200);
            RbColorToLDD.Add(82, 1004);
            RbColorToLDD.Add(84, 312);
            RbColorToLDD.Add(85, 268);
            RbColorToLDD.Add(86, 86);
            RbColorToLDD.Add(89, 195);
            RbColorToLDD.Add(92, 18);
            RbColorToLDD.Add(100, 100);
            RbColorToLDD.Add(110, 110);
            RbColorToLDD.Add(112, 112);
            RbColorToLDD.Add(114, 114);
            RbColorToLDD.Add(115, 115);
            RbColorToLDD.Add(117, 117);
            RbColorToLDD.Add(118, 118);
            RbColorToLDD.Add(120, 120);
            RbColorToLDD.Add(129, 129);
            RbColorToLDD.Add(132, 132);
            RbColorToLDD.Add(134, 139);
            RbColorToLDD.Add(135, 179);
            RbColorToLDD.Add(137, 145);
            RbColorToLDD.Add(142, 127);
            RbColorToLDD.Add(143, 143);
            RbColorToLDD.Add(148, 316);
            RbColorToLDD.Add(151, 208);
            RbColorToLDD.Add(158, 1000);
            RbColorToLDD.Add(178, 1001);
            RbColorToLDD.Add(179, 315);
            RbColorToLDD.Add(182, 182);
            RbColorToLDD.Add(183, 183);
            RbColorToLDD.Add(191, 191);
            RbColorToLDD.Add(212, 212);
            RbColorToLDD.Add(216, 216);
            RbColorToLDD.Add(226, 226);
            RbColorToLDD.Add(230, 230);
            RbColorToLDD.Add(232, 232);
            RbColorToLDD.Add(236, 284);
            RbColorToLDD.Add(272, 140);
            RbColorToLDD.Add(288, 141);
            RbColorToLDD.Add(294, 50);
            RbColorToLDD.Add(297, 297);
            RbColorToLDD.Add(308, 308);
            RbColorToLDD.Add(313, 11);
            RbColorToLDD.Add(320, 154);
            RbColorToLDD.Add(321, 321);
            RbColorToLDD.Add(322, 322);
            RbColorToLDD.Add(323, 323);
            RbColorToLDD.Add(326, 330);
            RbColorToLDD.Add(334, 310);
            RbColorToLDD.Add(335, 153);
            RbColorToLDD.Add(351, 22);
            RbColorToLDD.Add(366, 128);
            RbColorToLDD.Add(373, 136);
            RbColorToLDD.Add(378, 151);
            RbColorToLDD.Add(379, 135);
            RbColorToLDD.Add(383, 309);
            RbColorToLDD.Add(450, 12);
            RbColorToLDD.Add(462, 105);
            RbColorToLDD.Add(484, 38);
            RbColorToLDD.Add(503, 103);
            RbColorToLDD.Add(1000, 1005);
            RbColorToLDD.Add(1002, 1013);
            RbColorToLDD.Add(1003, 1014);
            RbColorToLDD.Add(1004, 1017);
            RbColorToLDD.Add(1005, 234);
            RbColorToLDD.Add(1006, 293);
            RbColorToLDD.Add(1007, 218);
        }

        public static PaletteItem GetPaletteItem(SetParts.Part rbSetPart)
        {
            if (!Initialized)
                Initialize();

            int origCount = LddPalette.Items.Count;

            if (LddPalette.Items.Any(i => i.ElementID == rbSetPart.ElementId))
                return (PaletteItem)LddPalette.Items.First(i => i.ElementID == rbSetPart.ElementId).Clone(rbSetPart.Quantity);

            if (!RbColorToLDD.ContainsKey(rbSetPart.RbColorId))
            {
                Trace.WriteLine("Could not match Rebrickable color to LDD.");
                return null;
            }

            int designId = 0;
            int lddColorId = RbColorToLDD[rbSetPart.RbColorId];
            PaletteItem paletteItem = null;

            if (int.TryParse(rbSetPart.PartId, out designId))
            {
                if (GetLddPaletteItem(designId, lddColorId, rbSetPart.ElementId, rbSetPart.Quantity, out paletteItem))
                    return paletteItem;
            }

            var rbPartInfo = RebrickableAPI.GetPart.Execute(new GetPartParameters(rbSetPart.PartId, true, true, false));
            paletteItem = GetItemFromPartInfo(rbPartInfo, lddColorId, rbSetPart.ElementId, rbSetPart.Quantity);

            //if (paletteItem == null && LddAssemblies.Contains(designId.ToString()))
            //{
            //    //Trace.WriteLine("Warning. Assembly found in LDD, but 
            //    return new Assembly(designId, rbSetPart.ElementId, rbSetPart.Quantity);
            //}
            if (LddPalette.Items.Count > origCount)
            {
                Trace.WriteLine("Items added to palette. Saving updated file.");
                XSerializable.Save(LddPalette, "LDD.PAXML");
            }
            return paletteItem;
        }

        static PaletteItem GetPaletteItem(int designId, int lddColor, string elementId, int quantity)
        {
            if (!Initialized)
                Initialize();
            /*
             * if elementId exists in base LDD palette
             *      return LDD palette items[elementId]
             *      
             * else if designId exists in base LDD palette
             * 
             *      if item is simple brick with no decorations and submaterials
             *          return new palette item(designId, lddColor, elementId, quantity)
             *      else
             *          get info from Rebrickable
             *          if decorations, pick default or most recurrent
             *          if submaterials, check if related to color, if not: good, else must ask user input
             *          if item is assembly, get sub-parts info from Rebrickable
             *              get sub-part designId, color (quantity of subpart is not supported by ldd, that also mean that all part with this designId will have the same color)
             *              repeat process (to fill material and/or decorations and/or sub-materials
             * else if designId exists in LDD primitives (db.lif)
             *      get info from Rebrickable
             *      
             * else
             *      return null
            */
            return null;
        }

        static PaletteItem GetItemFromPartInfo(PartInfo rbPartInfo, int lddColorId, string elementId, int quantity, bool nested = false)
        {
            PaletteItem paletteItem = null;
            int designId = 0;

            if (rbPartInfo.ExternalParts != null &&
               rbPartInfo.ExternalParts.LegoDesignIds != null &&
               rbPartInfo.ExternalParts.LegoDesignIds.Length > 0)
            {
                for (int i = 0; i < rbPartInfo.ExternalParts.LegoDesignIds.Length; i++)
                {
                    if (int.TryParse(rbPartInfo.ExternalParts.LegoDesignIds[i], out designId))
                    {
                        if (GetLddPaletteItem(designId, lddColorId, elementId, quantity, out paletteItem))
                            return paletteItem;
                    }
                }
            }

            if (!nested && 
                rbPartInfo.RelatedParts != null &&
                rbPartInfo.RelatedParts.Length > 0)
            {
                for (int i = 0; i < rbPartInfo.RelatedParts.Length; i++)
                {
                    if (rbPartInfo.RelatedParts[i].Type == "MOLD")
                    {
                        var altPartInfo = RebrickableAPI.GetPart.Execute(new GetPartParameters(rbPartInfo.RelatedParts[i].PartId, true, true, false));
                        paletteItem = GetItemFromPartInfo(altPartInfo, lddColorId, string.Empty, quantity, true);
                        if (paletteItem != null)
                            return paletteItem;
                    }
                }
            }

            int mainId = int.Parse(Regex.Match(rbPartInfo.PartId, @"\d+").Value);
            if (GetLddPaletteItem(mainId, lddColorId, string.Empty, quantity, out paletteItem))
                return paletteItem;

            return paletteItem;
        }

        static bool GetLddPaletteItem(int designId, int lddColor, string elementId, int quantity, out PaletteItem pItem)
        {
            pItem = null;
            if (!string.IsNullOrEmpty(elementId) && LddPalette.Items.Any(i => i.ElementID == elementId))
            {
                pItem = (PaletteItem)LddPalette.Items.First(i => i.ElementID == elementId).Clone(quantity);
            }
            else if (LddPalette.Items.OfType<Brick>().Any(b => b.DesignID == designId && b.MaterialID == lddColor))
            {
                pItem = LddPalette.Items.OfType<Brick>().First(b => b.DesignID == designId).Clone(elementId, lddColor, quantity);
                //LddPalette.Items.Add(pItem.Clone(0));
            }
            else if (LddPalette.Items.OfType<Brick>().Any(b => b.DesignID == designId))
            {
                pItem = LddPalette.Items.OfType<Brick>().First(b => b.DesignID == designId).Clone(elementId, lddColor, quantity);
                LddPalette.Items.Add(pItem.Clone(0));
            }
            else if (LddBricks.Contains(designId.ToString()))
            {
                pItem = new Brick(designId, elementId, lddColor, quantity);
                LddPalette.Items.Add(pItem.Clone(0));
            }
            else if (LddPalette.Items.OfType<Assembly>().Any(a => a.DesignID == designId))
            {
                var assemblyItem = (Assembly)LddPalette.Items.OfType<Assembly>().First(a => a.DesignID == designId).Clone(quantity);
                if (assemblyItem.Parts.Select(p => p.MaterialID).Distinct().Count() == 1)
                {
                    assemblyItem.ElementID = elementId;
                    assemblyItem.Parts.ForEach(p => p.MaterialID = lddColor);
                    pItem = assemblyItem;
                    return true;
                }
            }
            else if (LddAssemblies.Contains(designId.ToString()))
            {
                Trace.WriteLine("Warning. Found assembly for part in LDD, but none was defined for the specified ElementID. All sub-parts will be the same color (red).");
                pItem = new Assembly(designId, elementId, quantity);
            }
            return pItem != null;
        }
    }
}
