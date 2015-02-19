using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using HandshakeEmulator.DataStructures;
using System.Threading;
using SITCAB.RTDS;


namespace HandshakeEmulator
{
    public partial class HandshakeUi : Form
    {
        readonly List<Log> _logs = new List<Log>();
        readonly Equip _activeEquip = new Equip();
        Configuration _config;

        ParametersForm _paramForm;
        DowntimesForm _downForm;
        AboutBox1 _aboutBox;
        
        string[] _equipStatusParametersTags;
        readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
        string _root = "";
        string _activeCommand;
        int _indexCommand;

        Listener _worker;
        Thread _listenerThread;
        bool _listening;
        bool _running;

        int _windowWidth;
        int _windowHeight;
        int _lsvWidth;
        int _lsvHeight;

        public HandshakeUi()
        {
            InitializeComponent();
        }

        private void HandshakeUI_Load(object sender, EventArgs e)
        {
            _windowWidth = Width;
            _windowHeight = Height;
            _lsvWidth = logListView.Width;
            _lsvHeight = logListView.Height;

            _timer.Interval = 300;
            _timer.Tick += timer_Tick;
            _timer.Enabled = true;

            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("HandshakeEmulator.HandshakeSettings.Configuration.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                if (stream != null) _config = (Configuration)serializer.Deserialize(stream);
            }

            // Populate equipment combo box
            foreach (Equip eq in _config.EquipmentList)
                equipmentComboBox.Items.Add(eq.Name);

            // Populate commands combo box
            for (int j = 0; j < _config.EquipmentList.Count; j++)
                commandComboBox.Items.Add(_config.CommandList[j].Name);

            _aboutBox = new AboutBox1();
        }

        private void HandshakeUI_ResizeEnd(object sender, EventArgs e)
        {
            logListView.Width = _lsvWidth + Width - _windowWidth;
            logListView.Height = _lsvHeight + Height - _windowHeight;
        }

        // This routine constantly checks for new logs that need to be written
        void timer_Tick(object sender, EventArgs e)
        {
            lock(_logs)
            {
                foreach (Log l in _logs)
                {
                    // Building a new row for data logged
                    ListViewItem newRow = new ListViewItem { Text = l.Timestamp.ToString(CultureInfo.InvariantCulture) };
                    newRow.SubItems.Add(l.Equipment);
                    newRow.SubItems.Add(l.Level);
                    newRow.SubItems.Add(l.Message);

                    logListView.Items.Add(newRow);
                }
                _logs.Clear();
            }

            if (_activeCommand != null && _activeEquip != null)
                btnSendcommand.Enabled = !_running;
        }

        private void Listen()
        {
            while (_listening)
            {
                RTDSResult res = RTDS.ReadThrough(_root + _config.CmdReqChannelStatus);

                if (res.Values != null
                    && (short)res.Values[0] == 3
                    && _running == false
                    && _worker == null)
                {
                    _running = true;
                    statusStrip.BackColor = Color.Orange;

                    _worker = new Listener(_config, _activeEquip);
                    _worker.EventLog += listener_eventLog;
                    _worker.EventComplete += listener_eventComplete;
                    _worker.Start("HS");
                }

                Thread.Sleep(500);
            }
        }

        void listener_eventComplete(object sender, EventArgs e)
        {
            _running = false;
            _worker = null;
            statusStrip.BackColor = _listening ? Color.PaleGreen : Color.Empty;
        }

        void listener_eventLog(object sender, EventArgs e)
        {
            lock (_logs)
            {
                _logs.Add((Log)sender);
            }
        }

        public void ReturnToForm()
        {
            equipmentComboBox.Enabled = true;
        }

