// Zen Audio - Görev Çubuğu Ses Yöneticisi & Karıştırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace ZenAudio
{
    public class App : System.Windows.Application
    {
        private static Mutex _mutex;
        private NotifyIcon _trayIcon;
        private VolumeHud _hud;
        private MixerWindow _mixerWindow;
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc _mouseProc;

        [STAThread]
        public static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ZenAudio_SingleInstance_Mutex_SerkanSenturk_2026", out createdNew);

            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    "Zen Audio zaten arka planda çalışıyor!\nFare tekerleğini görev çubuğunun üzerine getirip kaydırarak sesi ayarlayabilir veya saat yanındaki simgeye tıklayabilirsiniz.\n\nGeliştirici: Serkan ŞENTÜRK",
                    "Zen Audio",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            App app = new App();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hud = new VolumeHud();
            _mixerWindow = new MixerWindow();

            _mouseProc = MouseHookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }

            SetupTrayIcon();

            // Periodic memory trimming (~2 MB RAM)
            System.Windows.Threading.DispatcherTimer trimTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            trimTimer.Tick += (s, ev) =>
            {
                try
                {
                    NativeMethods.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
                catch { }
            };
            trimTimer.Start();
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();

                if (msg == NativeMethods.WM_MOUSEWHEEL)
                {
                    NativeMethods.MSLLHOOKSTRUCT hookStruct = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));

                    if (NativeMethods.IsPointOverTaskbar(hookStruct.pt))
                    {
                        short delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                        float step = (delta > 0) ? 0.02f : -0.02f;

                        AudioEngine.AdjustMasterVolume(step);

                        float current = AudioEngine.GetMasterVolume();
                        bool isMuted = AudioEngine.GetMasterMute();

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _hud.ShowVolume(current, isMuted);
                        }));

                        return (IntPtr)1; // Handled
                    }
                }
                else if (msg == NativeMethods.WM_MBUTTONUP)
                {
                    NativeMethods.MSLLHOOKSTRUCT hookStruct = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));

                    if (NativeMethods.IsPointOverTaskbar(hookStruct.pt))
                    {
                        bool isMuted = AudioEngine.ToggleMasterMute();
                        float current = AudioEngine.GetMasterVolume();

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _hud.ShowVolume(current, isMuted);
                        }));

                        return (IntPtr)1;
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Zen Audio - Görev Çubuğu Ses Yöneticisi | Serkan ŞENTÜRK";
            _trayIcon.Icon = LoadAppIcon();
            _trayIcon.Visible = true;

            _trayIcon.Click += (s, e) =>
            {
                MouseEventArgs me = e as MouseEventArgs;
                if (me != null && me.Button == MouseButtons.Left)
                {
                    _mixerWindow.ShowAtTray();
                }
            };

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem titleItem = new ToolStripMenuItem("☯️ Zen Audio - Ses Yöneticisi");
            titleItem.Font = new System.Drawing.Font(titleItem.Font, System.Drawing.FontStyle.Bold);
            titleItem.Enabled = false;
            menu.Items.Add(titleItem);

            ToolStripMenuItem authorItem = new ToolStripMenuItem("👤 Geliştirici: Serkan ŞENTÜRK");
            authorItem.Font = new System.Drawing.Font(authorItem.Font.FontFamily, 8.5f, System.Drawing.FontStyle.Italic);
            authorItem.Enabled = false;
            menu.Items.Add(authorItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem mixerItem = new ToolStripMenuItem("🎛️ Ses Karıştırıcısını Aç (Uygulamalar)");
            mixerItem.Click += (s, e) => _mixerWindow.ShowAtTray();
            menu.Items.Add(mixerItem);

            ToolStripMenuItem muteItem = new ToolStripMenuItem("🔇 Sesi Kapat / Aç (Mute)");
            muteItem.Click += (s, e) =>
            {
                bool isMuted = AudioEngine.ToggleMasterMute();
                float current = AudioEngine.GetMasterVolume();
                _hud.ShowVolume(current, isMuted);
            };
            menu.Items.Add(muteItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem tipItem = new ToolStripMenuItem("💡 İpucu: Görev çubuğunda tekerleği çevirerek sesi ayarlayın");
            tipItem.Enabled = false;
            menu.Items.Add(tipItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem devProfileItem = new ToolStripMenuItem("🌐 Geliştirici Sayfası (GitHub)");
            devProfileItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(devProfileItem);

            ToolStripMenuItem projectRepoItem = new ToolStripMenuItem("⭐ Zen Audio GitHub");
            projectRepoItem.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578/Zen-Audio") { UseShellExecute = true }); } catch { }
            };
            menu.Items.Add(projectRepoItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("ℹ️ Zen Audio Hakkında");
            aboutItem.Click += (s, e) =>
            {
                System.Windows.MessageBox.Show(
                    "Zen Audio - Görev Çubuğu Ses Yöneticisi & Karıştırıcı v1.0.0\n\n" +
                    "🌟 Geliştirici: Serkan ŞENTÜRK\n" +
                    "Telif Hakkı: © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.\n\n" +
                    "Fare imlecini görev çubuğunun üzerine getirip tekerleği kaydırarak\n" +
                    "sesi anında açıp kısmanızı ve her uygulamanın (Spotify, Discord, Oyun vb.)\n" +
                    "sesini bağımsız olarak ayarlamanızı sağlar.\n\n" +
                    "• Görev çubuğunda tekerlekle ses ayarı (EarTrumpet alternatifi)\n" +
                    "• Orta tuşla (tekerlek tıklaması) tek tıkla ses kapatma (Mute)\n" +
                    "• Modern animasyonlu ekran üstü ses göstergesi (HUD)\n" +
                    "• Ultra Düşük Bellek (~2 MB RAM) ve Taşınabilir\n\n" +
                    "GitHub: https://github.com/836982756578/Zen-Audio",
                    "Zen Audio Hakkında",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("🚪 Çıkış");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
        }

        private System.Drawing.Icon LoadAppIcon()
        {
            try
            {
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoPath))
                {
                    return new System.Drawing.Icon(icoPath, SystemInformation.SmallIconSize);
                }
                return System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void ExitApplication()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }

            Shutdown();
        }
    }
}
