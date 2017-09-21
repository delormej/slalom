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
            this.SuspendLayout();
            // 
            // _panel1
            // 
            this._panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this._panel1.Location = new System.Drawing.Point(167, 0);
            this._panel1.Name = "_panel1";
            this._panel1.Size = new System.Drawing.Size(436, 672);
            this._panel1.TabIndex = 0;
            // 
            // _txtRadS
            // 
            this._txtRadS.Location = new System.Drawing.Point(15, 88);
            this._txtRadS.Name = "_txtRadS";
            this._txtRadS.Size = new System.Drawing.Size(100, 22);
            this._txtRadS.TabIndex = 1;
            this._txtRadS.Text = "1.2";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(136, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Swing Speed (rad/s)";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 125);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Boat Speed (mps)";
            // 
            // _txtBoadSpeedMps
            // 
            this._txtBoadSpeedMps.Location = new System.Drawing.Point(15, 145);
            this._txtBoadSpeedMps.Name = "_txtBoadSpeedMps";
            this._txtBoadSpeedMps.Size = new System.Drawing.Size(100, 22);
            this._txtBoadSpeedMps.TabIndex = 2;
            this._txtBoadSpeedMps.Text = "14";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "Rope Length";
            // 
            // _cmbRopeM
            // 
            this._cmbRopeM.FormattingEnabled = true;
            this._cmbRopeM.Items.AddRange(new object[] {
            "16"});
            this._cmbRopeM.Location = new System.Drawing.Point(15, 29);
            this._cmbRopeM.Name = "_cmbRopeM";
            this._cmbRopeM.Size = new System.Drawing.Size(121, 24);
            this._cmbRopeM.TabIndex = 0;
            this._cmbRopeM.Text = "16";
            // 
            // _btnDraw
            // 
            this._btnDraw.Location = new System.Drawing.Point(15, 188);
            this._btnDraw.Name = "_btnDraw";
            this._btnDraw.Size = new System.Drawing.Size(75, 29);
            this._btnDraw.TabIndex = 6;
            this._btnDraw.Text = "Draw";
            this._btnDraw.UseVisualStyleBackColor = true;
            // 
            // SlalomTrackerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(603, 672);
            this.Controls.Add(this._btnDraw);
            this.Controls.Add(this._cmbRopeM);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._txtBoadSpeedMps);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._txtRadS);
            this.Controls.Add(this._panel1);
            this.Name = "SlalomTrackerForm";
            this.Text = "Form1";
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
    }
}

