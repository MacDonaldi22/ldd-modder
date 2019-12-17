﻿using HtmlAgilityPack;
using LDDModder.LDD;
using LDDModder.LDD.Files;
using LDDModder.LDD.Primitives;
using LDDModder.PaletteMaker.Models.LDD;
using LDDModder.PaletteMaker.Models.Rebrickable;
using LDDModder.PaletteMaker.Rebrickable;
using LDDModder.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LDDModder.PaletteMaker.DB
{
    public class RebrickableDataImporter : DbInitializerModule
    {
        public string PartsCsvFile { get; set; }

        public string RelationshipsCsvFile { get; set; }

        public RebrickableDataImporter(SQLiteConnection connection, LDDEnvironment environment, CancellationToken cancellationToken) : base(connection, environment, cancellationToken)
        {

        }

        [Flags]
        public enum RebrickableDataType
        {   
            Colors = 1,
            Categories = 2,
            Themes = 4,
            PartsAndRelationships = 8,
            BaseData = Colors | Categories | Themes,
            All = Colors | Categories | Themes | PartsAndRelationships
        }

        public void ImportAllData(RebrickableDataType data = RebrickableDataType.All)
        {
            if (!IsCancellationRequested && data.HasFlag(RebrickableDataType.Colors))
                ImportColors();

            if (!IsCancellationRequested && data.HasFlag(RebrickableDataType.Categories))
                ImportCategories();

            if (!IsCancellationRequested && data.HasFlag(RebrickableDataType.Themes))
                ImportThemes();

            if (!IsCancellationRequested && data.HasFlag(RebrickableDataType.PartsAndRelationships))
                ImportPartsAndRelationships();
        }

        public void ImportBaseData()
        {
            NotifyBeginStep("Importing misc. data (Colors, categories and themes)");

            if (!IsCancellationRequested)
                ImportColors();

            if (!IsCancellationRequested)
                ImportCategories();

            if (!IsCancellationRequested)
                ImportThemes();
        }

        public void ImportColors()
        {
            using (var trans = Connection.BeginTransaction())
            using (var colorCmd = Connection.CreateCommand())
            using (var matchCmd = Connection.CreateCommand())
            {
                colorCmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbColorMatch>()}";
                colorCmd.ExecuteNonQuery();
                colorCmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbColor>()}";
                colorCmd.ExecuteNonQuery();
                colorCmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbColorMatch>()}'";
                colorCmd.ExecuteNonQuery();
                colorCmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbColor>()}'";
                colorCmd.ExecuteNonQuery();

                NotifyIndefiniteProgress("Downloading rebrickable colors...");

                var colors = RebrickableAPI.GetAllColors().ToList();

                NotifyTaskStart("Importing colors...", colors.Count);

                DbHelper.InitializeInsertCommand<RbColor>(colorCmd, x =>
                new
                {
                    x.ID,
                    x.Name,
                    x.IsTransparent,
                    x.RgbHex
                });

                DbHelper.InitializeInsertCommand<RbColorMatch>(matchCmd, x =>
                new
                {
                    x.RbColorID,
                    x.Platform,
                    x.ColorID,
                    x.ColorName
                });

                int totalToProcess = colors.Count;
                int totalProcessed = 0;

                void AddColorMatches(int colorID, PaletteMaker.Rebrickable.Models.ColorIds colorIds, string platform)
                {
                    int colorCount = colorIds?.ExtIds?.Count ?? 0;

                    for (int i = 0; i < colorCount; i++)
                    {
                        DbHelper.InsertWithParameters(matchCmd,
                            colorID,
                            platform,
                            colorIds.ExtIds[i],
                            colorIds.ExtDescrs[i][0]
                        );
                    }
                }

                foreach (var rbColor in colors)
                {
                    if (IsCancellationRequested)
                        break;

                    DbHelper.InsertWithParameters(colorCmd,
                        rbColor.Id,
                        rbColor.Name,
                        rbColor.IsTransparent,
                        rbColor.RgbHex
                    );

                    AddColorMatches(rbColor.Id, rbColor.ExternalColorIds.LEGO, "LEGO");
                    AddColorMatches(rbColor.Id, rbColor.ExternalColorIds.BrickLink, "BrickLink");

                    ReportProgress(++totalProcessed, totalToProcess);
                }

                trans.Commit();
            }
        }

        public void ImportCategories()
        {
            using (var trans = Connection.BeginTransaction())
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbCategory>()}";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbCategory>()}'";
                cmd.ExecuteNonQuery();

                NotifyIndefiniteProgress("Downloading rebrickable categories...");
                var categories = RebrickableAPI.GetAllCategories().ToList();
                NotifyTaskStart("Importing categories...", categories.Count);

                DbHelper.InitializeInsertCommand<RbCategory>(cmd, x => new { x.ID, x.Name });

                int totalToProcess = categories.Count;
                int totalProcessed = 0;

                foreach (var rbCat in categories)
                {
                    if (IsCancellationRequested)
                        break;

                    DbHelper.InsertWithParameters(cmd, rbCat.Id, rbCat.Name);

                    ReportProgress(++totalProcessed, totalToProcess);
                }

                trans.Commit();
            }
        }

        public void ImportThemes()
        {
            using (var trans = Connection.BeginTransaction())
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbTheme>()}";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbTheme>()}'";
                cmd.ExecuteNonQuery();
                
                NotifyIndefiniteProgress("Downloading rebrickable themes...");
                var themes = RebrickableAPI.GetAllThemes().ToList();
                NotifyTaskStart("Importing themes...", themes.Count);

                DbHelper.InitializeInsertCommand<RbTheme>(cmd, x => new { x.ID, x.Name, x.ParentThemeID });

                int totalToProcess = themes.Count;
                int totalProcessed = 0;

                foreach (var rbTheme in themes)
                {
                    if (IsCancellationRequested)
                        break;

                    DbHelper.InsertWithParameters(cmd, rbTheme.Id, rbTheme.Name, rbTheme.ParentID);

                    ReportProgress(++totalProcessed, totalToProcess);
                }

                trans.Commit();
            }
        }
    
        public void ImportPartsAndRelationships()
        {
            NotifyBeginStep("Importing parts and relationships");

            string tmpDownloadDir = null;

            try
            {
                if (string.IsNullOrEmpty(PartsCsvFile) || string.IsNullOrEmpty(RelationshipsCsvFile))
                {
                    NotifyIndefiniteProgress("Downloading rebrickable csv files...");
                    tmpDownloadDir = FileHelper.GetTempDirectory();
                    DownloadRebrickableCsvFiles(tmpDownloadDir);
                }

                if (!IsCancellationRequested)
                     ImportRebrickableParts();

                if (!IsCancellationRequested)
                    ImportRebrickableRelationships();
            }
            finally
            {
                if (!string.IsNullOrEmpty(tmpDownloadDir))
                    Task.Factory.StartNew(() => FileHelper.DeleteFileOrFolder(tmpDownloadDir, true, true));
            }
        }

        public static Regex IsPrintOrPatternRegex = new Regex("(?<=\\d[a-d]?)(p|pb|pr|ps|px|pat)\\d", RegexOptions.Compiled);
        public static Regex IsAssemblyRegex = new Regex("c\\d\\d[a-c]?$", RegexOptions.Compiled);

        private void ImportRebrickableParts()
        {
            if (!File.Exists(PartsCsvFile))
                return;

            NotifyProgressStatus("Reading parts from file...");
            var partCsv = IO.CsvFile.Read(PartsCsvFile, IO.CsvFile.Separator.Comma);
            partCsv.Rows[0].IsHeader = true;

            using (var trans = Connection.BeginTransaction())
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbPart>()}";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbPart>()}'";
                cmd.ExecuteNonQuery();

                DbHelper.InitializeInsertCommand<RbPart>(cmd, x =>
                new
                {
                    x.PartID,
                    x.Name,
                    x.CategoryID,
                    x.IsPrintOrPattern,
                    x.IsAssembly,
                    x.ParentPartID
                });

                int totalToProcess = partCsv.Rows.Count - 1;
                int totalProcessed = 0;

                foreach (var row in partCsv.Rows)
                {
                    if (IsCancellationRequested)
                        break;

                    if (row.IsHeader)
                        continue;

                    string partID = row[0];
                    string partName = row[1];
                    int? category = string.IsNullOrEmpty(row[2]) ? default(int?) : int.Parse(row[2]);

                    string parentPartID = null;
                    bool isPrintOrPattern = false;
                    bool isAssembly = IsAssemblyRegex.IsMatch(partID);

                    if (!(category == 17 || category == 58))//non-lego and stickers
                    {
                        var match = IsPrintOrPatternRegex.Match(partID);
                        isPrintOrPattern = match.Success;

                        if (isPrintOrPattern)
                        {
                            parentPartID = partID.Substring(0, match.Index);

                            if (IsAssemblyRegex.IsMatch(partID) && !IsAssemblyRegex.IsMatch(parentPartID))
                            {
                                parentPartID += partID.Substring(IsAssemblyRegex.Match(partID).Index);
                                isAssembly = true;
                            }
                        }
                    }

                    DbHelper.InsertWithParameters(cmd, partID, partName, category, isPrintOrPattern, isAssembly, parentPartID);

                    ReportProgress(++totalProcessed, totalToProcess);
                }

                trans.Commit();
            }
        }

        private void ImportRebrickableRelationships()
        {
            if (!File.Exists(RelationshipsCsvFile))
                return;

            NotifyProgressStatus("Reading relationships from file...");
            var relationsCsv = IO.CsvFile.Read(RelationshipsCsvFile, IO.CsvFile.Separator.Comma);
            relationsCsv.Rows[0].IsHeader = true;

            using (var trans = Connection.BeginTransaction())
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM {DbHelper.GetTableName<RbPartRelation>()}";
                cmd.ExecuteNonQuery();
                cmd.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{DbHelper.GetTableName<RbPartRelation>()}'";
                cmd.ExecuteNonQuery();

                DbHelper.InitializeInsertCommand<RbPartRelation>(cmd);
                DbHelper.OrderCommandParameters(cmd,
                    nameof(RbPartRelation.ParentPartID),
                    nameof(RbPartRelation.ChildPartID),
                    nameof(RbPartRelation.RelationType));

                int totalToProcess = relationsCsv.Rows.Count - 1;
                int totalProcessed = 0;

                foreach (var row in relationsCsv.Rows)
                {
                    if (IsCancellationRequested)
                        break;

                    if (row.IsHeader)
                        continue;

                    DbHelper.InsertWithParameters(cmd, row[2], row[1], row[0]);
                    ReportProgress(++totalProcessed, totalToProcess);
                }

                cmd.Parameters.Clear();
                cmd.CommandText = "";
                trans.Commit();
            }
        }

        public const string PARTS_FILENAME = "parts.csv";

        public const string RELATIONSHIPS_FILENAME = "part_relationships.csv";

        public static List<Tuple<string, string>> GetRebrickableDownloadLinks()
        {
            var web = new HtmlWeb();

            var htmlDoc = web.Load("https://rebrickable.com/downloads/");

            var downloadLinkNodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(., \".csv\")]");
            var downloadLinks = new List<Tuple<string, string>>();

            foreach (var linkNode in downloadLinkNodes)
            {
                downloadLinks.Add(new Tuple<string, string>(linkNode.InnerText, linkNode.Attributes["href"].Value));
            }
            return downloadLinks;
        }

        public void DownloadRebrickableCsvFiles(string destinationFolder)
        {
            Directory.CreateDirectory(destinationFolder);

            var links = GetRebrickableDownloadLinks();
            var partsLink = links.FirstOrDefault(x => x.Item1 == PARTS_FILENAME)?.Item2;
            var relastionshipsLink = links.FirstOrDefault(x => x.Item1 == RELATIONSHIPS_FILENAME)?.Item2;

            using (var wc = new WebClient())
            {
                PartsCsvFile = Path.Combine(destinationFolder, PARTS_FILENAME);
                //wc.DownloadFile(partsLink, PartsCsvFile);
                var downloadTask1 = wc.DownloadFileTaskAsync(partsLink, PartsCsvFile);
                downloadTask1.Wait(CancellationToken);

                RelationshipsCsvFile = Path.Combine(destinationFolder, RELATIONSHIPS_FILENAME);
                wc.DownloadFile(relastionshipsLink, RelationshipsCsvFile);
                var downloadTask2 = wc.DownloadFileTaskAsync(relastionshipsLink, RelationshipsCsvFile);
                downloadTask2.Wait(CancellationToken);
            }
        }

    }
}