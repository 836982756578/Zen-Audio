// Zen Audio - Görev Çubuğu Ses Yöneticisi & Karıştırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ZenAudio
{
    public class VolumeHud : Window
    {
        private readonly TextBlock _iconBlock;
        private readonly TextBlock _percentBlock;
        private readonly ProgressBar _progressBar;
        private readonly Border _pillBorder;
        private readonly DispatcherTimer _timer;

        public VolumeHud()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Focusable = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            _pillBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(246, 22, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 160, 100, 255)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16, 8, 18, 8),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 5,
                    Opacity = 0.65
                }
            };

            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _iconBlock = new TextBlock
            {
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            stack.Children.Add(_iconBlock);

            _progressBar = new ProgressBar
            {
                Width = 140,
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 100, 255)),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            stack.Children.Add(_progressBar);

            _percentBlock = new TextBlock
            {
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 36,
                TextAlignment = TextAlignment.Right
            };
            stack.Children.Add(_percentBlock);

            _pillBorder.Child = stack;
            Content = _pillBorder;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                DoubleAnimation fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(250));
                fade.Completed += (s2, e2) => Hide();
                BeginAnimation(OpacityProperty, fade);
            };
        }

        public void ShowVolume(float level, bool isMuted)
        {
            _timer.Stop();

            int pct = (int)Math.Round(level * 100);
            _progressBar.Value = pct;
            _percentBlock.Text = pct + "%";

            if (isMuted || pct == 0)
            {
                _iconBlock.Text = "🔇";
                _progressBar.Foreground = new SolidColorBrush(Color.FromRgb(200, 80, 80));
                _pillBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 220, 80, 80));
            }
            else
            {
                if (pct > 60) _iconBlock.Text = "🔊";
                else if (pct > 25) _iconBlock.Text = "🔉";
                else _iconBlock.Text = "🔈";

                _progressBar.Foreground = new SolidColorBrush(Color.FromRgb(160, 100, 255));
                _pillBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 160, 100, 255));
            }

            UpdateLayout();

            // Place near bottom center above taskbar
            Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2;
            Top = SystemParameters.PrimaryScreenHeight - 110;

            Opacity = 1.0;
            Show();
            _timer.Start();
        }
    }
}
