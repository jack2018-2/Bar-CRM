using System;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.Generic;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static ObservableCollection<Cocktails> cocktails = new ObservableCollection<Cocktails>();
        internal static BindingList<Cocktails_ingridients> cocktails_ingridients = new BindingList<Cocktails_ingridients>();
        internal static BindingList<Cocktails_tools> cocktails_tools = new BindingList<Cocktails_tools>();
        internal static BindingList<Ingridients> ingridients = new BindingList<Ingridients>();
        internal static BindingList<Tools> tools = new BindingList<Tools>();
        internal static NpgsqlConnection conn;


        public MainWindow()
        {
            InitializeComponent();
            ConnectDB();
            Console.WriteLine(conn.FullState);
            ReadDataFromDB();
            //CocktailsList.ItemsSource = cocktails;
            //ToolsList.ItemsSource = tools;
            cocktailType.ItemsSource = Enum.GetValues(typeof(enum1)).Cast<enum1>().ToList();
            cocktailBase.ItemsSource = ingridients;
            cocktailView.ItemsSource = cocktails;

        }

        internal void ReadDataFromDB()
        {
            ReadCocktailsFromDB();
            ReadCocktailsIngridientsFromDB();
            ReadCocktailsToolsFromDB();
            ReadIngridientsFromDB();
            ReadToolsFromDB();
        }

        internal static void ReadCocktailsFromDB()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM cocktails;", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Cocktails cocktail = new Cocktails()
                        {
                            id = Convert.ToInt32(reader["id"].ToString()),
                            name = reader["name"].ToString(),
                            description = reader["description"].ToString(),
                            degree = Convert.ToInt32(reader["degree"].ToString()),
                            picture = ObjectToByteArray(reader["picture"]),
                            volume = Convert.ToInt32(reader["volume"].ToString()),
                            receipt = reader["receipt"].ToString(),
                            group = reader["group"].ToString(),
                            basis_id = Convert.ToInt32(reader["basis_id"].ToString()),
                            taste = reader["taste"].ToString()
                        };
                        cocktails.Add(cocktail);
                    } 
                }
            }
        }

        internal static void ReadCocktailsIngridientsFromDB()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            BindingList<Cocktails_ingridients> result = new BindingList<Cocktails_ingridients>();
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM cocktails_ingredients;", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Cocktails_ingridients c = new Cocktails_ingridients()
                        {
                            cocktail_id = Convert.ToInt32(reader["cocktail_id"].ToString()),
                            ingridient_id = Convert.ToInt32(reader["ingredient_id"].ToString()),
                            volume = Convert.ToInt32(reader["volume"].ToString()),

                        };
                        cocktails_ingridients.Add(c);
                    }
                }
            }
        }

        internal static void ReadCocktailsToolsFromDB()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM cocktails_tools;", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Cocktails_tools c = new Cocktails_tools()
                        {
                            cocktail_id = Convert.ToInt32(reader["cocktail_id"].ToString()),
                            tool_id = Convert.ToInt32(reader["tool_id"].ToString()),
                        };
                        cocktails_tools.Add(c);
                    }
                }
            }
        }

        internal static void ReadIngridientsFromDB()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM ingredients;", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Ingridients c = new Ingridients()
                        {
                            id = Convert.ToInt32(reader["id"].ToString()),
                            name = reader["name"].ToString(),
                            degree = Convert.ToInt32(reader["degree"].ToString()),
                            type = reader["degree"].ToString(),
                            description = reader["degree"].ToString(),
                            picture = ObjectToByteArray(reader["degree"]),
                        };
                        ingridients.Add(c);
                    }
                }
            }
        }

        internal static void ReadToolsFromDB()
        {
            if (conn.State == System.Data.ConnectionState.Closed || conn.State == System.Data.ConnectionState.Connecting) throw new Exception("БД не подключилась");
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand("SELECT * FROM tools;", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Tools c = new Tools()
                        {
                            id = Convert.ToInt32(reader["id"].ToString()),
                            name = reader["name"].ToString(),
                            description = reader["description"].ToString(),
                            picture = ObjectToByteArray(reader["description"]),

                        };
                        tools.Add(c);
                    }
                }
            }
        }

        public static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static void ConnectDB()
        {
            var connect = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=cursework;";
            conn = new NpgsqlConnection(connect);
            conn.Open();
            //Console.WriteLine("Connected!");
        }

        private void CocktailsSearch(object sender, RoutedEventArgs e)
        {
            int _basis_id = -1;
            NpgsqlCommand npgsqlCommand = new NpgsqlCommand($"SELECT id FROM ingredients where name = '{cocktailBase.Text}';", conn);
            using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                    while (reader.Read())
                        _basis_id = Convert.ToInt32(reader["id"].ToString());
            }
            var res = from elem in cocktails where (elem.name.ToLower().Contains(cocktailName.Text.ToLower()) && elem.taste == cocktailType.Text && elem.basis_id == _basis_id) select elem;
            cocktailView.ItemsSource = res;
        }

        private void cocktailView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var res = from elem in cocktails where elem.id == ((Cocktails)((ListBox)sender).SelectedItem).name select elem;
            selectedName.Text = ((Cocktails)((ListBox)sender).SelectedItem)?.name;
            selectedReceipt.Text = ((Cocktails)((ListBox)sender).SelectedItem)?.receipt;
            /*using (var ms = new MemoryStream(((Cocktails)((ListBox)sender).SelectedItem)?.picture))
            {
                image.Source = System.Drawing.Image.FromStream(ms);
            }*/
            //BitmapSource bitmapSource = BitmapSource.Create(2, 2, 300, 300, PixelFormats.Indexed8, BitmapPalettes.Halftone8, ((Cocktails)((ListBox)sender).SelectedItem)?.picture, 2);

            //image.Source = bitmapSource;

        }

        private void AddCocktail(object sender, RoutedEventArgs e)
        {
            AddCocktail taskWindow = new AddCocktail();
            taskWindow.Show();
        }

        private void AddIngridient(object sender, RoutedEventArgs e)
        {
            AddIngridient taskWindow = new AddIngridient();
            taskWindow.Show();
        }
    }
}

// Insert some data
/*await using (var cmd = new NpgsqlCommand("INSERT INTO data (some_field) VALUES (@p)", conn))
{
    cmd.Parameters.AddWithValue("p", "Hello world");
    await cmd.ExecuteNonQueryAsync();
}*/
