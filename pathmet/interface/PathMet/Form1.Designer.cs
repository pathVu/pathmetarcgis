namespace PathMet
{
    partial class Form1
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
            this.chkbxPm = new System.Windows.Forms.CheckBox();
            this.pmStart = new System.Windows.Forms.Button();
            this.chkbxL = new System.Windows.Forms.CheckBox();
            this.chkbxC = new System.Windows.Forms.CheckBox();
            this.chkbxI = new System.Windows.Forms.CheckBox();
            this.chkbxE = new System.Windows.Forms.CheckBox();
            this.txtFName = new System.Windows.Forms.TextBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnRestart = new System.Windows.Forms.Button();
            this.btnTrippingHazard = new System.Windows.Forms.Button();
            this.btnBrokenSidewalk = new System.Windows.Forms.Button();
            this.btnVegetation = new System.Windows.Forms.Button();
            this.btnOther = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkbxPm
            // 
            this.chkbxPm.AutoCheck = false;
            this.chkbxPm.AutoSize = true;
            this.chkbxPm.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkbxPm.Location = new System.Drawing.Point(23, 65);
            this.chkbxPm.Name = "chkbxPm";
            this.chkbxPm.Size = new System.Drawing.Size(188, 24);
            this.chkbxPm.TabIndex = 1;
            this.chkbxPm.Text = "Connected to PathMet";
            this.chkbxPm.UseVisualStyleBackColor = true;
            this.chkbxPm.CheckedChanged += new System.EventHandler(this.chkbxPm_CheckedChanged);
            // 
            // pmStart
            // 
            this.pmStart.AutoSize = true;
            this.pmStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pmStart.Location = new System.Drawing.Point(40, 413);
            this.pmStart.Name = "pmStart";
            this.pmStart.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.pmStart.Size = new System.Drawing.Size(157, 50);
            this.pmStart.TabIndex = 4;
            this.pmStart.Text = "Start";
            this.pmStart.UseVisualStyleBackColor = true;
            this.pmStart.Click += new System.EventHandler(this.OnClick);
            // 
            // chkbxL
            // 
            this.chkbxL.AutoCheck = false;
            this.chkbxL.AutoSize = true;
            this.chkbxL.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkbxL.Location = new System.Drawing.Point(232, 65);
            this.chkbxL.Name = "chkbxL";
            this.chkbxL.Size = new System.Drawing.Size(68, 24);
            this.chkbxL.TabIndex = 5;
            this.chkbxL.Text = "Laser";
            this.chkbxL.UseVisualStyleBackColor = true;
            // 
            // chkbxC
            // 
            this.chkbxC.AutoCheck = false;
            this.chkbxC.AutoSize = true;
            this.chkbxC.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkbxC.Location = new System.Drawing.Point(315, 65);
            this.chkbxC.Name = "chkbxC";
            this.chkbxC.Size = new System.Drawing.Size(84, 24);
            this.chkbxC.TabIndex = 6;
            this.chkbxC.Text = "Camera";
            this.chkbxC.UseVisualStyleBackColor = true;
            // 
            // chkbxI
            // 
            this.chkbxI.AutoCheck = false;
            this.chkbxI.AutoSize = true;
            this.chkbxI.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkbxI.Location = new System.Drawing.Point(98, 115);
            this.chkbxI.Name = "chkbxI";
            this.chkbxI.Size = new System.Drawing.Size(58, 24);
            this.chkbxI.TabIndex = 7;
            this.chkbxI.Text = "IMU";
            this.chkbxI.UseVisualStyleBackColor = true;
            // 
            // chkbxE
            // 
            this.chkbxE.AutoCheck = false;
            this.chkbxE.AutoSize = true;
            this.chkbxE.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkbxE.Location = new System.Drawing.Point(216, 115);
            this.chkbxE.Name = "chkbxE";
            this.chkbxE.Size = new System.Drawing.Size(88, 24);
            this.chkbxE.TabIndex = 8;
            this.chkbxE.Text = "Encoder";
            this.chkbxE.UseVisualStyleBackColor = true;
            // 
            // txtFName
            // 
            this.txtFName.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFName.Location = new System.Drawing.Point(78, 186);
            this.txtFName.Name = "txtFName";
            this.txtFName.Size = new System.Drawing.Size(277, 35);
            this.txtFName.TabIndex = 9;
            // 
            // btnStop
            // 
            this.btnStop.AutoSize = true;
            this.btnStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStop.Location = new System.Drawing.Point(244, 413);
            this.btnStop.Name = "btnStop";
            this.btnStop.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnStop.Size = new System.Drawing.Size(153, 50);
            this.btnStop.TabIndex = 10;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.OnStop);
            // 
            // btnRestart
            // 
            this.btnRestart.AutoSize = true;
            this.btnRestart.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestart.Location = new System.Drawing.Point(122, 501);
            this.btnRestart.Margin = new System.Windows.Forms.Padding(2);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnRestart.Size = new System.Drawing.Size(178, 50);
            this.btnRestart.TabIndex = 11;
            this.btnRestart.Text = "Restart Service";
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // btnTrippingHazard
            // 
            this.btnTrippingHazard.AutoSize = true;
            this.btnTrippingHazard.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTrippingHazard.Location = new System.Drawing.Point(27, 247);
            this.btnTrippingHazard.Margin = new System.Windows.Forms.Padding(2);
            this.btnTrippingHazard.Name = "btnTrippingHazard";
            this.btnTrippingHazard.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnTrippingHazard.Size = new System.Drawing.Size(181, 50);
            this.btnTrippingHazard.TabIndex = 12;
            this.btnTrippingHazard.Text = "Tripping Hazard";
            this.btnTrippingHazard.UseVisualStyleBackColor = true;
            this.btnTrippingHazard.Click += new System.EventHandler(this.btnTrippingHazard_Click);
            // 
            // btnBrokenSidewalk
            // 
            this.btnBrokenSidewalk.AutoSize = true;
            this.btnBrokenSidewalk.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrokenSidewalk.Location = new System.Drawing.Point(216, 247);
            this.btnBrokenSidewalk.Margin = new System.Windows.Forms.Padding(2);
            this.btnBrokenSidewalk.Name = "btnBrokenSidewalk";
            this.btnBrokenSidewalk.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnBrokenSidewalk.Size = new System.Drawing.Size(187, 50);
            this.btnBrokenSidewalk.TabIndex = 13;
            this.btnBrokenSidewalk.Text = "Broken Sidewalk";
            this.btnBrokenSidewalk.UseVisualStyleBackColor = true;
            this.btnBrokenSidewalk.Click += new System.EventHandler(this.btnBrokenSidewalk_Click);
            // 
            // btnVegetation
            // 
            this.btnVegetation.AutoSize = true;
            this.btnVegetation.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnVegetation.Location = new System.Drawing.Point(40, 330);
            this.btnVegetation.Margin = new System.Windows.Forms.Padding(2);
            this.btnVegetation.Name = "btnVegetation";
            this.btnVegetation.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnVegetation.Size = new System.Drawing.Size(153, 50);
            this.btnVegetation.TabIndex = 14;
            this.btnVegetation.Text = "Vegetation";
            this.btnVegetation.UseVisualStyleBackColor = true;
            this.btnVegetation.Click += new System.EventHandler(this.btnVegetation_Click);
            // 
            // btnOther
            // 
            this.btnOther.AutoSize = true;
            this.btnOther.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOther.Location = new System.Drawing.Point(202, 330);
            this.btnOther.Margin = new System.Windows.Forms.Padding(2);
            this.btnOther.Name = "btnOther";
            this.btnOther.Padding = new System.Windows.Forms.Padding(25, 10, 25, 10);
            this.btnOther.Size = new System.Drawing.Size(153, 50);
            this.btnOther.TabIndex = 15;
            this.btnOther.Text = "Other";
            this.btnOther.UseVisualStyleBackColor = true;
            this.btnOther.Click += new System.EventHandler(this.btnOther_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnBrokenSidewalk);
            this.panel1.Controls.Add(this.chkbxL);
            this.panel1.Controls.Add(this.chkbxC);
            this.panel1.Controls.Add(this.btnRestart);
            this.panel1.Controls.Add(this.btnTrippingHazard);
            this.panel1.Controls.Add(this.chkbxI);
            this.panel1.Controls.Add(this.btnOther);
            this.panel1.Controls.Add(this.btnVegetation);
            this.panel1.Controls.Add(this.chkbxPm);
            this.panel1.Controls.Add(this.chkbxE);
            this.panel1.Controls.Add(this.btnStop);
            this.panel1.Controls.Add(this.pmStart);
            this.panel1.Controls.Add(this.txtFName);
            this.panel1.Location = new System.Drawing.Point(713, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(430, 615);
            this.panel1.TabIndex = 16;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1143, 614);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.CheckBox chkbxPm;
        private System.Windows.Forms.Button pmStart;
        private System.Windows.Forms.CheckBox chkbxL;
        private System.Windows.Forms.CheckBox chkbxC;
        private System.Windows.Forms.CheckBox chkbxI;
        private System.Windows.Forms.CheckBox chkbxE;
        private System.Windows.Forms.TextBox txtFName;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnRestart;
        private System.Windows.Forms.Button btnTrippingHazard;
        private System.Windows.Forms.Button btnBrokenSidewalk;
        private System.Windows.Forms.Button btnVegetation;
        private System.Windows.Forms.Button btnOther;
        private System.Windows.Forms.Panel panel1;
    }
}

