using System;
using System.Windows.Forms;
using CQG;

namespace DataNetClient.Forms
{
    public partial class ControlEditList : UserControl
    {
        public ControlEditList()
        {
            InitializeComponent();
        }

        private MetroBillCommands _Commands;
        /// <summary>
        /// Gets or sets the commands associated with the control.
        /// </summary>
        public MetroBillCommands Commands
        {
            get { return _Commands; }
            set
            {
                if (value != _Commands)
                {
                    MetroBillCommands oldValue = _Commands;
                    _Commands = value;
                    OnCommandsChanged(oldValue, value);
                }
            }
        }
        /// <summary>
        /// Called when Commands property has changed.
        /// </summary>
        /// <param name="oldValue">Old property value</param>
        /// <param name="newValue">New property value</param>
        protected virtual void OnCommandsChanged(MetroBillCommands oldValue, MetroBillCommands newValue)
        {
            if (newValue != null)
            {
                saveButton.Command = newValue.EditListCommands.Save;
                cancelButton.Command = newValue.EditListCommands.Cancel;                
            }
            else
            {
                saveButton.Command = null;
                cancelButton.Command = null;
            }
        }


        private void btnAdd_Click(object sender, EventArgs e)
        {
            foreach (String item in lbAvbList.SelectedItems)
            {
                lbSelList.Items.Add(item);
            }
            for (int i = 0; i < lbSelList.Items.Count; i++)
            {
                lbAvbList.Items.Remove(lbSelList.Items[i]);
            }
            //lbSelList.Items.Add(lbAvbList.SelectedItems);
            //lbAvbList.Items.Remove(lbAvbList.SelectedItems);
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
                lbAvbList.SetSelected(0, true);
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void btnAddAll_Click(object sender, EventArgs e)
        {
            lbSelList.Items.AddRange(lbAvbList.Items);
            lbAvbList.Items.Clear();
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void btnRemov_Click(object sender, EventArgs e)
        {
            foreach (String item in lbSelList.SelectedItems)
            {
                lbAvbList.Items.Add(item);
            }
            foreach (String item in lbAvbList.Items)
            {
                lbSelList.Items.Remove(item);
            }
            //lbAvbList.Items.Add(lbSelList.SelectedItems);
            //lbSelList.Items.Remove(lbSelList.SelectedItems);
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
                lbSelList.SetSelected(0, true);
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void btnRemovAll_Click(object sender, EventArgs e)
        {
            //lbAvbList.Items.AddRange(lbSelList.Items);
            foreach (String item in lbSelList.Items)
            {
                lbAvbList.Items.Add(item);
            }

            foreach (String item in lbAvbList.Items)
            {
                lbSelList.Items.Remove(item);
            }
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            saveButton.Command.Execute();
        }

        private void lbAvbList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void lbSelList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void lbSelList_Click(object sender, EventArgs e)
        {
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void lbAvbList_Click(object sender, EventArgs e)
        {
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
        }

        private void lbAvbList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void lbSelList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (lbSelList.Items.Count > 0)
            {
                btnRemov.Enabled = true;
                btnRemovAll.Enabled = true;
            }
            else
            {
                btnRemov.Enabled = false;
                btnRemovAll.Enabled = false;
            }
            if (lbAvbList.Items.Count > 0)
            {
                btnAdd.Enabled = true;
                btnAddAll.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = false;
                btnAddAll.Enabled = false;
            }
        }

        private void EditListControl_Load(object sender, EventArgs e)
        {
            this.cmbContinuationType.Items.Clear();
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctNoContinuation);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctStandard);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctStandardByMonth);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctActive);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctActiveByMonth);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctAdjusted);
            this.cmbContinuationType.Items.Add(eTimeSeriesContinuationType.tsctAdjustedByMonth);
            this.cmbContinuationType.SelectedIndex = 0;
            cmbHistoricalPeriod.SelectedIndex = 0;
        }

        private void checkBoxUseTI_CheckStateChanged(object sender, EventArgs e)
        {
             if (checkBoxUseTI.Checked)
                 grbTimeInterval.Enabled = true;
             else
                 grbTimeInterval.Enabled = false;
        }
    }
}
