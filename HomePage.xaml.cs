using Microsoft.Maui.Controls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace appcalorie;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        PopulateTable();
    }

    private void PopulateTable()
    {
        try
        {
            var data = FetchDataFromDatabase();

            var tableSection = new TableSection("Food Items");

            // Add header row
            var headerRow = new ViewCell();
            var headerGrid = CreateGrid();
            AddHeaderLabelToGrid(headerGrid, "Food Item", 0);
            AddHeaderLabelToGrid(headerGrid, "Vegetable", 1);
            AddHeaderLabelToGrid(headerGrid, "Meat", 2);
            AddHeaderLabelToGrid(headerGrid, "Total", 3);
            headerRow.View = headerGrid;
            tableSection.Add(headerRow);

            // Add data rows
            foreach (var row in data)
            {
                var viewCell = new ViewCell();
                var grid = CreateGrid();

                for (int i = 0; i < row.Length; i++)
                {
                    AddDataLabelToGrid(grid, row[i], i);
                }

                viewCell.View = grid;
                tableSection.Add(viewCell);
            }

            // Clear the existing content and add the new section
            DataTable.Root.Clear();
            DataTable.Root.Add(tableSection);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PopulateTable Error: {ex.Message}");
        }
    }

    private Grid CreateGrid()
    {
        var grid = new Grid
        {
            Padding = new Thickness(5),
            RowSpacing = 1,
            ColumnSpacing = 1,
            BackgroundColor = Colors.White
        };

        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        return grid;
    }

    private void AddHeaderLabelToGrid(Grid grid, string text, int column)
    {
        var label = new Label
        {
            Text = text,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(10, 8, 10, 10),
            BackgroundColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        grid.Children.Add(label);
        Grid.SetColumn(label, column);
    }

    private void AddDataLabelToGrid(Grid grid, string text, int column)
    {
        var label = new Label
        {
            Text = text,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            FontSize = 12,
            Padding = new Thickness(10),
            BackgroundColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var frame = new Frame
        {
            BorderColor = Colors.White,
            CornerRadius = 0,
            Padding = new Thickness(2),
            Content = label
        };

        grid.Children.Add(frame);
        Grid.SetColumn(frame, column);
    }

    private List<string[]> FetchDataFromDatabase()
    {
        var data = new List<string[]>();
        string connectionString = "server=127.0.0.1;user=merlyn;database=app_calorie;port=3306;password=app_calorie;";
        string query = "SELECT food_item, vegetable, meat, total FROM food_items";

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            data.Add(new string[]
                            {
                                    reader.GetString(0), // food_item
                                    reader.GetString(1), // vegetable
                                    reader.GetString(2), // meat
                                    reader.GetString(3)  // total
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FetchDataFromDatabase Error: {ex.Message}");
        }

        return data;
    }

    private async void OnCameraButtonClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();

            if (myPhoto != null)
            {
                // Save the file into local storage
                var newFile = Path.Combine(FileSystem.AppDataDirectory, $"{Path.GetRandomFileName()}.jpg");
                using Stream sourceStream = await myPhoto.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(newFile);
                await sourceStream.CopyToAsync(localFileStream);

                // Send the photo to Ollama for analysis
                var analysisResult = await AnalyzePhotoWithOllama(newFile);

                // Update the table with the analysis result
                if (analysisResult != null)
                {
                    var newRow = new string[]
                    {
                    analysisResult.Value.foodItem,
                    analysisResult.Value.vegetableCalorie,
                    analysisResult.Value.meatCalorie,
                    analysisResult.Value.totalCalorie
                    };

                    // Add the new row to the table
                    AddRowToTable(newRow);
                }
            }
        }
        else
        {
            await Shell.Current.DisplayAlert("OOPS", "Your device is not supported", "OK");
        }
    }


    private async Task<(string foodItem, string vegetableCalorie, string meatCalorie, string totalCalorie)?> AnalyzePhotoWithOllama(string filePath)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await httpClient.PostAsync("https://api.ollama.com/analyze", form);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(jsonResponse);

                string foodItem = json["food_name"]?.ToString();
                string vegetableCalorie = json["vegetable_calorie"]?.ToString();
                string meatCalorie = json["meat_calorie"]?.ToString();
                string totalCalorie = json["total_calorie"]?.ToString();

                return (foodItem, vegetableCalorie, meatCalorie, totalCalorie);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AnalyzePhotoWithOllama Error: {ex.Message}");
        }

        return null;
    }

    private void AddRowToTable(string[] row)
    {
        var tableSection = DataTable.Root[0]; // Assuming there's only one TableSection

        var viewCell = new ViewCell();
        var grid = CreateGrid();

        for (int i = 0; i < row.Length; i++)
        {
            AddDataLabelToGrid(grid, row[i], i);
        }

        viewCell.View = grid;
        tableSection.Add(viewCell);
    }
}