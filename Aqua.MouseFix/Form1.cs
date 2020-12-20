using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Aqua.MouseFix
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        
        private Hook hook;
        private int count = 0;

        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            hook = new Hook();
            hook.Blocked += BlockedCount;
            // AllocConsole();
        }

        private void BlockedCount()
        {
            label1.Text = $"Fixed: {++count}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // start
            hook.Start();
            Text = "Working";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // stop
            hook.Stop();
            Text = "Stopped";
        }
    }
}