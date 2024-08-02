using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PopUpBox
{
    public partial class Form1 : Form
    {
        private string message;
        private string sender;
        private string topic;
        private string id;

        public Form1(string sender, string topic, string message, string id)
        {
            InitializeComponent();
            this.sender = sender;
            this.topic = topic;
            this.message = message;
            this.id = id;

            FromTextbox.Text = sender;
            TitleTextBox.Text = topic;
            MessageTextbox.Text = message;
            MessageTextbox.ScrollBars = ScrollBars.Vertical;
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            var response = id + "<>" + DateTime.Now.ToString();
            Console.WriteLine(response);
            this.Close();
        }
    }
}
