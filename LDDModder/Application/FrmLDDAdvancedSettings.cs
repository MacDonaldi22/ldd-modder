﻿using LDDModder.LDD;
using LDDModder.LDD.General;
using LDDModder.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace LDDModder
{
    public partial class FrmLDDAdvancedSettings : Form
    {
        private bool AdminRightsNeeded = false;
        private Bitmap UacShieldBmp;

        public FrmLDDAdvancedSettings()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            CheckProgramFilesRights();

            LoadSettings();
        }


        private void LoadSettings()
        {
            chkDeveloperMode.Checked = GetSettingBoolean(PreferencesSettings.DeveloperMode, LDDLocation.AppData);
            chkShowTooltip.Checked = GetSettingBoolean(PreferencesSettings.ShowToolTip, LDDLocation.AppData);
            chkExtendedTooltip.Checked = GetSettingBoolean(PreferencesSettings.ShowExtendedBrickToolTip, LDDLocation.AppData);
            chkDoServerCall.Checked = !GetSettingBoolean(PreferencesSettings.DoServerCall, LDDLocation.ProgramFiles);
            chkVerbose.Checked = GetSettingBoolean(PreferencesSettings.Verbose, LDDLocation.ProgramFiles);

            var userModelDirValue = LDDManager.GetSettingValue(PreferencesSettings.UserModelDirectory, LDDLocation.AppData);
            btnTxtUserModelDir.Text = DecodeSettingPath(userModelDirValue);
        }

        private void CheckProgramFilesRights()
        {
            if (SecurityHelper.IsUserAdministrator)
                AdminRightsNeeded = false;
            else
                AdminRightsNeeded = !SecurityHelper.HasWritePermission(LDDManager.GetSettingsFilePath(LDDLocation.ProgramFiles));

            if (AdminRightsNeeded)
            {
                UacShieldBmp = NativeHelper.GetUacShieldIcon();
                pictureBox1.Image = UacShieldBmp;
                pictureBox2.Image = UacShieldBmp;
            }
            else
            {
                pictureBox1.Visible = pictureBox2.Visible = false;
                foreach (Control ctrl in tableLayoutPanel1.Controls)
                {
                    var ctrlCell = tableLayoutPanel1.GetCellPosition(ctrl);
                    if (ctrlCell.Column == 1)
                    {
                        var cSpan = tableLayoutPanel1.GetColumnSpan(ctrl);
                        tableLayoutPanel1.SetColumnSpan(ctrl, cSpan == 1 ? cSpan : cSpan - 1);
                        tableLayoutPanel1.SetColumn(ctrl, 2);
                    }
                }
                chkDoServerCall.Margin = new Padding(3);
                chkDoServerCall.Padding = new Padding(4, 0, 30, 0);
                chkVerbose.Margin = new Padding(3);
                chkVerbose.Padding = new Padding(4, 0, 30, 0);
            }
            
        }

        private void DeveloperModeLabel_ClickRedirect(object sender, EventArgs e)
        {
            chkDeveloperMode.Checked = !chkDeveloperMode.Checked;
        }

        private void ShowTooltipLabel_ClickRedirect(object sender, EventArgs e)
        {
            chkShowTooltip.Checked = !chkShowTooltip.Checked;
        }

        private void ExtendedTooltipLabel_ClickRedirect(object sender, EventArgs e)
        {
            chkExtendedTooltip.Checked = !chkExtendedTooltip.Checked;
        }

        private static string DecodeSettingPath(string pathValue)
        {
            if (string.IsNullOrEmpty(pathValue) || !pathValue.StartsWith("file"))
                return pathValue;

            pathValue = pathValue.Substring(5).Replace('/', '\\');
            pathValue = pathValue.Insert(1, ":");
            return pathValue;
        }

        private void btnTxtUserModelDir_ButtonClicked(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.SelectedPath = btnTxtUserModelDir.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    btnTxtUserModelDir.Text = dlg.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            LDDManager.SetSetting(PreferencesSettings.ShowToolTip, chkShowTooltip.Checked ? "yes" : "no", LDDLocation.AppData);
            LDDManager.SetSetting(PreferencesSettings.ShowExtendedBrickToolTip, chkExtendedTooltip.Checked && chkShowTooltip.Checked ? "yes" : "no", LDDLocation.AppData);

            LDDManager.SetSetting(PreferencesSettings.DeveloperMode, chkDeveloperMode.Checked ? "yes" : "no", LDDLocation.AppData);

            if (!AdminRightsNeeded)
            {
                LDDManager.SetSetting(PreferencesSettings.DoServerCall, !chkDoServerCall.Checked ? "yes" : "no", LDDLocation.ProgramFiles);
                //LDDManager.SetSetting(PreferencesSettings.Verbose, !chkDoServerCall.Checked ? "yes" : "no", LDDLocation.ProgramFiles);
            }
            else
            {
                var changedSettings = new List<PreferenceEntry>();
                if (!GetSettingBoolean(PreferencesSettings.DoServerCall, LDDLocation.ProgramFiles) != chkDoServerCall.Checked)
                    changedSettings.Add(new PreferenceEntry() { Key = PreferencesSettings.DoServerCall, Value = (!chkDoServerCall.Checked ? "yes" : "no"), Location = LDDLocation.ProgramFiles });

                if (changedSettings.Count > 0)
                {
                    var processInfo = new ProcessStartInfo()
                    {
                        Verb = "runas",
                        Arguments = "set " + changedSettings.Select(x=>x.Serialize()).Aggregate((a,b)=> a + " " + b),
                        FileName = Application.ExecutablePath
                    };
                    Process.Start(processInfo);
                }
            }

            //var currentVal = !GetSettingBoolean(PreferencesSettings.DoServerCall, LDDLocation.ProgramFiles);
            //if (currentVal != chkDoServerCall.Checked)
            //{
            //    if (SecurityHelper.IsUserAdministrator)
            //        LDDManager.SetSetting(PreferencesSettings.DoServerCall, !chkDoServerCall.Checked ? "yes" : "no", LDDLocation.ProgramFiles);
            //    else
            //    {
            //        var processInfo = new ProcessStartInfo() 
            //        {
            //            Verb = "runas", 
            //            Arguments = "set " + PreferencesSettings.DoServerCall + " " + (!chkDoServerCall.Checked ? "yes" : "no"),
            //            FileName = Application.ExecutablePath 
            //        };
            //        Process.Start(processInfo);
            //    }
            //}
        }

        private void chkShowTooltip_CheckedChanged(object sender, EventArgs e)
        {
            chkExtendedTooltip.Enabled = chkShowTooltip.Checked;
        }

        private static bool GetSettingBoolean(string key, LDDLocation loc)
        {
            string strVal = LDDManager.GetSettingValue(key, loc).Trim();
            return strVal == "1" || strVal.Equals("yes", StringComparison.InvariantCultureIgnoreCase);
        }

    }
}
