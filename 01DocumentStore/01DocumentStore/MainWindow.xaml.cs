﻿using Microsoft.Win32;
using Oracle.ManagedDataAccess;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<string> resultlist = new ObservableCollection<string>();
        Dictionary<string, string> map = new Dictionary<string, string>();

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
                openFileDialog1.Filter = "(X)HTM(L)-Dateien|*.HTM;*.HTML;*.XHTM;*.XHTML";

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
                string qry = "insert into docs_xx (filename, text) values('" + openFileDialog1.FileName + "'," + " :ClobParameter )";
                OracleParameter blobParameter = new OracleParameter();
                blobParameter.OracleDbType = OracleDbType.Clob;
                blobParameter.ParameterName = "ClobParameter";
                blobParameter.Value = Encoding.UTF8.GetString(blob, 0, blob.Length); ;

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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Initialize Oracle Server Connection
            OracleConnection con = new OracleConnection(@"user id=ctxsys;password=ctxsys;data source=" +
                                             "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)" +
                                             "(HOST=db.htl-villach.at)(PORT=1521))(CONNECT_DATA=" +
                                             "(SERVICE_NAME=ora11g)))");


            var cmd = new OracleCommand("DOCS_XX_MARKUP", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            //ASSIGN PARAMETERS TO BE PASSED
            cmd.Parameters.Add("suchstr", OracleDbType.Varchar2).Value = searchparam.Text;
            //THIS PARAMETER MAY BE USED TO RETURN RESULT OF PROCEDURE CALL
            //cmd.Parameters.Add("vSUCCESS", OracleDbType.Varchar2, 1);
            //cmd.Parameters["vSUCCESS"].Direction = ParameterDirection.Output;
            //USE THIS PARAMETER CASE CURSOR IS RETURNED FROM PROCEDURE
            //cmd.Parameters.Add("vCHASSIS_RESULT", OracleDbType.RefCursor, ParameterDirection.InputOutput);
            //CALL PROCEDURE
            con.Open();
            OracleDataAdapter da = new OracleDataAdapter(cmd);
            cmd.ExecuteNonQuery();

            cmd.Dispose();
            con.Close();
            //this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.resultstable.ItemsSource = this.resultlist;

            //Initialize Oracle Server Connection
            OracleConnection con = new OracleConnection(@"user id=ctxsys;password=ctxsys;data source=" +
                                             "(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)" +
                                             "(HOST=db.htl-villach.at)(PORT=1521))(CONNECT_DATA=" +
                                             "(SERVICE_NAME=ora11g)))");

            //Set insert query
            string qry = "select * from docs_xx_results";

            //Initialize OracleCommand object for insert.
            OracleCommand cmd = new OracleCommand(qry, con);


            //Open connection and execute insert query.
            con.Open();
            var odr = cmd.ExecuteReader();



            while (odr.Read())
            {

                map.Add(odr.GetString(0), odr.GetString(1));
            }

            this.resultlist.Clear();


            map.Keys.ToList().ForEach((str) =>
            {
                this.resultlist.Add(str);
            });



            MessageBox.Show("Image added to blob field");
            //GetImagesFromDatabase();
            cmd.Dispose();
            con.Close();
            //this.Close();
        }

        private void Resultstable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = this.resultstable.SelectedIndex;

            if (idx < 0) return;

            this.webbrowser.NavigateToString(this.map[(string) this.resultstable.SelectedValue]);
        }
    }
}
