using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using cBotBacktestAnalyzer.Models;
using cBotBacktestAnalyzer.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace cBotBacktestAnalyzer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BacktestDataService _backtestDataService;
    private List<AnalysisResult> _analysisResults = new List<AnalysisResult>();
    private FilterCriteria _filterCriteria = new FilterCriteria();
    private Dictionary<string, CartesianChart> _metricCharts = new Dictionary<string, CartesianChart>();
    private Dictionary<string, Canvas> _heatMapCanvases = new Dictionary<string, Canvas>();
    private bool _useMedian = false; // 中央値を使用するかどうか

    // Define all available metrics to display
    private readonly Dictionary<string, Func<Pass, double>> _availableMetrics = new Dictionary<string, Func<Pass, double>>
    {
        { "Fitness", p => p.Fitness },
        { "Equity", p => p.Equity },
        { "Balance", p => p.Balance },
        { "Net Profit", p => p.NetProfit },
        { "Trades", p => p.Trades },
        { "Winning Trades", p => p.WinningTrades },
        { "Losing Trades", p => p.LosingTrades },
        { "Winning Rate (%)", p => p.WinningTrades > 0 && p.Trades > 0 ? (double)p.WinningTrades / p.Trades * 100 : 0 },
        { "Profit Factor", p => p.ProfitFactor ?? 0 },
        { "Average Trade", p => p.AverageTrade ?? 0 },
        { "Max Equity Drawdown (%)", p => p.MaxEquityDrawdownPercent * 100 },
        { "Max Balance Drawdown (%)", p => p.MaxBalanceDrawdownPercent * 100 },
        { "Max Equity Drawdown (Absolute)", p => p.MaxEquityDrawdownAbsolute },
        { "Max Balance Drawdown (Absolute)", p => p.MaxBalanceDrawdownAbsolute }
    };
    
    // 指標の値が大きいほど良いかどうか（ヒートマップの色分けに使用）
    private readonly Dictionary<string, bool> _higherIsBetter = new Dictionary<string, bool>
    {
        { "Net Profit", true },
        { "Gross Profit", true },
        { "Profit Factor", true },
        { "Average Trade", true },
        { "Fitness", true },
        { "Equity", true },
        { "Balance", true },
        { "Trades", true },
        { "Winning Trades", true },
        { "Winning Rate (%)", true },
        { "Max Balance Drawdown (%)", false },
        { "Max Equity Drawdown (%)", false },
        { "Max Balance Drawdown (Absolute)", false },
        { "Max Equity Drawdown (Absolute)", false },
        { "Losing Trades", false }
    };
    
    // 各指標に対するヒートマップデータの取得メソッドのマッピング
    private readonly Dictionary<string, Func<HeatMapData, double[,]?>> _metricMapGetters = new Dictionary<string, Func<HeatMapData, double[,]?>>();

    public MainWindow()
    {
        try
        {
            App.LogMessage("MainWindow constructor started");
            
            // ラジオボタンのチェック変更イベントが発生する前にフィールドを初期化する
            _backtestDataService = new BacktestDataService();
            App.LogMessage("BacktestDataService initialization completed");
            
            // メトリックマップゲッターを初期化
            InitializeMetricMapGetters();
            App.LogMessage("MetricMapGetters initialization completed");
            
            // UIコンポーネントを初期化
            InitializeComponent();
            App.LogMessage("InitializeComponent completed");
            
            // 処理順序を制御するためにラジオボタンのイベントハンドラーを明示的に設定
            // XAMLで設定されているイベントハンドラーを上書き
            MeanRadioButton.Checked -= AggregationMethod_Changed;
            MedianRadioButton.Checked -= AggregationMethod_Changed;
            
            MeanRadioButton.Checked += AggregationMethod_Changed;
            MedianRadioButton.Checked += AggregationMethod_Changed;
            
            // 明示的に初期値を設定
            _useMedian = MedianRadioButton.IsChecked == true;
            
            App.LogMessage("MainWindow constructor completed");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in MainWindow constructor: {ex}");
            MessageBox.Show($"An error occurred during application initialization: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void InitializeMetricMapGetters()
    {
        try
        {
            App.LogMessage("InitializeMetricMapGetters started");
            _metricMapGetters.Add("Net Profit", data => data.NetProfitMap);
            _metricMapGetters.Add("Max Balance Drawdown (%)", data => data.MaxBalanceDrawdownPercentMap);
            _metricMapGetters.Add("Gross Profit", data => data.GrossProfitMap);
            _metricMapGetters.Add("Profit Factor", data => data.ProfitFactorMap);
            _metricMapGetters.Add("Average Trade", data => data.AverageTradeMap);
            _metricMapGetters.Add("Max Equity Drawdown (%)", data => data.MaxEquityDrawdownPercentMap);
            _metricMapGetters.Add("Max Balance Drawdown (Absolute)", data => data.MaxBalanceDrawdownAbsoluteMap);
            _metricMapGetters.Add("Max Equity Drawdown (Absolute)", data => data.MaxEquityDrawdownAbsoluteMap);
            _metricMapGetters.Add("Fitness", data => data.FitnessMap);
            _metricMapGetters.Add("Equity", data => data.EquityMap);
            _metricMapGetters.Add("Balance", data => data.BalanceMap);
            _metricMapGetters.Add("Trades", data => data.TradesMap);
            _metricMapGetters.Add("Winning Trades", data => data.WinningTradesMap);
            _metricMapGetters.Add("Losing Trades", data => data.LosingTradesMap);
            _metricMapGetters.Add("Winning Rate (%)", data => data.WinningRateMap);
            App.LogMessage("InitializeMetricMapGetters completed");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in InitializeMetricMapGetters: {ex}");
            throw;
        }
    }

    private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        App.LogMessage("LoadFileButton_Click started");
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Backtest files (*.optres)|*.optres|All files (*.*)|*.*",
            Title = "Select a backtest file"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            FilePathTextBlock.Text = openFileDialog.FileName;
            
            // Show loading indication
            LoadFileButton.IsEnabled = false;
            LoadFileButton.Content = "Loading...";
            
            try
            {
                // Load data asynchronously
                bool success = await _backtestDataService.LoadBacktestDataAsync(openFileDialog.FileName);
                
                if (success)
                {
                    // フィルタリング基準をリセット
                    _filterCriteria = new FilterCriteria();
                    
                    // フィルターUIを構築
                    BuildFilterUI();
                    
                    // Analyze parameters
                    _analysisResults = _backtestDataService.AnalyzeParameters(_filterCriteria, _useMedian);
                    
                    // Update UI with loaded data
                    UpdateParameterList();
                    UpdateParameterComboBoxes();
                    
                    // ヒートマップキャンバスを初期化
                    InitializeHeatMapCanvases();
                    
                    // 集計方法に基づいてタイトルを更新
                    UpdateTitles();
                    
                    MessageBox.Show("Backtest data loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to load backtest data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                LoadFileButton.IsEnabled = true;
                LoadFileButton.Content = "Open Backtest File";
            }
        }
        App.LogMessage("LoadFileButton_Click completed");
    }

    private void BuildFilterUI()
    {
        try
        {
            App.LogMessage("BuildFilterUI started");
            
            // パラメータフィルターパネルをクリア
            ParameterFilterPanel.Children.Clear();
            
            // 指標フィルターを追加
            AddMetricFiltersToPanel(ParameterFilterPanel);
            
            // ボタンを追加するStackPanelを作成
            StackPanel buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Vertical, 
                Margin = new Thickness(0, 10, 0, 0) 
            };
            
            // Apply Filterボタンを作成
            Button applyButton = new Button
            {
                Content = "Apply Filter",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            applyButton.Click += ApplyParameterFilterButton_Click;
            buttonPanel.Children.Add(applyButton);
            
            // Reset Filterボタンを作成
            Button resetButton = new Button
            {
                Content = "Reset Filter",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            resetButton.Click += ResetParameterFilterButton_Click;
            buttonPanel.Children.Add(resetButton);
            
            // ボタンパネルを追加
            ParameterFilterPanel.Children.Add(buttonPanel);
            
            // ヒートマップフィルターパネルをクリア
            HeatMapFilterPanel.Children.Clear();
            
            // 指標フィルターを追加
            AddMetricFiltersToPanel(HeatMapFilterPanel);
            
            // ボタンを追加するStackPanelを作成
            StackPanel heatMapButtonPanel = new StackPanel 
            { 
                Orientation = Orientation.Vertical, 
                Margin = new Thickness(0, 10, 0, 0) 
            };
            
            // Apply Filterボタンを作成
            Button heatMapApplyButton = new Button
            {
                Content = "Apply Filter",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            heatMapApplyButton.Click += ApplyHeatMapFilterButton_Click;
            heatMapButtonPanel.Children.Add(heatMapApplyButton);
            
            // Reset Filterボタンを作成
            Button heatMapResetButton = new Button
            {
                Content = "Reset Filter",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            heatMapResetButton.Click += ResetHeatMapFilterButton_Click;
            heatMapButtonPanel.Children.Add(heatMapResetButton);
            
            // ボタンパネルを追加
            HeatMapFilterPanel.Children.Add(heatMapButtonPanel);
            
            App.LogMessage("BuildFilterUI completed");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in BuildFilterUI: {ex}");
        }
    }

    private string MakeSafeName(string input)
    {
        // 特殊文字を削除
        string safeName = input
            .Replace(" ", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("%", "Percent")
            .Replace(".", "Dot")
            .Replace("-", "Minus")
            .Replace("+", "Plus");
        
        // 数字で始まる場合はアンダースコアを追加
        if (safeName.Length > 0 && char.IsDigit(safeName[0]))
        {
            safeName = "_" + safeName;
        }
        
        return safeName;
    }

    private void AddMetricFiltersToPanel(StackPanel panel)
    {
        // 「結果値フィルター」セクション
        panel.Children.Add(new TextBlock 
        { 
            Text = "Result Value Filters", 
            FontWeight = FontWeights.Bold, 
            Margin = new Thickness(0, 5, 0, 5) 
        });
        
        // 各指標のフィルターを追加
        foreach (var metric in _availableMetrics.Keys)
        {
            var filterGroup = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };
            
            // 指標名
            filterGroup.Children.Add(new TextBlock { Text = metric, Margin = new Thickness(0, 0, 0, 5) });
            
            // 安全な名前を生成
            string safeMetricName = MakeSafeName(metric);
            
            // 最小値セクション
            var minGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Auto) });
            minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Auto) });
            
            minGrid.Children.Add(new TextBlock 
            { 
                Text = "Min:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0) 
            });
            Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 0);
            
            var minTextBox = new TextBox 
            { 
                Name = $"{safeMetricName}_Min", 
                Tag = $"Metric|{metric}|Min",
                Margin = new Thickness(0, 0, 10, 0)
            };
            minGrid.Children.Add(minTextBox);
            Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 1);
            
            var minInclusiveCheck = new CheckBox 
            { 
                Content = "Inclusive", 
                IsChecked = true, 
                Name = $"{safeMetricName}_MinInclusive",
                Tag = $"Metric|{metric}|MinInclusive",
                VerticalAlignment = VerticalAlignment.Center 
            };
            minGrid.Children.Add(minInclusiveCheck);
            Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 2);
            
            filterGroup.Children.Add(minGrid);
            
            // 最大値セクション
            var maxGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
            maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Auto) });
            maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Auto) });
            
            maxGrid.Children.Add(new TextBlock 
            { 
                Text = "Max:", 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0) 
            });
            Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 0);
            
            var maxTextBox = new TextBox 
            { 
                Name = $"{safeMetricName}_Max", 
                Tag = $"Metric|{metric}|Max",
                Margin = new Thickness(0, 0, 10, 0)
            };
            maxGrid.Children.Add(maxTextBox);
            Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 1);
            
            var maxInclusiveCheck = new CheckBox 
            { 
                Content = "Inclusive", 
                IsChecked = false, 
                Name = $"{safeMetricName}_MaxInclusive",
                Tag = $"Metric|{metric}|MaxInclusive",
                VerticalAlignment = VerticalAlignment.Center 
            };
            maxGrid.Children.Add(maxInclusiveCheck);
            Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 2);
            
            filterGroup.Children.Add(maxGrid);
            panel.Children.Add(filterGroup);
        }
        
        // Numeric Parameter Filters section
        var numericParameters = _backtestDataService.OptimizableParameters
            .Where(p => p.ParameterType == "Double" || p.ParameterType == "Integer")
            .ToList();
        
        if (numericParameters.Any())
        {
            panel.Children.Add(new TextBlock 
            { 
                Text = "Numeric Parameter Filters", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 15, 0, 5) 
            });
            
            foreach (var parameter in numericParameters)
            {
                if (parameter.Name == null) continue;
                
                var parameterGroup = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };
                
                // パラメータ名
                string displayName = parameter.Metadata?.FriendlyName ?? parameter.Name;
                parameterGroup.Children.Add(new TextBlock 
                { 
                    Text = displayName, 
                    Margin = new Thickness(0, 0, 0, 5) 
                });
                
                // 安全な名前を生成
                string safeParameterName = MakeSafeName(parameter.Name);
                
                // 最小値セクション
                var minGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Auto) });
                minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                minGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Auto) });
                
                minGrid.Children.Add(new TextBlock 
                { 
                    Text = "Min:", 
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0) 
                });
                Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 0);
                
                var minTextBox = new TextBox 
                { 
                    Name = $"{safeParameterName}_Min", 
                    Tag = $"Parameter|{parameter.Name}|Min",
                    Margin = new Thickness(0, 0, 10, 0)
                };
                minGrid.Children.Add(minTextBox);
                Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 1);
                
                var minInclusiveCheck = new CheckBox 
                { 
                    Content = "Inclusive", 
                    IsChecked = true, 
                    Name = $"{safeParameterName}_MinInclusive",
                    Tag = $"Parameter|{parameter.Name}|MinInclusive",
                    VerticalAlignment = VerticalAlignment.Center 
                };
                minGrid.Children.Add(minInclusiveCheck);
                Grid.SetColumn(minGrid.Children[minGrid.Children.Count - 1], 2);
                
                parameterGroup.Children.Add(minGrid);
                
                // 最大値セクション
                var maxGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
                maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Auto) });
                maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                maxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100, GridUnitType.Auto) });
                
                maxGrid.Children.Add(new TextBlock 
                { 
                    Text = "Max:", 
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0) 
                });
                Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 0);
                
                var maxTextBox = new TextBox 
                { 
                    Name = $"{safeParameterName}_Max", 
                    Tag = $"Parameter|{parameter.Name}|Max",
                    Margin = new Thickness(0, 0, 10, 0)
                };
                maxGrid.Children.Add(maxTextBox);
                Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 1);
                
                var maxInclusiveCheck = new CheckBox 
                { 
                    Content = "Inclusive", 
                    IsChecked = false, 
                    Name = $"{safeParameterName}_MaxInclusive",
                    Tag = $"Parameter|{parameter.Name}|MaxInclusive",
                    VerticalAlignment = VerticalAlignment.Center 
                };
                maxGrid.Children.Add(maxInclusiveCheck);
                Grid.SetColumn(maxGrid.Children[maxGrid.Children.Count - 1], 2);
                
                parameterGroup.Children.Add(maxGrid);
                panel.Children.Add(parameterGroup);
            }
        }
        
        // Enumパラメータフィルターセクション
        if (_backtestDataService.OptimizableParameters.Any(p => p.ParameterType == "Enum"))
        {
            panel.Children.Add(new TextBlock 
            { 
                Text = "Enum Parameter Filters", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 15, 0, 5) 
            });
            
            foreach (var parameter in _backtestDataService.OptimizableParameters.Where(p => p.ParameterType == "Enum"))
            {
                if (parameter.Name == null || parameter.Metadata?.EnumValues == null) continue;
                
                var enumFilterGroup = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };
                
                // パラメータ名
                enumFilterGroup.Children.Add(new TextBlock 
                { 
                    Text = parameter.Metadata?.FriendlyName ?? parameter.Name, 
                    Margin = new Thickness(0, 0, 0, 5) 
                });
                
                // 安全な名前を生成
                string safeParameterName = MakeSafeName(parameter.Name);
                
                // 値選択用のリストボックス
                var listBox = new ListBox 
                { 
                    Name = $"{safeParameterName}_Values", 
                    Tag = parameter.Name,
                    SelectionMode = SelectionMode.Multiple,
                    MaxHeight = 100
                };
                
                // null安全に変更
                var enumValues = parameter.Metadata?.EnumValues;
                if (enumValues != null)
                {
                    foreach (var enumValue in enumValues)
                    {
                        listBox.Items.Add(new ListBoxItem 
                        { 
                            Content = enumValue.Name, 
                            Tag = enumValue.Value.ToString() 
                        });
                    }
                }
                
                enumFilterGroup.Children.Add(listBox);
                panel.Children.Add(enumFilterGroup);
            }
        }
    }

    private void InitializeHeatMapCanvases()
    {
        // 既存のヒートマップキャンバスをクリア
        _heatMapCanvases.Clear();
        HeatMapPanel.Children.Clear();
        
        // デフォルトのヒートマップキャンバスを追加
        _heatMapCanvases.Add("Net Profit", NetProfitHeatMapCanvas);
        _heatMapCanvases.Add("Max Balance Drawdown (%)", DrawdownHeatMapCanvas);
        
        // 残りの指標用のヒートマップキャンバスを動的に追加
        foreach (var metric in _availableMetrics.Keys.Where(m => 
                 m != "Net Profit" && m != "Max Balance Drawdown (%)"))
        {
            Grid heatMapGrid = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            heatMapGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            heatMapGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            TextBlock headerText = new TextBlock
            {
                Text = $"{metric} Heat Map",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            Grid.SetRow(headerText, 0);
            heatMapGrid.Children.Add(headerText);
            
            Border border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(border, 1);
            
            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            
            Canvas canvas = new Canvas
            {
                ClipToBounds = true,
                Background = Brushes.White
            };
            
            scrollViewer.Content = canvas;
            border.Child = scrollViewer;
            heatMapGrid.Children.Add(border);
            
            HeatMapPanel.Children.Add(heatMapGrid);
            
            // キャンバスを辞書に追加
            _heatMapCanvases.Add(metric, canvas);
        }
    }

    private void UpdateParameterList()
    {
        ParameterListBox.ItemsSource = _analysisResults;
        
        if (_analysisResults.Count > 0)
        {
            ParameterListBox.SelectedIndex = 0;
        }
    }

    private void UpdateParameterComboBoxes()
    {
        var optimizableParameters = _backtestDataService.OptimizableParameters;
        
        Parameter1ComboBox.ItemsSource = optimizableParameters;
        Parameter1ComboBox.DisplayMemberPath = "Name";
        Parameter1ComboBox.SelectedValuePath = "Name";
        
        Parameter2ComboBox.ItemsSource = optimizableParameters;
        Parameter2ComboBox.DisplayMemberPath = "Name";
        Parameter2ComboBox.SelectedValuePath = "Name";
        
        if (optimizableParameters.Count >= 2)
        {
            Parameter1ComboBox.SelectedIndex = 0;
            Parameter2ComboBox.SelectedIndex = 1;
        }
        else if (optimizableParameters.Count == 1)
        {
            Parameter1ComboBox.SelectedIndex = 0;
            Parameter2ComboBox.SelectedIndex = 0;
        }
    }

    private void ParameterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedAnalysis = ParameterListBox.SelectedItem as AnalysisResult;
        if (selectedAnalysis != null)
        {
            UpdateCharts(selectedAnalysis);
        }
    }

    private void UpdateCharts(AnalysisResult analysis)
    {
        // Clear existing charts
        ChartsPanel.Children.Clear();
        _metricCharts.Clear();

        // Create charts for each metric
        foreach (var metricPair in _availableMetrics)
        {
            string metricName = metricPair.Key;
            
            // Create grid container for chart
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, 20);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300) });
            
            // Create title
            var title = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5),
                Text = $"{metricName} Average"
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);
            
            // Create chart
            var chart = new CartesianChart
            {
                Height = 300
            };
            Grid.SetRow(chart, 1);
            grid.Children.Add(chart);
            
            // Add to panel
            ChartsPanel.Children.Add(grid);
            
            // Store chart reference
            _metricCharts[metricName] = chart;
        }

        // Retrieve the two default charts we need to access directly
        NetProfitChart = _metricCharts["Net Profit"];
        DrawdownChart = _metricCharts["Max Balance Drawdown (%)"];

        // Prepare data series and labels
        var labels = new List<string>();
        var seriesData = new Dictionary<string, ChartValues<double>>();
        var passCounts = new Dictionary<string, List<int>>();
        
        foreach (var metric in _availableMetrics.Keys)
        {
            seriesData[metric] = new ChartValues<double>();
            passCounts[metric] = new List<int>();
        }
        
        // Get parameter type from backtest data
        var parameterInfo = _backtestDataService.OptimizableParameters
            .FirstOrDefault(p => p.Name == analysis.ParameterName || p.Metadata?.FriendlyName == analysis.ParameterName);
            
        // Populate data for each chart
        foreach (var valueAnalysis in analysis.ValueAnalyses)
        {
            string displayValue = GetFormattedParameterValue(valueAnalysis.ParameterValue, parameterInfo);
            labels.Add(displayValue);
            
            foreach (var metric in _availableMetrics.Keys)
            {
                double value = GetMetricValue(valueAnalysis, metric);
                seriesData[metric].Add(value);
                passCounts[metric].Add(valueAnalysis.PassCount);
            }
        }
        
        // Update each chart with its data
        foreach (var metricPair in _availableMetrics)
        {
            string metricName = metricPair.Key;
            var chart = _metricCharts[metricName];
            
            // Configure chart
            chart.Series = new SeriesCollection();
            chart.AxisX = new AxesCollection();
            chart.AxisY = new AxesCollection();
            
            // Add data series
            var columnSeries = new ColumnSeries
            {
                Title = metricName,
                Values = seriesData[metricName],
                Fill = new SolidColorBrush(GetColorForMetric(metricName)),
                DataLabels = true
            };
            
            // カスタムツールチップを設定
            columnSeries.LabelPoint = point => {
                int index = (int)point.X;
                if (index >= 0 && index < passCounts[metricName].Count)
                {
                    return $"{point.Y:N2} (n={passCounts[metricName][index]})";
                }
                return $"{point.Y:N2}";
            };
            
            chart.Series.Add(columnSeries);
            
            // Configure axes
            chart.AxisX.Add(new Axis
            {
                Title = analysis.ParameterName ?? "Parameter",
                Labels = labels,
                Separator = new LiveCharts.Wpf.Separator { Step = 1 }
            });
            
            chart.AxisY.Add(new Axis
            {
                Title = metricName,
                LabelFormatter = value => value.ToString("N2") + (metricName.Contains("%") ? "%" : "")
            });
        }
    }

    private string GetFormattedParameterValue(string? rawValue, Parameter? parameter)
    {
        if (rawValue == null || parameter == null)
            return string.Empty;
            
        // For enum values, convert to string representation
        if (parameter.ParameterType == "Enum" && parameter.Metadata?.EnumValues != null)
        {
            if (int.TryParse(rawValue, out int enumValue))
            {
                var enumInfo = parameter.Metadata.EnumValues.FirstOrDefault(e => e.Value == enumValue);
                return enumInfo?.Name ?? rawValue;
            }
        }
        
        // For Double and Integer values, apply appropriate rounding
        if (parameter.ParameterType == "Double" && double.TryParse(rawValue, out double doubleValue))
        {
            // Check step size to determine appropriate precision - null安全に変更
            var stepValue = parameter.Metadata?.Step;
            if (stepValue != null)
            {
                string stepStr = stepValue.ToString() ?? "0";
                int decimalPlaces = stepStr.Contains('.') ? 
                    stepStr.Length - stepStr.IndexOf('.') - 1 : 0;
                    
                return Math.Round(doubleValue, decimalPlaces).ToString($"F{decimalPlaces}");
            }
            
            // Default rounding for doubles
            return Math.Round(doubleValue, 6).ToString();
        }
        
        return rawValue;
    }

    private double GetMetricValue(ParameterValueAnalysis valueAnalysis, string metricName)
    {
        switch (metricName)
        {
            case "Net Profit":
                return valueAnalysis.NetProfitAverage;
            case "Max Balance Drawdown (%)":
                return valueAnalysis.MaxBalanceDrawdownPercentAverage * 100;
            case "Profit Factor":
                return valueAnalysis.ProfitFactorAverage;
            case "Average Trade":
                return valueAnalysis.AverageTradeAverage;
            case "Max Equity Drawdown (%)":
                return valueAnalysis.MaxEquityDrawdownPercentAverage * 100;
            case "Max Balance Drawdown (Absolute)":
                return valueAnalysis.MaxBalanceDrawdownAbsoluteAverage;
            case "Max Equity Drawdown (Absolute)":
                return valueAnalysis.MaxEquityDrawdownAbsoluteAverage;
            case "Fitness":
                return valueAnalysis.FitnessAverage;
            case "Equity":
                return valueAnalysis.EquityAverage;
            case "Balance":
                return valueAnalysis.BalanceAverage;
            case "Trades":
                return valueAnalysis.TradesAverage;
            case "Winning Trades":
                return valueAnalysis.WinningTradesAverage;
            case "Losing Trades":
                return valueAnalysis.LosingTradesAverage;
            case "Winning Rate (%)":
                return valueAnalysis.WinningRateAverage;
            default:
                return 0;
        }
    }

    private Color GetColorForMetric(string metricName)
    {
        // 値が小さいほど良い指標（赤色）
        if (metricName == "Losing Trades" || 
            metricName == "Max Equity Drawdown (%)" || 
            metricName == "Max Balance Drawdown (%)" || 
            metricName == "Max Equity Drawdown (Absolute)" || 
            metricName == "Max Balance Drawdown (Absolute)")
        {
            return Colors.OrangeRed;
        }
        
        // 値が大きいほど良い指標（緑色）
        return Colors.Green;
    }

    private void GenerateHeatMapButton_Click(object sender, RoutedEventArgs e)
    {
        string? parameter1Name = Parameter1ComboBox.SelectedValue as string;
        string? parameter2Name = Parameter2ComboBox.SelectedValue as string;
        
        if (parameter1Name == null || parameter2Name == null)
        {
            MessageBox.Show("Please select two parameters for heat map analysis.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (parameter1Name == parameter2Name)
        {
            MessageBox.Show("Please select two different parameters for heat map analysis.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var heatMapData = _backtestDataService.CreateHeatMap(parameter1Name, parameter2Name, _filterCriteria, _useMedian);
        
        GenerateAllHeatMaps(heatMapData);
    }

    private void GenerateAllHeatMaps(HeatMapData data)
    {
        // すべての指標に対してヒートマップを生成
        foreach (var metricName in _metricMapGetters.Keys)
        {
            GenerateHeatMap(data, metricName);
        }
    }

    private void GenerateHeatMap(HeatMapData data, string metricName)
    {
        if (!_heatMapCanvases.TryGetValue(metricName, out Canvas? canvas) || canvas == null)
            return;
        
        if (!_metricMapGetters.TryGetValue(metricName, out var mapGetter) || mapGetter == null)
            return;
        
        var metricMap = mapGetter(data);
        if (metricMap == null || data.Parameter1Values.Count == 0 || data.Parameter2Values.Count == 0)
            return;
        
        // Get parameter info for formatting values - null安全に修正
        var parameter1 = data.Parameter1Name != null 
            ? _backtestDataService.OptimizableParameters.FirstOrDefault(p => p.Name == data.Parameter1Name) 
            : null;
        var parameter2 = data.Parameter2Name != null 
            ? _backtestDataService.OptimizableParameters.FirstOrDefault(p => p.Name == data.Parameter2Name) 
            : null;
        
        canvas.Children.Clear();
        
        const int cellWidth = 80;
        const int cellHeight = 40;
        const int labelWidth = 150;
        const int labelHeight = 40;
        
        // Find min/max values for color scaling
        double minValue = double.MaxValue;
        double maxValue = double.MinValue;
        
        for (int i = 0; i < data.Parameter1Values.Count; i++)
        {
            for (int j = 0; j < data.Parameter2Values.Count; j++)
            {
                double value = metricMap[i, j];
                if (!double.IsNaN(value))
                {
                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);
                }
            }
        }
        
        // 最小値と最大値が同じ場合のハンドリング
        if (minValue == maxValue)
        {
            minValue = maxValue - 1;
        }
        
        // Set canvas size
        canvas.Width = labelWidth + (data.Parameter2Values.Count * cellWidth);
        canvas.Height = labelHeight + (data.Parameter1Values.Count * cellHeight);
        
        // Draw parameter2 labels (column headers)
        for (int j = 0; j < data.Parameter2Values.Count; j++)
        {
            TextBlock label = new TextBlock
            {
                Text = GetFormattedParameterValue(data.Parameter2Values[j], parameter2),
                Width = cellWidth,
                Height = labelHeight,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            
            Canvas.SetLeft(label, labelWidth + (j * cellWidth));
            Canvas.SetTop(label, 0);
            
            canvas.Children.Add(label);
        }
        
        // Draw parameter1 labels (row headers)
        for (int i = 0; i < data.Parameter1Values.Count; i++)
        {
            TextBlock label = new TextBlock
            {
                Text = GetFormattedParameterValue(data.Parameter1Values[i], parameter1),
                Width = labelWidth,
                Height = cellHeight,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, labelHeight + (i * cellHeight));
            
            canvas.Children.Add(label);
        }
        
        // Draw cells
        for (int i = 0; i < data.Parameter1Values.Count; i++)
        {
            for (int j = 0; j < data.Parameter2Values.Count; j++)
            {
                double value = metricMap[i, j];
                int passCount = data.PassCountMap?[i, j] ?? 0;
                
                // Only draw cell if there are passes
                if (passCount > 0)
                {
                    // この指標が大きいほど良いかどうかを判断
                    bool isHigherBetter = _higherIsBetter.TryGetValue(metricName, out bool higher) ? higher : true;
                    
                    Rectangle cell = new Rectangle
                    {
                        Width = cellWidth,
                        Height = cellHeight,
                        Fill = new SolidColorBrush(GetImprovedHeatMapColor(value, minValue, maxValue, isHigherBetter)),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    
                    Canvas.SetLeft(cell, labelWidth + (j * cellWidth));
                    Canvas.SetTop(cell, labelHeight + (i * cellHeight));
                    
                    canvas.Children.Add(cell);
                    
                    // メトリックの単位に応じたフォーマット
                    string valueText = FormatMetricValue(metricName, value);
                    
                    TextBlock cellText = new TextBlock
                    {
                        Text = $"{valueText}\n({passCount})",
                        Width = cellWidth,
                        Height = cellHeight,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = GetTextColor(GetImprovedHeatMapColor(value, minValue, maxValue, isHigherBetter))
                    };
                    
                    Canvas.SetLeft(cellText, labelWidth + (j * cellWidth));
                    Canvas.SetTop(cellText, labelHeight + (i * cellHeight));
                    
                    canvas.Children.Add(cellText);
                }
                else
                {
                    // Draw empty cell
                    Rectangle cell = new Rectangle
                    {
                        Width = cellWidth,
                        Height = cellHeight,
                        Fill = Brushes.LightGray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    
                    Canvas.SetLeft(cell, labelWidth + (j * cellWidth));
                    Canvas.SetTop(cell, labelHeight + (i * cellHeight));
                    
                    canvas.Children.Add(cell);
                    
                    TextBlock cellText = new TextBlock
                    {
                        Text = "N/A",
                        Width = cellWidth,
                        Height = cellHeight,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.Black
                    };
                    
                    Canvas.SetLeft(cellText, labelWidth + (j * cellWidth));
                    Canvas.SetTop(cellText, labelHeight + (i * cellHeight));
                    
                    canvas.Children.Add(cellText);
                }
            }
        }
    }

    private string FormatMetricValue(string metricName, double value)
    {
        // 指標に応じたフォーマットを適用
        if (metricName.Contains("(%)"))
        {
            return $"{value:N2}%";
        }
        else if (metricName == "Trades" || metricName == "Winning Trades" || metricName == "Losing Trades")
        {
            return $"{value:N0}";
        }
        else
        {
            return $"{value:N2}";
        }
    }

    private Color GetImprovedHeatMapColor(double value, double min, double max, bool isHigherBetter = true)
    {
        // 青（良い）から赤（悪い）へのグラデーション
        // 正規化した値 0 = 最悪、1 = 最良
        double normalizedValue;
        if (max == min)
            normalizedValue = 0.5;
        else
            normalizedValue = (value - min) / (max - min);
        
        // 大きいほど良い指標の場合、正規化した値をそのまま使用
        // 小さいほど良い指標の場合、正規化した値を反転
        if (!isHigherBetter)
            normalizedValue = 1 - normalizedValue;
        
        // 青（良い）から赤（悪い）へのグラデーション
        byte r = (byte)(255 * (1 - normalizedValue));
        byte g = (byte)(Math.Min(normalizedValue, 0.5) * 2 * 255); // 緑は0.5までは増加、それ以上は減少
        byte b = (byte)(255 * normalizedValue);
        
        return Color.FromRgb(r, g, b);
    }

    private void ApplyParameterFilterButton_Click(object sender, RoutedEventArgs e)
    {
        // フィルター基準を取得
        BuildFilterCriteria(ParameterFilterPanel);
        
        // 再分析してパラメータリストを更新
        _analysisResults = _backtestDataService.AnalyzeParameters(_filterCriteria, _useMedian);
        UpdateParameterList();
        
        MessageBox.Show("Filter applied to parameter analysis.", "Filter Applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyHeatMapFilterButton_Click(object sender, RoutedEventArgs e)
    {
        // フィルター基準を取得
        BuildFilterCriteria(HeatMapFilterPanel);
        
        // ヒートマップの更新
        string? parameter1Name = Parameter1ComboBox.SelectedValue as string;
        string? parameter2Name = Parameter2ComboBox.SelectedValue as string;
        
        if (parameter1Name != null && parameter2Name != null && parameter1Name != parameter2Name)
        {
            var heatMapData = _backtestDataService.CreateHeatMap(parameter1Name, parameter2Name, _filterCriteria, _useMedian);
            GenerateAllHeatMaps(heatMapData);
        }
        
        MessageBox.Show("Filter applied to heat maps.", "Filter Applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BuildFilterCriteria(StackPanel filterPanel)
    {
        _filterCriteria = new FilterCriteria();
        
        // UIからフィルター基準を構築
        foreach (var element in filterPanel.Children)
        {
            if (element is StackPanel stackPanel)
            {
                Dictionary<string, (TextBox? minTextBox, TextBox? maxTextBox, CheckBox? minInclusiveCheck, CheckBox? maxInclusiveCheck)> filterControls = new Dictionary<string, (TextBox?, TextBox?, CheckBox?, CheckBox?)>();
                
                // まず全ての関連するコントロールを収集
                foreach (var child in stackPanel.Children)
                {
                    if (child is Grid grid)
                    {
                        string? filterType = null;
                        string? filterName = null;
                        string? controlType = null;
                        
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is TextBox textBox && textBox.Tag != null)
                            {
                                var tagParts = textBox.Tag.ToString()?.Split('|');
                                if (tagParts?.Length == 3)
                                {
                                    filterType = tagParts[0];
                                    filterName = tagParts[1];
                                    controlType = tagParts[2]; // Min or Max
                                    
                                    string key = $"{filterType}|{filterName}";
                                    if (!filterControls.ContainsKey(key))
                                    {
                                        filterControls[key] = (null, null, null, null);
                                    }
                                    
                                    var current = filterControls[key];
                                    if (controlType == "Min")
                                    {
                                        filterControls[key] = (textBox, current.maxTextBox, current.minInclusiveCheck, current.maxInclusiveCheck);
                                    }
                                    else if (controlType == "Max")
                                    {
                                        filterControls[key] = (current.minTextBox, textBox, current.minInclusiveCheck, current.maxInclusiveCheck);
                                    }
                                }
                            }
                            else if (gridChild is CheckBox checkBox && checkBox.Tag != null)
                            {
                                var tagParts = checkBox.Tag.ToString()?.Split('|');
                                if (tagParts?.Length == 3)
                                {
                                    filterType = tagParts[0];
                                    filterName = tagParts[1];
                                    controlType = tagParts[2]; // MinInclusive or MaxInclusive
                                    
                                    string key = $"{filterType}|{filterName}";
                                    if (!filterControls.ContainsKey(key))
                                    {
                                        filterControls[key] = (null, null, null, null);
                                    }
                                    
                                    var current = filterControls[key];
                                    if (controlType == "MinInclusive")
                                    {
                                        filterControls[key] = (current.minTextBox, current.maxTextBox, checkBox, current.maxInclusiveCheck);
                                    }
                                    else if (controlType == "MaxInclusive")
                                    {
                                        filterControls[key] = (current.minTextBox, current.maxTextBox, current.minInclusiveCheck, checkBox);
                                    }
                                }
                            }
                        }
                    }
                    else if (child is ListBox listBox && listBox.Tag != null)
                    {
                        string parameterName = listBox.Tag.ToString() ?? string.Empty;
                        List<string> selectedValues = new List<string>();
                        
                        foreach (ListBoxItem item in listBox.SelectedItems)
                        {
                            if (item.Tag != null)
                            {
                                selectedValues.Add(item.Tag.ToString() ?? string.Empty);
                            }
                        }
                        
                        if (selectedValues.Count > 0)
                        {
                            _filterCriteria.EnumCriteria.Add(new EnumFilterCriterion
                            {
                                ParameterName = parameterName,
                                SelectedValues = selectedValues
                            });
                        }
                    }
                }
                
                // 収集したコントロール情報からフィルター条件を構築
                foreach (var kvp in filterControls)
                {
                    string[] keyParts = kvp.Key.Split('|');
                    if (keyParts.Length != 2) continue;
                    
                    string filterType = keyParts[0];
                    string filterName = keyParts[1];
                    var controls = kvp.Value;
                    
                    double? minValue = null;
                    double? maxValue = null;
                    bool isMinInclusive = controls.minInclusiveCheck?.IsChecked == true;
                    bool isMaxInclusive = controls.maxInclusiveCheck?.IsChecked == true;
                    
                    if (controls.minTextBox != null && !string.IsNullOrWhiteSpace(controls.minTextBox.Text) && 
                        double.TryParse(controls.minTextBox.Text, out double min))
                    {
                        minValue = min;
                    }
                    
                    if (controls.maxTextBox != null && !string.IsNullOrWhiteSpace(controls.maxTextBox.Text) && 
                        double.TryParse(controls.maxTextBox.Text, out double max))
                    {
                        maxValue = max;
                    }
                    
                    if (minValue.HasValue || maxValue.HasValue)
                    {
                        if (filterType == "Metric")
                        {
                            _filterCriteria.NumericCriteria.Add(new NumericFilterCriterion
                            {
                                MetricName = filterName,
                                MinValue = minValue,
                                MaxValue = maxValue,
                                IsMinInclusive = isMinInclusive,
                                IsMaxInclusive = isMaxInclusive
                            });
                        }
                        else if (filterType == "Parameter")
                        {
                            _filterCriteria.ParameterCriteria.Add(new ParameterFilterCriterion
                            {
                                ParameterName = filterName,
                                MinValue = minValue,
                                MaxValue = maxValue,
                                IsMinInclusive = isMinInclusive,
                                IsMaxInclusive = isMaxInclusive
                            });
                        }
                    }
                }
            }
        }
    }

    private Brush GetTextColor(Color backgroundColor)
    {
        // Calculate luminance to determine if text should be black or white
        double luminance = (0.299 * backgroundColor.R + 0.587 * backgroundColor.G + 0.114 * backgroundColor.B) / 255;
        return luminance > 0.5 ? Brushes.Black : Brushes.White;
    }

    // 集計方法が変更されたときのイベントハンドラ
    private void AggregationMethod_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            App.LogMessage("AggregationMethod_Changed event started");
            
            // 集計方法の設定を更新
            bool previousMedian = _useMedian;
            _useMedian = MedianRadioButton.IsChecked == true;
            App.LogMessage($"Aggregation method changed: _useMedian = {_useMedian} (previous: {previousMedian})");
            
            // データがロードされていない場合は処理をスキップ
            if (_backtestDataService == null || !_backtestDataService.IsDataLoaded)
            {
                App.LogMessage("Skipping update because no data is loaded");
                return;
            }
            
            // 集計方法が実際に変更された場合のみ再計算
            if (previousMedian != _useMedian)
            {
                // パラメータ分析結果を再計算
                _analysisResults = _backtestDataService.AnalyzeParameters(_filterCriteria, _useMedian);
                UpdateParameterList();
                App.LogMessage("Analysis results recalculated");
                
                // 選択中のパラメータ分析を更新
                if (ParameterListBox?.SelectedItem is AnalysisResult selectedAnalysis)
                {
                    App.LogMessage("Updating analysis for selected parameter");
                    UpdateCharts(selectedAnalysis);
                }
                
                // ヒートマップが表示されている場合は再生成
                if (Parameter1ComboBox != null && Parameter2ComboBox != null)
                {
                    string? parameter1Name = Parameter1ComboBox.SelectedValue as string;
                    string? parameter2Name = Parameter2ComboBox.SelectedValue as string;
                    
                    if (parameter1Name != null && parameter2Name != null && parameter1Name != parameter2Name)
                    {
                        App.LogMessage("Regenerating heat maps");
                        var heatMapData = _backtestDataService.CreateHeatMap(parameter1Name, parameter2Name, _filterCriteria, _useMedian);
                        GenerateAllHeatMaps(heatMapData);
                    }
                }
                
                // チャートとヒートマップのタイトルを更新
                UpdateTitles();
            }
            else
            {
                App.LogMessage("Skipping recalculation because aggregation method has not changed");
            }
            
            App.LogMessage("AggregationMethod_Changed event completed");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in AggregationMethod_Changed: {ex}");
            // エラーメッセージを表示せず、静かに例外を処理
        }
    }
    
    // チャートとヒートマップのタイトルを更新
    private void UpdateTitles()
    {
        try
        {
            App.LogMessage("UpdateTitles started");
            string aggregationMethod = _useMedian ? "Median" : "Average";
            
            // チャートのタイトルを更新
            if (_metricCharts != null && _metricCharts.Count > 0)
            {
                foreach (var metricPair in _metricCharts)
                {
                    var chartGrid = metricPair.Value?.Parent as Grid;
                    if (chartGrid != null && chartGrid.Children.Count > 0 && chartGrid.Children[0] is TextBlock titleBlock)
                    {
                        string metricName = metricPair.Key;
                        titleBlock.Text = $"{metricName} {aggregationMethod}";
                    }
                }
            }
            
            // ヒートマップのタイトルを更新
            if (HeatMapPanel != null && HeatMapPanel.Children.Count > 0)
            {
                foreach (var panel in HeatMapPanel.Children)
                {
                    if (panel is Grid grid && grid.Children.Count > 0 && grid.Children[0] is TextBlock titleBlock)
                    {
                        string titleText = titleBlock.Text;
                        if (titleText.Contains("Heat Map"))
                        {
                            string metricName = titleText.Replace(" Heat Map", "");
                            titleBlock.Text = $"{metricName} {aggregationMethod} Heat Map";
                        }
                    }
                }
            }
            
            App.LogMessage("UpdateTitles completed");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in UpdateTitles: {ex}");
            // エラーメッセージを表示せず、静かに例外を処理
        }
    }
    
    // フィルターをリセットするイベントハンドラ
    private void ResetParameterFilterButton_Click(object sender, RoutedEventArgs e)
    {
        // フィルターをリセット
        _filterCriteria = new FilterCriteria();
        
        // UIをクリア
        ClearFilterUI(ParameterFilterPanel);
        
        // フィルターUIを再構築
        BuildFilterUI();
        
        // パラメータリストを更新
        _analysisResults = _backtestDataService.AnalyzeParameters(_filterCriteria, _useMedian);
        UpdateParameterList();
        
        MessageBox.Show("All filters have been reset.", "Filters Reset", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void ResetHeatMapFilterButton_Click(object sender, RoutedEventArgs e)
    {
        // フィルターをリセット
        _filterCriteria = new FilterCriteria();
        
        // UIをクリア
        ClearFilterUI(HeatMapFilterPanel);
        
        // フィルターUIを再構築
        BuildFilterUI();
        
        // ヒートマップが表示されている場合は再生成
        string? parameter1Name = Parameter1ComboBox.SelectedValue as string;
        string? parameter2Name = Parameter2ComboBox.SelectedValue as string;
        
        if (parameter1Name != null && parameter2Name != null && parameter1Name != parameter2Name)
        {
            var heatMapData = _backtestDataService.CreateHeatMap(parameter1Name, parameter2Name, _filterCriteria, _useMedian);
            GenerateAllHeatMaps(heatMapData);
        }
        
        MessageBox.Show("All filters have been reset.", "Filters Reset", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    // フィルターUIをクリアする
    private void ClearFilterUI(StackPanel panel)
    {
        try
        {
            App.LogMessage($"ClearFilterUI started: {panel.Name}");
            
            // パネルをクリア前に、既存のボタンを検索
            Button? applyButton = null;
            Button? resetButton = null;
            
            foreach (var child in panel.Children)
            {
                if (child is StackPanel sp && sp.Orientation == Orientation.Vertical)
                {
                    foreach (var subChild in sp.Children)
                    {
                        if (subChild is Button btn)
                        {
                            if (btn.Content.ToString() == "Apply Filter")
                                applyButton = btn;
                            else if (btn.Content.ToString() == "Reset Filter")
                                resetButton = btn;
                        }
                    }
                }
            }
            
            // パネルをクリア
            panel.Children.Clear();
            
            // ボタンパネルを再作成
            StackPanel buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Vertical, 
                Margin = new Thickness(0, 10, 0, 0) 
            };
            
            // 既存のボタンがあれば再利用、なければ新規作成
            if (applyButton == null)
            {
                applyButton = new Button
                {
                    Content = "Apply Filter",
                    Width = 120,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                
                if (panel == ParameterFilterPanel)
                    applyButton.Click += ApplyParameterFilterButton_Click;
                else
                    applyButton.Click += ApplyHeatMapFilterButton_Click;
            }
            
            if (resetButton == null)
            {
                resetButton = new Button
                {
                    Content = "Reset Filter",
                    Width = 120,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                
                if (panel == ParameterFilterPanel)
                    resetButton.Click += ResetParameterFilterButton_Click;
                else
                    resetButton.Click += ResetHeatMapFilterButton_Click;
            }
            
            // 必ず両方のボタンをパネルに追加
            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(resetButton);
            
            // ボタンパネルを追加
            panel.Children.Add(buttonPanel);
            
            App.LogMessage($"ClearFilterUI completed: {panel.Name}");
        }
        catch (Exception ex)
        {
            App.LogMessage($"Error in ClearFilterUI: {ex}");
        }
    }
}