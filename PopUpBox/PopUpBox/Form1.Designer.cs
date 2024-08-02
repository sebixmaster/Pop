namespace PopUpBox
{
    partial class Form1
    {
        /// <summary>
        /// Wymagana zmienna projektanta.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Wyczyść wszystkie używane zasoby.
        /// </summary>
        /// <param name="disposing">prawda, jeżeli zarządzane zasoby powinny zostać zlikwidowane; Fałsz w przeciwnym wypadku.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Kod generowany przez Projektanta formularzy systemu Windows

        /// <summary>
        /// Metoda wymagana do obsługi projektanta — nie należy modyfikować
        /// jej zawartości w edytorze kodu.
        /// </summary>
        private void InitializeComponent()
        {
            this.FromTextbox = new System.Windows.Forms.TextBox();
            this.From = new System.Windows.Forms.Label();
            this.Message = new System.Windows.Forms.Label();
            this.MessageTextbox = new System.Windows.Forms.TextBox();
            this.Title = new System.Windows.Forms.Label();
            this.TitleTextBox = new System.Windows.Forms.TextBox();
            this.Confirm = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // FromTextbox
            // 
            this.FromTextbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.FromTextbox.Enabled = true;
            this.FromTextbox.TabStop = false;
            this.FromTextbox.Location = new System.Drawing.Point(245, 30);
            this.FromTextbox.Font = new System.Drawing.Font("Arial", 13F);
            this.ForeColor = System.Drawing.Color.Black;
            this.FromTextbox.Name = "FromTextbox";
            this.FromTextbox.ReadOnly = true;
            this.FromTextbox.Size = new System.Drawing.Size(210, 13);
            this.FromTextbox.TabIndex = 0;
            // 
            // From
            // 
            this.From.AutoSize = true;
            this.From.Font = new System.Drawing.Font("Arial", 13F);
            this.From.Location = new System.Drawing.Point(330, 4);
            this.From.Name = "From";
            this.From.Size = new System.Drawing.Size(39, 21);
            this.From.TabIndex = 1;
            this.From.Text = "Od:";
            // 
            // Message
            // 
            this.Message.AutoSize = true;
            this.Message.Font = new System.Drawing.Font("Arial", 13F);
            this.Message.Location = new System.Drawing.Point(295, 102);
            this.Message.Name = "Message";
            this.Message.Size = new System.Drawing.Size(108, 21);
            this.Message.TabIndex = 3;
            this.Message.Text = "Wiadomość:";
            // 
            // MessageTextbox
            // 
            this.MessageTextbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageTextbox.Font = new System.Drawing.Font("Arial", 13F);
            this.MessageTextbox.Enabled = true;
            this.MessageTextbox.TabStop = false;
            this.MessageTextbox.Location = new System.Drawing.Point(85, 125);
            this.MessageTextbox.Multiline = true;
            this.MessageTextbox.Name = "MessageTextbox";
            this.MessageTextbox.ReadOnly = true;
            this.MessageTextbox.Size = new System.Drawing.Size(530, 150);
            this.MessageTextbox.TabIndex = 2;
            this.MessageTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

            // 
            // Title
            // 
            this.Title.AutoSize = true;
            this.Title.Font = new System.Drawing.Font("Arial", 13F);
            this.Title.Location = new System.Drawing.Point(320, 55);
            this.Title.Name = "Title";
            this.Title.Size = new System.Drawing.Size(54, 21);
            this.Title.TabIndex = 5;
            this.Title.Text = "Tytuł:";
            // 
            // TitleTextBox
            // 
            this.TitleTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TitleTextBox.Font = new System.Drawing.Font("Arial", 13F);
            this.TitleTextBox.Enabled = true;
            this.TitleTextBox.TabStop = false;
            this.TitleTextBox.Location = new System.Drawing.Point(85, 77);
            this.TitleTextBox.Name = "TitleTextBox";
            this.TitleTextBox.ReadOnly = true;
            this.TitleTextBox.Size = new System.Drawing.Size(530, 13);
            this.TitleTextBox.TabIndex = 4;
            // 
            // Confirm
            // 
            this.Confirm.Font = new System.Drawing.Font("Arial", 10F);
            this.Confirm.Location = new System.Drawing.Point(260, 290);
            this.Confirm.Name = "Confirm";
            this.Confirm.Size = new System.Drawing.Size(180, 25);
            this.Confirm.TabIndex = 6;
            this.Confirm.Text = "Potwierdzam przeczytanie";
            this.Confirm.UseVisualStyleBackColor = true;
            this.Confirm.Click += new System.EventHandler(this.buttonConfirm_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 340);
            this.Controls.Add(this.Confirm);
            this.Controls.Add(this.Title);
            this.Controls.Add(this.TitleTextBox);
            this.Controls.Add(this.Message);
            this.Controls.Add(this.MessageTextbox);
            this.Controls.Add(this.From);
            this.Controls.Add(this.FromTextbox);
            this.Name = "Form1";
            this.Text = "Pop-up message";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox FromTextbox;
        private System.Windows.Forms.Label From;
        private System.Windows.Forms.Label Message;
        private System.Windows.Forms.TextBox MessageTextbox;
        private System.Windows.Forms.Label Title;
        private System.Windows.Forms.TextBox TitleTextBox;
        private System.Windows.Forms.Button Confirm;
    }
}

