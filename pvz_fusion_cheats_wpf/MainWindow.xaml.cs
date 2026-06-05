using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace pvz_fusion_cheats_wpf
{
    public partial class MainWindow : Window
    {
        public static bool IsEnglish { get; set; } = false;

        public static string T(string zh, string en)
        {
            return IsEnglish ? en : zh;
        }

        private NativeMemory pm = null;
        private IntPtr baseAddress = IntPtr.Zero;
        private DispatcherTimer attachTimer;
        private bool attached = false;
        private bool ignoreEvents = false;

        private CooldownFeature cooldownFeature = new CooldownFeature();
        private SunFeature sunFeature = new SunFeature();
        private PlacementFeature placementFeature = new PlacementFeature();
        private InvincibleFeature invincibleFeature = new InvincibleFeature();
        private OneHitKillFeature oneHitKillFeature = new OneHitKillFeature();
        private AccelerateFeature accelerateFeature = new AccelerateFeature();
        private SpeedFeature speedFeature = new SpeedFeature();

        public MainWindow()
        {
            InitializeComponent();

            // Auto-detect system language
            string cultName = System.Globalization.CultureInfo.CurrentUICulture.Name;
            IsEnglish = !cultName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

            UpdateLanguageUI();

            // Set up DispatcherTimer to periodically scan and attach to the game
            attachTimer = new DispatcherTimer();
            attachTimer.Interval = TimeSpan.FromSeconds(1.5);
            attachTimer.Tick += AttachTimer_Tick;
            attachTimer.Start();
        }

        public void UpdateLanguageUI()
        {
            // Update Window Title
            this.Title = T("PVZ Fusion 修改器 v7", "PVZ Fusion Trainer v7");

            // Header Texts
            Text_Title.Text = T("PVZ Fusion 3.6.1 修改器", "PVZ Fusion 3.6.1 Trainer");
            Text_SubTitle.Text = T("v7 GUI", "v7 GUI");

            // Language switch button content
            Btn_Language.Content = IsEnglish ? "中" : "EN";

            // Connection Status
            if (attached && pm != null && pm.GameProcess != null)
            {
                StatusLabel.Text = T($"已附加进程 PID: {pm.GameProcess.Id}", $"Attached PID: {pm.GameProcess.Id}");
                StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            else
            {
                Process[] p = Process.GetProcessesByName("PlantsVsZombiesRH");
                if (p.Length > 0)
                {
                    StatusLabel.Text = T("权限不足，请使用管理员身份运行", "Access denied. Run as Administrator");
                    StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                }
                else
                {
                    StatusLabel.Text = T("等待游戏启动...", "Waiting for game to start...");
                    StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192));
                }
            }

            // Feature Card Labels
            Text_CooldownTitle.Text = cooldownFeature.Name;
            Text_CooldownDesc.Text = cooldownFeature.Description;

            Text_SunTitle.Text = sunFeature.Name;
            Text_SunDesc.Text = sunFeature.Description;

            Text_PlacementTitle.Text = placementFeature.Name;
            Text_PlacementDesc.Text = placementFeature.Description;

            Text_InvincibleTitle.Text = invincibleFeature.Name;
            Text_InvincibleDesc.Text = invincibleFeature.Description;

            Text_OneHitKillTitle.Text = oneHitKillFeature.Name;
            Text_OneHitKillDesc.Text = oneHitKillFeature.Description;

            Text_AccelerateTitle.Text = accelerateFeature.Name;
            Text_AccelerateDesc.Text = accelerateFeature.Description;

            Text_SpeedTitle.Text = speedFeature.Name;
            Text_SpeedDesc.Text = speedFeature.Description;

            // Footer Actions
            Btn_AdminRelaunch.Content = T("管理员身份运行", "Run as Administrator");
            Btn_EnableAll.Content = T("一键开启所有", "Enable All");
            Btn_RestoreAll.Content = T("全部还原", "Restore All");
        }

        private void Language_Click(object sender, RoutedEventArgs e)
        {
            IsEnglish = !IsEnglish;
            UpdateLanguageUI();
        }

        private static bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void AttachTimer_Tick(object? sender, EventArgs e)
        {
            if (attached)
            {
                if (pm == null || !pm.IsGameRunning())
                {
                    DetachGame();
                }
            }
            else
            {
                TryAttachGame();
            }
        }

        private void TryAttachGame()
        {
            pm = new NativeMemory();
            if (pm.Attach("PlantsVsZombiesRH", "GameAssembly.dll"))
            {
                baseAddress = pm.BaseAddress;
                attached = true;

                // Update UI state
                StatusDot.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                UpdateLanguageUI();

                Toggle_Cooldown.IsEnabled = true;
                Toggle_Sun.IsEnabled = true;
                Toggle_Placement.IsEnabled = true;
                Toggle_Invincible.IsEnabled = true;
                Toggle_OneHitKill.IsEnabled = true;
                Toggle_Accelerate.IsEnabled = true;
                Toggle_Speed.IsEnabled = true;

                Btn_AdminRelaunch.Visibility = Visibility.Collapsed;
            }
            else
            {
                pm.Dispose();
                pm = null;

                // Check if access is denied
                Process[] p = Process.GetProcessesByName("PlantsVsZombiesRH");
                if (p.Length > 0)
                {
                    StatusDot.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Yellow/Orange
                    UpdateLanguageUI();

                    if (!IsAdmin())
                    {
                        Btn_AdminRelaunch.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    StatusDot.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    UpdateLanguageUI();
                    Btn_AdminRelaunch.Visibility = Visibility.Collapsed;
                }

                DisableTogglesUI();
            }
        }

        private void DetachGame()
        {
            attached = false;
            if (pm != null)
            {
                pm.Dispose();
                pm = null;
            }

            StatusDot.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            UpdateLanguageUI();

            DisableTogglesUI();
        }

        private void DisableTogglesUI()
        {
            ignoreEvents = true;

            Toggle_Cooldown.IsChecked = false;
            Toggle_Cooldown.IsEnabled = false;

            Toggle_Sun.IsChecked = false;
            Toggle_Sun.IsEnabled = false;

            Toggle_Placement.IsChecked = false;
            Toggle_Placement.IsEnabled = false;

            Toggle_Invincible.IsChecked = false;
            Toggle_Invincible.IsEnabled = false;

            Toggle_OneHitKill.IsChecked = false;
            Toggle_OneHitKill.IsEnabled = false;

            Toggle_Accelerate.IsChecked = false;
            Toggle_Accelerate.IsEnabled = false;

            Toggle_Speed.IsChecked = false;
            Toggle_Speed.IsEnabled = false;
            Slider_Speed.IsEnabled = false;
            Slider_Speed.Value = 1.0;

            // Reset features states
            cooldownFeature.Cleanup(null!);
            sunFeature.Cleanup(null!);
            placementFeature.Cleanup(null!);
            invincibleFeature.Cleanup(null!);
            oneHitKillFeature.Cleanup(null!);
            accelerateFeature.Cleanup(null!);
            speedFeature.Cleanup(null!);

            ignoreEvents = false;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Feature_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreEvents || !attached || pm == null) return;
            var toggle = (ToggleButton)sender;
            CheatFeature? feature = GetFeatureFromToggle(toggle);
            if (feature != null)
            {
                bool success = feature.Enable(pm, baseAddress);
                if (!success)
                {
                    ignoreEvents = true;
                    toggle.IsChecked = false;
                    ignoreEvents = false;
                    MessageBox.Show(
                        T($"激活失败: '{feature.Name}'，请确认已进入关卡内。", $"Activation failed: '{feature.Name}', make sure you are in a level."), 
                        T("提示", "Tip"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning
                    );
                }
            }
        }

        private void Feature_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreEvents || !attached || pm == null) return;
            var toggle = (ToggleButton)sender;
            CheatFeature? feature = GetFeatureFromToggle(toggle);
            feature?.Disable(pm, baseAddress);
        }

        private void SpeedFeature_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreEvents || !attached || pm == null) return;
            Slider_Speed.IsEnabled = true;
            double speed = Slider_Speed.Value;
            bool success = speedFeature.SetSpeed(pm, baseAddress, speed);
            if (!success)
            {
                ignoreEvents = true;
                Toggle_Speed.IsChecked = false;
                Slider_Speed.IsEnabled = false;
                ignoreEvents = false;
                MessageBox.Show(
                    T("激活游戏变速失败，请确认已在关卡内。", "Failed to activate game speed adjustment, make sure you are in a level."), 
                    T("提示", "Tip"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
            }
        }

        private void SpeedFeature_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreEvents || !attached || pm == null) return;
            Slider_Speed.IsEnabled = false;
            speedFeature.Disable(pm, baseAddress);
        }

        private void Slider_Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Text_SpeedValue != null)
            {
                Text_SpeedValue.Text = e.NewValue.ToString("0.0");
            }

            if (!attached || pm == null || Toggle_Speed == null || Toggle_Speed.IsChecked != true) return;
            speedFeature.SetSpeed(pm, baseAddress, e.NewValue);
        }

        private void Btn_EnableAll_Click(object sender, RoutedEventArgs e)
        {
            if (!attached || pm == null) return;
            ignoreEvents = true;

            int success = 0;
            if (!cooldownFeature.Enabled) { if (cooldownFeature.Enable(pm, baseAddress)) { Toggle_Cooldown.IsChecked = true; success++; } }
            if (!sunFeature.Enabled) { if (sunFeature.Enable(pm, baseAddress)) { Toggle_Sun.IsChecked = true; success++; } }
            if (!placementFeature.Enabled) { if (placementFeature.Enable(pm, baseAddress)) { Toggle_Placement.IsChecked = true; success++; } }
            if (!invincibleFeature.Enabled) { if (invincibleFeature.Enable(pm, baseAddress)) { Toggle_Invincible.IsChecked = true; success++; } }
            if (!oneHitKillFeature.Enabled) { if (oneHitKillFeature.Enable(pm, baseAddress)) { Toggle_OneHitKill.IsChecked = true; success++; } }
            if (!accelerateFeature.Enabled) { if (accelerateFeature.Enable(pm, baseAddress)) { Toggle_Accelerate.IsChecked = true; success++; } }

            ignoreEvents = false;

            StatusLabel.Text = T($"一键开启完成！共新激活了 {success} 项功能。", $"Enable-all completed! Newly activated {success} features.");
            StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }

        private void Btn_RestoreAll_Click(object sender, RoutedEventArgs e)
        {
            if (!attached || pm == null) return;
            ignoreEvents = true;

            cooldownFeature.Disable(pm, baseAddress); Toggle_Cooldown.IsChecked = false;
            sunFeature.Disable(pm, baseAddress); Toggle_Sun.IsChecked = false;
            placementFeature.Disable(pm, baseAddress); Toggle_Placement.IsChecked = false;
            invincibleFeature.Disable(pm, baseAddress); Toggle_Invincible.IsChecked = false;
            oneHitKillFeature.Disable(pm, baseAddress); Toggle_OneHitKill.IsChecked = false;
            accelerateFeature.Disable(pm, baseAddress); Toggle_Accelerate.IsChecked = false;
            
            speedFeature.Disable(pm, baseAddress);
            Toggle_Speed.IsChecked = false;
            Slider_Speed.Value = 1.0;
            Slider_Speed.IsEnabled = false;

            ignoreEvents = false;

            StatusLabel.Text = T("全部修改已还原！", "All features restored successfully!");
            StatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }

        private void Btn_AdminRelaunch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    T($"提权失败: {ex.Message}", $"Elevation failed: {ex.Message}"), 
                    T("错误", "Error"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            }
        }

        private CheatFeature? GetFeatureFromToggle(ToggleButton toggle)
        {
            if (toggle == Toggle_Cooldown) return cooldownFeature;
            if (toggle == Toggle_Sun) return sunFeature;
            if (toggle == Toggle_Placement) return placementFeature;
            if (toggle == Toggle_Invincible) return invincibleFeature;
            if (toggle == Toggle_OneHitKill) return oneHitKillFeature;
            if (toggle == Toggle_Accelerate) return accelerateFeature;
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            attachTimer.Stop();
            if (attached && pm != null)
            {
                cooldownFeature.Disable(pm, baseAddress);
                sunFeature.Disable(pm, baseAddress);
                placementFeature.Disable(pm, baseAddress);
                invincibleFeature.Disable(pm, baseAddress);
                oneHitKillFeature.Disable(pm, baseAddress);
                accelerateFeature.Disable(pm, baseAddress);
                speedFeature.Disable(pm, baseAddress);

                cooldownFeature.Cleanup(pm);
                sunFeature.Cleanup(pm);
                placementFeature.Cleanup(pm);
                invincibleFeature.Cleanup(pm);
                oneHitKillFeature.Cleanup(pm);
                accelerateFeature.Cleanup(pm);
                speedFeature.Cleanup(pm);

                pm.Dispose();
            }
            base.OnClosed(e);
        }
    }
}