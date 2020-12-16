using Npgsql;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;


namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для AddTool.xaml
    /// </summary>
    public partial class AddIngridient : Window
    {
        public AddIngridient()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var cmd = new NpgsqlCommand($"INSERT INTO ingredients (name, degree, type, description) VALUES ('{name.Text}', '{degree.Text}', '{type.Text}', '{description.Text}')", MainWindow.conn))
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
                MainWindow.ingridients = new BindingList<Ingridients>();
                MainWindow.ReadCocktailsFromDB();
                Close();
            }
        }
    }
}
