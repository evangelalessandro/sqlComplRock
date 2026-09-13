using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace SmartSQLStudio
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            
            // Imposta il server di default
            ServerComboBox.Text = "localhost";
            AuthComboBox.SelectedIndex = 0;
            
            // Aggiorna UI in base all'autenticazione
            AuthComboBox.SelectionChanged += (s, e) => UpdateAuthUI();
        }

        private void UpdateAuthUI()
        {
            bool isWindowsAuth = AuthComboBox.SelectedIndex == 0;
            UsernameTextBox.IsEnabled = !isWindowsAuth;
            PasswordBox.IsEnabled = !isWindowsAuth;
            
            if (isWindowsAuth)
            {
                UsernameTextBox.Text = Environment.UserName;
                PasswordBox.Password = "";
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerComboBox.Text.Trim();
            string database = DatabaseTextBox.Text.Trim() ?? "master";
            bool windowsAuth = AuthComboBox.SelectedIndex == 0;
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(server))
            {
                MessageBox.Show("Inserisci il nome del server", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connectionString;
            if (windowsAuth)
            {
                connectionString = $"Server={server};Database={database};Integrated Security=true;TrustServerCertificate=true;";
            }
            else
            {
                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Inserisci il username", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                connectionString = $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=true;";
            }

            StatusTextBlock.Text = "Connessione in corso...";
            ConnectButton.IsEnabled = false;

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                // Test connessione
                using var command = new SqlCommand("SELECT @@VERSION", connection);
                var version = await command.ExecuteScalarAsync();
                
                StatusTextBlock.Text = $"Connesso: {version.ToString()?.Substring(0, Math.Min(50, version.ToString().Length))}...";
                StatusTextBlock.Foreground = FindResource("SuccessBrush") as System.Windows.Media.Brush;

                // Apri la finestra principale passando la connection string
                var mainWindow = new MainWindow(connectionString, server, database);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Errore: {ex.Message}";
                StatusTextBlock.Foreground = FindResource("ErrorBrush") as System.Windows.Media.Brush;
                ConnectButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
