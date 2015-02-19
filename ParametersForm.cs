using System;
using System.Windows.Forms;
using HandshakeEmulator.DataStructures;
using SITCAB.RTDS;


namespace HandshakeEmulator
{
    public partial class ParametersForm : Form
    {
        readonly HandshakeUi _parent;
        readonly string[] _tags = new string[12];

        public ParametersForm(HandshakeUi parent, Configuration config, string activeEquip)
        {
            InitializeComponent();

            string root = config.UnitName + "\\" + activeEquip;
            _parent = parent;

            // These are all the tags that we are interested in
            _tags[0] = root + "STS_Req_String_1";
            _tags[1] = root + "STS_Req_String_2";
            _tags[2] = root + "STS_Req_Float_1";
            _tags[3] = root + "STS_Req_Float_2";
            _tags[4] = root + "STS_Req_Float_3";
            _tags[5] = root + "STS_Req_Float_4";
            _tags[6] = root + "STS_Req_Float_5";
            _tags[7] = root + "STS_Req_Float_6";
            _tags[8] = root + "STS_Req_Float_7";
            _tags[9] = root + "STS_Req_Int_1";
            _tags[10] = root + "STS_Req_Int_2";
            _tags[11] = root + "STS_Req_Int_3";

            RTDSResult[] tagsValues = RTDS.ReadThrough(_tags);

            if (tagsValues[0].IsGood
                && tagsValues[0].Values != null)
            {
                textBox1.Text = tagsValues[0].Values[0].ToString();
                textBox2.Text = tagsValues[1].Values[0].ToString();
                textBox3.Text = tagsValues[2].Values[0].ToString();
                textBox4.Text = tagsValues[3].Values[0].ToString();
                textBox5.Text = tagsValues[4].Values[0].ToString();
                textBox6.Text = tagsValues[5].Values[0].ToString();
                textBox7.Text = tagsValues[6].Values[0].ToString();
                textBox8.Text = tagsValues[7].Values[0].ToString();
                textBox9.Text = tagsValues[8].Values[0].ToString();
                textBox10.Text = tagsValues[9].Values[0].ToString();
                textBox11.Text = tagsValues[10].Values[0].ToString();
                textBox12.Text = tagsValues[11].Values[0].ToString();
            }
        }

        private void ParametersForm_Load(object sender, EventArgs e)
        {

        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            RTDS.WriteThrough(_tags[0], textBox1.Text);
            RTDS.WriteThrough(_tags[1], textBox2.Text);
            RTDS.WriteThrough(_tags[2], textBox3.Text);
            RTDS.WriteThrough(_tags[3], textBox4.Text);
            RTDS.WriteThrough(_tags[4], textBox5.Text);
            RTDS.WriteThrough(_tags[5], textBox6.Text);
            RTDS.WriteThrough(_tags[6], textBox7.Text);
            RTDS.WriteThrough(_tags[7], textBox8.Text);
            RTDS.WriteThrough(_tags[8], textBox9.Text);
            RTDS.WriteThrough(_tags[9], textBox10.Text);
            RTDS.WriteThrough(_tags[10], textBox11.Text);
            RTDS.WriteThrough(_tags[11], textBox12.Text);

            _parent.ReturnToForm();
            Hide();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _parent.ReturnToForm();
            Hide();
        }

        private string _readRTDSTag(string tag)
        {
            var res = RTDS.ReadThrough(tag);
            return (res != null && res.Values[0] != null && res.IsGood) ? res.Values[0].ToString() : "null";
        }

        private void label1_Click(object sender, EventArgs e)
        {
            textBox1.Text = _readRTDSTag(_tags[0]);
        }

        private void label2_Click(object sender, EventArgs e)
        {
            textBox2.Text = _readRTDSTag(_tags[1]);
        }

        private void label3_Click(object sender, EventArgs e)
        {
            textBox3.Text = _readRTDSTag(_tags[2]);
        }

        private void label4_Click(object sender, EventArgs e)
        {
            textBox4.Text = _readRTDSTag(_tags[3]);
        }

        private void label5_Click(object sender, EventArgs e)
        {
            textBox5.Text = _readRTDSTag(_tags[4]);
        }

        private void label6_Click(object sender, EventArgs e)
        {
            textBox6.Text = _readRTDSTag(_tags[5]);
        }

        private void label7_Click(object sender, EventArgs e)
        {
            textBox7.Text = _readRTDSTag(_tags[6]);
        }

        private void label8_Click(object sender, EventArgs e)
        {
            textBox8.Text = _readRTDSTag(_tags[7]);
        }

        private void label9_Click(object sender, EventArgs e)
        {
            textBox9.Text = _readRTDSTag(_tags[8]);
        }

        private void label10_Click(object sender, EventArgs e)
        {
            textBox10.Text = _readRTDSTag(_tags[9]);
        }

        private void label11_Click(object sender, EventArgs e)
        {
            textBox11.Text = _readRTDSTag(_tags[10]);
        }

        private void label12_Click(object sender, EventArgs e)
        {
            textBox12.Text = _readRTDSTag(_tags[11]);
        }
    }
}
