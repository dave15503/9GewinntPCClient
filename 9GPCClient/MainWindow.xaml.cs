using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace _9GPCClient
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Button[,] Fields;
        private Timer GameRefreshTimer;

        public const string DataFilePath = "data.credencials";

        private string Username, Password;
        private string CurrentToken;
        private GameData Game;
        private bool GameReady = false;

        private Color Player0Col = Color.FromRgb(0x46, 0x00, 0xff);
        private Color Player1Col = Color.FromRgb(0xBD, 0x42, 0x42);

        public MainWindow()
        {
            InitializeComponent();

            // beim starten prüfen ob config datei existiert
            if (!File.Exists(DataFilePath))
            {
                // neues fenster das auffordert zu registrieren öffnen
                RegisterWindow wnd = new RegisterWindow();
                wnd.ShowDialog();
            }

            readCredencials();

            // Grafikelemente: //
            Fields = new Button[9, 9];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {

                    Fields[i, j] = new Button();

                    Fields[i, j].Background = Brushes.Transparent;
                    Fields[i, j].BorderBrush = Brushes.Black;


                    //dickere borders um die 3 felder abzutrennen

                    Thickness thk = new Thickness(0.5);
                    double thicker = 1;


                    if (i % 3 != 1 && j % 3 != 1 || true)
                    {
                        // Linien zeichnen
                        switch (j % 3)
                        {
                            case 0:
                                thk.Top = thicker;
                                break;
                            case 2:
                                thk.Bottom = thicker;
                                break;

                        }

                        switch (i % 3)
                        {
                            case 0:
                                thk.Left = thicker;
                                break;
                            case 2:
                                thk.Right = thicker;
                                break;
                        }
                    }

                    Fields[i, j].BorderThickness = thk;

                    Fields[i, j].Tag = new Coordinate(i, j);
                    Fields[i, j].Click += fieldClick;

                    Grid.SetColumn(Fields[i, j], i);
                    Grid.SetRow(Fields[i, j], j);
                    pnlGame.Children.Add(Fields[i, j]);
                }
            }

            // Timer:
            GameRefreshTimer = new Timer();
            GameRefreshTimer.Interval = 2000; // alle 2000ms auslösen
            GameRefreshTimer.AutoReset = true; // immer wieder refreshen!
            GameRefreshTimer.Elapsed += GameRefresh;
            GameRefreshTimer.Enabled = false; // erst mit einer Methode anmachen
            // GameRefreshTimer.Enabled = true; // Timer erst mit einem Knopfdruck aktiveren, bei verbindung mit dem spiel

        }

        private void readCredencials()
        {
            string[] lines = File.ReadAllLines(DataFilePath);
            Username = lines[0];
            Password = lines[1];
        }

        private void GameRefresh(object sender, ElapsedEventArgs e)
        {
            try
            {
                Refresh();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("hallo" + ex.Message);
            }
        }

        private async Task Refresh()
        {
            // Status Daten vom Server holen mithilfe eines HttpRequests
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("username", Username);
            param.Add("password", Password);
            param.Add("token", CurrentToken);

            HttpContent content = new FormUrlEncodedContent(param);
            HttpClient client = new HttpClient();
            lblPending.Dispatcher.Invoke(new Action(() =>
            {
                lblPending.Content = "Aktualisieren";
            }));
            HttpResponseMessage resp = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/Game/GetStatus.php", content);
            string returnCode = await resp.Content.ReadAsStringAsync();

            switch (returnCode)
            {
                case "1":
                    prompt("Unerwarteter Datenbankfehler!");
                    return;
                case "3":
                    prompt("Nutzername nicht bekanne [sollte nicht vorkommen]");
                    return;
                case "4":
                    prompt("Falsches Passwort [sollte nicht vorkommen]");
                    return;
                case "7":
                    prompt("Spielsession existiert nicht [sollte nicht vorkommen]");
                    return;
                case "8":
                    prompt("Zugriff verweigert [sollte nicht vorkommen]");
                    return;
                case "9":
                    lblPending.Dispatcher.Invoke(new Action(() =>
                    {
                        lblPending.Content = "Warten auf Mitspieler";
                    }));
                    break;
                default:
                    lblPending.Dispatcher.Invoke(new Action(() =>
                    {
                        lblPending.Content = "Bereit";
                    }));
                    GameReady = true;
                    Game = JsonSerializer.Deserialize<GameData>(returnCode);
                    if(Game.ActiveSection == -2)
                    {
                        GameReady = false; // Gewinner ermittelt!
                        GameRefreshTimer.Enabled = false;
                    }
                    drawUI();
                    return;
            }
        }

        private void drawUI()
        {
            // anhand der Tabellarischen Daten in GameData objekt Game
            // mithilfe von linien und Kreisen in das Raster aus Buttons zeichnen
            Application.Current.Dispatcher.Invoke((Action)delegate{

                // Kleine Felder Zeichnen

                double smallfieldWidth = pnlGame.ColumnDefinitions[0].ActualWidth;
                double smallfieldHeight = pnlGame.RowDefinitions[0].ActualHeight;
                const double smallfieldMargin = 1d;

                int fieldidx, section;
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        fieldidx = calculateIndex(i, j);
                        section = calculateSection(i, j);

                        Canvas c = new Canvas();
                        c.Width = smallfieldWidth;
                        c.Height = smallfieldHeight;
                        c.Background = Brushes.Transparent;

                        // kleine kreuze und kreise malen:
                        if (Game.Field[section][fieldidx] == 1) // 1 = kreuz
                        {
                            // zwei linien in den Button malen
                            Line l0 = new Line();
                            l0.StrokeThickness = 1;
                            l0.Stroke = new SolidColorBrush(Player0Col);
                            l0.X1 = smallfieldMargin;
                            l0.Y1 = smallfieldMargin;
                            l0.X2 = smallfieldWidth - smallfieldMargin;
                            l0.Y2 = smallfieldHeight - smallfieldMargin;

                            Line l1 = new Line();
                            l1.StrokeThickness = 1;
                            l1.Stroke = new SolidColorBrush(Player0Col);
                            l1.X1 = smallfieldMargin;
                            l1.Y1 = smallfieldHeight - smallfieldMargin;
                            l1.X2 = smallfieldWidth - smallfieldMargin;
                            l1.Y2 = smallfieldMargin;

                            
                            c.Children.Add(l0);
                            c.Children.Add(l1);
                            Fields[i, j].Content = c;

                        }
                        else if (Game.Field[section][fieldidx] == 2) // 2 = kreis
                        {
                            Ellipse el = new Ellipse();
                            el.Stroke = new SolidColorBrush(Player1Col);
                            el.StrokeThickness = 1;
                            el.Width = smallfieldWidth - 2 * smallfieldMargin;
                            el.Height = smallfieldHeight - 2 * smallfieldMargin;
                            Canvas.SetTop(el, smallfieldMargin);
                            Canvas.SetLeft(el, smallfieldMargin);

                            c.Children.Add(el);
                            Fields[i, j].Content = c;
                        }



                    }
                }

                // Große Felder 
                // größen festlegen
                double bigfieldWidth = pnlGameOverlay.ColumnDefinitions[0].ActualWidth;
                double bigfieldHeight = pnlGameOverlay.RowDefinitions[0].ActualHeight;
                const double bigfieldMargin = 2d;

                // das Selektierte Feld markieren
                pnlGameOverlay.Children.Clear(); // alle vorherigen Markierungen entfernen
                int bfieldx = 0, bfieldy = 0;
                if(Game.ActiveSection < 0 || Game.ActiveSection > 8 || Game.ActiveSection == -1)
                {
                    ; //nichts zeichnen
                }
                else
                {
                    bfieldx = Game.ActiveSection % 3;
                    if (Game.ActiveSection < 3) bfieldy = 0;
                    else if (Game.ActiveSection < 6) bfieldy = 1;
                    else if (Game.ActiveSection < 9) bfieldy = 2;

                    Rectangle rect = new Rectangle();
                    rect.Width = bigfieldWidth;
                    rect.Height = bigfieldHeight;
                    rect.Fill = Brushes.LightGreen;
                    Grid.SetRow(rect, bfieldy);
                    Grid.SetColumn(rect, bfieldx);

                    pnlGameOverlay.Children.Add(rect);
                }

                // Große Kreise und Kreuze malen
                int bsection;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        bsection = i * 3 + j;

                        Canvas c = new Canvas();
                        c.Width = bigfieldWidth;
                        c.Height = bigfieldHeight;
                        c.Background = Brushes.Transparent;
                        c.VerticalAlignment = VerticalAlignment.Top;
                        c.HorizontalAlignment = HorizontalAlignment.Left;

                        // kleine kreuze und kreise malen:
                        if (Game.BigField[bsection] == 1) // 1 = kreuz
                        {
                            // zwei linien in den Button malen
                            Line l0 = new Line();
                            l0.StrokeThickness = 3;
                            l0.Stroke = new SolidColorBrush(Player0Col);
                            l0.X1 = bigfieldMargin;
                            l0.Y1 = bigfieldMargin;
                            l0.X2 = bigfieldWidth - bigfieldMargin;
                            l0.Y2 = bigfieldHeight - bigfieldMargin;

                            Line l1 = new Line();
                            l1.StrokeThickness = 3;
                            l1.Stroke = new SolidColorBrush(Player0Col);
                            l1.X1 = bigfieldMargin;
                            l1.Y1 = bigfieldHeight - bigfieldMargin;
                            l1.X2 = bigfieldWidth - bigfieldMargin;
                            l1.Y2 = bigfieldMargin;

                            c.Children.Add(l0);
                            c.Children.Add(l1);
                        }
                        else if (Game.BigField[bsection] == 2) // 2 = kreis
                        {
                            Ellipse el = new Ellipse();
                            el.Stroke = new SolidColorBrush(Player1Col);
                            el.StrokeThickness = 3;
                            el.Width = bigfieldWidth - 2 * bigfieldMargin;
                            el.Height = bigfieldHeight - 2 * bigfieldMargin;
                            Canvas.SetTop(el, bigfieldMargin);
                            Canvas.SetLeft(el, bigfieldMargin);

                            c.Children.Add(el);

                        }
                        else continue; // feld leer, nächster schleifendurchlauf

                        // feld nicht leer
                        // Canvas ins Grid einsetzen
                        Grid.SetColumn(c, j);
                        Grid.SetRow(c, i);
                        pnlGameOverlay.Children.Add(c);

                        // Hintergrund der sektion ausgrauen
                        Rectangle rect = new Rectangle();
                        rect.Width = bigfieldWidth;
                        rect.Height = bigfieldHeight;
                        rect.Fill = new SolidColorBrush(Color.FromArgb(100, 0xba, 0xab, 0xab));
                        Grid.SetRow(rect, i);
                        Grid.SetColumn(rect, j);
                        pnlGameOverlay.Children.Add(rect);

                        //DIe Knöpfe in dieser Sektion entfernen!
                        //Panel p;

                        //for (int si = 0; si < 3; si++)
                        //{
                        //    for (int sj = 0; sj < 3; sj++)
                        //    {
                        //        p = (Panel)Fields[i * 3 + si, j * 3 + sj].Content;
                        //        if (p != null)
                        //        {
                        //            p.Visibility = Visibility.Hidden;
                        //        }
                        //        //Fields[i * 3 + si, j * 3 + sj].IsEnabled = false;

                        //    }
                        //}

                        //


                    }
                }
                // Das seitliche Panel mit Daten beschreiben:
                lblTurn.Content = Game.Turn == 1 ? "ICH" : "GEGNER";
                
                lblSection.Content = Game.ActiveSection < 0 || Game.ActiveSection > 8 ? "Freie Wahl" : Game.ActiveSection.ToString();

                if(GameReady == false && Game.ActiveSection == -2)
                {
                    // Gewinner!
                    prompt(Game.Turn == 1 ? "ICH habe gewonnen" : "GEGNER hat gewonnen");
                }

            });
        }

        // Methode wird aufgerufen wenn ein Knopf im Feld gedrückt wird
        private void fieldClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Coordinate btnpos = (Coordinate)btn.Tag;
            int fieldidx = calculateIndex(btnpos.X, btnpos.Y);
            int section = calculateSection(btnpos.X, btnpos.Y);

            //testen ob das spiel schon bereit ist:
            if (GameReady)
            {
                // checken wer am zug ist:
                if (Game.Turn == 1)
                {
                    // ich am zug
                    //checken ob es das richtige große feld ist oder freie platzwahl!
                    if (section == Game.ActiveSection || Game.ActiveSection == -1)
                    {
                        // checken ob das feld nicht schon belegt wurde
                        if (Game.Field[section][fieldidx] != 0)
                        {
                            prompt("Feld bereits belegt!");
                            return;
                        }
                        else
                        {
                            //feld nicht belegt, kann gesetzt werden
                            setField(section, fieldidx);
                        }


                    }
                    else
                    {
                        // du darfst nicht in diese Sektion setzen
                        prompt("Falsche Sektion");
                        return;
                    }

                }
                else
                {
                    prompt("Du bist nicht am zug");
                    return;
                }
            }
            else
            {
                prompt("Spielsession noch nicht bereit!");
            }
            

        }

        private async Task setField(int section, int fieldidx)
        {
            //richtige section
            //HTTP request um das feld zu setzen
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("username", Username);
            param.Add("password", Password);
            param.Add("token", CurrentToken);
            param.Add("section", section.ToString());
            param.Add("field", fieldidx.ToString());


            HttpContent content = new FormUrlEncodedContent(param);
            HttpClient client = new HttpClient();
            lblPending.Dispatcher.Invoke(new Action(() =>
            {
                lblPending.Content = "Aktualisieren";
            }));
            HttpResponseMessage resp = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/Game/SetField.php", content);
            string returnCode = await resp.Content.ReadAsStringAsync();

            switch (returnCode)
            {
                case "1":
                    prompt("Unerwarteter Datenbankfehler!");
                    return;
                case "3":
                    prompt("Nutzername nicht bekanne [sollte nicht vorkommen]");
                    return;
                case "4":
                    prompt("Falsches Passwort [sollte nicht vorkommen]");
                    return;
                case "7":
                    prompt("Spielsession existiert nicht [sollte nicht vorkommen]");
                    return;
                case "8":
                    prompt("Zugriff verweigert [sollte nicht vorkommen]");
                    return;
                default: //Daten des neuen Statuses werden zurückggegeben
                    lblPending.Dispatcher.Invoke(new Action(() =>
                    {
                        lblPending.Content = "Bereit";
                    }));

                    Game = JsonSerializer.Deserialize<GameData>(returnCode);
                    if (Game.ActiveSection == -2)
                    {
                        GameReady = false; // Gewinner ermittelt!
                    }
                    drawUI();
                    return;
            }
        }

 


        private void btnNewGame_Click(object sender, RoutedEventArgs e)
        {
            newGame();
        }

        private async Task newGame()
        {
            // Erstellen eines Games vom Server anfragen
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("username", Username);
            param.Add("password", Password);

            FormUrlEncodedContent content = new FormUrlEncodedContent(param);

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/StartGame.php", content);

            string returnCode = await response.Content.ReadAsStringAsync();

            switch (returnCode)
            {
                case "1":
                    prompt("Unerwarteter Datenbankfehler!");
                    return;
                case "3":
                    prompt("Nutzername nicht bekanne [sollte nicht vorkommen]");
                    return;
                case "4":
                    prompt("Falsches Passwort [sollte nicht vorkommen]");
                    return;
                default:
                    // rückgabe ist der token als string
                    this.CurrentToken = returnCode;
                    GameRefreshTimer.Enabled = true;
                    txtTokenOutput.Dispatcher.Invoke(new Action(() =>
                    {
                        txtTokenOutput.Text = returnCode;
                    })); 
                    lblPending.Dispatcher.Invoke(new Action(() =>
                    {
                        lblPending.Content = "Warten auf Mitspieler";
                    }));
                    Game = new GameData();
                    Refresh();
                    return;
            }


        }

        private void btnConnectToGame_Click(object sender, RoutedEventArgs e)
        {
            connectToGame();
        }

        private async Task connectToGame()
        {
            string token = txtTokenInput.Text;
            // Erstellen eines Games vom Server anfragen
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("username", Username);
            param.Add("password", Password);
            param.Add("token", token);

            FormUrlEncodedContent content = new FormUrlEncodedContent(param);

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/JoinGame.php", content);

            string returnCode = await response.Content.ReadAsStringAsync();

            switch (returnCode)
            {
                case "1":
                    prompt("Unerwarteter Datenbankfehler!");
                    return;
                case "3":
                    prompt("Nutzername nicht bekanne [sollte nicht vorkommen]");
                    return;
                case "4":
                    prompt("Falsches Passwort [sollte nicht vorkommen]");
                    return;
                case "7":
                    prompt("Spielsession Existiert nicht");
                    return;
                case "0":
                    this.CurrentToken = token;
                    GameRefreshTimer.Enabled = true;
                    lblPending.Dispatcher.Invoke(new Action(() =>
                    {
                        lblPending.Content = "Bereit";
                    }));
                    GameReady = true;
                    Game = new GameData();
                    Refresh();
                    return;
            }
        }

        private void btnEndGame_Click(object sender, RoutedEventArgs e)
        {
            //testen ob das token überhaupt gesetzt ist:
            if(CurrentToken == null)
            {
                prompt("Keine Spielsession offen");
            }
            else endGame();
        }

        private async Task endGame()
        {
            // Erstellen eines Games vom Server anfragen
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("username", Username);
            param.Add("password", Password);
            param.Add("token", CurrentToken);

            FormUrlEncodedContent content = new FormUrlEncodedContent(param);

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync("http://davetestserver.bplaced.net/9Gewinnt/EndGame.php", content);

            string returnCode = await response.Content.ReadAsStringAsync();

            switch (returnCode)
            {
                case "1":
                    prompt("Unerwarteter Datenbankfehler!");
                    return;
                case "3":
                    prompt("Nutzername nicht bekanne [sollte nicht vorkommen]");
                    return;
                case "4":
                    prompt("Falsches Passwort [sollte nicht vorkommen]");
                    return;
                default:
                    this.CurrentToken = null;
                    GameRefreshTimer.Enabled = false;
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        txtTokenOutput.Text = "";
                        lblPending.Content = "Spiel beendet, warten auf neues Spiel";
                        clearUI();
                    });
                    return;
            }
        }

        private void clearUI()
        {
            for(int i = 0; i < 9; i++)
            {
                for(int j = 0; j < 9; j++)
                {
                    Fields[i, j].Content = new Canvas();
                }
            }
            pnlGameOverlay.Children.Clear();
        }

        private void prompt(string text)
        {
            Label lbl = new Label();
            lbl.Content = text;
            lbl.Foreground = Brushes.Red;
            pnlPrompt.Children.Insert(0, lbl);
        }

        private Coordinate calculateCoordinate(int section, int fieldidx)
        {
            Coordinate c = new Coordinate();
            c.X = section % 3 * 3 + fieldidx % 3;
            if (section < 3) c.Y = 0;
            else if (section < 6) c.Y = 3;
            else if (section < 9) c.Y = 6;

            if (fieldidx < 3) c.Y += 0;
            else if (fieldidx < 6) c.Y += 1;
            else if (fieldidx < 9) c.Y += 2;
            return c;

        }

        private int calculateIndex(int fieldX, int fieldY)
        {
            return fieldX % 3 + fieldY % 3 * 3;
        }

        private void btnCopyTokenToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtTokenOutput.Text);
        }

        private void btnCredits_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Idee:\n\tVictoria Ruff\nProgrammierung:\n\tDavid Schuberth\nBetatester:\n\tJoshua Matthes\n\tNico Goller", "Credits");
        }

        private int calculateSection(int fieldX, int fieldY)
        {
            if (fieldX < 3 && fieldY < 3) return 0;
            else if (fieldX < 6 && fieldY < 3) return 1;
            else if (fieldX < 9 && fieldY < 3) return 2;
            else if (fieldX < 3 && fieldY < 6) return 3;
            else if (fieldX < 6 && fieldY < 6) return 4;
            else if (fieldX < 9 && fieldY < 6) return 5;
            else if (fieldX < 3 && fieldY < 9) return 6;
            else if (fieldX < 6 && fieldY < 9) return 7;
            else if (fieldX < 9 && fieldY < 9) return 8;
            else throw new Exception("Ungültige Feldzahl!");
        }

    }

    public struct Coordinate
    {

        public int X, Y;

        public Coordinate(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
