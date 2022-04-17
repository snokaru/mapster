using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Mapster.ClientApplication;

public partial class MainWindow : Window
{
    private class ServiceResponse
    {
        public int tileCount { get; set; }
        public byte[][]? imageData { get; set; }
    }

    private int _clickCounter = 0;
    private static HttpClient _httpClient = new HttpClient();

    // Model used for the button text
    private DataModel Model { get; set; } = new DataModel();
    // Model used for the list of items
    private ObservableCollection<MapTile> Items { get; set; } = new ObservableCollection<MapTile>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Model.Data = "Add Item";
    }

    [Obsolete]
    void OnButtonPressed(object? sender, RoutedEventArgs eventArgs)
    {
        Console.WriteLine($"Button clicked {++_clickCounter} times");

        try
        {
            var response = _httpClient.GetAsync("http://localhost:8080/render?minLon=1.388397216796875&minLat=42.402164470921285&maxLon=1.8024444580078125&maxLat=42.67688269641377&size=2000").Result;
            if (response.IsSuccessStatusCode)
            {
                using var bsonReader = new BsonReader(response.Content.ReadAsStream());
                var pngs = (new JsonSerializer()).Deserialize<ServiceResponse>(bsonReader);
                if (pngs != null && pngs.imageData != null)
                {
                    foreach (var bytes in pngs.imageData)
                    {
                        Items.Add(new MapTile(bytes, 2000));
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}
