namespace DataNetClient.Forms
{
    partial class FormSettings
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSettings));
            this.superTabControl1 = new DevComponents.DotNetBar.SuperTabControl();
            this.superTabControlPanel2 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.labelX14 = new DevComponents.DotNetBar.LabelX();
            this.nudEndBar = new System.Windows.Forms.NumericUpDown();
            this.superTabItem2 = new DevComponents.DotNetBar.SuperTabItem();
            this.superTabControlPanel9 = new DevComponents.DotNetBar.SuperTabControlPanel();
            this.labelX5 = new DevComponents.DotNetBar.LabelX();
            this.superTabControlPanel8 = new DevComponents.DotNetBar.SuperTabControlPanel();
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).BeginInit();
            this.superTabControl1.SuspendLayout();
            this.superTabControlPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudEndBar)).BeginInit();
            this.superTabControlPanel9.SuspendLayout();
            this.SuspendLayout();
            // 
            // superTabControl1
            // 
            this.superTabControl1.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            // 
            // 
            // 
            this.superTabControl1.ControlBox.CloseBox.Name = "";
            // 
            // 
            // 
            this.superTabControl1.ControlBox.MenuBox.Name = "";
            this.superTabControl1.ControlBox.Name = "";
            this.superTabControl1.ControlBox.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabControl1.ControlBox.MenuBox,
            this.superTabControl1.ControlBox.CloseBox});
            this.superTabControl1.Controls.Add(this.superTabControlPanel2);
            this.superTabControl1.Controls.Add(this.superTabControlPanel9);
            this.superTabControl1.Controls.Add(this.superTabControlPanel8);
            this.superTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControl1.FixedTabSize = new System.Drawing.Size(90, 0);
            this.superTabControl1.ForeColor = System.Drawing.Color.Black;
            this.superTabControl1.Location = new System.Drawing.Point(0, 0);
            this.superTabControl1.Name = "superTabControl1";
            this.superTabControl1.ReorderTabsEnabled = true;
            this.superTabControl1.SelectedTabFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.superTabControl1.SelectedTabIndex = 0;
            this.superTabControl1.Size = new System.Drawing.Size(407, 305);
            this.superTabControl1.TabAlignment = DevComponents.DotNetBar.eTabStripAlignment.Left;
            this.superTabControl1.TabFont = new System.Drawing.Font("Segoe UI", 9F);
            this.superTabControl1.TabIndex = 2;
            this.superTabControl1.Tabs.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.superTabItem2});
            this.superTabControl1.TabStyle = DevComponents.DotNetBar.eSuperTabStyle.Office2010BackstageBlue;
            this.superTabControl1.TabVerticalSpacing = 3;
            // 
            // superTabControlPanel2
            // 
            this.superTabControlPanel2.Controls.Add(this.labelX14);
            this.superTabControlPanel2.Controls.Add(this.nudEndBar);
            this.superTabControlPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel2.Location = new System.Drawing.Point(92, 0);
            this.superTabControlPanel2.Name = "superTabControlPanel2";
            this.superTabControlPanel2.Size = new System.Drawing.Size(315, 305);
            this.superTabControlPanel2.TabIndex = 1;
            this.superTabControlPanel2.TabItem = this.superTabItem2;
            // 
            // labelX14
            // 
            this.labelX14.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.labelX14.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX14.ForeColor = System.Drawing.Color.Black;
            this.labelX14.Location = new System.Drawing.Point(22, 26);
            this.labelX14.Name = "labelX14";
            this.labelX14.Size = new System.Drawing.Size(151, 23);
            this.labelX14.TabIndex = 43;
            this.labelX14.Text = "Default value for Finish:";
            this.labelX14.TextAlignment = System.Drawing.StringAlignment.Far;
            // 
            // nudEndBar
            // 
            this.nudEndBar.BackColor = System.Drawing.Color.White;
            this.nudEndBar.ForeColor = System.Drawing.Color.Black;
            this.nudEndBar.Location = new System.Drawing.Point(179, 26);
            this.nudEndBar.Maximum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nudEndBar.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.nudEndBar.Name = "nudEndBar";
            this.nudEndBar.Size = new System.Drawing.Size(124, 22);
            this.nudEndBar.TabIndex = 42;
            this.nudEndBar.Value = new decimal(new int[] {
            3000,
            0,
            0,
            -2147483648});
            // 
            // superTabItem2
            // 
            this.superTabItem2.AttachedControl = this.superTabControlPanel2;
            this.superTabItem2.GlobalItem = false;
            this.superTabItem2.Name = "superTabItem2";
            this.superTabItem2.Text = "General";
            // 
            // superTabControlPanel9
            // 
            this.superTabControlPanel9.Controls.Add(this.labelX5);
            this.superTabControlPanel9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel9.Location = new System.Drawing.Point(92, 0);
            this.superTabControlPanel9.Name = "superTabControlPanel9";
            this.superTabControlPanel9.Size = new System.Drawing.Size(315, 305);
            this.superTabControlPanel9.TabIndex = 3;
            // 
            // labelX5
            // 
            this.labelX5.BackColor = System.Drawing.Color.Transparent;
            // 
            // 
            // 
            this.labelX5.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelX5.ForeColor = System.Drawing.Color.Black;
            this.labelX5.Location = new System.Drawing.Point(0, 0);
            this.labelX5.Name = "labelX5";
            this.labelX5.Size = new System.Drawing.Size(315, 305);
            this.labelX5.TabIndex = 7;
            this.labelX5.TextAlignment = System.Drawing.StringAlignment.Center;
            // 
            // superTabControlPanel8
            // 
            this.superTabControlPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.superTabControlPanel8.Location = new System.Drawing.Point(92, 0);
            this.superTabControlPanel8.Name = "superTabControlPanel8";
            this.superTabControlPanel8.Size = new System.Drawing.Size(315, 305);
            this.superTabControlPanel8.TabIndex = 2;
            // 
            // FormSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(407, 305);
            this.Controls.Add(this.superTabControl1);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSettings_FormClosing);
            this.Load += new System.EventHandler(this.FormSettings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.superTabControl1)).EndInit();
            this.superTabControl1.ResumeLayout(false);
            this.superTabControlPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudEndBar)).EndInit();
            this.superTabControlPanel9.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevComponents.DotNetBar.SuperTabControl superTabControl1;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel2;
        private DevComponents.DotNetBar.SuperTabItem superTabItem2;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel9;
        private DevComponents.DotNetBar.LabelX labelX5;
        private DevComponents.DotNetBar.SuperTabControlPanel superTabControlPanel8;
        private DevComponents.DotNetBar.LabelX labelX14;
        private System.Windows.Forms.NumericUpDown nudEndBar;

    }
}