        // Start listening
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!_listening)
            {
                lock (_logs)
                {
                    _logs.Add(new Log(_activeEquip.Name, "INFO", _activeEquip.Name + " (" + _activeEquip.Id + ") is running"));
                }

                toolStripStatusLabel2.Text = @"Running";
                statusStrip.BackColor = Color.PaleGreen;
                equipmentComboBox.Enabled = false;

                lock (this)
                {
                    if (_listenerThread != null && _listenerThread.IsAlive) return;

                    _listening = true;
                    _listenerThread = new Thread(Listen);
                    _listenerThread.Start();
                }
            }
        }

        // Stop listening
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_listening)
            {
                lock (_logs)
                {
                    _logs.Add(new Log(_activeEquip.Name, "INFO", _activeEquip.Name + " (" + _activeEquip.Id + ") is stopped"));
                }

                toolStripStatusLabel2.Text = @"Stopped";
                statusStrip.BackColor = Color.Tomato;
                equipmentComboBox.Enabled = true;

                lock (this)
                {
                    if (_listenerThread == null || !_listenerThread.IsAlive) return;

                    _listening = false;
                    if (!_listenerThread.Join(10000))
                        _listenerThread.Abort();
                }
            }
        }

        // Sending a fake command
        private void btnSendcommand_Click(object sender, EventArgs e)
        {
            if (_running == false && _worker == null)
            {
                string transId = (radioButton2.Checked && transactionIdTextBox.Enabled) ? transactionIdTextBox.Text : "RANDOM";

                btnSendcommand.Enabled = false;
                statusStrip.BackColor = Color.Orange;
                _running = true;

                _worker = new Listener(_config, _activeEquip);
                _worker.EventLog += listener_eventLog;
                _worker.EventComplete += listener_eventComplete;
                _worker.Start("HSR", _activeEquip.Id, transId, _activeCommand);
            }
        }

        private void btnSetParameters_Click(object sender, EventArgs e)
        {
            if (_paramForm == null)
                _paramForm = new ParametersForm(this, _config, _activeEquip.Prefix);
            _paramForm.Show();
            equipmentComboBox.Enabled = false;
        }

        private void btnOtherDowntimes_Click(object sender, EventArgs e)
        {
            if (_downForm == null)
                _downForm = new DowntimesForm(this, _config, _activeEquip.Prefix);
            _downForm.Show();
            equipmentComboBox.Enabled = false;
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(@"Do you really want to quit?", @"Exit", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                btnStop_Click(null, null);
                Application.Exit();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // This will eventually be changed to "notepad.exe" if needed for external use
                System.Diagnostics.Process.Start("notepad++.exe", @"C:\HandshakeEmulator\HandshakeEmulator\HandshakeSettings\Configuration.xml");
            }
            catch(Exception ex)
            {
                lock (_logs)
                {
                    _logs.Add(new Log("CRITICAL", "Configuration file was not found: " + ex.Message));
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _aboutBox.Show();
        }

        private void equipmentComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // This functions performs a set of instructions which aim to set
            // the interface back to a clean initial status every time
            // the user selects a new equipment
            int index = equipmentComboBox.SelectedIndex;

            _activeEquip.Name = _config.EquipmentList[index].Name;
            _activeEquip.Prefix = _config.EquipmentList[index].Prefix;
            _activeEquip.Id = _config.EquipmentList[index].Id;

            equipmentToolStripStatusLabel.Text = _activeEquip.Name;
            _root = _config.UnitName + "\\" + _activeEquip.Prefix;
            _equipStatusParametersTags = _config.Parameters.EquipStatusParameters.ConvertAll(p => _root + p.Suffix).ToArray();

            // Check for an already created ParametersForm
            if (_paramForm != null)
            {
                _paramForm.Dispose();
                _paramForm = null;
            }

            // Check for an already created DowntimesForm
            if (_downForm != null)
            {
                _downForm.Dispose();
                _downForm = null;
            }

            statusStrip.BackColor = Color.Empty;

            UpdateEquipStatus();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                transactionIdTextBox.Enabled = true;
                transactionIdTextBox.Focus();
            }
            else transactionIdTextBox.Enabled = false;
        }

        // Displaying the log file if double click on any entry on the log UI
        private void logListView_MouseDoubleClick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("notepad++.exe", @"C:\LogFiles\HandshakeEmulator\Log.log");
            }
            catch (Exception ex)
            {
                lock (_logs)
                {
                    _logs.Add(new Log("CRITICAL", "Log file was not found: " + ex.Message));
                }
            }
        }

        private void numericErrorCodes_ValueChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_root + _config.ReqErrorCodes, numericErrorCodes.Value);
        }

        private void commandComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _indexCommand = commandComboBox.SelectedIndex;
            _activeCommand = _config.CommandList[_indexCommand].Id;

            if (_activeEquip != null)
                btnSendcommand.Enabled = true;
        }

        private void UpdateEquipStatus()
        {
            RTDSResult[] tagsValues = RTDS.ReadThrough(_equipStatusParametersTags);

            if (!tagsValues[0].IsGood
                && tagsValues[0].Values == null)
            {
                lock (_logs)
                {
                    _logs.Add(new Log(_activeEquip.Name, "ERROR", "RTDS returned null value"));
                }

                btnStart.Enabled = false;
                btnStop.Enabled = false;
                btnSetParameters.Enabled = false;
                btnOtherDowntimes.Enabled = false;
                numericErrorCodes.Enabled = false;

                toolStripStatusLabel2.Text = @"Disconnected";
            }
            else 
            {
                enableCheckboxes();

                if (_activeEquip != null)
                {
                    checkBox1.Checked = (bool) tagsValues[0].Values[0];
                    checkBox2.Checked = (bool) tagsValues[1].Values[0];
                    checkBox3.Checked = (bool) tagsValues[2].Values[0];
                    checkBox4.Checked = (bool) tagsValues[3].Values[0];
                    checkBox5.Checked = (bool) tagsValues[4].Values[0];
                    checkBox6.Checked = (bool) tagsValues[5].Values[0];
                    checkBox7.Checked = (bool) tagsValues[6].Values[0];
                    checkBox8.Checked = (bool) tagsValues[7].Values[0];
                    checkBox9.Checked = (bool) tagsValues[8].Values[0];
                    checkBox10.Checked = (bool) tagsValues[9].Values[0];
                    checkBox11.Checked = (bool) tagsValues[10].Values[0];
                    checkBox12.Checked = (bool) tagsValues[11].Values[0];
                    checkBox13.Checked = (bool) tagsValues[12].Values[0];
                    checkBox14.Checked = (bool) tagsValues[13].Values[0];
                    checkBox15.Checked = (bool) tagsValues[14].Values[0];
                    checkBox16.Checked = (bool) tagsValues[15].Values[0];
                    checkBox17.Checked = (bool) tagsValues[16].Values[0];
                    checkBox18.Checked = (bool) tagsValues[17].Values[0];
                    checkBox19.Checked = (bool) tagsValues[18].Values[0];
                    checkBox20.Checked = (bool) tagsValues[19].Values[0];
                    checkBox21.Checked = (bool) tagsValues[20].Values[0];

                    btnSendcommand.Enabled = true;
                }

                toolStripStatusLabel2.Text = @"Idle";

                btnStart.Enabled = true;
                btnStop.Enabled = true;
                btnSetParameters.Enabled = true;
                btnOtherDowntimes.Enabled = true;

                numericErrorCodes.Enabled = true;
                numericErrorCodes.Value = 0;
                numericErrorCodes_ValueChanged(null, null);
            }
        }

        // Updating all the check boxes
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[0], checkBox1.Checked);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[1], checkBox2.Checked);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[2], checkBox3.Checked);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[3], checkBox4.Checked);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[4], checkBox5.Checked);
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[5], checkBox6.Checked);
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[6], checkBox7.Checked);
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[7], checkBox8.Checked);
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[8], checkBox9.Checked);
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[9], checkBox10.Checked);
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[10], checkBox11.Checked);
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[11], checkBox12.Checked);
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[12], checkBox13.Checked);
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[13], checkBox14.Checked);
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[14], checkBox15.Checked);
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[15], checkBox16.Checked);
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[16], checkBox17.Checked);
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[17], checkBox18.Checked);
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[18], checkBox19.Checked);
        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[19], checkBox20.Checked);
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_equipStatusParametersTags[20], checkBox21.Checked);
        }


        private void enableCheckboxes()
        {
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
            checkBox3.Enabled = true;
            checkBox4.Enabled = true;
            checkBox5.Enabled = true;
            checkBox6.Enabled = true;
            checkBox7.Enabled = true;
            checkBox8.Enabled = true;
            checkBox9.Enabled = true;
            checkBox10.Enabled = true;
            checkBox11.Enabled = true;
            checkBox12.Enabled = true;
            checkBox13.Enabled = true;
            checkBox14.Enabled = true;
            checkBox15.Enabled = true;
            checkBox16.Enabled = true;
            checkBox17.Enabled = true;
            checkBox18.Enabled = true;
            checkBox19.Enabled = true;
            checkBox20.Enabled = true;
            checkBox21.Enabled = true;
        }

        private void disableCheckboxes()
        {
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            checkBox3.Enabled = false;
            checkBox4.Enabled = false;
            checkBox5.Enabled = false;
            checkBox6.Enabled = false;
            checkBox7.Enabled = false;
            checkBox8.Enabled = false;
            checkBox9.Enabled = false;
            checkBox10.Enabled = false;
            checkBox11.Enabled = false;
            checkBox12.Enabled = false;
            checkBox13.Enabled = false;
            checkBox14.Enabled = false;
            checkBox15.Enabled = false;
            checkBox16.Enabled = false;
            checkBox17.Enabled = false;
            checkBox18.Enabled = false;
            checkBox19.Enabled = false;
            checkBox20.Enabled = false;
            checkBox21.Enabled = false;
        }
    }
}
