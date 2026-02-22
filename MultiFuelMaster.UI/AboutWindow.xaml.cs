using System;
using System.Reflection;
using System.Windows;

namespace MultiFuelMaster.UI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                    VersionText.Text = $"Версия {version.Major}.{version.Minor}.{version.Build}";
            }
            catch { VersionText.Text = "Версия 1.0.0"; }

            BuildDateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Close();
    }
}
