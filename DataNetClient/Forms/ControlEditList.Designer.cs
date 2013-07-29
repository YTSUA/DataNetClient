namespace DataNetClient.Forms
{
    partial class ControlEditList
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelEx1 = new DevComponents.DotNetBar.PanelEx();
            this.saveButton = new DevComponents.DotNetBar.ButtonX();
            this.checkBoxUseTI = new System.Windows.Forms.CheckBox();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.labelX4 = new DevComponents.DotNetBar.LabelX();
            this.cmbHistoricalPeriod = new System.Windows.Forms.ComboBox();
            this.cmbContinuationType = new System.Windows.Forms.ComboBox();
            this.btnRemovAll = new System.Windows.Forms.Button();
            this.btnRemov = new System.Windows.Forms.Button();
            this.btnAddAll = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.lbSelList = new System.Windows.Forms.ListBox();
            this.lbAvbList = new System.Windows.Forms.ListBox();
            this.labelX3 = new DevComponents.DotNetBar.LabelX();
            this.labelX2 = new DevComponents.DotNetBar.LabelX();
            this.grbTimeInterval = new System.Windows.Forms.GroupBox();
            this.labelX7 = new DevComponents.DotNetBar.LabelX();
            this.labelX6 = new DevComponents.DotNetBar.LabelX();
            this.endTimeCollect = new System.Windows.Forms.DateTimePicker();
            this.startTimeCollect = new System.Windows.Forms.DateTimePicker();
            this.cancelButton = new DevComponents.DotNetBar.ButtonX();
            this.labelXTitle = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.textBoxXListName = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panelEx1.SuspendLayout();
            this.grbTimeInterval.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelEx1
            // 
            this.panelEx1.CanvasColor = System.Drawing.SystemColors.Control;
            this.panelEx1.ColorSchemeStyle = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.panelEx1.Controls.Add(this.saveButton);
            this.panelEx1.Controls.Add(this.checkBoxUseTI);
            this.panelEx1.Controls.Add(this.labelX5);
            this.panelEx1.Controls.Add(this.labelX4);
            this.panelEx1.Controls.Add(this.cmbHistoricalPeriod);
            this.panelEx1.Controls.Add(this.cmbContinuationType);
            this.panelEx1.Controls.Add(this.btnRemovAll);
            this.panelEx1.Controls.Add(this.btnRemov);
            this.panelEx1.Controls.Add(this.btnAddAll);
            this.panelEx1.Controls.Add(this.btnAdd);
            this.panelEx1.Controls.Add(this.lbSelList);
            this.panelEx1.Controls.Add(this.lbAvbList);
            this.panelEx1.Controls.Add(this.labelX3);
            this.panelEx1.Controls.Add(this.labelX2);
            this.panelEx1.Controls.Add(this.grbTimeInterval);
            this.panelEx1.Controls.Add(this.cancelButton);
            this.panelEx1.Controls.Add(this.labelXTitle);
            this.panelEx1.Controls.Add(this.labelX1);
            this.panelEx1.Controls.Add(this.textBoxXListName);
            this.panelEx1.Location = new System.Drawing.Point(85, 0);
            this.panelEx1.Name = "panelEx1";
            this.panelEx1.Size = new System.Drawing.Size(577, 416);
            this.panelEx1.Style.Alignment = System.Drawing.StringAlignment.Center;
            this.panelEx1.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.BarBackground;
            this.panelEx1.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
            this.panelEx1.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
            this.panelEx1.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
            this.panelEx1.Style.GradientAngle = 90;
            this.panelEx1.TabIndex = 11;
            // 
            // saveButton
            // 
            this.saveButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.saveButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.saveButton.Location = new System.Drawing.Point(361, 369);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(92, 29);
            this.saveButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.saveButton.TabIndex = 70;
            this.saveButton.Visible = false;
            // 
            // checkBoxUseTI
            // 
            this.checkBoxUseTI.AutoSize = true;
            this.checkBoxUseTI.Location = new System.Drawing.Point(37, 129);
            this.checkBoxUseTI.Name = "checkBoxUseTI";
            this.checkBoxUseTI.Size = new System.Drawing.Size(111, 17);
            this.checkBoxUseTI.TabIndex = 69;
            this.checkBoxUseTI.Text = "Use time interval";
            this.checkBoxUseTI.UseVisualStyleBackColor = true;
            this.checkBoxUseTI.CheckStateChanged += new System.EventHandler(this.checkBoxUseTI_CheckStateChanged);
            // 
            // labelX5
            // 
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Location = new System.Drawing.Point(343, 170);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(97, 20);
            this.labelX5.TabIndex = 68;
            this.labelX5.Text = "Selected symbols:";
            // 
            // labelX4
            // 
            // 
            // 
            // 
            this.labelX4.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX4.Location = new System.Drawing.Point(16, 170);
            this.labelX4.Name = "labelX4";
            this.labelX4.Size = new System.Drawing.Size(94, 20);
            this.labelX4.TabIndex = 67;
            this.labelX4.Text = "Available symbols:";
            // 
            // cmbHistoricalPeriod
            // 
            this.cmbHistoricalPeriod.BackColor = System.Drawing.Color.White;
            this.cmbHistoricalPeriod.DropDownHeight = 250;
            this.cmbHistoricalPeriod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHistoricalPeriod.ForeColor = System.Drawing.Color.Black;
            this.cmbHistoricalPeriod.FormattingEnabled = true;
            this.cmbHistoricalPeriod.IntegralHeight = false;
            this.cmbHistoricalPeriod.Items.AddRange(new object[] {
            "1 minute",
            "2 minutes",
            "3 minutes",
            "5 minutes",
            "10 minutes",
            "15 minutes",
            "30 minutes",
            "60 minutes",
            "240 minutes",
            "Daily",
            "Weekly",
            "Monthly",
            "Quarterly",
            "Semiannual",
            "Yearly"});
            this.cmbHistoricalPeriod.Location = new System.Drawing.Point(97, 81);
            this.cmbHistoricalPeriod.Name = "cmbHistoricalPeriod";
            this.cmbHistoricalPeriod.Size = new System.Drawing.Size(147, 21);
            this.cmbHistoricalPeriod.TabIndex = 66;
            // 
            // cmbContinuationType
            // 
            this.cmbContinuationType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbContinuationType.BackColor = System.Drawing.Color.White;
            this.cmbContinuationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbContinuationType.ForeColor = System.Drawing.Color.Black;
            this.cmbContinuationType.FormattingEnabled = true;
            this.cmbContinuationType.Location = new System.Drawing.Point(385, 81);
            this.cmbContinuationType.Name = "cmbContinuationType";
            this.cmbContinuationType.Size = new System.Drawing.Size(172, 21);
            this.cmbContinuationType.TabIndex = 65;
            // 
            // btnRemovAll
            // 
            this.btnRemovAll.Enabled = false;
            this.btnRemovAll.Location = new System.Drawing.Point(252, 300);
            this.btnRemovAll.Name = "btnRemovAll";
            this.btnRemovAll.Size = new System.Drawing.Size(75, 23);
            this.btnRemovAll.TabIndex = 61;
            this.btnRemovAll.Text = "<<";
            this.btnRemovAll.UseVisualStyleBackColor = true;
            this.btnRemovAll.Click += new System.EventHandler(this.btnRemovAll_Click);
            // 
            // btnRemov
            // 
            this.btnRemov.Enabled = false;
            this.btnRemov.Location = new System.Drawing.Point(252, 271);
            this.btnRemov.Name = "btnRemov";
            this.btnRemov.Size = new System.Drawing.Size(75, 23);
            this.btnRemov.TabIndex = 62;
            this.btnRemov.Text = "<";
            this.btnRemov.UseVisualStyleBackColor = true;
            this.btnRemov.Click += new System.EventHandler(this.btnRemov_Click);
            // 
            // btnAddAll
            // 
            this.btnAddAll.Enabled = false;
            this.btnAddAll.Location = new System.Drawing.Point(252, 242);
            this.btnAddAll.Name = "btnAddAll";
            this.btnAddAll.Size = new System.Drawing.Size(75, 23);
            this.btnAddAll.TabIndex = 59;
            this.btnAddAll.Text = ">>";
            this.btnAddAll.UseVisualStyleBackColor = true;
            this.btnAddAll.Click += new System.EventHandler(this.btnAddAll_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Enabled = false;
            this.btnAdd.Location = new System.Drawing.Point(252, 213);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 60;
            this.btnAdd.Text = ">";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // lbSelList
            // 
            this.lbSelList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lbSelList.FormattingEnabled = true;
            this.lbSelList.Location = new System.Drawing.Point(343, 190);
            this.lbSelList.Name = "lbSelList";
            this.lbSelList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbSelList.Size = new System.Drawing.Size(219, 160);
            this.lbSelList.TabIndex = 58;
            this.lbSelList.Click += new System.EventHandler(this.lbSelList_Click);
            this.lbSelList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbSelList_DrawItem);
            this.lbSelList.SelectedIndexChanged += new System.EventHandler(this.lbSelList_SelectedIndexChanged);
            // 
            // lbAvbList
            // 
            this.lbAvbList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.lbAvbList.FormattingEnabled = true;
            this.lbAvbList.Location = new System.Drawing.Point(16, 190);
            this.lbAvbList.Name = "lbAvbList";
            this.lbAvbList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbAvbList.Size = new System.Drawing.Size(219, 160);
            this.lbAvbList.Sorted = true;
            this.lbAvbList.TabIndex = 57;
            this.lbAvbList.Click += new System.EventHandler(this.lbAvbList_Click);
            this.lbAvbList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbAvbList_DrawItem);
            this.lbAvbList.SelectedIndexChanged += new System.EventHandler(this.lbAvbList_SelectedIndexChanged);
            // 
            // labelX3
            // 
            // 
            // 
            // 
            this.labelX3.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX3.Location = new System.Drawing.Point(252, 81);
            this.labelX3.Name = "labelX3";
            this.labelX3.Size = new System.Drawing.Size(129, 20);
            this.labelX3.TabIndex = 56;
            this.labelX3.Text = "Continuation Types:";
            this.labelX3.TextAlignment = System.Drawing.StringAlignment.Far;
            // 
            // labelX2
            // 
            // 
            // 
            // 
            this.labelX2.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX2.Location = new System.Drawing.Point(16, 81);
            this.labelX2.Name = "labelX2";
            this.labelX2.Size = new System.Drawing.Size(75, 20);
            this.labelX2.TabIndex = 55;
            this.labelX2.Text = "Timeframe:";
            this.labelX2.TextAlignment = System.Drawing.StringAlignment.Far;
            // 
            // grbTimeInterval
            // 
            this.grbTimeInterval.Controls.Add(this.labelX7);
            this.grbTimeInterval.Controls.Add(this.labelX6);
            this.grbTimeInterval.Controls.Add(this.endTimeCollect);
            this.grbTimeInterval.Controls.Add(this.startTimeCollect);
            this.grbTimeInterval.Enabled = false;
            this.grbTimeInterval.Location = new System.Drawing.Point(186, 108);
            this.grbTimeInterval.Name = "grbTimeInterval";
            this.grbTimeInterval.Size = new System.Drawing.Size(376, 56);
            this.grbTimeInterval.TabIndex = 50;
            this.grbTimeInterval.TabStop = false;
            this.grbTimeInterval.Text = "Time interval";
            // 
            // labelX7
            // 
            // 
            // 
            // 
            this.labelX7.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX7.Location = new System.Drawing.Point(197, 9);
            this.labelX7.Name = "labelX7";
            this.labelX7.Size = new System.Drawing.Size(88, 20);
            this.labelX7.TabIndex = 70;
            this.labelX7.Text = "End time point:";
            // 
            // labelX6
            // 
            // 
            // 
            // 
            this.labelX6.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX6.Location = new System.Drawing.Point(16, 9);
            this.labelX6.Name = "labelX6";
            this.labelX6.Size = new System.Drawing.Size(109, 20);
            this.labelX6.TabIndex = 69;
            this.labelX6.Text = "Start time point:";
            // 
            // endTimeCollect
            // 
            this.endTimeCollect.Location = new System.Drawing.Point(197, 30);
            this.endTimeCollect.Name = "endTimeCollect";
            this.endTimeCollect.Size = new System.Drawing.Size(145, 22);
            this.endTimeCollect.TabIndex = 39;
            // 
            // startTimeCollect
            // 
            this.startTimeCollect.Location = new System.Drawing.Point(16, 30);
            this.startTimeCollect.Name = "startTimeCollect";
            this.startTimeCollect.Size = new System.Drawing.Size(132, 22);
            this.startTimeCollect.TabIndex = 38;
            this.startTimeCollect.Value = new System.DateTime(2012, 9, 5, 0, 0, 0, 0);
            // 
            // cancelButton
            // 
            this.cancelButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.cancelButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.cancelButton.Location = new System.Drawing.Point(470, 369);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(92, 29);
            this.cancelButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cancelButton.TabIndex = 21;
            this.cancelButton.Text = "Cancel";
            this.toolTip1.SetToolTip(this.cancelButton, "Return without saving");
            // 
            // labelXTitle
            // 
            // 
            // 
            // 
            this.labelXTitle.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelXTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelXTitle.Location = new System.Drawing.Point(16, 10);
            this.labelXTitle.Name = "labelXTitle";
            this.labelXTitle.Size = new System.Drawing.Size(228, 32);
            this.labelXTitle.TabIndex = 19;
            this.labelXTitle.Text = "EDIT SYMBOLS LIST";
            // 
            // labelX1
            // 
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(16, 48);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(75, 20);
            this.labelX1.TabIndex = 12;
            this.labelX1.Text = "List Name:";
            this.labelX1.TextAlignment = System.Drawing.StringAlignment.Far;
            // 
            // textBoxXListName
            // 
            this.textBoxXListName.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.textBoxXListName.Border.Class = "TextBoxBorder";
            this.textBoxXListName.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.textBoxXListName.ForeColor = System.Drawing.Color.Black;
            this.textBoxXListName.Location = new System.Drawing.Point(97, 48);
            this.textBoxXListName.Name = "textBoxXListName";
            this.textBoxXListName.Size = new System.Drawing.Size(460, 22);
            this.textBoxXListName.TabIndex = 11;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::DataNetClient.Properties.Resources.backbutton1;
            this.pictureBox1.Location = new System.Drawing.Point(3, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(76, 416);
            this.pictureBox1.TabIndex = 12;
            this.pictureBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox1, "Save and return");
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // EditListControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panelEx1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "EditListControl";
            this.Size = new System.Drawing.Size(752, 416);
            this.Load += new System.EventHandler(this.EditListControl_Load);
            this.panelEx1.ResumeLayout(false);
            this.panelEx1.PerformLayout();
            this.grbTimeInterval.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        internal DevComponents.DotNetBar.Controls.TextBoxX textBoxXListName;
        internal DevComponents.DotNetBar.PanelEx panelEx1;
        internal DevComponents.DotNetBar.ButtonX cancelButton;
        internal DevComponents.DotNetBar.LabelX labelXTitle;
        internal DevComponents.DotNetBar.LabelX labelX1;
        internal System.Windows.Forms.GroupBox grbTimeInterval;
        internal System.Windows.Forms.DateTimePicker endTimeCollect;
        internal System.Windows.Forms.DateTimePicker startTimeCollect;
        internal DevComponents.DotNetBar.LabelX labelX3;
        internal DevComponents.DotNetBar.LabelX labelX2;
        internal System.Windows.Forms.Button btnRemovAll;
        internal System.Windows.Forms.Button btnRemov;
        internal System.Windows.Forms.Button btnAddAll;
        internal System.Windows.Forms.Button btnAdd;
        internal System.Windows.Forms.ListBox lbSelList;
        internal System.Windows.Forms.ListBox lbAvbList;
        internal System.Windows.Forms.PictureBox pictureBox1;
        internal System.Windows.Forms.ComboBox cmbHistoricalPeriod;
        internal System.Windows.Forms.ComboBox cmbContinuationType;
        internal DevComponents.DotNetBar.LabelX labelX5;
        internal DevComponents.DotNetBar.LabelX labelX4;
        internal DevComponents.DotNetBar.LabelX labelX7;
        internal DevComponents.DotNetBar.LabelX labelX6;
        internal System.Windows.Forms.CheckBox checkBoxUseTI;
        internal DevComponents.DotNetBar.ButtonX saveButton;
        private System.Windows.Forms.ToolTip toolTip1;

    }
}
