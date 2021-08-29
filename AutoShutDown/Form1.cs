using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Diagnostics;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace AutoShutDown
{
    public partial class Form1 : Form
    {
        private static readonly string FilePath = $"C:\\Users\\{Environment.UserName}\\asd.txt";
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
            if (ReadingFile() != 0) //file exists
            {
                RunApplication(ReadingFile(), true);
            }
        }
        private static void TimerEventProcessor(Timer timer, int timeToShutDown)
        {
            if (GetLastInputTime() == timeToShutDown*60 - 300)//показываем предупреждающее сообщение за 5 минут до выключения
            {
                var form2 = new Form2();
                form2.Show();
            }
            else if (GetLastInputTime() == timeToShutDown*60)
            {
                timer.Stop();
                Process.Start("cmd", "/c shutdown -s -f -t 00");//выключение компьютера
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RunApplication(int.Parse(this.comboBox.SelectedItem.ToString()),false);
            WritingTimeToFile(this.comboBox.SelectedItem.ToString());
            
        }

        public void RunApplication(int timeToShutDown, bool runFromAutoStart)
        {
            ChangeTurnOffSettings(timeToShutDown);
            CreateShortcut("asd", Environment.GetFolderPath(Environment.SpecialFolder.Startup), @"C:\Program Files (x86)\ASD\AutoShutDown.exe");//добавление программы в автозагрузку
            timer.Tick += (sender, args) => TimerEventProcessor(timer, timeToShutDown);
            timer.Interval = 1000;
            timer.Start();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.notifyIcon.Visible = true;
            if (!runFromAutoStart)
            {
                this.notifyIcon.Text = this.Text;
                this.notifyIcon.BalloonTipTitle = "The application ASD is running";
                this.notifyIcon.BalloonTipText = " ";
                this.notifyIcon.ShowBalloonTip(100);
            }
            this.Hide();
        }

        public void WritingTimeToFile(string time)
        {
            var sw = new StreamWriter(FilePath, false, System.Text.Encoding.Default);
            sw.WriteLine(time);
            sw.Close();
        }

        private int ReadingFile()
        {
            if (File.Exists(FilePath))
            {
                var sr = new StreamReader(FilePath);
                string str = sr.ReadLine();//считываем время, через которое нужно выключить компьютер из файла
                sr.Close();

                if (str == null || str.All(char.IsDigit) == false || int.Parse(str) > 100 || int.Parse(str) < int.Parse(comboBox.SelectedItem.ToString()))//если файл был вручную измене пользователем и там появлиилсь символы или изменено время, то по умолчани ставим время 30 минут
                {
                    str = comboBox.SelectedItem.ToString();
                    WritingTimeToFile(str);
                }

                return int.Parse(str);
            }
            return 0;//файл не существует
        }
        public void ChangeTurnOffSettings(int timeToShutDown)//изменения времени перехода компьютера в спящий режим, режим гибернации, выключения монитора
        {
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change monitor-timeout-ac {timeToShutDown+5}"));//Timeout to turn off the display (plugged in)
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change monitor-timeout-dc {timeToShutDown + 5}"));//Timeout to turn off the display (battery)
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change standby-timeout-ac {timeToShutDown + 5}"));//Timeout to go to sleep (plugged in)
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change standby-timeout-dc {timeToShutDown + 5}"));//Timeout to go to sleep (battery)
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change hibernate-timeout-ac {timeToShutDown + 5}"));//Timeout to go into hibernate (plugged in)
            Process.Start(new ProcessStartInfo("cmd", $"/C powercfg /change hibernate-timeout-dc {timeToShutDown + 5}")); //Timeout to go into hibernate(battery)
        }

        //Creating a shortcut file in the Startup Folder
        public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation)
        {
            string shortcutLocation = System.IO.Path.Combine(shortcutPath, shortcutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
            shortcut.TargetPath = targetFileLocation;// The path of the file that will launch when the shortcut is run;
            shortcut.Save();// Save the shortcut
        }
    }   

}
