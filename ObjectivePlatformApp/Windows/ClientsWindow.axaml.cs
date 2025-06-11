using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Interactivity;
using ObjectivePlatformApp.Data;
using ObjectivePlatformApp.Models;
using System.Linq;
using System;
using Avalonia.Media;
using System.Collections.Generic;

namespace ObjectivePlatformApp
{
    public partial class ClientsWindow : UserControl
    {
        private List<Clients> _allClients = new List<Clients>();

        public ClientsWindow()
        {
            InitializeComponent();
            var createButton = this.FindControl<Button>("CreateClientButton");
            createButton.Click += CreateClient_Click;

            var backButton = this.FindControl<Button>("BackButton");
            backButton.Click += BackButton_Click;

            var editButton = this.FindControl<Button>("EditClientButton");
            editButton.Click += EditSelectedClient_Click;
            editButton.IsEnabled = false;

            var deleteButton = this.FindControl<Button>("DeleteClientButton");
            deleteButton.Click += DeleteSelectedClient_Click;
            deleteButton.IsEnabled = false;

            LoadClients();
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            (TopLevel.GetTopLevel(this) as Window)?.Close();
        }

        private void LoadClients()
        {
            using (var db = new AppDbContext())
            {
                _allClients = db.Clients.ToList();
                FilterClients();
            }
        }

        private void FilterClients(string searchText = "")
        {
            var clientsPanel = this.FindControl<StackPanel>("ClientsPanel");
            clientsPanel.Children.Clear();

            var filteredClients = string.IsNullOrWhiteSpace(searchText)
                ? _allClients
                : _allClients.Where(c =>
                    IsFuzzyMatch(c.FirstName, searchText, 3) ||
                    IsFuzzyMatch(c.MiddleName, searchText, 3) ||
                    IsFuzzyMatch(c.LastName, searchText, 3) ||
                    IsFuzzyMatch(c.Phone, searchText, 2) ||
                    IsFuzzyMatch(c.Email, searchText, 2)).ToList();

            foreach (var client in filteredClients)
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
                        new ColumnDefinition(GridLength.Auto)
                    }
                };

                var clientText = new TextBlock
                {
                    Text = $"{client.Id} - {client.LastName} {client.FirstName} {client.MiddleName} | " +
                           $"Телефон: {client.Phone} | Email: {client.Email}",
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(clientText, 0);

                var selectButton = new Button
                {
                    Content = "Выбрать",
                    Margin = new Thickness(5, 0, 5, 0),
                    Tag = client.Id,
                    Classes = { "editButtonStyle" }
                };
                selectButton.Click += SelectClient_Click;
                Grid.SetColumn(selectButton, 1);

                grid.Children.Add(clientText);
                grid.Children.Add(selectButton);

                border.Child = grid;
                clientsPanel.Children.Add(border);
            }
        }

        private void SelectClient_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int clientId)
            {
                using (var db = new AppDbContext())
                {
                    var selectedClient = db.Clients.FirstOrDefault(c => c.Id == clientId);
                    if (selectedClient != null)
                    {
                        SelectedClientId = clientId;
                        
                        // Активируем кнопки редактирования и удаления
                        var editButton = this.FindControl<Button>("EditClientButton");
                        var deleteButton = this.FindControl<Button>("DeleteClientButton");
                        editButton.IsEnabled = true;
                        deleteButton.IsEnabled = true;
                        
                        // Сбрасываем выделение всех строк
                        var clientsPanel = this.FindControl<StackPanel>("ClientsPanel");
                        foreach (var child in clientsPanel.Children)
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

        // Добавляем свойство для хранения ID выбранного клиента
        public int? SelectedClientId { get; private set; }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = sender as TextBox;
            FilterClients(searchTextBox?.Text ?? "");
        }

        private bool IsMatch(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return false;
            if (string.IsNullOrEmpty(target)) return false;

            source = source.ToLower();
            target = target.ToLower();

            if (source.Contains(target) || target.Contains(source))
                return true;

            return LevenshteinDistance(source, target) <= 3;
        }

        private int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

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

        private void CreateClient_Click(object? sender, RoutedEventArgs e)
        {
            var newClient = new Clients();
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
            mainWindow.Content = new EditClient(newClient, true);
        }

        private void EditSelectedClient_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedClientId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var client = db.Clients.FirstOrDefault(c => c.Id == SelectedClientId.Value);
                    if (client != null)
                    {
                        var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
                        mainWindow.Content = new EditClient(client);
                    }
                }
            }
        }

        private async void DeleteSelectedClient_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedClientId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var client = db.Clients.FirstOrDefault(c => c.Id == SelectedClientId.Value);
                    if (client != null)
                    {
                        bool hasDemands = db.Demands.Any(d => d.ClientId == SelectedClientId.Value);
                        bool hasOffers = db.Offers.Any(o => o.ClientId == SelectedClientId.Value);

                        if (hasDemands || hasOffers)
                        {
                            var errorWindow = new Window
                            {
                                Title = "Ошибка",
                                Content = new TextBlock
                                {
                                    Text = "Невозможно удалить клиента, так как он участвует в сделках или предложениях.",
                                    Margin = new Thickness(20),
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 16
                                },
                                SizeToContent = SizeToContent.WidthAndHeight,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                MinWidth = 350
                            };
                            await errorWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
                            return;
                        }

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
                            Text = $"Вы уверены, что хотите удалить клиента:\n{client.LastName} {client.FirstName} {client.MiddleName}?",
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
                            db.Clients.Remove(client);
                            db.SaveChanges();
                            
                            // Сбрасываем выбранного клиента и деактивируем кнопки
                            SelectedClientId = null;
                            var editButton = this.FindControl<Button>("EditClientButton");
                            var deleteButton = this.FindControl<Button>("DeleteClientButton");
                            editButton.IsEnabled = false;
                            deleteButton.IsEnabled = false;
                            
                            LoadClients();
                            confirmDialog.Close();

                            var successWindow = new Window
                            {
                                Title = "Успех",
                                Content = new TextBlock
                                {
                                    Text = $"Клиент {client.LastName} {client.FirstName} успешно удален.",
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
    }
}