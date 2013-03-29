namespace MinerBot
{
    partial class MinerBot
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
            this.components = new System.ComponentModel.Container();
            this.lblState = new System.Windows.Forms.Label();
            this.txtStation = new System.Windows.Forms.TextBox();
            this.lblStation = new System.Windows.Forms.Label();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.btnCurrentStation = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.lblCurRoidOre = new System.Windows.Forms.Label();
            this.lblEstimatedMined = new System.Windows.Forms.Label();
            this.lblDroneState = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(12, 9);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(38, 13);
            this.lblState.TabIndex = 0;
            this.lblState.Text = "State: ";
            // 
            // txtStation
            // 
            this.txtStation.Location = new System.Drawing.Point(58, 59);
            this.txtStation.Name = "txtStation";
            this.txtStation.Size = new System.Drawing.Size(100, 20);
            this.txtStation.TabIndex = 1;
            this.txtStation.TextChanged += new System.EventHandler(this.txtStation_TextChanged);
            // 
            // lblStation
            // 
            this.lblStation.AutoSize = true;
            this.lblStation.Location = new System.Drawing.Point(12, 62);
            this.lblStation.Name = "lblStation";
            this.lblStation.Size = new System.Drawing.Size(40, 13);
            this.lblStation.TabIndex = 2;
            this.lblStation.Text = "Station";
            // 
            // chkActive
            // 
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(15, 85);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(56, 17);
            this.chkActive.TabIndex = 3;
            this.chkActive.Text = "Active";
            this.chkActive.UseVisualStyleBackColor = true;
            this.chkActive.CheckedChanged += new System.EventHandler(this.chkActive_CheckedChanged);
            // 
            // btnCurrentStation
            // 
            this.btnCurrentStation.Location = new System.Drawing.Point(164, 57);
            this.btnCurrentStation.Name = "btnCurrentStation";
            this.btnCurrentStation.Size = new System.Drawing.Size(90, 23);
            this.btnCurrentStation.TabIndex = 4;
            this.btnCurrentStation.Text = "Current Station";
            this.btnCurrentStation.UseVisualStyleBackColor = true;
            this.btnCurrentStation.Click += new System.EventHandler(this.btnCurrentStation_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // lblCurRoidOre
            // 
            this.lblCurRoidOre.AutoSize = true;
            this.lblCurRoidOre.Location = new System.Drawing.Point(12, 149);
            this.lblCurRoidOre.Name = "lblCurRoidOre";
            this.lblCurRoidOre.Size = new System.Drawing.Size(71, 13);
            this.lblCurRoidOre.TabIndex = 5;
            this.lblCurRoidOre.Text = "CurRoid Ore: ";
            // 
            // lblEstimatedMined
            // 
            this.lblEstimatedMined.AutoSize = true;
            this.lblEstimatedMined.Location = new System.Drawing.Point(12, 173);
            this.lblEstimatedMined.Name = "lblEstimatedMined";
            this.lblEstimatedMined.Size = new System.Drawing.Size(91, 13);
            this.lblEstimatedMined.TabIndex = 6;
            this.lblEstimatedMined.Text = "Estimated Mined: ";
            // 
            // lblDroneState
            // 
            this.lblDroneState.AutoSize = true;
            this.lblDroneState.Location = new System.Drawing.Point(12, 31);
            this.lblDroneState.Name = "lblDroneState";
            this.lblDroneState.Size = new System.Drawing.Size(47, 13);
            this.lblDroneState.TabIndex = 7;
            this.lblDroneState.Text = "Drones: ";
            // 
            // MinerBot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(288, 206);
            this.Controls.Add(this.lblDroneState);
            this.Controls.Add(this.lblEstimatedMined);
            this.Controls.Add(this.lblCurRoidOre);
            this.Controls.Add(this.btnCurrentStation);
            this.Controls.Add(this.chkActive);
            this.Controls.Add(this.lblStation);
            this.Controls.Add(this.txtStation);
            this.Controls.Add(this.lblState);
            this.Name = "MinerBot";
            this.Text = "MinerBot";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.TextBox txtStation;
        private System.Windows.Forms.Label lblStation;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.Button btnCurrentStation;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label lblCurRoidOre;
        private System.Windows.Forms.Label lblEstimatedMined;
        private System.Windows.Forms.Label lblDroneState;

    }
}

