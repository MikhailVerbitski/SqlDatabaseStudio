using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SqlDatabaseStudio
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public class PairField
        {
            public string Text { get; set; }
            public string Input { get; set; }
        }

        private List<PairField> addListBoxFields;
        public List<PairField> AddListBoxFields
        {
            get { return addListBoxFields; }
            set
            {
                addListBoxFields = value;
                OnPropertyChanged("AddListBoxFields");
            }
        }

        private List<PairCombo> addListBoxCombo;
        public List<PairCombo> AddListBoxCombo
        {
            get { return addListBoxCombo; }
            set
            {
                addListBoxCombo = value;
                OnPropertyChanged("AddListBoxCombo");
            }
        }

        public ObservableCollection<string> Tables { get; set; }

        private string selectedTableName;
        public string SelectedTableName
        {
            get { return selectedTableName; }
            set
            {
                selectedTableName = value;
                OnPropertyChanged("SelectedTableName");
            }
        }

        private string message;
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        private string commandSQL;
        public string CommandSQL
        {
            get { return commandSQL; }
            set
            {
                commandSQL = value;
                OnPropertyChanged("CommandSQL");
            }
        }

        private Context context;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Tables = new ObservableCollection<string>();
        }

        public void TableSelected(object sender, SelectionChangedEventArgs e)
        {
            SelectedTableName = e.AddedItems.Cast<string>().First();
            RefreshSelectedTable();
            AddListBoxFields = new List<PairField>();
            AddListBoxCombo = new List<PairCombo>();
        }
        public void RefreshSelectedTable()
        {
            try
            {
                var dataTable = context.Select("*", SelectedTableName);
                RefreshSelectedTable(dataTable);
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void RefreshSelectedTable(DataTable dataTable)
        {
            var gridView = new GridView();
            if (dataTable != null)
            {
                foreach (DataColumn item in dataTable.Columns)
                {
                    gridView.Columns.Add(new GridViewColumn() { Header = item.ColumnName, DisplayMemberBinding = new Binding(item.ColumnName) });
                }
            }
            else
            {
                Notification("Table is empty", 1000);
            }
            TableView.View = gridView;
            TableView.ItemsSource = dataTable?.DefaultView ?? new DataView();
        }

        public void Add(object sender, RoutedEventArgs e)
        {
            try
            {
                AddListBoxCombo = context.GetForeignKeys(SelectedTableName);
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void Remove(object sender, RoutedEventArgs e)
        {
            try
            {
                context.Delete(SelectedTableName, Convert.ToInt32((TableView.SelectedItem as DataRowView).Row.ItemArray.First()));
                RefreshSelectedTable();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void Insert(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AddListBoxFields.Any(a => a.Input == null) || AddListBoxCombo.Any(a => a.Selected == null))
                {
                    throw new Exception("Все поля должны быть заполнены");
                }
                var where = $"{SelectedTableName}({AddListBoxFields.Select(a => a.Text).Concat(AddListBoxCombo.Select(a => a.Text)).Aggregate((a, b) => $"{a}, {b}")})";
                var valueString = AddListBoxFields.Select(a => a.Input).Concat(AddListBoxCombo.Select(a => a.Selected)).Select(a => $"'{a}'").Aggregate((a, b) => $"{a}, {b}");
                context.Insert(where, valueString);
                AddListBoxFields = new List<PairField>();
                AddListBoxFields = new List<PairField>();
                RefreshSelectedTable();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void SQLRequest(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = context.Execute(CommandSQL);
                RefreshSelectedTable(data);
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }

        public void OpenDatabase(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Database (*.mdf)|*.mdf";
            if (openFileDialog.ShowDialog() == true)
            {
                var path = openFileDialog.FileName;
                OpenDatabase(path);
            }
        }
        private void OpenDatabase(string path = null)
        {
            if(context == null)
            {
                context = new Context(path);
            }
            else
            {
                context.ChangeDatabase(path);
            }
            try
            {
                foreach (var item in context.Tables)
                {
                    Tables.Add(item);
                }
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }

        public void Notification(string message, int time = 6000)
        {
            this.Message = message;
            var syn = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                await Task.Delay(time);
                syn.Post(new SendOrPostCallback(a => Message = ""), null);
            });
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
