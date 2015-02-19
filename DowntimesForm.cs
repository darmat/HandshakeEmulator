using System;
using System.Windows.Forms;
using HandshakeEmulator.DataStructures;
using SITCAB.RTDS;


namespace HandshakeEmulator
{
    public partial class DowntimesForm : Form
    {
        readonly HandshakeUi _parent;
        readonly string[] _downtimesParametersTags;

        public DowntimesForm(HandshakeUi parent, Configuration config, string activeEquip)
        {
            InitializeComponent();

            string equip = activeEquip;
            string root = config.UnitName + "\\" + equip;
            _parent = parent;

            _downtimesParametersTags = config.Parameters.DowntimesParameters.ConvertAll(p => root + p.Suffix).ToArray();

            RTDSResult[] tagsValues = RTDS.ReadThrough(_downtimesParametersTags);

            if (tagsValues[0].IsGood
                && tagsValues[0].Values != null)
            {
                checkBox1.Checked = (bool)tagsValues[0].Values[0];
                checkBox2.Checked = (bool)tagsValues[1].Values[0];
                checkBox3.Checked = (bool)tagsValues[2].Values[0];
                checkBox4.Checked = (bool)tagsValues[3].Values[0];
                checkBox5.Checked = (bool)tagsValues[4].Values[0];
                checkBox6.Checked = (bool)tagsValues[5].Values[0];
                checkBox7.Checked = (bool)tagsValues[6].Values[0];
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[0], checkBox1.Checked);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[1], checkBox2.Checked);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[2], checkBox3.Checked);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[3], checkBox4.Checked);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[4], checkBox5.Checked);
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[5], checkBox6.Checked);
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_downtimesParametersTags[6], checkBox7.Checked);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _parent.ReturnToForm();
            Hide();
        }
    }
}
