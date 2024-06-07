using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Thanos_el_guarro
{
    public partial class ThanosTwerk : Form
    {
        private static WindowsMediaPlayer player = new WindowsMediaPlayer();

        public static Task MusicTask;

        public ThanosTwerk()
        {
            InitializeComponent();

            SetProcessShutdownParameters(0x3FF, SHUTDOWN_NORETRY);

            this.ShowInTaskbar = false;

            this.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            this.Text = Application.ProductName;

            MusicTask = Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;

                string tempAudioPath = Path.Combine(Path.GetTempPath(), Application.ProductName + ".mp3");

                if (!File.Exists(tempAudioPath)) File.WriteAllBytes(tempAudioPath, Properties.Resources.CprAudio);

                player.URL = tempAudioPath;
                player.settings.volume = 50;
                player.settings.setMode("loop", true);
                player.settings.autoStart = true;
            });

            GC.Collect();

            MusicTask.Wait();
        }

        private static Point StartPoint = Point.Empty;
        private static Size GifSize = Size.Empty;
        private static Screen StartScreen = null;

        private async void ThanosTwerk_Shown(object sender, EventArgs e)
        {
            Bitmap[] ThanosGif = ParseFrames(Properties.Resources.ThanosTwerk);

            StartScreen = Screen.FromPoint(new Point(this.Location.X, this.Location.Y));

            this.Refresh();
            this.Update();

            GifSize = ThanosGif[0].Size;

            this.Location = StartPoint = new Point(new Random().Next(StartScreen.Bounds.Size.Width - GifSize.Width), StartScreen.Bounds.Size.Height - GifSize.Height + GifSize.Height / 10 - 20);

            MusicTask.Wait();

            player.controls.play();

            while (true)
            {
                foreach (Image image in ThanosGif)
                {
                    await Task.Delay(25);

                    ImagePictureBox.Image = image;
                }
            }
        }

        private Bitmap[] ParseFrames(Bitmap Animation)
        {
            int Length = Animation.GetFrameCount(FrameDimension.Time);

            Bitmap[] Frames = new Bitmap[Length];

            for (int Index = 0; Index < Length; Index++)
            {
                Animation.SelectActiveFrame(FrameDimension.Time, Index);
                Frames[Index] = new Bitmap(Animation.Size.Width, Animation.Size.Height);

                Graphics.FromImage(Frames[Index]).DrawImage(Animation, new Point(0, 0));
            }

            Animation.Dispose();

            return Frames;
        }

        private bool DraggingForm = false;
        private Point DragCursorPoint;
        private Point DragFormPoint;

        private void ImagePictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            DraggingForm = true;
            DragCursorPoint = Cursor.Position;
            DragFormPoint = this.Location;
            Cursor.Hide();
        }

        private void ImagePictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            Cursor.Show();

            DraggingForm = false;
        }

        private void ImagePictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (DraggingForm)
            {
                Point ProximePoint = Point.Add(DragFormPoint, new Size(Point.Subtract(Cursor.Position, new Size(DragCursorPoint))));

                Screen CurrentScreen = Screen.FromPoint(ProximePoint);

                int excessValue = CurrentScreen.Bounds.Size.Height - GifSize.Height + GifSize.Height / 10 - 20;

                if (CurrentScreen.DeviceName == StartScreen.DeviceName && ProximePoint.Y > excessValue)
                {
                    ProximePoint.Y = excessValue;
                }

                this.Location = ProximePoint;
            }
        }

        private void ThanosTwerk_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                Program.Unprotect();
            }

            e.Cancel = this.Visible;
        }

        public const int WM_QUERYENDSESSION = 0x0011;
        public const int WM_ENDSESSION = 0x0016;
        public const uint SHUTDOWN_NORETRY = 0x00000001;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string reason);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);
        [DllImport("kernel32.dll")] private static extern bool SetProcessShutdownParameters(uint dwLevel, uint dwFlags);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_QUERYENDSESSION || m.Msg == WM_ENDSESSION) Program.Unprotect();

            base.WndProc(ref m);
        }
    }
}