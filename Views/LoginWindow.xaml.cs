using MySql.Data.MySqlClient;
using Projekat_B_isTovar.Resources;
using Projekat_B_isTovar.UserThemes;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Projekat_B_isTovar.Views
{
    public partial class LoginWindow : Window
    {
        private UserSettings settings;
        private LoginStrings loc;
        public LoginWindow()
        {
            InitializeComponent();

            settings = UserSettings.Load();

            loc = new LoginStrings();
            DataContext = loc;

            ApplyManager.ApplyTheme(settings.Theme);
            ApplyManager.ApplyLanguage(settings.Language, loc);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = $"{loc.EmptyFieldsMessage}";
                return;
            }

            try
            {
                var userData = GetUserFromDatabase(username, password);
                if (userData != null)
                {
                    int role = userData.Value.Role;
                    int userId = userData.Value.UserId;

                    SessionManager.CurrentUserId = userId;
                    SessionManager.CurrentUserRole = role;
                    SessionManager.CurrentUsername = username;

                    Window next;

                    if (role == 0)
                        next = new DispatcherHome();
                    else
                        next = new DriverHome(); 

                    next.Show();

                    this.Close();
                    return;
                }
                else
                {
                    lblError.Text = $"{loc.InvalidCredentialsMessage}";
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"{Strings.ConnectionError}: " + ex.Message);
            }
        }

        private (int UserId, int Role)? GetUserFromDatabase(string username, string password)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT idKorisnika, uloga FROM korisnik WHERE korisnickoIme=@user AND lozinka=@pass";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", password);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int userId = reader.GetInt32("idKorisnika");
                    int role = reader.GetInt32("uloga");
                    return (userId, role);
                }
            }
            return null;
        }

        /*private (int UserId, int Role)? GetUserFromDatabase(string username, string password)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT idKorisnika, uloga, lozinka FROM korisnik WHERE korisnickoIme=@user";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32("idKorisnika");
                        int role = reader.GetInt32("uloga");
                        string storedHash = reader.GetString("lozinka");

                        // Heširaj unesenu lozinku i usporedi s pohranjenim hešom
                        string inputHash = HashPasswordSHA256(password);
                        if (inputHash == storedHash)
                        {
                            return (userId, role);
                        }
                    }
                }
            }
            return null;
        }*/

        private string HashPasswordSHA256(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2")); // x2 formatira bajt kao dva heksadecimalna znaka
                }
                return builder.ToString();
            }
        }

        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

      
    }
}
