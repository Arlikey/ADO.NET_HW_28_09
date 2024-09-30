using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = GetConnectionString();
        string srcAccountNumber = "3";
        string destAccountNumber = "2";
        decimal transferAmount = 100.00m;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                string checkBalanceQuery = "SELECT Balance FROM Accounts WHERE AccountNumber = @AccountNumber";
                using (SqlCommand command = new SqlCommand(checkBalanceQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@AccountNumber", srcAccountNumber);
                    decimal currentBalance = (decimal)command.ExecuteScalar();

                    if (currentBalance < transferAmount)
                    {
                        throw new Exception("Недостаточно средств.");
                    }
                }
                string withdrawQuery = "UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountNumber = @AccountNumber";
                using (SqlCommand command = new SqlCommand(withdrawQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Amount", transferAmount);
                    command.Parameters.AddWithValue("@AccountNumber", srcAccountNumber);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Во время операция списания средств произошла ошибка.");
                    }
                }

                string depositQuery = "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountNumber = @AccountNumber";
                using (SqlCommand command = new SqlCommand(depositQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Amount", transferAmount);
                    command.Parameters.AddWithValue("@AccountNumber", destAccountNumber);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("Целевой счет не найден.");
                    }
                }

                transaction.Commit();
                Console.WriteLine("Перевод успешно выполнен.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("Ошибка при выполнении транзакции: " + ex.Message);
            }
        }
    }

    private static string GetConnectionString()
    {
        var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
        var configuration = configurationBuilder.Build();

        return configuration.GetConnectionString("DefaultConnection");
    }
}