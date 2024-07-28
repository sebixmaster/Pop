using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopUp
{
    public partial class Form1 : Form
    {
        private string message;
        private string sender;
        private string topic;

        public Form1(string sender, string topic, string message)
        {
            InitializeComponent();
            this.sender = sender;
            this.topic = topic;
            this.message = message;

            labelSender.Text = $"From: {sender}";
            labelTopic.Text = $"Topic: {topic}";
            textBoxMessage.Text = message;
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            var response = DateTime.Now.ToString();
            Console.WriteLine(response);
            this.Close();
        }
    }
}
