using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NET_HW_28_09
{
    internal class AdditionalTask1
    {
        static void Main(string[] args)
        {
            string connectionString = GetConnectionString();

            int auctionID = 1;
            int bidderID = 3;
            string bidderName = "Jeorge";
            decimal newBidAmount = 100.00m;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string getCurrentBidQuery = "SELECT CurrentBid FROM Auctions WHERE AuctionID = @AuctionID";
                    using (SqlCommand getCurrentBidCommand = new SqlCommand(getCurrentBidQuery, connection, transaction))
                    {
                        getCurrentBidCommand.Parameters.AddWithValue("@AuctionID", auctionID);
                        decimal currentBid = (decimal)getCurrentBidCommand.ExecuteScalar();

                        if (newBidAmount <= currentBid)
                        {
                            throw new Exception("Новая ставка должна быть выше текущей.");
                        }
                    }

                    string checkBidderQuery = "SELECT COUNT(1) FROM Bidders WHERE BidderID = @BidderID";
                    using (SqlCommand checkBidderCommand = new SqlCommand(checkBidderQuery, connection, transaction))
                    {
                        checkBidderCommand.Parameters.AddWithValue("@BidderID", bidderID);
                        int bidderExists = (int)checkBidderCommand.ExecuteScalar();

                        if (bidderExists == 0)
                        {
                            string insertBidderQuery = "INSERT INTO Bidders (BidderName) VALUES (@BidderName); SELECT SCOPE_IDENTITY();";
                            using (SqlCommand insertBidderCommand = new SqlCommand(insertBidderQuery, connection, transaction))
                            {
                                insertBidderCommand.Parameters.AddWithValue("@BidderName", bidderName);
                                bidderID = Convert.ToInt32(insertBidderCommand.ExecuteScalar());
                            }
                        }
                    }

                    string updateAuctionQuery = "UPDATE Auctions SET CurrentBid = @NewBid WHERE AuctionID = @AuctionID";
                    using (SqlCommand updateAuctionCommand = new SqlCommand(updateAuctionQuery, connection, transaction))
                    {
                        updateAuctionCommand.Parameters.AddWithValue("@NewBid", newBidAmount);
                        updateAuctionCommand.Parameters.AddWithValue("@AuctionID", auctionID);
                        updateAuctionCommand.ExecuteNonQuery();
                    }

                    string insertBidQuery = @"
                    INSERT INTO Bids (AuctionID, BidderID, BidAmount, BidTime)
                    VALUES (@AuctionID, @BidderID, @BidAmount, @BidTime)";
                    using (SqlCommand insertBidCommand = new SqlCommand(insertBidQuery, connection, transaction))
                    {
                        insertBidCommand.Parameters.AddWithValue("@AuctionID", auctionID);
                        insertBidCommand.Parameters.AddWithValue("@BidderID", bidderID);
                        insertBidCommand.Parameters.AddWithValue("@BidAmount", newBidAmount);
                        insertBidCommand.Parameters.AddWithValue("@BidTime", DateTime.Now);
                        insertBidCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    Console.WriteLine("Ставка успешно сделана.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine("Ошибка при совершении ставки: " + ex.Message);
                }
            }

        }
        private static string GetConnectionString()
        {
            var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = configurationBuilder.Build();

            return configuration.GetConnectionString("AuctionConnection");
        }
    }
}
