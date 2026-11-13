// Zen Audio - Görev Çubuğu Ses Yöneticisi & Karıştırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ZenAudio
{
    public class MixerWindow : Window
    {
        private StackPanel _sessionsList;
        private Slider _masterSlider;
        private Button _masterMuteBtn;

        public MixerWindow()
        {
            Title = "Zen Audio - Ses Karıştırıcı";
            Width = 360;
            Height = 460;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;

            BuildUI();

            Deactivated += (s, e) => Hide();
            KeyDown += (s, e) => { if (e.Key == Key.Escape) Hide(); };
        }

        public void ShowAtTray()
        {
            // Position above bottom right
            Left = SystemParameters.PrimaryScreenWidth - Width - 16;
            Top = SystemParameters.PrimaryScreenHeight - Height - 60;

            RefreshMixer();
            Show();
            Activate();
        }

        private void BuildUI()
        {
            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(246, 22, 24, 34)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 26,
                    ShadowDepth = 6,
                    Opacity = 0.65
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Master
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Apps List
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Header
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock title = new TextBlock
            {
                Text = "☯️ Zen Audio - Ses Karıştırıcı",
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 0);

            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 24,
                Height = 24,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => Hide();
            Grid.SetColumn(closeBtn, 1);

            header.Children.Add(title);
            header.Children.Add(closeBtn);
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Master Volume Card
            Border masterCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            StackPanel masterStack = new StackPanel();
            TextBlock masterLabel = new TextBlock
            {
                Text = "Ana Ses (Master Volume)",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8)
            };
            masterStack.Children.Add(masterLabel);

            Grid masterControl = new Grid();
            masterControl.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            masterControl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _masterMuteBtn = new Button
            {
                Content = "🔊",
                FontSize = 14,
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _masterMuteBtn.Click += (s, e) =>
            {
                bool isMuted = AudioEngine.ToggleMasterMute();
                _masterMuteBtn.Content = isMuted ? "🔇" : "🔊";
            };
            Grid.SetColumn(_masterMuteBtn, 0);

            _masterSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = AudioEngine.GetMasterVolume() * 100,
                VerticalAlignment = VerticalAlignment.Center
            };
            _masterSlider.ValueChanged += (s, e) =>
            {
                AudioEngine.SetMasterVolume((float)(_masterSlider.Value / 100.0));
            };
            Grid.SetColumn(_masterSlider, 1);

            masterControl.Children.Add(_masterMuteBtn);
            masterControl.Children.Add(_masterSlider);
            masterStack.Children.Add(masterControl);

            masterCard.Child = masterStack;
            Grid.SetRow(masterCard, 1);
            mainGrid.Children.Add(masterCard);

            // Apps List
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _sessionsList = new StackPanel();
            scroll.Content = _sessionsList;

            Grid.SetRow(scroll, 2);
            mainGrid.Children.Add(scroll);

            // Footer
            TextBlock footer = new TextBlock
            {
                Text = "Tekerlek ile görev çubuğunda ses ayarla • Serkan ŞENTÜRK",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 145, 160)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(footer, 3);
            mainGrid.Children.Add(footer);

            rootBorder.Child = mainGrid;
            Content = rootBorder;
        }

        private void RefreshMixer()
        {
            _masterSlider.Value = AudioEngine.GetMasterVolume() * 100;
            _masterMuteBtn.Content = AudioEngine.GetMasterMute() ? "🔇" : "🔊";

            _sessionsList.Children.Clear();

            List<AudioSessionItem> sessions = AudioEngine.GetAppSessions();
            if (sessions.Count == 0)
            {
                _sessionsList.Children.Add(new TextBlock
                {
                    Text = "Şu anda ses çalan uygulama yok.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 145, 160)),
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            foreach (AudioSessionItem app in sessions)
            {
                Border appCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                StackPanel sp = new StackPanel();

                TextBlock appTitle = new TextBlock
                {
                    Text = app.ProcessName,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                sp.Children.Add(appTitle);

                Grid appControl = new Grid();
                appControl.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                appControl.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Button appMuteBtn = new Button
                {
                    Content = app.IsMuted ? "🔇" : "🔊",
                    FontSize = 12,
                    Width = 24,
                    Height = 24,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                appMuteBtn.Click += (s, e) =>
                {
                    try
                    {
                        bool isMuted;
                        app.SimpleAudioVolume.GetMute(out isMuted);
                        app.SimpleAudioVolume.SetMute(!isMuted, Guid.Empty);
                        appMuteBtn.Content = !isMuted ? "🔇" : "🔊";
                    }
                    catch { }
                };
                Grid.SetColumn(appMuteBtn, 0);

                Slider appSlider = new Slider
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = app.Volume * 100,
                    VerticalAlignment = VerticalAlignment.Center
                };
                appSlider.ValueChanged += (s, e) =>
                {
                    try
                    {
                        app.SimpleAudioVolume.SetMasterVolume((float)(appSlider.Value / 100.0), Guid.Empty);
                    }
                    catch { }
                };
                Grid.SetColumn(appSlider, 1);

                appControl.Children.Add(appMuteBtn);
                appControl.Children.Add(appSlider);
                sp.Children.Add(appControl);

                appCard.Child = sp;
                _sessionsList.Children.Add(appCard);
            }
        }
    }
}
