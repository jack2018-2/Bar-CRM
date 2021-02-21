using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace СocktailParser
{
    class Program
    {
        static string connectionString = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=cursework;";
        static void Main(string[] args)
        {
            bool exit = false;
            while (exit == false)
            {
                Console.WriteLine("1) Сохранить коктейли в базу данных;\n2) Сохранить инструментов в базу данных;\n3) Сохранить ингридиентов в базу данных;\n0) Выход.");
                switch (Console.ReadLine())
                {
                    case "1":
                        {
                            WriteCocktails();
                            break;
                        }
                    case "2":
                        {
                            WriteTools();
                            break;
                        }
                    case "3":
                        {
                            WriteIngredients();
                            break;
                        }
                    case "0":
                        {
                            exit = true;
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Неизвестная команда, попробуйте снова.");
                            break;
                        }
                }
            }
        }
        #region CocktailMethods
        static void WriteCocktails()
        {
            string infoPath = "CocktailsBackup.json";
            if (!File.Exists(infoPath)) return;

            NpgsqlConnection npgSqlConnection = new NpgsqlConnection(connectionString);
            npgSqlConnection.Open();
            Console.WriteLine("Соединение с БД открыто");
            //NpgsqlCommand npgSqlCommand = new NpgsqlCommand("set search_path = \"publi"+"c\"", npgSqlConnection);
            CocktailsData[] cocktailList;
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(CocktailsData[]));
            using (StreamReader stream = new StreamReader(infoPath, System.Text.Encoding.Default))
            {
                cocktailList = jsonFormatter.ReadObject(stream.BaseStream) as CocktailsData[];
            }
            foreach (CocktailsData c in cocktailList)
            {
                if (isCocktailAlreadyInDB(npgSqlConnection, c)) continue;
                WriteCocktailWithTriggerFunctionalityToDB(npgSqlConnection, c);
                //WriteCocktailToDB(npgSqlConnection, c);
                WriteCocktailToolsToDB(npgSqlConnection, c);
                WriteCocktailIngredientsToDB(npgSqlConnection, c);
            }
            npgSqlConnection.Close();
        }
        static void WriteCocktailToDB(NpgsqlConnection Connection, CocktailsData cocktail)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Images\\{Regex.Replace(Regex.Replace(Regex.Replace(cocktail.Name, "\"", ""), @"\?", ""), "\'", "")}.jpg");
            Image image = Image.FromFile(imagePath);
            image = ResizeImage(image, CalculateNewSize(image).width, CalculateNewSize(image).height);
            byte[] pictureBytes = ImageToByteArray(image);
            var taste = cocktail.Tags.Where(t => t == "кислые" || t == "горькие" || t == "сладкие" || t == "пряные" || t == "соленые" || t == "травяные" || t == "кофейные" || t == "сливочные" || t == "сауэры" || t == "цитрусовые").FirstOrDefault();
            if (taste is null) taste = "нейтральные";
            string group = cocktail.Tags.Aggregate((t, x) => t + ", " + x).ToString();
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"INSERT INTO cocktails(name, description, picture, receipt, taste, \"group\") VALUES ('{Regex.Replace(cocktail.Name, "\'", "")}', '{cocktail.Description}', :dataParam, '{cocktail.Recipe}', '{GetTaste(taste)}', '{group}')", Connection);
            NpgsqlParameter param = new NpgsqlParameter("dataParam", NpgsqlDbType.Bytea);
            param.Value = pictureBytes;
            npgSqlCommand.Parameters.Add(param);
            int count = npgSqlCommand.ExecuteNonQuery();
            if (count == 1)
                Console.WriteLine("Запись вставлена");
            else
                Console.WriteLine("Не удалось вставить новую запись");
        }
        static void WriteCocktailWithTriggerFunctionalityToDB(NpgsqlConnection Connection, CocktailsData cocktail)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Images\\{Regex.Replace(Regex.Replace(Regex.Replace(cocktail.Name, "\"", ""), @"\?", ""), "\'", "")}.jpg");
            Image image = Image.FromFile(imagePath);
            image = ResizeImage(image, CalculateNewSize(image).width, CalculateNewSize(image).height);
            byte[] pictureBytes = ImageToByteArray(image);
            var taste = cocktail.Tags.Where(t => t == "кислые" || t == "горькие" || t == "сладкие" || t == "пряные" || t == "соленые" || t == "травяные" || t == "кофейные" || t == "сливочные" || t == "сауэры" || t == "цитрусовые").FirstOrDefault();
            if (taste is null) taste = "нейтральные";
            string group = cocktail.Tags.Aggregate((t, x) => t + ", " + x).ToString();
            int volume = cocktail.Ingredients.Select(i => i.Item2).Aggregate((x, y) => x + y);
            List<(int id, string name, int degree, int volume)> ing = new List<(int id, string name, int degree, int volume)>();
            foreach((string, int) i in cocktail.Ingredients)
            {
                string name = new string(i.Item1.Split(" ").ToArray().TakeWhile(s => s != "Finlandia" && s != "Aperol" && s != "BOLS" && s != "Sierra" && s != "Woodford" && s != "Campari" && s != "Laphroaig" && s != "Hennessy" && s != "Jack" && s != "The").ToArray().Aggregate((x,y) => x+$" { y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "BOLS").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "Sierra").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "Woodford").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "Aperol").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());    
                NpgsqlCommand command = new NpgsqlCommand($"SELECT id, name, degree FROM ingredients WHERE name = '{Regex.Replace(name.ToLower(), "\'", "")}'; ", Connection);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        ing.Add((Convert.ToInt32(reader["id"].ToString()), reader["name"].ToString(), Convert.ToInt32(reader["degree"].ToString()), i.Item2));
                    }
                }
            }
            int basis;
            int degree = 0;
            try
            {
                basis = ing.Where(i => i.volume == ing.Where(i => i.degree > 0).Max(i => i.volume)).First().id;
                degree = ing.Sum(x => (int)(((double)x.volume / (double)volume) * (double)x.degree));
            }
            catch
            {
                basis = ing.Where(i => i.volume == ing.Max(i => i.volume)).First().id;
            }
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"INSERT INTO cocktails(name, description, picture, receipt, taste, \"group\", volume, degree, basis_id) VALUES ('{Regex.Replace(cocktail.Name, "\'", "")}', '{cocktail.Description}', :dataParam, '{cocktail.Recipe}', '{GetTaste(taste)}', '{group}', {volume}, {degree}, {basis})", Connection);
            NpgsqlParameter param = new NpgsqlParameter("dataParam", NpgsqlDbType.Bytea);
            param.Value = pictureBytes;
            npgSqlCommand.Parameters.Add(param);
            int count = npgSqlCommand.ExecuteNonQuery();
            if (count == 1)
                Console.WriteLine("Запись вставлена");
            else
                Console.WriteLine("Не удалось вставить новую запись");
        }
        static void WriteCocktailToolsToDB(NpgsqlConnection Connection, CocktailsData cocktail)
        {
            int cocktail_id;
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"SELECT id, name FROM cocktails WHERE name = '{Regex.Replace(cocktail.Name, "\'", "")}'; ", Connection);
            using (NpgsqlDataReader reader = npgSqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    cocktail_id = Convert.ToInt32(reader["id"].ToString());
                }
                else
                    return;
            }
            foreach (string t in cocktail.Tools) 
            {
                int tool_id;
                NpgsqlCommand Command = new NpgsqlCommand($"SELECT id, name FROM tools WHERE name = '{t}';", Connection);
                using (NpgsqlDataReader reader = Command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        tool_id = Convert.ToInt32(reader["id"].ToString());
                    }
                    else
                        continue;
                }
                Command = new NpgsqlCommand($"INSERT INTO cocktails_tools (cocktail_id, tool_id) VALUES ({cocktail_id}, {tool_id})", Connection);
                Command.ExecuteNonQuery();
            }
        }
        static void WriteCocktailIngredientsToDB(NpgsqlConnection Connection, CocktailsData cocktail)
        {
            int cocktail_id;
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"SELECT id, name FROM cocktails WHERE name = '{Regex.Replace(cocktail.Name, "\'", "")}'; ", Connection);
            using (NpgsqlDataReader reader = npgSqlCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    cocktail_id = Convert.ToInt32(reader["id"].ToString());
                }  
                else
                    return;
            }
            foreach ((string,int) t in cocktail.Ingredients)
            {
                int ingredient_id; 
                string name = new string(t.Item1.Split(" ").ToArray().TakeWhile(s => s != "Finlandia" && s != "BOLS" && s != "Aperol" && s != "Sierra" && s != "Woodford" && s != "Campari" && s != "Laphroaig" && s != "Hennessy" && s != "Jack" && s != "The").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());

                //string name = new string(t.Item1.Split(" ").ToArray().TakeWhile(s => s != "Finlandia").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "BOLS").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "Sierra").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                //name = new string(name.Split(" ").ToArray().TakeWhile(s => s != "Woodford").ToArray().Aggregate((x, y) => x + $" {y}").ToCharArray());
                NpgsqlCommand Command = new NpgsqlCommand($"SELECT id, name FROM ingredients WHERE name = '{Regex.Replace(name.ToLower(), "\'", "")}';", Connection);
                using (NpgsqlDataReader reader = Command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        ingredient_id = Convert.ToInt32(reader["id"].ToString());
                    }
                    else
                        continue;
                }
                Command = new NpgsqlCommand($"INSERT INTO cocktails_ingredients (cocktail_id, ingredient_id, volume) VALUES ({cocktail_id}, {ingredient_id}, {t.Item2})", Connection);
                Command.ExecuteNonQuery();
            }
        }
        static bool isCocktailAlreadyInDB(NpgsqlConnection Connection, CocktailsData cocktail)
        {
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"SELECT name FROM cocktails WHERE name = '{Regex.Replace(cocktail.Name, "\'", "")}';", Connection);
            using (NpgsqlDataReader reader = npgSqlCommand.ExecuteReader())
            {
                return reader.HasRows;
            }
        }
        #endregion
        #region ToolMethods
        static void WriteTools()
        {
            string infoPath = "ToolsBackup.json";
            if (!File.Exists(infoPath)) return;

            NpgsqlConnection npgSqlConnection = new NpgsqlConnection(connectionString);
            npgSqlConnection.Open();
            Console.WriteLine("Соединение с БД открыто");
            ToolsData[] toolsList;
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(ToolsData[]));
            using (StreamReader stream = new StreamReader(infoPath, System.Text.Encoding.Default))
            {
                toolsList = jsonFormatter.ReadObject(stream.BaseStream) as ToolsData[];
            }
            foreach (ToolsData t in toolsList)
            {
                if (isToolAlreadyInDB(npgSqlConnection, t)) continue;
                WriteToolToDB(npgSqlConnection, t);
            }
            npgSqlConnection.Close();
        }
        static void WriteToolToDB(NpgsqlConnection Connection, ToolsData tool)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ToolsImages\\{Regex.Replace(Regex.Replace(tool.Name, "\"", ""), "\'", "")}.jpg");
            Image image = Image.FromFile(imagePath);
            image = ResizeImage(image, CalculateNewSize(image).width, CalculateNewSize(image).height);
            byte[] pictureBytes = ImageToByteArray(image);
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"INSERT INTO tools (name, description, picture) VALUES ('{Regex.Replace(tool.Name, "\'", "")}', '{tool.Description}', :dataParam)", Connection);
            NpgsqlParameter param = new NpgsqlParameter("dataParam", NpgsqlDbType.Bytea);
            param.Value = pictureBytes;
            npgSqlCommand.Parameters.Add(param);
            int count = npgSqlCommand.ExecuteNonQuery();
            if (count == 1)
                Console.WriteLine("Запись вставлена");
            else
            {
                Console.WriteLine("Не удалось вставить новую запись");
                throw new Exception();
            }
        }
        static bool isToolAlreadyInDB(NpgsqlConnection Connection, ToolsData tool)
        {
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"SELECT name FROM tools WHERE name = '{Regex.Replace(tool.Name, "\'", "")}';", Connection);
            using (NpgsqlDataReader reader = npgSqlCommand.ExecuteReader())
            {
                return reader.HasRows;
            }
        }
        #endregion
        #region IngredientsMethods
        static void WriteIngredients()
        {
            string infoPath = "IngredientsBackup.json";
            if (!File.Exists(infoPath)) return;
            NpgsqlConnection npgSqlConnection = new NpgsqlConnection(connectionString);
            npgSqlConnection.Open();
            Console.WriteLine("Соединение с БД открыто");
            IngredientsData[] ingredientList;
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(IngredientsData[]));
            using (StreamReader stream = new StreamReader(infoPath, System.Text.Encoding.Default))
            {
                ingredientList = jsonFormatter.ReadObject(stream.BaseStream) as IngredientsData[];
            }
            foreach (IngredientsData i in ingredientList)
            {
                if (isIngredientAlreadyInDB(npgSqlConnection, i)) continue;
                WriteIngredientToDB(npgSqlConnection, i);
            }
            npgSqlConnection.Close();
        }
        static void WriteIngredientToDB(NpgsqlConnection Connection, IngredientsData ingredient)
        {
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"IngredientsImages\\{Regex.Replace(Regex.Replace(ingredient.Name, "\"", ""), "\'", "")}.jpg");
                Image image = Image.FromFile(imagePath);
                image = ResizeImage(image, CalculateNewSize(image).width, CalculateNewSize(image).height);
                byte[] pictureBytes = ImageToByteArray(image);
                NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"INSERT INTO ingredients (name, description, picture, degree) VALUES ('{Regex.Replace(ingredient.Name.ToLower(), "\'", "")}', '{ingredient.Description}', :dataParam, {ingredient.Degree})", Connection);
                NpgsqlParameter param = new NpgsqlParameter("dataParam", NpgsqlDbType.Bytea);
                param.Value = pictureBytes;
                npgSqlCommand.Parameters.Add(param);
                int count = npgSqlCommand.ExecuteNonQuery();
                if (count == 1)
                    Console.WriteLine("Запись вставлена");
                else
                {
                    Console.WriteLine("Не удалось вставить новую запись");
                    throw new Exception();
                }
            }
            catch
            {
                Console.WriteLine("не сработало(");
            }
        }
        static bool isIngredientAlreadyInDB(NpgsqlConnection Connection, IngredientsData ingredient)
        {
            NpgsqlCommand npgSqlCommand = new NpgsqlCommand($"SELECT name FROM ingredients WHERE name = '{Regex.Replace(ingredient.Name.ToLower(), "\'", "")}';", Connection);
            using (NpgsqlDataReader reader = npgSqlCommand.ExecuteReader())
            {
                return reader.HasRows;
            }
        }
        #endregion
        static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return (Image)destImage;
        }
        static (int height, int width) CalculateNewSize(Image image)
        {
            int scope = image.Height / 100;
            return (100, image.Width / scope);
        }
        public static byte[] ImageToByteArray(Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, ImageFormat.Gif);
            return ms.ToArray();
        }
        static string GetTaste(string taste)
        {
            return taste switch
            {
                "пряные" => "spicy",
                "соленые" => "salty",
                "сладкие" => "sweet",
                "кислые" => "sour",
                "горькие" => "bitter",
                "травяные" => "herbal",
                "кофейные" => "coffee",
                "сливочные" => "creamy",
                "сауэры" => "sour",
                "цитрусовые" => "citrus",
                _ => "neutral"
            };
        }    
    }
}
