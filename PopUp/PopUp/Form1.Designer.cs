namespace PopUp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label labelSender;
        private System.Windows.Forms.Label labelTopic;
        private System.Windows.Forms.TextBox textBoxMessage;
        private System.Windows.Forms.Button buttonConfirm;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelSender = new System.Windows.Forms.Label();
            this.labelTopic = new System.Windows.Forms.Label();
            this.textBoxMessage = new System.Windows.Forms.TextBox();
            this.buttonConfirm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelSender
            // 
            this.labelSender.AutoSize = true;
            this.labelSender.Location = new System.Drawing.Point(13, 13);
            this.labelSender.Name = "labelSender";
            this.labelSender.Size = new System.Drawing.Size(38, 15);
            this.labelSender.TabIndex = 0;
            this.labelSender.Text = "label1";
            // 
            // labelTopic
            // 
            this.labelTopic.AutoSize = true;
            this.labelTopic.Location = new System.Drawing.Point(13, 39);
            this.labelTopic.Name = "labelTopic";
            this.labelTopic.Size = new System.Drawing.Size(38, 15);
            this.labelTopic.TabIndex = 1;
            this.labelTopic.Text = "label2";
            // 
            // textBoxMessage
            // 
            this.textBoxMessage.Location = new System.Drawing.Point(13, 67);
            this.textBoxMessage.Multiline = true;
            this.textBoxMessage.Name = "textBoxMessage";
            this.textBoxMessage.ReadOnly = true;
            this.textBoxMessage.Size = new System.Drawing.Size(259, 96);
            this.textBoxMessage.TabIndex = 2;
            // 
            // buttonConfirm
            // 
            this.buttonConfirm.Location = new System.Drawing.Point(13, 170);
            this.buttonConfirm.Name = "buttonConfirm";
            this.buttonConfirm.Size = new System.Drawing.Size(259, 23);
            this.buttonConfirm.TabIndex = 3;
            this.buttonConfirm.Text = "Confirm";
            this.buttonConfirm.UseVisualStyleBackColor = true;
            this.buttonConfirm.Click += new System.EventHandler(this.buttonConfirm_Click);
            // 
            // MessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 211);
            this.Controls.Add(this.buttonConfirm);
            this.Controls.Add(this.textBoxMessage);
            this.Controls.Add(this.labelTopic);
            this.Controls.Add(this.labelSender);
            this.Name = "MessageForm";
            this.Text = "Message";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

