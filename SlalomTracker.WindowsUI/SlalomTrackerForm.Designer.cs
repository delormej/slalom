namespace WindowsFormsApp1
{
    partial class SlalomTrackerForm
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
            this._panel1 = new System.Windows.Forms.Panel();
            this._txtRadS = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this._txtBoadSpeedMps = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._cmbRopeM = new System.Windows.Forms.ComboBox();
            this._btnDraw = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this._panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _panel1
            // 
            this._panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this._panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._panel1.Controls.Add(this.vScrollBar1);
            this._panel1.Location = new System.Drawing.Point(136, 24);
            this._panel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._panel1.Name = "_panel1";
            this._panel1.Size = new System.Drawing.Size(305, 511);
            this._panel1.TabIndex = 0;
            this._panel1.Paint += new System.Windows.Forms.PaintEventHandler(this._panel1_Paint_1);
            // 
            // _txtRadS
            // 
            this._txtRadS.Location = new System.Drawing.Point(11, 72);
            this._txtRadS.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._txtRadS.Name = "_txtRadS";
            this._txtRadS.Size = new System.Drawing.Size(76, 20);
            this._txtRadS.TabIndex = 1;
            this._txtRadS.Text = "1.2";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 55);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Swing Speed (rad/s)";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 102);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Boat Speed (mps)";
            // 
            // _txtBoadSpeedMps
            // 
            this._txtBoadSpeedMps.Location = new System.Drawing.Point(11, 118);
            this._txtBoadSpeedMps.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._txtBoadSpeedMps.Name = "_txtBoadSpeedMps";
            this._txtBoadSpeedMps.Size = new System.Drawing.Size(76, 20);
            this._txtBoadSpeedMps.TabIndex = 2;
            this._txtBoadSpeedMps.Text = "14";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 7);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Rope Length";
            // 
            // _cmbRopeM
            // 
            this._cmbRopeM.FormattingEnabled = true;
            this._cmbRopeM.Items.AddRange(new object[] {
            "16"});
            this._cmbRopeM.Location = new System.Drawing.Point(11, 24);
            this._cmbRopeM.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._cmbRopeM.Name = "_cmbRopeM";
            this._cmbRopeM.Size = new System.Drawing.Size(92, 21);
            this._cmbRopeM.TabIndex = 0;
            this._cmbRopeM.Text = "16";
            // 
            // _btnDraw
            // 
            this._btnDraw.Location = new System.Drawing.Point(11, 153);
            this._btnDraw.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._btnDraw.Name = "_btnDraw";
            this._btnDraw.Size = new System.Drawing.Size(56, 24);
            this._btnDraw.TabIndex = 6;
            this._btnDraw.Text = "Draw";
            this._btnDraw.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 190);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(56, 24);
            this.button1.TabIndex = 7;
            this.button1.Text = "Load";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(133, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Course Pass";
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Location = new System.Drawing.Point(290, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 511);
            this.vScrollBar1.TabIndex = 0;
            // 
            // SlalomTrackerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 546);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button1);
            this.Controls.Add(this._btnDraw);
            this.Controls.Add(this._cmbRopeM);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._txtBoadSpeedMps);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._txtRadS);
            this.Controls.Add(this._panel1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "SlalomTrackerForm";
            this.Text = "Form1";
            this._panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel _panel1;
        private System.Windows.Forms.TextBox _txtRadS;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _txtBoadSpeedMps;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox _cmbRopeM;
        private System.Windows.Forms.Button _btnDraw;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.VScrollBar vScrollBar1;
        private System.Windows.Forms.Label label4;
    }
}

