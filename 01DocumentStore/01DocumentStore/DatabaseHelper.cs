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
        private string prefix;
        private string suffix;

        private OracleConnection connection;

        private DatabaseHelper(string username, string password, string hostname, int port, string serviceName, string prefix, string suffix)
        {
            this.username = username;
            this.password = password;
            this.hostname = hostname;
            this.port = port;
            this.serviceName = serviceName;
            this.prefix = prefix;
            this.suffix = suffix;

            connection = new OracleConnection(
                $"user id={username};password={password};data source=" +
                "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)" +
                $"(HOST={hostname})(PORT={port}))(CONNECT_DATA=" +
                $"(SERVICE_NAME={serviceName})))");
        }

        public static DatabaseHelper GetInstance(string username, string password, string hostname, int port, string serviceName, string prefix, string suffix)
        {
            if(instance != null) {
                throw new InvalidOperationException("DatabaseHelper has already been intialized!");
            }

            return instance = new DatabaseHelper(username, password, hostname, port, serviceName, prefix, suffix);
        }

        public static DatabaseHelper GetInstance()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("DatabaseHelper has not been intialized yet!");
            }

            return instance;
        }

        public bool CheckSchema()
        {
            bool hasDocsTable = false;
            bool hasIndexOnDocsTable = false;
            bool hasResultTable = false;
            bool hasMarkupProcedure = false;

            string query = $"SELECT table_name FROM USER_TABLES WHERE table_name = '{this.prefix}DOCS{this.suffix}'";
            OracleCommand command = new OracleCommand(query, this.connection);
            this.connection.Open();
            OracleDataReader oDR = command.ExecuteReader();
            if (oDR.HasRows) hasDocsTable = true;
            command.Dispose();
            this.connection.Close();

            if(!hasDocsTable)
            {
                query = $"CREATE TABLE {this.prefix}DOCS{this.suffix} (filename VARCHAR2(255) PRIMARY KEY, text CLOB)";
                command = new OracleCommand(query, this.connection);
                this.connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
                this.connection.Close();
            }

            query = $"SELECT index_name FROM USER_INDEXES WHERE index_name = '{this.prefix}DOCS_INDEX{this.suffix}'";
            command = new OracleCommand(query, this.connection);
            this.connection.Open();
            oDR = command.ExecuteReader();
            if (oDR.HasRows) hasIndexOnDocsTable = true;
            command.Dispose();
            this.connection.Close();

            if (!hasIndexOnDocsTable)
            {
                query = $@"
                    CREATE INDEX {this.prefix}DOCS_INDEX{this.suffix}
                    ON {this.prefix}DOCS{this.suffix}(text)
                        INDEXTYPE IS CTXSYS.CONTEXT
	                PARAMETERS ('FILTER CTXSYS.NULL_FILTER SECTION GROUP CTXSYS.HTML_SECTION_GROUP sync (on commit)')
                ";
                command = new OracleCommand(query, this.connection);
                this.connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
                this.connection.Close();
            }

            query = $"SELECT table_name FROM USER_TABLES WHERE table_name = '{this.prefix}DOCS_RESULTS{this.suffix}'";
            command = new OracleCommand(query, this.connection);
            this.connection.Open();
            oDR = command.ExecuteReader();
            if (oDR.HasRows) hasResultTable = true;
            command.Dispose();
            this.connection.Close();
            
            if(!hasResultTable)
            {
                query = $"CREATE TABLE {this.prefix}DOCS_RESULTS{this.suffix} (filename VARCHAR2(255) PRIMARY KEY, text CLOB)";
                command = new OracleCommand(query, this.connection);
                this.connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
                this.connection.Close();
            }

            query = $"SELECT object_name  FROM USER_PROCEDURES WHERE object_name = '{this.prefix}DOCS_RESULTS{this.suffix}'";
            command = new OracleCommand(query, this.connection);
            this.connection.Open();
            oDR = command.ExecuteReader();
            if (oDR.HasRows) hasMarkupProcedure = true;
            command.Dispose();
            this.connection.Close();

            if(!hasMarkupProcedure)
            {
                query = $@"
                    CREATE procedure {this.prefix}DOCS_MARKUP{this.suffix} (suchstr varchar2)
                     as
                        qid number;
                    lobMarkup CLOB; --Character Large Object
                    begin
                      qid := 1;
                      for c_rec in (
                        select filename, text
                        from {this.prefix}DOCS{this.suffix}
                        where contains(text, suchstr, 1) > 0)
                    loop
                        dbms_output.put_line('-----VOR--------');
                        dbms_output.put_line('filename ' || c_rec.filename || ' qid ' || qid);

                        CTX_DOC.MARKUP(
                            '{this.prefix}DOCS_INDEX{this.suffix}',
                            to_char(c_rec.filename),
                            suchstr,
                            lobMarkup,
                            starttag => '<I><FONT COLOR=RED>',
                            endtag => '</FONT></I>'
                        );

                        insert into {this.prefix}DOCS_RESULTS{this.suffix} values(c_rec.FILENAME, lobMarkup);
                        dbms_output.put_line('-----NACH--------');

                        qid:= qid + 1;
                    end loop;
                end {this.prefix}DOCS_MARKUP{this.suffix};
                ";
                command = new OracleCommand(query, this.connection);
                this.connection.Open();
                command.ExecuteNonQuery();
                command.Dispose();
                this.connection.Close();
            }

            return true;
        }

        public int SaveDocument(string fileName, byte[] text)
        {
            string query = $"insert into {this.prefix}DOCS{this.suffix} (filename, text) VALUES(:fileNameParameter, :textParameter)";

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

            string query = $"SELECT * FROM {this.prefix}DOCS{this.suffix}";

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
            OracleCommand command = new OracleCommand($"{this.prefix}DOCS_MARKUP{this.suffix}", this.connection);
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

            string query = $"SELECT * FROM {this.prefix}DOCS_RESULTS{this.suffix}";

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
