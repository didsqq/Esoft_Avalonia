using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ObjectivePlatformApp.Data;
using ObjectivePlatformApp.Models;
using System.Linq;
using System;
using Avalonia.Media;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Avalonia.Layout;

namespace ObjectivePlatformApp
{
    public partial class RealEstatesWindow : UserControl
    {
        private List<RealEstates> _allRealEstates = new List<RealEstates>();
        private List<Point> _searchPolygon = new List<Point>();

        public RealEstatesWindow()
        {
            InitializeComponent();
            var createButton = this.FindControl<Button>("CreateRealEstateButton");
            createButton.Click += CreateRealEstate_Click;

            var backButton = this.FindControl<Button>("BackButton");
            backButton.Click += BackButton_Click;

            var editButton = this.FindControl<Button>("EditRealEstateButton");
            editButton.Click += EditSelectedRealEstate_Click;
            editButton.IsEnabled = false;

            var deleteButton = this.FindControl<Button>("DeleteRealEstateButton");
            deleteButton.Click += DeleteSelectedRealEstate_Click;
            deleteButton.IsEnabled = false;

            var typeFilterComboBox = this.FindControl<ComboBox>("TypeFilterComboBox");
            typeFilterComboBox.SelectionChanged += TypeFilterComboBox_SelectionChanged;

            LoadRealEstates();
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            (TopLevel.GetTopLevel(this) as Window)?.Close();
        }

        private void LoadRealEstates()
        {
            using (var db = new AppDbContext())
            {
                _allRealEstates = db.RealEstates
                    .Include(re => re.RealEstateType)
                    .Include(re => re.District)
                    .ToList();
                FilterRealEstates();
            }
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
                return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t))
                return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private bool IsFuzzyMatch(string source, string target, int maxDistance)
        {
            if (string.IsNullOrEmpty(source)) return false;
            if (string.IsNullOrEmpty(target)) return false;

            source = source.ToLower();
            target = target.ToLower();

            if (source.Contains(target) || target.Contains(source))
                return true;

            return LevenshteinDistance(source, target) <= maxDistance;
        }

