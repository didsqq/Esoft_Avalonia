using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using ObjectivePlatformApp.Data;
using ObjectivePlatformApp.Models;
using System.Linq;
using System;
using Avalonia.Media;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ObjectivePlatformApp
{
    public partial class AgentsWindow : UserControl
    {
        private List<Agents> _allAgents = new List<Agents>();

        public AgentsWindow()
        {
            InitializeComponent();
            var createButton = this.FindControl<Button>("CreateAgentButton");
            createButton.Click += CreateAgent_Click;

            var backButton = this.FindControl<Button>("BackButton");
            backButton.Click += BackButton_Click;

            var editButton = this.FindControl<Button>("EditAgentButton");
            editButton.Click += EditSelectedAgent_Click;
            editButton.IsEnabled = false; // Изначально кнопка неактивна

            var deleteButton = this.FindControl<Button>("DeleteAgentButton");
            deleteButton.Click += DeleteSelectedAgent_Click;
            deleteButton.IsEnabled = false; // Изначально кнопка неактивна

            LoadAgents();
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            (TopLevel.GetTopLevel(this) as Window)?.Close();
        }

        private void LoadAgents()
        {
            using (var db = new AppDbContext())
            {
                _allAgents = db.Agents.ToList();
                FilterAgents();
            }
        }

        private void FilterAgents(string searchText = "")
        {
            var agentsPanel = this.FindControl<StackPanel>("AgentsPanel");
            agentsPanel.Children.Clear();

            var filteredAgents = string.IsNullOrWhiteSpace(searchText)
                ? _allAgents
                : _allAgents.Where(a =>
                    IsFuzzyMatch(a.FirstName, searchText, 3) ||
                    IsFuzzyMatch(a.MiddleName, searchText, 3) ||
                    IsFuzzyMatch(a.LastName, searchText, 3)).ToList();

            foreach (var agent in filteredAgents)
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

                var agentText = new TextBlock
                {
                    Text = $"{agent.Id} - {agent.LastName} {agent.FirstName} {agent.MiddleName} | Комиссия: {agent.Commision}%",
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(agentText, 0);

                var selectButton = new Button
                {
                    Content = "Выбрать",
                    Margin = new Thickness(5, 0, 5, 0),
                    Tag = agent.Id,
                    Classes = { "editButtonStyle" }
                };
                selectButton.Click += SelectAgent_Click;
                Grid.SetColumn(selectButton, 1);

                grid.Children.Add(agentText);
                grid.Children.Add(selectButton);

                border.Child = grid;
                agentsPanel.Children.Add(border);
            }
        }

        private void SelectAgent_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int agentId)
            {
                using (var db = new AppDbContext())
                {
                    var selectedAgent = db.Agents.FirstOrDefault(a => a.Id == agentId);
                    if (selectedAgent != null)
                    {
                        SelectedAgentId = agentId;
                        
                        // Активируем кнопки редактирования и удаления
                        var editButton = this.FindControl<Button>("EditAgentButton");
                        var deleteButton = this.FindControl<Button>("DeleteAgentButton");
                        editButton.IsEnabled = true;
                        deleteButton.IsEnabled = true;
                        
                        // Сбрасываем выделение всех строк
                        var agentsPanel = this.FindControl<StackPanel>("AgentsPanel");
                        foreach (var child in agentsPanel.Children)
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

        // Добавляем свойство для хранения ID выбранного риэлтора
        public int? SelectedAgentId { get; private set; }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = sender as TextBox;
            FilterAgents(searchTextBox?.Text ?? "");
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

        private int LevenshteinDistance(string s, string t)
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

        private void CreateAgent_Click(object? sender, RoutedEventArgs e)
        {
            var newAgent = new Agents();
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
            mainWindow.Content = new EditAgent(newAgent, true);
        }

        private void EditSelectedAgent_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedAgentId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var agent = db.Agents.FirstOrDefault(a => a.Id == SelectedAgentId.Value);
                    if (agent != null)
                    {
                        var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
                        mainWindow.Content = new EditAgent(agent);
                    }
                }
            }
        }

        private async void DeleteSelectedAgent_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedAgentId.HasValue)
            {
                using (var db = new AppDbContext())
                {
                    var agent = db.Agents.FirstOrDefault(a => a.Id == SelectedAgentId.Value);
                    if (agent != null)
                    {
                        bool hasDemands = db.Demands.Any(d => d.AgentId == SelectedAgentId.Value);
                        bool hasOffers = db.Offers.Any(o => o.AgentId == SelectedAgentId.Value);

                        if (hasDemands || hasOffers)
                        {
                            var errorWindow = new Window
                            {
                                Title = "Ошибка",
                                Content = new TextBlock
                                {
                                    Text = "Невозможно удалить риэлтора, так как он участвует в сделках или предложениях.",
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
                            Text = $"Вы уверены, что хотите удалить риэлтора:\n{agent.LastName} {agent.FirstName} {agent.MiddleName}?",
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
                            db.Agents.Remove(agent);
                            db.SaveChanges();
                            
                            // Сбрасываем выбранного риэлтора и деактивируем кнопки
                            SelectedAgentId = null;
                            var editButton = this.FindControl<Button>("EditAgentButton");
                            var deleteButton = this.FindControl<Button>("DeleteAgentButton");
                            editButton.IsEnabled = false;
                            deleteButton.IsEnabled = false;
                            
                            LoadAgents();
                            confirmDialog.Close();

                            var successWindow = new Window
                            {
                                Title = "Успех",
                                Content = new TextBlock
                                {
                                    Text = $"Риэлтор {agent.LastName} {agent.FirstName} успешно удален.",
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