using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATUpdaterBot
{
    internal class Person : DbInteractable
    {
        internal long id;
        internal string username;
        internal DateTime start_date;

        public Person()
        {

        }
        public Person(int id, string username, DateTime start_date)
        {
            this.id = id;
            this.username = username;
            this.start_date = start_date;

        }
        public override bool ToDB(String connectionString)
        {
            if (!CheckDB(connectionString))
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String query = $"INSERT INTO Person " +
                        "(Person_id, Username, Start_date) VALUES " +
                        $"(@Person_id, @Username, @Start_date);";
                    using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@Person_id", id);
                        cmd.Parameters.AddWithValue("@Username", username);
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
                String query = $"SELECT Person_id FROM Person WHERE Person_id=@Person_id";
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@Person_id", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                if (int.Parse(reader.GetString(0)) == id)
                                {
                                    Console.WriteLine("Пользователь существует");
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
    }
}
