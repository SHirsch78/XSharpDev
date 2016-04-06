﻿using System;
using System.Windows.Forms;
using System.Security.Permissions;

namespace XSharp.Project
{
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]

    public partial class XSharpSLEPropertyForm : Form
    {
        public XSharpSLEPropertyForm()
        {
            InitializeComponent();
        }

        internal void SetMacros(XSharpBuildMacros mc)
        {
            MacrosList.BeginUpdate();
            MacrosList.Items.Clear();

            foreach (var p in mc)
            {
                ListViewItem i = new ListViewItem();

                i.Text = String.Concat("$(", p.MacroName, ")");
                i.SubItems.Add(p.Value);

                MacrosList.Items.Add(i);
            }

            MacrosList.Items[0].Selected = true;
            MacrosList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            MacrosList.EndUpdate();
        }


        private void InsertMacroButton_Click(object sender, EventArgs e)
        {
            PropertyText.Paste(MacrosList.SelectedItems[0].Text);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            XSharpMLEPropertyForm form = (XSharpMLEPropertyForm)sender;
            form.Close();
        }

        private void InsertFilenameBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlgOpenFile = new OpenFileDialog();

            if (dlgOpenFile.ShowDialog() == DialogResult.OK)
            {
                PropertyText.Paste(dlgOpenFile.FileNames[0]);
            }
        }

        private void InsertPathBtn_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlgFolder = new FolderBrowserDialog();
            dlgFolder.ShowNewFolderButton = false;

            if (dlgFolder.ShowDialog() == DialogResult.OK)
            {
                PropertyText.Paste(dlgFolder.SelectedPath);
            }
        }

        private void MacrosList_DoubleClick(object sender, EventArgs e)
        {
            PropertyText.Paste(MacrosList.SelectedItems[0].Text);
        }
    }
}
