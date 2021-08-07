using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace AutoShutDown
{
    public partial class Form1 : Form
    {
       
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        static int GetLastInputTime()
        {
            int idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (UInt32)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            int envTicks = Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                int lastInputTick = (Int32)lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }
            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }


        public Form1()
        {
            InitializeComponent();
            timer.Tick += (sender, args) => TimerEventProcessor(sender, args, label2, timer);
            timer.Interval = 1000;
            timer.Start();
           
        }

        private static void TimerEventProcessor(Object myObject, EventArgs myEventArgs, Label label, Timer timer)
        {
            label.Text = GetLastInputTime().ToString();//каждую секунду выводим на Label время простоя в секундах

            if (GetLastInputTime() == 25)
            {

                MessageBox.Show(
                        "Ваш компьютер будет выключен. Нажмите ОК для отмены выключения.",
                        "Выключение компьютера",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);
            }
            if (GetLastInputTime() == 30)
            {
                timer.Stop();
                Process.Start("cmd", "/c shutdown -s -f -t 00");
            }
        }
    }
}
