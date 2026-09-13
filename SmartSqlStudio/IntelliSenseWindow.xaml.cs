using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartSQLStudio
{
    public partial class IntelliSenseWindow : Window
    {
        private readonly ListBox _suggestionsListBox;
        private readonly TextBox _searchBox;
        private List<string> _allSuggestions = new();
        private string? _selectedValue;

        public string? SelectedValue => _selectedValue;

        public IntelliSenseWindow(List<string> keywords, List<string> tables, List<Snippet> snippets)
        {
            Title = "IntelliSense - Suggerimenti";
            Width = 400;
            Height = 500;
            WindowStyle = WindowStyle.ToolWindow;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Search box
            _searchBox = new TextBox
            {
                Margin = new Thickness(5),
                Padding = new Thickness(8),
                FontSize = 14
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            Grid.SetRow(_searchBox, 0);
            grid.Children.Add(_searchBox);

            // Suggestions list
            _suggestionsListBox = new ListBox
            {
                Margin = new Thickness(5),
                FontSize = 13,
                BorderThickness = new Thickness(1)
            };
            _suggestionsListBox.MouseDoubleClick += SuggestionsListBox_MouseDoubleClick;
            _suggestionsListBox.KeyDown += SuggestionsListBox_KeyDown;
            Grid.SetRow(_suggestionsListBox, 1);
            grid.Children.Add(_suggestionsListBox);

            Content = grid;

            // Combina tutti i suggerimenti
            _allSuggestions.AddRange(keywords.Select(k => $"🔑 {k} (Keyword)"));
            _allSuggestions.AddRange(tables.Select(t => $"📋 {t} (Tabella)"));
            _allSuggestions.AddRange(snippets.Select(s => $"📝 {s.Name} (Snippet)"));

            _suggestionsListBox.ItemsSource = _allSuggestions;
            
            if (_allSuggestions.Count > 0)
            {
                _suggestionsListBox.SelectedIndex = 0;
            }

            Loaded += (s, e) => {
                _searchBox.Focus();
                _suggestionsListBox.ScrollIntoView(_suggestionsListBox.Items[0]);
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = _searchBox.Text.ToLower();
            var filtered = string.IsNullOrEmpty(filter)
                ? _allSuggestions
                : _allSuggestions.Where(s => s.ToLower().Contains(filter)).ToList();

            _suggestionsListBox.ItemsSource = filtered;

            if (filtered.Count > 0)
            {
                _suggestionsListBox.SelectedIndex = 0;
            }
        }

        private void SuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }

        private void SuggestionsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                SelectCurrentItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        private void SelectCurrentItem()
        {
            if (_suggestionsListBox.SelectedItem is string selected)
            {
                // Estrai solo il nome senza emoji e tipo
                var parts = selected.Split(' ');
                _selectedValue = parts.Length > 1 ? string.Join(" ", parts.Skip(1).Take(parts.Length - 2)) : selected;
                
                DialogResult = true;
                Close();
            }
        }
    }
}
