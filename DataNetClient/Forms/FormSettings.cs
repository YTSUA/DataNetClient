using System;
using System.Windows.Forms;

namespace DataNetClient.Forms
{
    public partial class FormSettings : DevComponents.DotNetBar.Metro.MetroForm
    {
        public FormSettings()
        {
            InitializeComponent();
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            nudEndBar.Value = Properties.Settings.Default.valFinish;            
        }

        private void FormSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.valFinish = (int)(nudEndBar.Value);            
            Properties.Settings.Default.Save();
        }
    }
}