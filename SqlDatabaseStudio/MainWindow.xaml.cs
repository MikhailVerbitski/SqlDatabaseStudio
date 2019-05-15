using Microsoft.Win32;
using SqlDatabaseStudio.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
        private List<PairTableField> addListBoxFields = new List<PairTableField>();
        public List<PairTableField> AddListBoxFields
        {
            get { return addListBoxFields; }
            set
            {
                addListBoxFields = value;
                OnPropertyChanged("AddListBoxFields");
            }
        }

        private List<TableField> addListBoxCombo = new List<TableField>();
        public List<TableField> AddListBoxCombo
        {
            get { return addListBoxCombo; }
            set
            {
                addListBoxCombo = value;
                OnPropertyChanged("AddListBoxCombo");
            }
        }

        private string selectedStoredProcedures;
        public string SelectedStoredProcedures
        {
            get { return selectedStoredProcedures; }
            set
            {
                selectedStoredProcedures = value;
                OnPropertyChanged("SelectedStoredProcedures");
            }
        }

        public ObservableCollection<string> StoredProcedures { get; set; }

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

        private string currentTheme = "Light";
        public string CurrentTheme
        {
            get { return currentTheme; }
            set
            {
                currentTheme = value;
                OnPropertyChanged("CurrentTheme");
            }
        }

        private string numberTables;
        public string NumberTables
        {
            get { return numberTables == null ? "" : $"({numberTables})"; }
            set
            {
                numberTables = value == "0" ? null : value;
                OnPropertyChanged("NumberTables");
            }
        }

        private Context context;
        private bool InsertOrUpdate = false;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            Closed += (obj, e) => { context?.Dispose(); };
        }

        public void ChangeTheme(object sender, EventArgs e)
        {
            List<string> styles = new List<string> { "Light", "Dark" };
            CurrentTheme = styles.First(a => a != (sender as MenuItem).Header as string);
            var uri = new Uri(CurrentTheme + ".xaml", UriKind.Relative);
            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
            Application.Current.Resources.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }

        public void TableSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                SelectedTableName = e.AddedItems.Cast<string>().First();
                RefreshSelectedTable();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void Add(object sender, RoutedEventArgs e)
        {
            try
            {
                var columns = (TableView.ItemsSource as DataView)
                        .Table
                        .Columns
                        .Cast<DataColumn>()
                        .Skip(1)
                        .Select(a => a.ColumnName);
                RowDataEntryField(columns);
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void Update(object sender, RoutedEventArgs e)
        {
            try
            {
                var columns = (TableView.ItemsSource as DataView).Table.Columns.Cast<DataColumn>().Select(a => a.ToString()).ToList();
                var columnsData = (TableView.SelectedItem as DataRowView).Row.ItemArray.Select(a => a.ToString()).ToList();
                RowDataEntryField(columns);
                AddListBoxCombo.ForEach(a => a.Selected = columnsData[columns.IndexOf(a.TableFieldName)]);
                AddListBoxFields.ForEach(a => a.Input = columnsData[columns.IndexOf(a.Text)]);
                InsertOrUpdate = true;
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
        public void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                var columns = AddListBoxFields.Select(a => a.Text).Concat(AddListBoxCombo.Select(a => a.TableFieldName));
                var values = AddListBoxFields.Select(a => a.Input).Concat(AddListBoxCombo.Select(a => a.GetId()));
                if (AddListBoxFields.Any(a => a.Input == null) || AddListBoxCombo.Any(a => a.Selected == null))
                {
                    Notification("Все поля должны быть заполнены");
                    return;
                }
                else if(InsertOrUpdate)
                {
                    context.Update(SelectedTableName, Convert.ToInt32(values.First()), columns.Skip(1), values.Skip(1));
                }
                else
                {
                    context.Insert(SelectedTableName, columns, values);
                }
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
                CommandSQL = CommandSQL.Replace("= '", "= N'");
                CommandSQL = CommandSQL.Replace("='", "=N'");
                var data = context.Execute(CommandSQL);
                RefreshSelectedTable(data);
                SelectedTableName = "SQL Request";
                UpdateStoredProcedures();
                Notification("Success");
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void OpenDatabase(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Database (*.mdf)|*.mdf";
                if (openFileDialog.ShowDialog() == true)
                {
                    var path = openFileDialog.FileName;
                    OpenDatabase(path);
                }
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void FieldChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ForeignKeyFields.Items.Refresh();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void ExecuteStoredProcedure(object sender, EventArgs e)
        {
            try
            {
                var fields = AddListBoxFields.Select(a => a.Text).Concat(AddListBoxCombo.Select(a => a.TableFieldName));
                var values = AddListBoxFields.Select(a => a.Input).Concat(AddListBoxCombo.Select(a => a.GetId()));
                if (AddListBoxFields.Any(a => a.Input == null) || AddListBoxCombo.Any(a => a.Selected == null))
                {
                    Notification("Все поля должны быть заполнены");
                    return;
                }
                else
                {
                    var data = context.ExecuteStoredProcedure(SelectedStoredProcedures, fields, values);
                    RefreshSelectedTable(data);
                    SelectedTableName = SelectedStoredProcedures;
                    UpdateStoredProcedures();
                }
                UpdateTables();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        public void StoredProceduresSelected(object sender, RoutedEventArgs e)
        {
            var parameters = context.GetParametersOfStoredProcedure(SelectedStoredProcedures);
            var bufAddListBoxCombo = new List<TableField>();
            var bufAddListBoxFields = new List<PairTableField>();
            foreach (var param in parameters)
            {
                var spl = param.Substring(1).Split('_');
                if (Tables.Contains(spl[0]))
                {
                    bufAddListBoxCombo.Add(new TableField()
                    {
                        Text = spl[0],
                        TableFieldName = param,
                        ForeignTable = context.Select(spl[1], spl[0])
                    });
                }
                else
                {
                    bufAddListBoxFields.Add(new PairTableField() { Text = spl[0] });
                }
            }
            AddListBoxCombo = bufAddListBoxCombo;
            AddListBoxFields = bufAddListBoxFields;
        }
        public void CommandSQLTextChange(object sender, EventArgs e)
        {
            var h = (Application.Current.MainWindow.Content as Panel).ActualHeight;
            var test = (11.22 - (h / (sender as TextBox).ActualHeight)) / 2.3;
            (sender as TextBox).FontSize = 16 - test;
        }
        private void RefreshSelectedTable()
        {
            InsertOrUpdate = false;
            AddListBoxFields = new List<PairTableField>();
            AddListBoxCombo = new List<TableField>();
            if(SelectedTableName != "" && SelectedTableName != null)
            {
                var dataTable = context.Select("*", SelectedTableName);
                RefreshSelectedTable(dataTable);
            }
        }
        private void RefreshSelectedTable(DataTable dataTable)
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
        private void RowDataEntryField(IEnumerable<string> columns)
        {
            AddListBoxCombo = context.GetForeignKeys(SelectedTableName)?.GroupBy(a => a.Text).Select(a => a.First()).ToList() ?? new List<TableField>();
            AddListBoxFields = columns.Except(AddListBoxCombo.Select(a => a.TableFieldName)).Select(a => new PairTableField() { Text = a }).ToList();
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
                UpdateTables();
                UpdateStoredProcedures();
            }
            catch (Exception ex)
            {
                Notification(ex.Message);
            }
        }
        private void UpdateTables()
        {
            Tables = new ObservableCollection<string>(context.Tables);
            NumberTables = Tables.Count.ToString();
            OnPropertyChanged("Tables");
        }
        private void UpdateStoredProcedures()
        {
            StoredProcedures = new ObservableCollection<string>(context.StoredProcedures);

            context.GetParametersOfStoredProcedure("GetPeople");

            OnPropertyChanged("StoredProcedures");
        }
        private void Notification(string message, int time = 6000)
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
