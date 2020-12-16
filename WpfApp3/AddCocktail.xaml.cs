using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для AddItem.xaml
    /// </summary>
    public partial class AddCocktail : Window
    {
        public AddCocktail()
        {
            InitializeComponent();
            taste.ItemsSource = Enum.GetValues(typeof(enum1)).Cast<enum1>().ToList();
            cocktailBase.ItemsSource = MainWindow.ingridients;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int _basis_id = -1;
                NpgsqlCommand npgsqlCommand = new NpgsqlCommand($"SELECT id FROM ingredients where name = '{cocktailBase.Text}';", MainWindow.conn);
                using (NpgsqlDataReader reader = npgsqlCommand.ExecuteReader())
                {
                    if (reader.HasRows)
                        while (reader.Read())
                            _basis_id = Convert.ToInt32(reader["id"].ToString());
                }
                using (var cmd = new NpgsqlCommand($"insert into cocktails (name, description, degree, volume, receipt, \"group\", basis_id, taste) VALUES ('{name.Text}','{description.Text}', {degree.Text}, {volume.Text},'{receipt.Text}','{group.Text}','{_basis_id}','{taste.Text}')", MainWindow.conn))
                {
                    //cmd.Parameters.AddWithValue("p", "Hello world");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Произошла ошибка: {exc.Message}");
            }
            finally
            {
                MainWindow.cocktails = new ObservableCollection<Cocktails>();
                MainWindow.ReadCocktailsFromDB();
                Close();
            }
        }
    }
}
