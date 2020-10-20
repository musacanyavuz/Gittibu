using System;
using GittiBu.Models;
using Dapper.FastCrud;
using Npgsql;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace GittiBu.Services
{
    public class BaseService : IDisposable
    {
        private NpgsqlConnection _connection;
        // private const string ConnectionString = "Server=109.232.220.87;Database=GittiBu;User Id=postgres;Password=wQb3jbFJ9meRdJ46"; // prod
        private string ConnectionString = ""; //"Server=localhost;Database=GittiBu;User Id=postgres;Password=postgres"; // local
       //   private const string ConnectionString = "Server=109.232.220.87;Database=test2_gittibu;User Id=postgres;Password=wQb3jbFJ9meRdJ46"; // test
        public BaseService()
        {
            
            ConnectionString = AppConfiguration.GetConnectionString();
            _connection = new NpgsqlConnection(ConnectionString);
        }

        public NpgsqlConnection GetConnection()
        {
            OrmConfiguration.DefaultDialect = SqlDialect.PostgreSql;
            if (_connection != null)
                return _connection;
            _connection = new NpgsqlConnection(ConnectionString);
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