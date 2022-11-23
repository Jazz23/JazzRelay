namespace JazzRelay
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(153)))), ((int)(((byte)(232)))));
            this.panel1.Location = new System.Drawing.Point(-1, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1281, 674);
            this.panel1.TabIndex = 0;
            this.panel1.Resize += new System.EventHandler(this.Panel_Resize);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(170)))), ((int)(((byte)(232)))));
            this.panel2.Location = new System.Drawing.Point(1280, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(640, 337);
            this.panel2.TabIndex = 1;
            this.panel2.Resize += new System.EventHandler(this.Panel_Resize);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(187)))), ((int)(((byte)(232)))));
            this.panel3.Location = new System.Drawing.Point(1280, 337);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(640, 337);
            this.panel3.TabIndex = 2;
            this.panel3.Resize += new System.EventHandler(this.Panel_Resize);
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(158)))), ((int)(((byte)(204)))), ((int)(((byte)(232)))));
            this.panel4.Location = new System.Drawing.Point(1280, 674);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(640, 337);
            this.panel4.TabIndex = 2;
            this.panel4.Resize += new System.EventHandler(this.Panel_Resize);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(221)))), ((int)(((byte)(242)))));
            this.panel5.Location = new System.Drawing.Point(640, 674);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(640, 337);
            this.panel5.TabIndex = 3;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(238)))), ((int)(((byte)(255)))));
            this.panel6.Location = new System.Drawing.Point(0, 674);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(640, 337);
            this.panel6.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1920, 1009);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        public Panel panel1;
        public Panel panel2;
        public Panel panel3;
        public Panel panel4;
        public Panel panel5;
        public Panel panel6;
    }
}