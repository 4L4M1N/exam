using MySql.Data.MySqlClient;

namespace DemoApi
{
    public class DataContext
    {
        public string ConnctionString { get; set; }
        public DataContext(string connectionString)
        {
            this.ConnctionString = connectionString;
        }
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(this.ConnctionString);
        }
    }
}