using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;

namespace AutoShutDown
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Lastinputinfo
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(Lastinputinfo));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref Lastinputinfo plii);

        static int GetLastInputTime()
        {
            var idleTime = 0;
            Lastinputinfo lastInputInfo = new Lastinputinfo();
            lastInputInfo.cbSize = (UInt32)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            var envTicks = Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                var lastInputTick = (Int32)lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }
            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        public Form1()
        {
            InitializeComponent();
            comboBox.SelectedIndex = 0;
            CheckingFile();//проверка наличия файла с записанным временем автовыключения и считывание значения
        }
        private static void TimerEventProcessor(Timer timer, string timeToShutDowm)
        {
            if (GetLastInputTime() == int.Parse(timeToShutDowm)*60 - 60)//показываем предупреждающее сообщение за 3 минуты до выключения
            {
                var form2 = new Form2();
                form2.Show();
            }
            else if (GetLastInputTime() == int.Parse(timeToShutDowm)*60)
            {
                timer.Stop();
                //Process.Start("cmd", "/c shutdown -s -f -t 00");
                MessageBox.Show("комп выключается...");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RunApplication(this.comboBox.SelectedItem.ToString());
            WritingTimeToFile(this.comboBox.SelectedItem.ToString());
        }

        public void RunApplication(string timeToShutDown)
        {
            timer.Tick += (sender, args) => TimerEventProcessor(timer, timeToShutDown);
            timer.Interval = 1000;
            timer.Start();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.notifyIcon.Visible = true;
            this.notifyIcon.Text = this.Text;
            this.notifyIcon.BalloonTipTitle = "Приложение AutoShutDown запущено";
            this.notifyIcon.BalloonTipText = "Приложение запущено, добавлено в автозагрузку и будет работать в фоновом режиме";
            this.notifyIcon.ShowBalloonTip(100);
            this.Hide();
        }

        public void CheckingFile()
        {
            if (File.Exists($"C:\\Users\\{Environment.UserName}\\Documents\\asd.txt"))
            {
                var sr = new StreamReader($"C:\\Users\\{Environment.UserName}\\Documents\\asd.txt");
                string str = sr.ReadLine();//считываем время, через которое нужно выключить компьютер из файла
                sr.Close();

                if (str.All(char.IsDigit) == false || int.Parse(str) > 100)//если файл был вручную измене пользователем и там появлиилсь символы или изменено время, то по умолчани ставим время 30 минут
                {
                    str = "30";
                    WritingTimeToFile(str);
                }
                RunApplication(str);
                sr.Close();
            }
        }

        public void WritingTimeToFile(string time)
        {
            var sw = new StreamWriter($"C:\\Users\\{Environment.UserName}\\Documents\\asd.txt", false, System.Text.Encoding.Default);
            sw.WriteLine(time);
            sw.Close();
        }

        
      
    }
}
