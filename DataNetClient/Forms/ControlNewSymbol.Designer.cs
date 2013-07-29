namespace DataNetClient.Forms
{
    partial class ControlNewSymbol
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
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.saveButton = new DevComponents.DotNetBar.ButtonX();
            this.cancelButtonX = new DevComponents.DotNetBar.ButtonX();
            this.labelXTitle = new DevComponents.DotNetBar.LabelX();
            this.labelX1 = new DevComponents.DotNetBar.LabelX();
            this.ui_textBoxXSymbolName = new DevComponents.DotNetBar.Controls.TextBoxX();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // saveButton
            // 
            this.saveButton.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.saveButton.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.saveButton.Location = new System.Drawing.Point(225, 223);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(92, 29);
            this.saveButton.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.saveButton.TabIndex = 74;
            this.saveButton.Text = "Add";
            this.toolTip1.SetToolTip(this.saveButton, "Add symbol to list");
            // 
            // cancelButtonX
            // 
            this.cancelButtonX.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
            this.cancelButtonX.ColorTable = DevComponents.DotNetBar.eButtonColor.OrangeWithBackground;
            this.cancelButtonX.Location = new System.Drawing.Point(412, 223);
            this.cancelButtonX.Name = "cancelButtonX";
            this.cancelButtonX.Size = new System.Drawing.Size(92, 29);
            this.cancelButtonX.Style = DevComponents.DotNetBar.eDotNetBarStyle.StyleManagerControlled;
            this.cancelButtonX.TabIndex = 75;
            this.cancelButtonX.Text = "Cancel";
            this.toolTip1.SetToolTip(this.cancelButtonX, "Add symbol to list");
            // 
            // labelXTitle
            // 
            // 
            // 
            // 
            this.labelXTitle.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelXTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelXTitle.Location = new System.Drawing.Point(72, 12);
            this.labelXTitle.Name = "labelXTitle";
            this.labelXTitle.Size = new System.Drawing.Size(228, 32);
            this.labelXTitle.TabIndex = 73;
            this.labelXTitle.Text = "NEW SYMBOL";
            // 
            // labelX1
            // 
            // 
            // 
            // 
            this.labelX1.BackgroundStyle.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.labelX1.Location = new System.Drawing.Point(225, 158);
            this.labelX1.Name = "labelX1";
            this.labelX1.Size = new System.Drawing.Size(75, 20);
            this.labelX1.TabIndex = 72;
            this.labelX1.Text = "Symbol Name:";
            this.labelX1.TextAlignment = System.Drawing.StringAlignment.Far;
            // 
            // ui_textBoxXSymbolName
            // 
            this.ui_textBoxXSymbolName.BackColor = System.Drawing.Color.White;
            // 
            // 
            // 
            this.ui_textBoxXSymbolName.Border.Class = "TextBoxBorder";
            this.ui_textBoxXSymbolName.Border.CornerType = DevComponents.DotNetBar.eCornerType.Square;
            this.ui_textBoxXSymbolName.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ui_textBoxXSymbolName.ForeColor = System.Drawing.Color.Black;
            this.ui_textBoxXSymbolName.Location = new System.Drawing.Point(225, 184);
            this.ui_textBoxXSymbolName.Name = "ui_textBoxXSymbolName";
            this.ui_textBoxXSymbolName.Size = new System.Drawing.Size(279, 22);
            this.ui_textBoxXSymbolName.TabIndex = 71;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::DataNetClient.Properties.Resources.backbutton1;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(44, 44);
            this.pictureBox1.TabIndex = 76;
            this.pictureBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox1, "Cancel");
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // ControlNewSymbol
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.cancelButtonX);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.labelXTitle);
            this.Controls.Add(this.labelX1);
            this.Controls.Add(this.ui_textBoxXSymbolName);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "ControlNewSymbol";
            this.Size = new System.Drawing.Size(752, 416);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip toolTip1;
        internal DevComponents.DotNetBar.ButtonX saveButton;
        internal DevComponents.DotNetBar.LabelX labelXTitle;
        internal DevComponents.DotNetBar.LabelX labelX1;
        internal DevComponents.DotNetBar.Controls.TextBoxX ui_textBoxXSymbolName;
        internal DevComponents.DotNetBar.ButtonX cancelButtonX;
        internal System.Windows.Forms.PictureBox pictureBox1;

    }
}
