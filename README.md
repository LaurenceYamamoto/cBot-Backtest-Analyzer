# cBOT Backtest Analyzer

A Windows application for analyzing backtest data from cTrader cBOTS. This tool helps traders visualize and understand how different parameter combinations affect trading performance.

## Features

- **Load and analyze backtest data** (.optres files) from cTrader cBOTS
- **Parameter analysis**: Visualize how changes in a single parameter affect various performance metrics
- **Heat map analysis**: Explore how combinations of two parameters affect performance metrics
- **Multiple metrics visualization**: View the impact of parameters on:
  - Fitness
  - Equity
  - Balance
  - Net Profit
  - Trades
  - Winning Trades
  - Losing Trades
  - Winning Rate (%)
  - Profit Factor
  - Average Trade
  - Max Equity Drawdown (%)
  - Max Balance Drawdown (%)
  - Max Equity Drawdown (Absolute)
  - Max Balance Drawdown (Absolute)
- **Advanced filtering**: Apply filters based on multiple criteria
  - Result value filters (filter by result metrics)
  - Numeric parameter filters (filter by parameter values)
  - Enum parameter filters with multiple selection
  - Combine multiple filters with AND logic
- **Color-coded visualization**: 
  - Blue-to-red gradient for better visual analysis
  - Automatically adjusts colors based on whether higher or lower values are better for each metric
- **Enhanced tooltips**: Hover over chart bars to see detailed information including sample size

## Installation

### Prerequisites
- Windows operating system (Windows 10 or 11) and x86_64 CPU - You need to build yourself for other OS or architectures.
- cTrader platform backtest results

### Installation Steps (Windows x86_64)

1. Download the latest release (ZIP file) from the releases page
2. Extract the ZIP file to a location of your choice
3. Run `cBotBacktestAnalyzer.exe`

### For Other Platforms
1. Clone this repository
2. Build the project using .NET 9.0 SDK
3. Use the appropriate runtime identifier for your target platform:
   ```
   dotnet publish -c Release -r <RID> --self-contained true
   ```
   Where `<RID>` is the runtime identifier for your platform (e.g., `osx-x64`, `linux-x64`, etc.)

Note that while the codebase is built with .NET 9.0, which theoretically supports cross-platform deployment, some features may be Windows-specific as this application is primarily designed for Windows environments.

## Usage

### Loading Backtest Data

1. Click "Open Backtest File" and select your .optres file (detailed backtest data folder with the same name as .optres file must exist in the same folder)
2. The application will load and analyze the backtest data
3. Parameters that were optimized during backtesting will appear in the list on the left

### Parameter Analysis

1. Select a parameter from the list on the left
2. The application will display charts showing how different values of this parameter affect various performance metrics
3. Scroll down to see all available metrics
4. Use the filter section to focus on specific results based on multiple criteria
5. Hover over chart bars to see the value and sample size (n)

### Heat Map Analysis

1. Go to the "Heat Map Analysis" tab
2. Select two parameters to analyze from the dropdown menus
3. Click "Generate Heat Map"
4. View heat maps showing how combinations of these parameters affect all available metrics
5. Use the filter expander to set filtering criteria 

### Filtering Results

1. Expand the "Filter Settings" section in either tab
2. Set minimum and/or maximum values for any result metrics
3. Set minimum and/or maximum values for numeric parameters
4. Select values for enum parameters (if available)
5. Click "Apply Filter" to update the analysis based on your criteria
6. All charts and heat maps will be updated to include only the filtered results

## Understanding the Heat Maps

- **Color coding**: Blue represents better results, red represents worse results
- **Cell values**: Each cell shows the average metric value and the number of passes (n) with that parameter combination
- **Metrics interpretation**:
  - For metrics like Net Profit, Profit Factor, etc., higher values are better (shown in blue)
  - For metrics like Drawdown, lower values are better (color scale is inverted)

## File Structure

The application expects the following structure for backtest files:
- Main .optres file containing backtest settings and results
- Folder with the same name as the .optres file (without extension)
  - Subfolders numbered by pass ID
    - parameters.cbotset file inside each pass folder

## Troubleshooting

If you encounter any issues:
1. Make sure your backtest files and folder structure are correct
2. Verify that the parameters were actually optimized in cTrader (marked with "optimize": true)

## License

Please read the attatchet "LICENSE" file (BSD-3-Clause license).
