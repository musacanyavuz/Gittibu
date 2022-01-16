using Dapper.FastCrud;
using GittiBu.Models;
using Microsoft.AspNetCore.Hosting;
using MySql.Data.MySqlClient;
using System;

namespace GittiBu.Services
{
    public class BaseService : IDisposable
    {
        private MySqlConnection _connection;
       public bool isDev { get; set; } 
        // private const string ConnectionString = "Server=109.232.220.87;Database=GittiBu;User Id=postgres;Password=wQb3jbFJ9meRdJ46"; // prod
        private string ConnectionString = ""; //"Server=localhost;Database=GittiBu;User Id=postgres;Password=postgres"; // local
       //   private const string ConnectionString = "Server=109.232.220.87;Database=test2_gittibu;User Id=postgres;Password=wQb3jbFJ9meRdJ46"; // test
        public BaseService(IHostingEnvironment _env)
        {
            isDev = _env.IsDevelopment();
            ConnectionString = AppConfiguration.GetConnectionString();
            _connection = new MySqlConnection(ConnectionString);
        }
        public BaseService()
        {

            ConnectionString = AppConfiguration.GetConnectionString();
            _connection = new MySqlConnection(ConnectionString);
        }

        public MySqlConnection GetConnection()
        {
            OrmConfiguration.DefaultDialect = SqlDialect.MySql;
            if (_connection != null)
                return _connection;
            _connection = new MySqlConnection(ConnectionString);
            return _connection;
        }

        public void Log(Log log)
        {
            try
            {
                GetConnection().Insert(log);
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        public void BeginTransaction()
        {
            try
            {
                GetConnection().BeginTransaction();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}