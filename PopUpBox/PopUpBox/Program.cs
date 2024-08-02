using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopUpBox
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length < 1)
            {
                MessageBox.Show("No message received", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                var message = args[0];
                string[] strings = message.Split(new String[] { "<<<>>>" }, StringSplitOptions.None);
                if (strings[0] != "0x007")
                {
                    MessageBox.Show($"Do not mess-up with me! {strings[0]}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    Application.Run(new Form1(strings[1], strings[2], strings[3], strings[4]));
                }    
            }
        }
    }
}
