using MySql.Data.MySqlClient;
using Projekat_B_isTovar.Resources;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Projekat_B_isTovar.Views
{
    public partial class MyProfileWindow : Window
    {
        private int userId;
        private MyProfileStrings loc;

        public MyProfileWindow(int userId)
        {
            InitializeComponent();
            loc = new MyProfileStrings();
            DataContext = loc;

            this.userId = userId;
            Loaded += (s, e) => LoadUserData(); // Move LoadUserData to Loaded event to ensure UI is initialized
        }

        private void LoadUserData()
        {
            try
            {
                using var conn = Database.GetConnection();
                conn.Open();

                // First, get the user's role
                string roleQuery = "SELECT uloga FROM korisnik WHERE idKorisnika=@id";
                MySqlCommand roleCmd = new MySqlCommand(roleQuery, conn);
                roleCmd.Parameters.AddWithValue("@id", userId);
                var roleResult = roleCmd.ExecuteScalar();
                if (roleResult == null)
                {
                    MessageBox.Show("Korisnik nije pronađen.");
                    return;
                }
                int uloga = Convert.ToInt32(roleResult);

                // Base query for common user data
                string query = @"SELECT k.korisnickoIme, k.ime, k.prezime, k.email, k.brojTelefona, k.uloga
                                FROM korisnik k
                                WHERE k.idKorisnika=@id";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", userId);

                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtUsername.Text = reader.GetString("korisnickoIme");
                    txtIme.Text = reader.GetString("ime");
                    txtPrezime.Text = reader.GetString("prezime");
                    txtEmail.Text = reader.GetString("email");
                    txtTelefon.Text = reader.GetString("brojTelefona");
                    txtPassword.Password = "";
                }
                else
                {
                    MessageBox.Show("Korisnički podaci nisu pronađeni.");
                    reader.Close();
                    return;
                }
                reader.Close();

                // Fetch status based on role
                string status = loc.Unavailable; // Default to Unavailable
                if (uloga == 0) // Dispatcher
                {
                    string statusQuery = "SELECT status FROM dispecer WHERE idKorisnika=@id";
                    MySqlCommand statusCmd = new MySqlCommand(statusQuery, conn);
                    statusCmd.Parameters.AddWithValue("@id", userId);
                    var result = statusCmd.ExecuteScalar();
                    status = result != null && result.ToString() == "1" ? loc.Available : loc.Unavailable;
                }
                else if (uloga == 1) // Driver
                {
                    string dostupnostQuery = "SELECT dostupnost FROM vozac WHERE idKorisnika=@id";
                    MySqlCommand dostupnostCmd = new MySqlCommand(dostupnostQuery, conn);
                    dostupnostCmd.Parameters.AddWithValue("@id", userId);
                    var result = dostupnostCmd.ExecuteScalar();
                    status = result != null && Convert.ToInt32(result) == 1 ? loc.Available : loc.Unavailable;
                }

                // Ensure ComboBox items are loaded before setting selection
                if (cbStatus.Items.Count == 0)
                {
                    MessageBox.Show("ComboBox stavke nisu učitane. Proverite lokalizaciju.");
                    return;
                }

                // Set ComboBox selection safely
                var selectedItem = cbStatus.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content?.ToString() == status);
                if (selectedItem != null)
                {
                    cbStatus.SelectedItem = selectedItem;
                }
                else
                {
                    // Debugging: Log if no matching item is found
                    MessageBox.Show($"Nema podudaranja za status: '{status}'. Dostupne opcije: " +
                        string.Join(", ", cbStatus.Items.Cast<ComboBoxItem>().Select(i => i.Content?.ToString())));
                    cbStatus.SelectedIndex = 1; // Default to Unavailable
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"{Strings.Error}: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška prilikom učitavanja podataka: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPassword = txtPassword.Password.Trim();
                UpdateUserInDatabase(userId, txtUsername.Text.Trim(), txtIme.Text.Trim(),
                                   txtPrezime.Text.Trim(), txtEmail.Text.Trim(),
                                   newPassword, txtTelefon.Text.Trim());
                MessageBox.Show($"{Strings.DataChanged}");
                this.Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"{Strings.Error}: {ex.Message}");
            }
        }

        private void UpdateUserInDatabase(int id, string username, string ime, string prezime,
                                        string email, string password, string telefon)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // Update korisnik table
            string updateQuery;
            MySqlCommand cmd;

            if (string.IsNullOrEmpty(password))
            {
                updateQuery = @"UPDATE korisnik SET korisnickoIme=@user, ime=@ime, prezime=@prezime,
                              email=@email, brojTelefona=@tel WHERE idKorisnika=@id";
                cmd = new MySqlCommand(updateQuery, conn);
            }
            else
            {
                string hashedPassword = HashPassword.HashSHA256(password);
                updateQuery = @"UPDATE korisnik SET korisnickoIme=@user, ime=@ime, prezime=@prezime,
                              email=@email, lozinka=@pass, brojTelefona=@tel WHERE idKorisnika=@id";
                cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@pass", hashedPassword);
            }

            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@ime", ime);
            cmd.Parameters.AddWithValue("@prezime", prezime);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@tel", telefon);
            cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();

            // Update status based on role
            string status = cbStatus.SelectedItem != null && ((ComboBoxItem)cbStatus.SelectedItem).Content?.ToString() == loc.Available
                ? loc.Available
                : loc.Unavailable;
            string statusValue = status == loc.Available ? "1" : "0";

            // Get user role
            string roleQuery = "SELECT uloga FROM korisnik WHERE idKorisnika=@id";
            MySqlCommand roleCmd = new MySqlCommand(roleQuery, conn);
            roleCmd.Parameters.AddWithValue("@id", id);
            int uloga = Convert.ToInt32(roleCmd.ExecuteScalar());

            if (uloga == 0) // Dispatcher
            {
                string updateStatusQuery = "UPDATE dispecer SET status=@status WHERE idKorisnika=@id";
                MySqlCommand statusCmd = new MySqlCommand(updateStatusQuery, conn);
                statusCmd.Parameters.AddWithValue("@status", statusValue);
                statusCmd.Parameters.AddWithValue("@id", id);
                statusCmd.ExecuteNonQuery();
            }
            else if (uloga == 1) // Driver
            {
                string updateDostupnostQuery = "UPDATE vozac SET dostupnost=@dostupnost WHERE idKorisnika=@id";
                MySqlCommand dostupnostCmd = new MySqlCommand(updateDostupnostQuery, conn);
                dostupnostCmd.Parameters.AddWithValue("@dostupnost", Convert.ToInt32(statusValue));
                dostupnostCmd.Parameters.AddWithValue("@id", id);
                dostupnostCmd.ExecuteNonQuery();
            }
        }
    }
}