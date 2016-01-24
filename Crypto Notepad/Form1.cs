﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crypto_Notepad
{
    public partial class MainWindow : Form
    {
        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam);
        public static string filename = "Unnamed.cnp";
        public static string key = "";
        public static bool keyChanged = false;
        public static bool settingsChanged = false;
        public static string[] args = Environment.GetCommandLineArgs();
        Properties.Settings ps = Properties.Settings.Default;
        int caretPos = 0; 

        public MainWindow()
        {
            InitializeComponent();
        }

        #region SaltMac
        void SaltMAC()
        {
            var address = "";
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    address = networkInterface.GetPhysicalAddress().ToString();
                }
            }

            if (ps.FirstLaunch == false)
            {
                DialogResult res = new DialogResult();
                using (new CenterWinDialog(this))
                {
                    res = MessageBox.Show("Get The Salt from mac address? (You can edit it by himself in Settings)", "Crypto Notepad",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                }

                if (res == DialogResult.Yes)
                {
                    ps.TheSalt = address;
                    ps.FirstLaunch = true;
                }

                if (res == DialogResult.No)
                {
                    ps.FirstLaunch = true;
                }
                ps.Save();
            }
        }
        #endregion

        void DecryptAES()
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();
            if (Form2.OkPressed == false)
            {
                OpenFile.FileName = "";
                return;
            }
            Form2.OkPressed = false;

            try
            {
                string opnfile = File.ReadAllText(OpenFile.FileName);
                string NameWithotPath = Path.GetFileName(OpenFile.FileName);
                string de = AES.Decrypt(opnfile, key, ps.TheSalt, ps.HashAlgorithm, ps.PasswordIterations, "16CHARSLONG12345", ps.KeySize);
                customRTB.Text = de;

                toolStripStatusLabel1.Text = NameWithotPath;
                toolStripStatusLabel1.ToolTipText = (OpenFile.FileName);
                filename = OpenFile.FileName;

                string cc2 = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
                customRTB.Select(Convert.ToInt32(cc2), 0);
            }
            catch (CryptographicException)
            {
                using (new CenterWinDialog(this))
                {
                    DialogResult dialogResult = MessageBox.Show("Invalid key!", "Crypto Notepad", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Retry)
                    {
                        DecryptAES();
                    }
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OpenFile.ShowDialog() != DialogResult.OK) return;
            {
                if (!OpenFile.FileName.Contains(".cnp"))
                {
                    string opnfile = File.ReadAllText(OpenFile.FileName);
                    string NameWithotPath = Path.GetFileName(OpenFile.FileName);
                    customRTB.Text = opnfile;
                    toolStripStatusLabel1.Text = NameWithotPath;
                    toolStripStatusLabel1.ToolTipText = (OpenFile.FileName);
                    string cc2 = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
                    customRTB.Select(Convert.ToInt32(cc2), 0);
                    return;
                }

                DecryptAES();
            }
        }

        private void openAsotiations()
        {
            Form2 Form2 = new Form2();
            Form2.StartPosition = FormStartPosition.CenterScreen;

            try
            {
                string NameWithotPath = Path.GetFileName(args[1]);
                string opnfile = File.ReadAllText(args[1]);

                Form2.ShowDialog();
                if (Form2.OkPressed == false)
                {
                    OpenFile.FileName = "";
                    return;
                }
                Form2.OkPressed = false;

                string de = AES.Decrypt(opnfile, key, ps.TheSalt, ps.HashAlgorithm, ps.PasswordIterations, "16CHARSLONG12345", ps.KeySize);
                customRTB.Text = de;

                toolStripStatusLabel1.Text = NameWithotPath;
                toolStripStatusLabel1.ToolTipText = (args[1]);

                filename = args[1];
                string cc = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
                customRTB.Select(Convert.ToInt32(cc), 0);
            }

            catch
            {
                DialogResult dialogResult = MessageBox.Show("Invalid key!", "Crypto Notepad", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.Retry)
                {
                    openAsotiations();
                }
            }
        }


        private void newToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();

            if (Form2.OkPressed == false)
            {
                return;
            }

            if (Form2.OkPressed == true)
            {
                Form2.OkPressed = false;
                if (SaveFile.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                customRTB.Clear();
                StreamWriter sw = new StreamWriter(SaveFile.FileName);
                string NameWithotPath = Path.GetFileName(SaveFile.FileName);
                toolStripStatusLabel1.Text = NameWithotPath;
                toolStripStatusLabel1.ToolTipText = (SaveFile.FileName);
                filename = SaveFile.FileName;
                sw.Close();
            }
        }

        private async void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string NameWithotPath = Path.GetFileName(OpenFile.FileName);

            if (string.IsNullOrEmpty(key))
            {
                Form2 Form2 = new Form2();
                Form2.ShowDialog();
                if (Form2.OkPressed == false)
                {
                    return;
                }
                Form2.OkPressed = false;
            }

            if (SaveFile.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            filename = SaveFile.FileName;

            string noenc = customRTB.Text;
            string en = AES.Encrypt(customRTB.Text, key, ps.TheSalt, ps.HashAlgorithm, ps.PasswordIterations, "16CHARSLONG12345", ps.KeySize);
            customRTB.Text = en;
            StreamWriter sw = new StreamWriter(filename);
            int i = customRTB.Lines.Count();
            int j = 0;
            i = i - 1;
            while (j <= i)
            {
                sw.WriteLine(customRTB.Lines.GetValue(j).ToString());
                j = j + 1;
            }
            sw.Close();
            customRTB.Text = noenc;
            toolStripStatusLabel1.Text = "Saved in: " + filename;
            string cc = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
            customRTB.Select(Convert.ToInt32(cc), 0);
            await Task.Delay(4000);
            toolStripStatusLabel1.Text = "Ready";

        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            ps.WindowLocation = this.Location;
            ps.MenuWrap = переносПоСловамToolStripMenuItem.Checked;
            ps.RichWrap = customRTB.WordWrap;
            if (this.WindowState == FormWindowState.Normal)
            {
                ps.WindowSize = this.Size;
            }

            ps.Save();

            {
                string f = "Unnamed.cnp";
                if (customRTB.Text != "")
                {
                    DialogResult res = new DialogResult();
                    using (new CenterWinDialog(this))
                    {
                        res = MessageBox.Show("Save changes in:\n" + filename + " ?",
                                                     "Crypto Notepad",
                                                     MessageBoxButtons.YesNoCancel,
                                                     MessageBoxIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            if (filename == f)
                            {
                                SaveFile.FileName = f;
                                saveAsToolStripMenuItem_Click(this, new EventArgs());
                            }

                            if (filename != f)
                            {
                                saveToolStripMenuItem1_Click_1(this, new EventArgs());
                            }
                            Application.Exit();
                        }

                        try
                        {
                            if (res == DialogResult.No)
                            {
                                Environment.Exit(0);
                            }

                            if (res == DialogResult.Cancel)
                            {

                                e.Cancel = true;
                            }

                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        void LineAndColumn()
        {
            int line = customRTB.GetLineFromCharIndex(customRTB.SelectionStart);
            int column = customRTB.SelectionStart - customRTB.GetFirstCharIndexFromLine(line);
            line += 1;
            column += 1;

            lineStripStatusLabel.Text = "Line: " + line.ToString();
            columnStripStatusLabel.Text = "Column: " + column.ToString();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            string pos = ps.WindowLocation.ToString();

            if (pos != "{X=0,Y=0}")
            {
                this.Location = ps.WindowLocation;
            }

            this.Size = ps.WindowSize;

            customRTB.Font = new Font(ps.RichTextFont, ps.RichTextSize);
            customRTB.ForeColor = ps.RichForeColor;
            customRTB.BackColor = ps.RichBackColor;
            wordWrapToolStripMenuItem.Checked = ps.MenuWrap;
            customRTB.WordWrap = ps.RichWrap;
            customRTB.SelectionIndent += 6;
            customRTB.SelectionRightIndent += 7;
            panel2.Visible = ps.ShowToolbar;

            if (ps.ShowToolbar == false)
            {
                panel2.Visible = false;
                int h = customRTB.Height;
                h += 23;
                customRTB.Height = h;
                customRTB.Location = new Point(0, 25);
            }

            if (ps.ShowToolbar == true)
            {
                panel2.Visible = true;
            }

            SaltMAC();

            if (ps.AutoCheckUpdate == true)
            {
                Thread up = new Thread(() => сheckForUpdates(false));
                up.Start();
            }

            LineAndColumn();

            DeleteUpdateFiles();

            if (args.Length > 1)
            {
                openAsotiations();
            }
        }

        public void DeleteUpdateFiles()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
            string UpdaterExe = exePath + "Updater.exe";
            string UpdateZip = exePath + "Crypto-Notepad-Update.zip";
            string ZipDll = exePath + "Ionic.Zip.dll";

            if (File.Exists(UpdaterExe))
            {
                File.Delete(UpdaterExe);
            }

            if (File.Exists(UpdateZip))
            {
                File.Delete(UpdateZip);
            }

            if (File.Exists(ZipDll))
            {
                File.Delete(ZipDll);
            }
        }

        private void очиститьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customRTB.Clear();
        }

        private async void saveToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            string f = "Unnamed.cnp";
            string NameWithotPath = Path.GetFileName(OpenFile.FileName);

            if (filename == f)
            {
                SaveFile.FileName = f;
                saveAsToolStripMenuItem_Click(this, new EventArgs());

                if (Form2.OkPressed == false)
                {
                    return;
                }

                Form2.OkPressed = false;
            }

            string noenc = customRTB.Text;
            string en = AES.Encrypt(customRTB.Text, key, ps.TheSalt, ps.HashAlgorithm, ps.PasswordIterations, "16CHARSLONG12345", ps.KeySize);
            customRTB.Text = en;
            StreamWriter sw = new StreamWriter(filename);
            int i = customRTB.Lines.Count();
            int j = 0;
            i = i - 1;

            while (j <= i)
            {
                sw.WriteLine(customRTB.Lines.GetValue(j).ToString());
                j = j + 1;
            }

            sw.Close();
            customRTB.Text = noenc;
            string cc = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
            customRTB.Select(Convert.ToInt32(cc), 0);
            toolStripStatusLabel1.Text = "Saved in: " + filename;
            await Task.Delay(4000);
            toolStripStatusLabel1.Text = "Ready";
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (customRTB.Focused == true)
            {
                customRTB.SelectAll();
            }
            else
            {
                searchTextBox.SelectAll();
            }
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (customRTB.Focused == true)
            {
                customRTB.Cut();
            }
            else
            {
                searchTextBox.Cut();
            }
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (customRTB.Focused == true)
            {
                customRTB.Copy();
            }
            else
            {
                searchTextBox.Copy();
            }
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (customRTB.Focused == true)
            {
                customRTB.Paste(DataFormats.GetFormat(DataFormats.Text));
            }
            else
            {
                searchTextBox.Paste();
            }
        }

        private async void deleteFileToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (filename != "Unnamed.cnp")
            {
                using (new CenterWinDialog(this))
                {
                    if (MessageBox.Show("Delete file: " + filename + " ?", "Crypto Notepad", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        File.Delete(filename);
                        toolStripStatusLabel1.Text = filename + " deleted";
                        customRTB.Clear();
                        key = "";
                        pictureBox11.Enabled = false;
                        filename = "Unnamed.cnp";
                        await Task.Delay(4000);
                        toolStripStatusLabel1.Text = "Ready";
                        return;
                    }
                }
            }
        }

        private void openFileLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filename != "Unnamed.cnp")
            {
                Process.Start("explorer.exe", @"/select, " + filename);
            }
        }

        private void переносПоСловамToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wordWrapToolStripMenuItem.Checked == true)
            {
                customRTB.WordWrap = true;
            }
            if (wordWrapToolStripMenuItem.Checked == false)
            {
                customRTB.WordWrap = false;
            }
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            отменаToolStripMenuItem_Click(this, new EventArgs());
        }

        public void отменаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (customRTB.CanUndo == true)
            {
                customRTB.Undo();
            }
            else customRTB.Redo();
        }

        private void вырезатьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.Cut();
        }

        private void копироватьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.Copy();
        }

        private void вставитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.Paste(DataFormats.GetFormat(DataFormats.Text));
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (customRTB.SelectionLength != 0)
            {
                вырезатьToolStripMenuItem1.Enabled = true;
                копироватьToolStripMenuItem1.Enabled = true;
                удалитьToolStripMenuItem1.Enabled = true;
            }
            if (customRTB.SelectionLength == 0)
            {
                вырезатьToolStripMenuItem1.Enabled = false;
                копироватьToolStripMenuItem1.Enabled = false;
                удалитьToolStripMenuItem1.Enabled = false;
            }
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            customRTB.SelectedText = "";
        }

        private void удалитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.SelectedText = "";
        }

        private void порядокЧтенияСправаНалевоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (порядокЧтенияСправаНалевоToolStripMenuItem.Checked == true)
            {
                customRTB.RightToLeft = RightToLeft.Yes;
            }
            if (порядокЧтенияСправаНалевоToolStripMenuItem.Checked == false)
            {
                customRTB.RightToLeft = RightToLeft.No;
            }
        }

        private void очиститьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.Clear();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutFrom a = new AboutFrom();
            a.ShowDialog(this);
        }

        private void выделитьВсеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            customRTB.SelectAll();
        }

        private void changeKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeKeyForm c = new ChangeKeyForm();
            c.ShowDialog(this);
        }

        public int FindMyTextNext(string text, int start)
        {
            int returnValue = -1;
            if (text.Length > 0 && start >= 0)
            {
                int indexToText = customRTB.Find(text, start, RichTextBoxFinds.MatchCase);
                if (indexToText >= 0)
                {
                    returnValue = indexToText;
                }
            }
            return returnValue;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            bool isexist = customRTB.Highlight(searchTextBox.Text, ps.HighlightsColor, chkMatchCase.Checked, chkMatchWholeWord.Checked);
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                searchTextBox.Text = "";
                panel1.Visible = false;
                customRTB.Height = customRTB.Height += 27;
                customRTB.Focus();
                customRTB.DeselectAll();
                customRTB.SelectionStart = caretPos;
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void customRTB_SelectionChanged_1(object sender, EventArgs e)
        {
            LineAndColumn();
            if (customRTB.SelectionLength != 0)
            {
                pictureBox8.Enabled = true;
                pictureBox9.Enabled = true;
            }
            if (customRTB.SelectionLength == 0)
            {
                pictureBox8.Enabled = false;
                pictureBox9.Enabled = false;
            }
        }

        private void customRTB_TextChanged(object sender, EventArgs e)
        {
            LineAndColumn();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm sf = new SettingsForm();
        }

        private async void MainWindow_Activated(object sender, EventArgs e)
        {
            if (settingsChanged == true)
            {
                customRTB.Font = new Font(ps.RichTextFont, ps.RichTextSize);
                customRTB.ForeColor = ps.RichForeColor;
                customRTB.BackColor = ps.RichBackColor;

                if (ps.ShowToolbar == false && panel2.Visible == true)
                {
                    panel2.Visible = false;
                    int h = customRTB.Height;
                    h += 23;
                    customRTB.Height = h;
                    customRTB.Location = new Point(0, 25);
                }

                if (ps.ShowToolbar == true && panel2.Visible == false)
                {
                    panel2.Visible = true;
                    int h = customRTB.Height;
                    h -= 23;
                    customRTB.Height = h;
                    customRTB.Location = new Point(0, 48);
                }
            }

            if (keyChanged == true)
            {
                toolStripStatusLabel1.Text = "Key was changed";
                await Task.Delay(4000);
                toolStripStatusLabel1.Text = "Ready";
            }

            if (key == "")
            {
                pictureBox11.Enabled = false;
                pictureBox13.Enabled = false;
            }

            if (key != "")
            {
                pictureBox11.Enabled = true;
                pictureBox13.Enabled = true;
            }
        }

        private void chkMatchCase_CheckedChanged(object sender, EventArgs e)
        {
            bool isexist = customRTB.Highlight(searchTextBox.Text, ps.HighlightsColor, chkMatchCase.Checked, chkMatchWholeWord.Checked);
        }

        private void chkMatchWholeWord_CheckedChanged(object sender, EventArgs e)
        {
            bool isexist = customRTB.Highlight(searchTextBox.Text, ps.HighlightsColor, chkMatchCase.Checked, chkMatchWholeWord.Checked);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            findToolStripMenuItem2_Click(this, new EventArgs());
        }

        private void findToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (panel1.Visible == false)
            {
                panel1.Visible = true;
                searchTextBox.Focus();
                //SendMessage(customRTB.Handle, (uint)0x00B6, (UIntPtr)0, (IntPtr)(-1));
                customRTB.Height = customRTB.Height - 27;
                return;
            }

            if (panel1.Visible == true)
            {
                searchTextBox.Text = "";
                panel1.Visible = false;
                customRTB.Height = customRTB.Height += 27;
                customRTB.Focus();
                //SendMessage(customRTB.Handle, (uint)0x00B6, (UIntPtr)0, (IntPtr)(1));
                customRTB.DeselectAll();
                customRTB.SelectionStart = caretPos;
                return;
            }
        }

        private void documentationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Sigmanor/Crypto-Notepad/wiki/Documentation-%28ENG%29");
        }

        private void сервисToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (key == "")
            {
                changeKeyToolStripMenuItem.Enabled = false;
                lockToolStripMenuItem.Enabled = false;
            }

            if (key != "") 
            {
                changeKeyToolStripMenuItem.Enabled = true;
                lockToolStripMenuItem.Enabled = true;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            newToolStripMenuItem_Click_1(this, new EventArgs());
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem1_Click_1(this, new EventArgs());
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            openFileLocationToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            deleteFileToolStripMenuItem_Click_1(this, new EventArgs());
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            вырезатьToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            копироватьToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            вставитьToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            changeKeyToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            settingsToolStripMenuItem_Click(this, new EventArgs());
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            int h = customRTB.Height;
            h += 23;
            customRTB.Height = h;
            customRTB.Location = new Point(0, 25);
            ps.ShowToolbar = false;
            ps.Save();
        }

        private void pictureBox12_MouseEnter(object sender, EventArgs e)
        {
            pictureBox12.Image = Properties.Resources.close_b;
        }

        private void pictureBox12_MouseLeave(object sender, EventArgs e)
        {
            pictureBox12.Image = Properties.Resources.close_g;
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.close_b;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.close_g;
        }

        private void правкаToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (customRTB.SelectionLength != 0)
            {
                вырезатьToolStripMenuItem.Enabled = true;
                копироватьToolStripMenuItem.Enabled = true;
                удалитьToolStripMenuItem.Enabled = true;
            }
            if (customRTB.SelectionLength == 0)
            {
                вырезатьToolStripMenuItem.Enabled = false;
                копироватьToolStripMenuItem.Enabled = false;
                удалитьToolStripMenuItem.Enabled = false;
            }
        }

        private void сheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Thread up = new Thread(() => сheckForUpdates(true));
            up.Start();
        }

        public void сheckForUpdates(bool autoCheck)
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead("https://raw.githubusercontent.com/Sigmanor/Crypto-Notepad/master/version.txt");
            StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            string version = Application.ProductVersion;
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";

            int ver = Convert.ToInt32(version.Replace(".", "")), con = Convert.ToInt32(content.Replace(".", ""));

            if (con != ver)
            {
                MainMenu.Invoke((Action)delegate
                {
                    using (new CenterWinDialog(this))
                    {
                        DialogResult res = new DialogResult();

                        res = MessageBox.Show("New version is available. Install it now?", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (res == DialogResult.Yes)
                        {
                            File.WriteAllBytes(exePath + "Ionic.Zip.dll", Properties.Resources.Ionic_Zip);
                            File.WriteAllBytes(exePath + "Updater.exe", Properties.Resources.Updater);

                            var pr = new Process();
                            pr.StartInfo.FileName = exePath + "Updater.exe";
                            pr.StartInfo.Arguments = "/u";
                            pr.Start();
                            Application.Exit();
                        }
                    }
                });
            }

            if (con == ver && autoCheck == true)
            {
                MainMenu.Invoke((Action)delegate
                {
                    using (new CenterWinDialog(this))
                    {
                        MessageBox.Show("Crypto Notepad is up to date.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });
            }
        }
        
        void AutoLock(bool minimize)
        {
            Form2 f2 = new Form2();
            key = "";
            caretPos = customRTB.SelectionStart;
            this.Hide();
            if (minimize == true)
            {
                f2.WindowState = FormWindowState.Minimized;
            }
            f2.ShowDialog();

            if (Form2.OkPressed == false)
            {
                key = "";
                customRTB.Clear();
                toolStripStatusLabel1.Text = "Ready";
                OpenFile.FileName = "";
                this.Show();
                return;
            }
            Form2.OkPressed = false;

            try
            {
                OpenFile.FileName = toolStripStatusLabel1.ToolTipText;
                string opnfile = File.ReadAllText(OpenFile.FileName);
                string NameWithotPath = Path.GetFileName(OpenFile.FileName);
                string de = AES.Decrypt(opnfile, key, ps.TheSalt, ps.HashAlgorithm, ps.PasswordIterations, "16CHARSLONG12345", ps.KeySize);

                toolStripStatusLabel1.Text = NameWithotPath;
                toolStripStatusLabel1.ToolTipText = (OpenFile.FileName);
                filename = OpenFile.FileName;

                string cc2 = customRTB.Text.Length.ToString(CultureInfo.InvariantCulture);
                customRTB.SelectionStart = caretPos;
                this.Show();
            }
            catch (CryptographicException)
            {
                DialogResult dialogResult = MessageBox.Show("Invalid key!", "Crypto Notepad", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                if (dialogResult == DialogResult.Retry)
                {
                    AutoLock(false);
                }
                if (dialogResult == DialogResult.Cancel)
                {
                    key = "";
                    customRTB.Clear();
                    toolStripStatusLabel1.Text = "Ready";
                    OpenFile.FileName = "";
                    this.Show();
                    return;
                }
            }

        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const int SC_MINIMIZE = 0xF020;

            if (m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MINIMIZE && ps.AutoLock == true && key != "")
            {
                AutoLock(true);
                return;
            }

            base.WndProc(ref m);
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            AutoLock(false);
        }

        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoLock(false);
        }

        private void файлToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (OpenFile.FileName == "")
            {
                openFileLocationToolStripMenuItem.Enabled = false;
                deleteFileToolStripMenuItem.Enabled = false;
            }

            if (OpenFile.FileName != "")
            {
                openFileLocationToolStripMenuItem.Enabled = true;
                deleteFileToolStripMenuItem.Enabled = true;
            }
        }

        private void customRTB_Click(object sender, EventArgs e)
        {
           caretPos = customRTB.SelectionStart;
        }

        private void customRTB_KeyDown(object sender, KeyEventArgs e)
        {
            caretPos = customRTB.SelectionStart;
        }

    }
}