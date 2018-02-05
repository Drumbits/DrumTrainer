using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Drumz.UI.Desktop
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            userControl11.Log += UserControl11_Log;
        }

        private void UserControl11_Log(string message)
        {
            if (richTextBox1.InvokeRequired)
            {
                MessagerHandler m = OnLog;
                richTextBox1.Invoke(m, new object[] { message});
            }
            else
                richTextBox1.AppendText(message+"\n");

        }
        private void OnLog(string message)
        {
            richTextBox1.AppendText(message + "\n");
        }
    }
}
