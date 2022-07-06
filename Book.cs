using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATUpdaterBot
{
    internal class Book : DbInteractable
    {
        internal string id;
        internal string author;
        internal string title;
        internal int chapters;
        internal string mod_date;

        public Book()
        {

        }
        public Book(string id, string author, string title, int chapters, string mod_date)
        {
            this.id = id;
            this.author = author; 
            this.title = title;
            this.chapters = chapters;
            this.mod_date = mod_date;
        }
        public override bool ToDB(String connectionString)
        {
            if (!CheckDB(connectionString))
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String query = $"INSERT INTO Books " +
                        $"(Book_id, Author, Chapters, Mod_Date, Title) VALUES " +
                        $"(@Book_id, @Author, @Chapters, @Mod_Date, @Title);";
                    using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@Book_id", id);
                        cmd.Parameters.AddWithValue("@Author", author);
                        cmd.Parameters.AddWithValue("@Chapters", chapters);

                        mod_date = mod_date.Replace('T', ' ');
                        mod_date = mod_date.Remove(mod_date.Length - 9);
                        cmd.Parameters.AddWithValue("@Mod_Date", DateTime.Parse(mod_date));
                        cmd.Parameters.AddWithValue("@Title",title);
                        cmd.ExecuteNonQuery();
                    }
                }
                Console.WriteLine("Insert проведен");
            }
            return true;    
        }

        public override bool CheckDB(String connectionString)
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                String query = $"SELECT Book_id FROM Books WHERE Book_id=@Book_id";
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@Book_id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.GetString(0) == id)
                                {
                                    Console.WriteLine("Книга есть");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Книги нет");
            return false;
        }

        //public bool UpdateDB(String connectionString)
        //{
        //    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
        //    {
        //        sqlConnection.Open();
        //        String query = $"SELECT Book_id FROM Books WHERE Book_id=@Book_id";
        //        using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
        //        {
        //            cmd.Parameters.AddWithValue("@Book_id", id);
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    if (reader.HasRows)
        //                    {
        //                        if (reader.GetString(0) == id)
        //                        {
        //                            Console.WriteLine("Книга есть");
        //                            return true;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    Console.WriteLine("Книги нет");
        //    return false;
        //}

        private static Book GetBookInfo(string link, long chatId) // две ветки - либо полную инфу, либо только главы и дату изменения
        {
            Book book = new Book();
            try
            {
                string lastChapterName = "";
                using (HttpClientHandler web = new HttpClientHandler())
                {
                    using (var client = new HttpClient(web))
                    {
                        using (HttpResponseMessage site = client.GetAsync(link).Result)
                        {
                            if (site.IsSuccessStatusCode)
                            {
                                var html = site.Content.ReadAsStringAsync().Result;
                                if (!string.IsNullOrEmpty(html))
                                {
                                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                                    doc.LoadHtml(html);

                                    //Парсинг глав
                                    var chapters = doc.DocumentNode.SelectNodes("/html/body/div/div/section/div/div/div/div/div/div/div/div/div/div/ul/li");
                                    if (chapters != null && chapters.Count > 0)
                                    {
                                        book.chapters = chapters.Count;
                                        foreach (var chapter in chapters)
                                        {
                                            var ashki = chapter.ChildNodes.FindFirst("a");
                                            if (ashki != null)
                                            {
                                                lastChapterName = ashki.InnerText;
                                            }
                                            else
                                            {
                                                lastChapterName = chapter.SelectNodes("span").First().InnerText;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Глав нет");
                                    }

                                    //Парсинг атрибутов книги
                                    book.title = doc.DocumentNode.SelectSingleNode("html/body/div/div/section/div/div/div[1]/div/div/div/div/h1").InnerText.Trim();
                                    book.author = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[1]/span/a").InnerText.Trim();

                                    if (doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]").HasAttributes)
                                        book.mod_date = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]").GetAttributeValue("data-time", "Неизвестно").Trim();
                                    else book.mod_date = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]/span").GetAttributeValue("data-time", "Неизвестно").Trim();
                                    // bot.SendTextMessageAsync(chatId, $"Книга: {book.title}.\nПоследняя глава: {lastChapterName}");
                                }
                            }
                        }
                    }
                }
                book.id = link.Substring(link.IndexOf("work/") + 5);
            }
            catch (Exception exc) { Console.WriteLine(exc.Message); }
            return book;
        }
    }
}