        private void TypeFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // При изменении фильтра обновляем список
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            FilterRealEstates(searchTextBox?.Text ?? "");
        }

        private void FilterRealEstates(string searchText = "")
        {
            var realEstatesPanel = this.FindControl<StackPanel>("RealEstatesPanel");
            realEstatesPanel.Children.Clear();

            var typeFilterComboBox = this.FindControl<ComboBox>("TypeFilterComboBox");
            int selectedType = typeFilterComboBox.SelectedIndex;
            // 0 - Все, 1 - Дом, 2 - Земля, 3 - Квартира

            var filteredRealEstates = string.IsNullOrWhiteSpace(searchText)
                ? _allRealEstates
                : _allRealEstates.Where(re =>
                    IsFuzzyMatch(re.City, searchText, 3) ||
                    IsFuzzyMatch(re.Street, searchText, 3) ||
                    IsFuzzyMatch(re.House?.ToString(), searchText, 1) ||
                    IsFuzzyMatch(re.Flat?.ToString(), searchText, 1)).ToList();

            if (selectedType > 0)
            {
                string typeName = selectedType == 1 ? "Дом" : selectedType == 2 ? "Земля" : "Квартира";
                filteredRealEstates = filteredRealEstates.Where(re => re.RealEstateType != null && re.RealEstateType.Name == typeName).ToList();
            }

            foreach (var realEstate in filteredRealEstates)
            {
                var border = new Border
                {
                    Margin = new Thickness(0, 0, 0, 5),
                    Padding = new Thickness(10),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(5)
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
                };

                var realEstateText = new TextBlock
                {
                    Text = $"{realEstate.Id} - {realEstate.City}, {realEstate.Street} {realEstate.House}/{realEstate.Flat} | " +
                           $"Тип: {realEstate.RealEstateType?.Name} | Район: {realEstate.District?.Name} | " +
                           $"Площадь: {realEstate.Area} м² | Комнат: {realEstate.Rooms} | Этаж: {realEstate.Floor}",
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(realEstateText, 0);

                var selectButton = new Button
                {
                    Content = "Выбрать",
                    Margin = new Thickness(5, 0, 5, 0),
                    Tag = realEstate.Id,
                };
                selectButton.Classes.Add("editButtonStyle");
                selectButton.Click += SelectRealEstate_Click;
                Grid.SetColumn(selectButton, 1);

                grid.Children.Add(realEstateText);
                grid.Children.Add(selectButton);

                border.Child = grid;
                realEstatesPanel.Children.Add(border);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = sender as TextBox;
            FilterRealEstates(searchTextBox?.Text ?? "");
        }

        public void SetSearchPolygon(List<Point> polygon)
        {
            _searchPolygon = polygon;
            FilterRealEstates();
        }

        private void CreateRealEstate_Click(object? sender, RoutedEventArgs e)
        {
            var newRealEstate = new RealEstates();
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
            mainWindow.Content = new EditRealEstatesWindow(newRealEstate, true);
        }

        private void EditSelectedRealEstate_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedRealEstateId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var realEstate = db.RealEstates.FirstOrDefault(re => re.Id == SelectedRealEstateId.Value);
                    if (realEstate != null)
                    {
                        var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
                        mainWindow.Content = new EditRealEstatesWindow(realEstate);
                    }
                }
            }
        }

        private async void DeleteSelectedRealEstate_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedRealEstateId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var realEstate = db.RealEstates.FirstOrDefault(re => re.Id == SelectedRealEstateId.Value);
                    if (realEstate != null)
                    {
                        var confirmDialog = new Window
                        {
                            Title = "Подтверждение удаления",
                            SizeToContent = SizeToContent.WidthAndHeight,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            MinWidth = 300
                        };

                        var stackPanel = new StackPanel
                        {
                            Margin = new Thickness(20)
                        };

                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = $"Вы уверены, что хотите удалить объект недвижимости с ID: {realEstate.Id}?",
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 20)
                        });

                        var buttonsPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 10
                        };

                        var yesButton = new Button { Content = "Да" };
                        var noButton = new Button { Content = "Нет" };

                        yesButton.Click += async (s, args) =>
                        {
                            db.RealEstates.Remove(realEstate);
                            db.SaveChanges();

                            // Сбрасываем выбранный объект и деактивируем кнопки
                            SelectedRealEstateId = null;
                            var editButton = this.FindControl<Button>("EditRealEstateButton");
                            var deleteButton = this.FindControl<Button>("DeleteRealEstateButton");
                            editButton.IsEnabled = false;
                            deleteButton.IsEnabled = false;

                            LoadRealEstates();
                            confirmDialog.Close();

                            var successWindow = new Window
                            {
                                Title = "Успех",
                                Content = new TextBlock
                                {
                                    Text = $"Объект недвижимости успешно удалён.",
                                    Margin = new Thickness(20),
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 16
                                },
                                SizeToContent = SizeToContent.WidthAndHeight,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            };
                            await successWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
                        };

                        noButton.Click += (s, args) =>
                        {
                            confirmDialog.Close();
                        };

                        buttonsPanel.Children.Add(yesButton);
                        buttonsPanel.Children.Add(noButton);
                        stackPanel.Children.Add(buttonsPanel);
                        confirmDialog.Content = stackPanel;
                        await confirmDialog.ShowDialog(TopLevel.GetTopLevel(this) as Window);
                    }
                }
            }
        }

        private void SelectRealEstate_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int realEstateId)
            {
                using (var db = new AppDbContext())
                {
                    var selectedRealEstate = db.RealEstates.FirstOrDefault(re => re.Id == realEstateId);
                    if (selectedRealEstate != null)
                    {
                        SelectedRealEstateId = realEstateId;

                        // Активируем кнопки редактирования и удаления
                        var editButton = this.FindControl<Button>("EditRealEstateButton");
                        var deleteButton = this.FindControl<Button>("DeleteRealEstateButton");
                        editButton.IsEnabled = true;
                        deleteButton.IsEnabled = true;

                        // Сбрасываем выделение всех строк
                        var realEstatesPanel = this.FindControl<StackPanel>("RealEstatesPanel");
                        foreach (var child in realEstatesPanel.Children)
                        {
                            if (child is Border border)
                            {
                                border.Background = Brushes.White;
                            }
                        }

                        // Выделяем выбранную строку
                        if (button.Parent?.Parent is Border selectedBorder)
                        {
                            selectedBorder.Background = new SolidColorBrush(Color.Parse("#E6F2FA"));
                        }
                    }
                }
            }
        }

        // Добавляем свойство для хранения ID выбранного объекта недвижимости
        public int? SelectedRealEstateId { get; private set; }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}