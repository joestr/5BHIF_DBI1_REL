using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _01DocumentStore
{

    class DatabaseHelper
    {

        private static DatabaseHelper instance;

        private string username;
        private string password;
        private string hostname;
        private int port;
        private string serviceName;

        private OracleConnection connection;
        
        private string suffix = "b17";

        private DatabaseHelper(string username, string password, string hostname, int port, string serviceName)
        {
            this.username = username;
            this.password = password;
            this.hostname = hostname;
            this.port = port;
            this.serviceName = serviceName;

            connection = new OracleConnection(
                $"user id={username};password={password};data source=" +
                "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)" +
                $"(HOST={hostname})(PORT={port}))(CONNECT_DATA=" +
                $"(SERVICE_NAME={serviceName})))");
        }

        public static DatabaseHelper GetInstance(string username, string password, string hostname, int port, string serviceName)
        {
            if(instance != null) {
                throw new InvalidOperationException("DatabaseHelper has already been intialized!");
            }

            return instance = new DatabaseHelper(username, password, hostname, port, serviceName);
        }

        public static DatabaseHelper GetInstance()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("DatabaseHelper has not been intialized yet!");
            }

            return instance;
        }

        public bool CheckSchema(string tableSuffix)
        {
            throw new NotImplementedException("Not implemented! Take care of the schema yourself!");
        }

        public int SaveDocument(string fileName, byte[] text)
        {
            string query = $"insert into docs_{suffix} (filename, text) values(:fileNameParameter, :textParameter)";

            OracleParameter textParameter = new OracleParameter();
            textParameter.OracleDbType = OracleDbType.Clob;
            textParameter.ParameterName = "textParameter";
            textParameter.Value = Encoding.UTF8.GetString(text, 0, text.Length);

            OracleParameter fileNameParameter = new OracleParameter();
            fileNameParameter.OracleDbType = OracleDbType.Varchar2;
            fileNameParameter.ParameterName = "fileNameParameter";
            fileNameParameter.Value = fileName;

            OracleCommand command = new OracleCommand(query, this.connection);

            command.Parameters.Add(fileNameParameter);
            command.Parameters.Add(textParameter);

            this.connection.Open();
            int result = command.ExecuteNonQuery();

            command.Dispose();
            this.connection.Close();

            return result;
        }

        public IDictionary<string, string> GetSavedDocuments()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string query = $"select * from docs_{this.suffix}";

            OracleCommand cmd = new OracleCommand(query, this.connection);

            this.connection.Open();
            var odr = cmd.ExecuteReader();

            while (odr.Read())
            {
                result.Add(odr.GetString(0), odr.GetString(1));
            }

            cmd.Dispose();
            this.connection.Close();

            return result;
        }

        public int FilterSavedDocuments(string word)
        {
            OracleCommand command = new OracleCommand($"DOCS_{this.suffix}_MARKUP", this.connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add("suchstr", OracleDbType.Varchar2).Value = word;

            //THIS PARAMETER MAY BE USED TO RETURN RESULT OF PROCEDURE CALL
            //cmd.Parameters.Add("vSUCCESS", OracleDbType.Varchar2, 1);
            //cmd.Parameters["vSUCCESS"].Direction = ParameterDirection.Output;
            //USE THIS PARAMETER CASE CURSOR IS RETURNED FROM PROCEDURE
            //cmd.Parameters.Add("vCHASSIS_RESULT", OracleDbType.RefCursor, ParameterDirection.InputOutput);
            //CALL PROCEDURE
            this.connection.Open();
            OracleDataAdapter da = new OracleDataAdapter(command);
            int result = command.ExecuteNonQuery();

            command.Dispose();
            this.connection.Close();

            return result;
        }

        public IDictionary<string, string> GetFilteredDocuments()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string query = $"select * from docs_{this.suffix}_results";

            OracleCommand cmd = new OracleCommand(query, this.connection);

            this.connection.Open();
            var odr = cmd.ExecuteReader();

            while (odr.Read())
            {
                result.Add(odr.GetString(0), odr.GetString(1));
            }

            cmd.Dispose();
            this.connection.Close();

            return result;
        }
    }
}
