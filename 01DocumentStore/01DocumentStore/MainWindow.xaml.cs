using Microsoft.Win32;
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
        private ObservableCollection<string> filteredDocumentList = new ObservableCollection<string>();
        Dictionary<string, string> filteredDocuments = new Dictionary<string, string>();

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
                openFileDialog.Filter = "(X)HTM(L)-Dateien|*.HTM;*.HTML;*.XHTM;*.XHTML";

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

            int idx = this.ListBox_SavedDocuments.SelectedIndex;

            if (idx < 0) return;

            this.webbrowser.NavigateToString(this.savedDocuments[(string)this.ListBox_SavedDocuments.SelectedValue]);
        }

        private void ListBox_FilteredDocument_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ListBox_SavedDocuments.SelectedIndex = -1;

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
    }
}
