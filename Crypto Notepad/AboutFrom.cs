﻿using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Crypto_Notepad
{
    public partial class AboutFrom : Form
    {
        public AboutFrom()
        {
            InitializeComponent();
        }

        /*Form Events*/
        private void AboutWindow_Load(object sender, EventArgs e)
        {
            Version vrs = new Version(Application.ProductVersion);
            AppVersionLabel.Text = string.Format("Crypto Notepad " + "{0}.{1}.{2}", vrs.Major, vrs.Minor, vrs.Build);
        }

        private void AboutFrom_Click(object sender, EventArgs e)
        {
            AppLogoPictureBox.Focus();
        }
        /*Form Events*/


        /*Main Information*/
        private void AppVersionLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Sigmanor/Crypto-Notepad/wiki/Release-Notes");
        }

        private void AuthorLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://sigmanor.github.io/");
        }

        private void LicenseLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Sigmanor/Crypto-Notepad/blob/master/LICENSE");
        }

        private void GithubLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Sigmanor/Crypto-Notepad");
        }

        private void AppLogoPictureBox_Click(object sender, EventArgs e)
        {
            Process.Start("https://crypto-notepad.github.io/");
        }
        /*Main Information*/


        /*Perform open links*/
        private void TPDRichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }
        /*Perform open links*/

    }
}
