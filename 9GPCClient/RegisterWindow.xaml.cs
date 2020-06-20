using System.IO;
using System.Windows;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace _9GPCClient
{
    /// <summary>
    /// Interaktionslogik für RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {

        private string username, password;

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            username = txtUsername.Text;
            password = txtPassword.Text;

            registerRequest(); // lieber auf den asynchronen thread warten, freezt das UI so schön... hach
        }

        private async Task registerRequest()
        {

            // HttpRequest an die API
            Dictionary<string, string> param = new Dictionary<string, string>();

            param.Add("username", username);
            param.Add("password", password);

            var content = new FormUrlEncodedContent(param);


            HttpClient client = new HttpClient();
            var response = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/Register.php", content);

            string returnCode = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(returnCode);

            switch (returnCode)
            {
                case "0":
                    // erfolgreich!
                    File.WriteAllText(MainWindow.DataFilePath, username + "\n" + password);
                    MessageBox.Show("Registrierung Erfolgreich, Registrierungsdaten lokal gespeichert, keine weitere Anmeldung nötig!");
                    this.Dispatcher.Invoke(new System.Action(() =>
                    {
                        this.Close();
                    }));
                    return;
                case "1":
                    MessageBox.Show("Unerwarteter Datenbankfehler!");
                    return;
                case "2":
                    MessageBox.Show("Nutzername existiert bereits!");
                    return;
                case "5":
                    MessageBox.Show("Passwort zu kurz, mindestens 5 Zeichen");
                    return;
                case "6":
                    MessageBox.Show("Nutzername zu kurz, mindestens 3 Zeichen");
                    return;
            }
        }
    }
}
