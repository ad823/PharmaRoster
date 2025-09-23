using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLUI;
using MySql.Data;
using Basic;
using System.ComponentModel;
using System.Reflection;
namespace PharmaRosterLib
{
    static public class MethodClass
    {
        static string server = "127.0.0.1";
        static string port = "3306";
        static string username = "user";
        static string password = "66437068";
        static string dbname = "pharma_roster";
        /// <summary>
        /// 依據 Class<T> 產生 Table 定義，支援 [Description("型別,索引")]
        /// </summary>
        public static Table CheckCreatTable<T>()
        {
            Type type = typeof(T);
            // 取得類別名稱或 [Description] 作為表名
            string tableName = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
            Table table = new Table(type);
            table.Server = server;
            table.Port = port;
            table.Username = username;
            table.Password = password;
            table.DBName = dbname;
            SQLUI.SQLControl sQLControl = new SQLControl(table);
            if (!sQLControl.IsTableCreat()) sQLControl.CreatTable(table);
            else sQLControl.CheckAllColumnName(table, true);
            return table;
        }
        public static SQLControl GetSQLControl<T>()
        {
            Type type = typeof(T);
            string tableName = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
            Table table = new Table(type);
            table.Server = server;
            table.Port = port;
            table.Username = username;
            table.Password = password;
            table.DBName = dbname;

            return new SQLControl(table);
        }
    }
}
