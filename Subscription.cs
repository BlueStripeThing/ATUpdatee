using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATUpdaterBot
{
    internal class Subscription : DbInteractable
    {
        internal Book book;
        internal long person;
        internal DateTime start_date;

        public Subscription()
        {

        }
        public Subscription(Book book, long person, DateTime start_date)
        {
            this.book = book;
            this.person = person;
            this.start_date = start_date;

        }
        public override bool ToDB(String connectionString)
        {
            book.ToDB(connectionString);
            if (!CheckDB(connectionString))
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String query = $"INSERT INTO Subscription " +
                        $"(Person_id, Book_id,Start_date) VALUES " +
                        $"(@Person_id, @Book_id, @Start_date);";
                    using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@Person_id", person);
                        cmd.Parameters.AddWithValue("@Book_id", book.id);
                        cmd.Parameters.AddWithValue("@Start_date", start_date);
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
                String query = $"SELECT Person_id FROM Subscription WHERE Person_id=@Person_id AND Book_id=@Book_id";
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@Person_id", person);
                    cmd.Parameters.AddWithValue("@Book_id", book.id.ToString());
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.GetString(0) != null)
                                {
                                    Console.WriteLine("Подписка уже оформлена");
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Пользователя не существует");
            return false;
        }


        static Queue<Book> ShowSubscriptions(long person_id, String connectionString)
        {
            Queue<Book> books = new Queue<Book>();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                String query = $"Select Book_id, Author, Chapters, Title from Books where Book_id in (Select Book_id from Subscription where Person_id = @Person_id)";
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@Person_id", person_id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetString(0) != null)
                            {
                                reader.GetString(0);
                            }
                        }
                    }
                }
            }
            return books;
        }
    }
}
