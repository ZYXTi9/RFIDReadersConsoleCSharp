﻿using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data;

namespace RfidReader.Database
{
    class MySqlDatabase : IDisposable
    {
        public MySqlConnection Con;
        public MySqlDatabase()
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration _configuration = builder.Build();
            string? MyConnectionString = _configuration.GetConnectionString("MySqlDB");

            Con = new MySqlConnection(MyConnectionString);
            this.Con.Open();
        }
        public void OpenConnection()
        {
            if (this.Con.State != ConnectionState.Open)
            {
                this.Con.Open();
            }
        }
        public void Dispose()
        {
            this.Con.Dispose();
        }
    }
}
