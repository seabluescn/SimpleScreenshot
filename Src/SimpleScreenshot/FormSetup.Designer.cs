namespace SimpleScreenshot
{
    partial class FormSetup
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
            this.btnSetupHotKey = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lblReset = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSetupHotKey
            // 
            this.btnSetupHotKey.Location = new System.Drawing.Point(163, 77);
            this.btnSetupHotKey.Name = "btnSetupHotKey";
            this.btnSetupHotKey.Size = new System.Drawing.Size(237, 36);
            this.btnSetupHotKey.TabIndex = 0;
            this.btnSetupHotKey.Text = "Ctrl + Alt + Shift + S";
            this.btnSetupHotKey.UseVisualStyleBackColor = true;
            this.btnSetupHotKey.Click += new System.EventHandler(this.btnSetupHotKey_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(54, 86);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "快捷键：";
            // 
            // lblReset
            // 
            this.lblReset.AutoSize = true;
            this.lblReset.ForeColor = System.Drawing.Color.Red;
            this.lblReset.Location = new System.Drawing.Point(434, 88);
            this.lblReset.Name = "lblReset";
            this.lblReset.Size = new System.Drawing.Size(82, 15);
            this.lblReset.TabIndex = 2;
            this.lblReset.Text = "重启后生效";
            this.lblReset.Visible = false;
            // 
            // FormSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(561, 352);
            this.Controls.Add(this.lblReset);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSetupHotKey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormSetup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Simple Screenshot Setup";
            this.Load += new System.EventHandler(this.FormSetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSetupHotKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblReset;
    }
}