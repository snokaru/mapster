using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MapsterGUI
{
    public partial class MainWindow : Window
    {
        private int _clickCounter = 0;

        // Model used for the button text
        private DataModel Model { get; set; } = new DataModel();
        // Model used for the list of items
        private ObservableCollection<DataModel> Items { get; set; } = new ObservableCollection<DataModel>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Model.Data = "Add Item";
        }

        void OnButtonPressed(object? sender, RoutedEventArgs eventArgs)
        {
            Console.WriteLine($"Button clicked {++_clickCounter} times");

            Items.Add(new DataModel($"Item {_clickCounter}"));
        }
    }
}