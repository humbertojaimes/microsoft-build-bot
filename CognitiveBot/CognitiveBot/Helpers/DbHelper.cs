using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using CognitiveBot.Model;

namespace CognitiveBot.Helpers
{
    public class DbHelper
    {

        public static List<Product_Model>   GetProducts(string product, string dataSource, string user, string password)
        {
            List<Product_Model> products = new List<Product_Model>();
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = dataSource;
                builder.UserID = user;
                builder.Password = password;
                builder.InitialCatalog = "AdventureWorks";


                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    connection.Open();
                    StringBuilder sb = new StringBuilder();

                    if (string.IsNullOrWhiteSpace(product))
                    {
                        sb.Append("select distinct(p.ProductID), p.Name, isnull(Color, ''), ListPrice, ThumbNailPhoto as Photo, c.Name as Category, m.Name as Model from SalesLT.Product p join SalesLT.ProductCategory c on p.ProductCategoryID = c.ProductCategoryID join SalesLT.ProductModel m on p.ProductModelID = m.ProductModelID join SalesLT.SalesOrderDetail s on s.ProductID = p.ProductID ");
                        sb.Append(" where p.ProductID in (select top 5 p.ProductID from SalesLT.Product p join SalesLT.SalesOrderDetail s on s.ProductID = p.ProductID group by p.ProductID order by sum(s.OrderQty) desc)");

                    }
                    else
                    {
                        sb.Append("select ProductID, p.Name, isnull(Color, ''), ListPrice, ThumbNailPhoto as Photo, c.Name as Category, m.Name as Model ");
                        sb.Append(" FROM [AdventureWorks].[SalesLT].[Product] p ");
                        sb.Append(" JOIN [AdventureWorks].[SalesLT].[ProductCategory] c on p.ProductCategoryID = c.ProductCategoryID ");
                        sb.Append(" JOIN [AdventureWorks].[SalesLT].[ProductModel] m on p.ProductModelID = m.ProductModelID");
                        sb.Append($" WHERE p.Name LIKE '%{product}%'");
                    }

                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                products.Add(
                                    new Product_Model
                                    {
                                        ProductID = reader.GetInt32(0),
                                        Name = reader.GetString(1),
                                        Color = reader.GetString(2),
                                        ListPrice = reader.GetDecimal(3),
                                        PhotoBytes = (byte[])reader["Photo"],
                                        Category = reader.GetString(5),
                                        Model = reader.GetString(6)

                                    });
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            return products;
        }

        public static Product_Model GetProduct(int product, string dataSource, string user, string password)
        {
            Product_Model products = new Product_Model();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = dataSource;
                builder.UserID = user;
                builder.Password = password;
                builder.InitialCatalog = "AdventureWorks";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("select ProductID, p.Name, isnull(Color, ''), ListPrice, ThumbNailPhoto as Photo, c.Name as Category, m.Name as Model ");
                    sb.Append(" FROM [AdventureWorks].[SalesLT].[Product] p ");
                    sb.Append(" JOIN [AdventureWorks].[SalesLT].[ProductCategory] c on p.ProductCategoryID = c.ProductCategoryID ");
                    sb.Append(" JOIN [AdventureWorks].[SalesLT].[ProductModel] m on p.ProductModelID = m.ProductModelID");
                    sb.Append($" WHERE p.[ProductID] = {product}");
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products = 
                                    new Product_Model
                                    {
                                        ProductID = reader.GetInt32(0),
                                        Name = reader.GetString(1),
                                        Color = reader.GetString(2),
                                        ListPrice = reader.GetDecimal(3),
                                        PhotoBytes = (byte[])reader["Photo"],

                                        Category = reader.GetString(5),
                                        Model = reader.GetString(6)

                                    };
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            return products;
        }

        public static CustomerShort GetCustomer(string email, string dataSource, string user, string password)
        {
            var customer = new CustomerShort();

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                builder.DataSource = dataSource;
                builder.UserID = user;
                builder.Password = password;
                builder.InitialCatalog = "AdventureWorks";

                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {

                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("select CustomerID, CompanyName, EmailAddress, CONCAT(Title, FirstName, LastName, Suffix) AS CustomerName ");
                    sb.Append(" FROM [AdventureWorks].[SalesLT].[Customer] ");
                    sb.Append($" WHERE EmailAddress = '{email}'");

                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                customer = new CustomerShort()
                                {
                                    CustomerID = reader.GetInt32(0),
                                    CompanyName = reader.GetString(1),
                                    EmailAddress = reader.GetString(2),
                                    CustomerName = reader.GetString(3)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            return customer;
        }
    }
}
