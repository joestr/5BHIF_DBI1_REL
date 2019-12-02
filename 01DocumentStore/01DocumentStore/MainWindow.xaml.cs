using Microsoft.Win32;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _01DocumentStore
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {



                OpenFileDialog openFileDialog1;  
                openFileDialog1 = new OpenFileDialog();

                bool succ = (bool) openFileDialog1.ShowDialog();

                if (!succ) return;

                


                //Read Image Bytes into a byte array
                byte[] blob = File.ReadAllBytes(openFileDialog1.FileName);

                //Initialize Oracle Server Connection
                OracleConnection con = new OracleConnection(@"user id=ctxsys;password=ctxsys;data source=" +
                                                 "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)" +
                                                 "(HOST=db.htl-villach.at)(PORT=1521))(CONNECT_DATA=" +
                                                 "(SERVICE_NAME=ora11g)))");

                //Set insert query
                string qry = "insert into docs_xx (filename, text) values('" + openFileDialog1.FileName + "'," + " :BlobParameter )";
                OracleParameter blobParameter = new OracleParameter();
                blobParameter.OracleDbType = OracleDbType.Blob;
                blobParameter.ParameterName = "BlobParameter";
                blobParameter.Value = blob;

                //Initialize OracleCommand object for insert.
                OracleCommand cmd = new OracleCommand(qry, con);

                //We are passing Name and Blob byte data as Oracle parameters.
                cmd.Parameters.Add(blobParameter);

                //Open connection and execute insert query.
                con.Open();
                cmd.ExecuteNonQuery();

                MessageBox.Show("Image added to blob field");
                //GetImagesFromDatabase();
                cmd.Dispose();
                con.Close();
                //this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
