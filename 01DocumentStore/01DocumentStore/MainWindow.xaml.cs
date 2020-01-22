using Microsoft.Win32;
using Oracle.ManagedDataAccess;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private ObservableCollection<string> filteredDocumentList = new ObservableCollection<string>();
        Dictionary<string, string> filteredDocuments = new Dictionary<string, string>();

        private ObservableCollection<string> encryptedDocumentList = new ObservableCollection<string>();
        Dictionary<string, string> encryptedDocuments = new Dictionary<string, string>();

        private ObservableCollection<string> savedDocumentList = new ObservableCollection<string>();
        Dictionary<string, string> savedDocuments = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            DatabaseHelper.GetInstance("ctxsys_userb", "ctxsysuserb", "db.htl-villach.at", 1521, "ora11g", "ds_", "_17");
        }

        private void Button_SelectAndUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog;  
                openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "(X)HTM(L)-Datei, PDF-Datei|*.HTM;*.HTML;*.XHTM;*.XHTML;*.PDF";

                bool success = (bool) openFileDialog.ShowDialog();

                if (!success) return;

                byte[] clob = File.ReadAllBytes(openFileDialog.FileName);

                DatabaseHelper.GetInstance().SaveDocument(openFileDialog.FileName, clob);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Button_Search_Click(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.GetInstance().FilterSavedDocuments(TextField_Search.Text);
        }

        private void Button_Refresh_ListBox_SavedDocuments(object sender, RoutedEventArgs e)
        {
            this.ListBox_SavedDocuments.ItemsSource = this.savedDocumentList;
            this.savedDocuments.Clear();
            this.savedDocumentList.Clear();

            var savedDocuments = DatabaseHelper.GetInstance().GetSavedDocuments();

            foreach(KeyValuePair<string, string> keyValuePair in savedDocuments)
            {
                this.savedDocuments.Add(keyValuePair.Key, keyValuePair.Value);
                this.savedDocumentList.Add(keyValuePair.Key);
            }
        }

        private void Button_Refresh_ListBox_FilteredDocuments(object sender, RoutedEventArgs e)
        {
            this.ListBox_FilteredDocuments.ItemsSource = this.filteredDocumentList;
            this.filteredDocuments.Clear();
            this.filteredDocumentList.Clear();

            var filteredDocuments = DatabaseHelper.GetInstance().GetFilteredDocuments();

            foreach (KeyValuePair<string, string> keyValuePair in filteredDocuments)
            {
                this.filteredDocuments.Add(keyValuePair.Key, keyValuePair.Value);
                this.filteredDocumentList.Add(keyValuePair.Key);
            }
        }

        private void ListBox_SavedDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ListBox_FilteredDocuments.SelectedIndex = -1;
            this.ListBox_EncryptedDocuments.SelectedIndex = -1;

            int idx = this.ListBox_SavedDocuments.SelectedIndex;

            if (idx < 0) return;

            this.webbrowser.NavigateToString(this.savedDocuments[(string)this.ListBox_SavedDocuments.SelectedValue]);
        }

        private void ListBox_FilteredDocument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ListBox_SavedDocuments.SelectedIndex = -1;
            this.ListBox_EncryptedDocuments.SelectedIndex = -1;

            int idx = this.ListBox_FilteredDocuments.SelectedIndex;

            if (idx < 0) return;

            this.webbrowser.NavigateToString(this.filteredDocuments[(string)this.ListBox_FilteredDocuments.SelectedValue]);
        }

        private void Button_CheckSchemaClick(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.GetInstance().CheckSchema();
        }

        private void Button_Truncate_ListBox_FilteredDocuments_Click(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.GetInstance().TruncateResultsTable();
        }

        private void Button_EncryptDocument(object sender, RoutedEventArgs e)
        {
            int idx = this.ListBox_SavedDocuments.SelectedIndex;

            if (idx < 0) return;

            string data = EncryptString("key".PadLeft(32, 'x'), this.savedDocuments[(string)this.ListBox_SavedDocuments.SelectedValue]);

            DatabaseHelper.GetInstance().SaveEncryptedDoucment((string)this.ListBox_SavedDocuments.SelectedValue, Encoding.UTF8.GetBytes(data));
        }

        private void Button_DecryptDocument(object sender, RoutedEventArgs e)
        {
            int idx = this.ListBox_EncryptedDocuments.SelectedIndex;

            if (idx < 0) return;

            string data = DecryptString("key".PadLeft(32, 'x'), this.encryptedDocuments[(string)this.ListBox_EncryptedDocuments.SelectedValue]);

            this.webbrowser.NavigateToString(data);
        }

        private void Button_Refresh_ListBox_EncryptedDocuments(object sender, RoutedEventArgs e)
        {
            this.ListBox_EncryptedDocuments.ItemsSource = this.encryptedDocumentList;
            this.encryptedDocuments.Clear();
            this.encryptedDocumentList.Clear();

            var encryptedDocuments = DatabaseHelper.GetInstance().GetEncryptedDocuments();

            foreach (KeyValuePair<string, string> keyValuePair in encryptedDocuments)
            {
                this.encryptedDocuments.Add(keyValuePair.Key, keyValuePair.Value);
                this.encryptedDocumentList.Add(keyValuePair.Key);
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void ListBox_EncryptedDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ListBox_FilteredDocuments.SelectedIndex = -1;
            this.ListBox_SavedDocuments.SelectedIndex = -1;
        }

        private void Button_Truncate_EncryptedDocuments(object sender, RoutedEventArgs e)
        {
            DatabaseHelper.GetInstance().TruncateEncryptedTable();
        }

        public static string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
