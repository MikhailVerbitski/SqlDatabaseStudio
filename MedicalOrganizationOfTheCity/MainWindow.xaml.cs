using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MedicalOrganizationOfTheCity
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<string> Tables { get; set; }

        private string selectedTableName = "None";
        public string SelectedTableName
        {
            get { return selectedTableName; }
            set
            {
                selectedTableName = value;
                OnPropertyChanged("SelectedTableName");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            Tables = new ObservableCollection<string>();

            using (var context = new Context())
            {
                foreach (var item in context.Tables.Where(a => !a.Contains('_')))
                {
                    Tables.Add(item);
                }
            }
        }
        public void TableSelected(object sender, SelectionChangedEventArgs e)
        {
            SelectedTableName = e.AddedItems.Cast<string>().First();
            RefreshSelectedTable();
        }
        public void RefreshSelectedTable()
        {
            using (var context = new Context())
            {
                var dataTable = context.Select("*", SelectedTableName);
                var gridView = new GridView();
                foreach (DataColumn item in dataTable.Columns)
                {
                    gridView.Columns.Add(new GridViewColumn() { Header = item.ColumnName, DisplayMemberBinding = new Binding(item.ColumnName) });
                }
                TableView.View = gridView;
                TableView.ItemsSource = dataTable.DefaultView;
            }
        }

        public class Pair
        {
            public string Text { get; set; }
            public string Input { get; set; }
        }
        private List<Pair> addListBox;
        public List<Pair> AddListBox
        {
            get { return addListBox; }
            set
            {
                addListBox = value;
                OnPropertyChanged("AddListBox");
            }
        }

        public void Add(object sender, RoutedEventArgs e)
        {
            AddListBox = (TableView.ItemsSource as DataView)
                .Table
                .Columns
                .Cast<DataColumn>()
                .Skip(1)
                .Select(a => new Pair() { Text = a.ColumnName })
                .ToList();
        }
        public void Remove(object sender, RoutedEventArgs e)
        {

        }
        public void Insert(object sender, RoutedEventArgs e)
        {
            using (var context = new Context())
            {
                var where = $"{SelectedTableName}({AddListBox.Select(a => a.Text).Aggregate((a, b) => $"{a}, {b}")})";
                context.Insert(where, AddListBox.Select(a => $"'{a.Input}'").Aggregate((a, b) => $"{a}, {b}"));
            }
            AddListBox = new List<Pair>();
            RefreshSelectedTable();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
