using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NLog;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace VepfDemo
{
    public partial class ScottPlotDemoWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // 1. Telemetry State
        private const int PointCount = 5000;
        private readonly double[] _voltageData = new double[PointCount];
        private readonly double[] _tempData = new double[PointCount];

        // 2. Heatmap State
        private const int HeatmapSize = 25;
        private readonly double[,] _heatmapData = new double[HeatmapSize, HeatmapSize];
        private Heatmap? _heatmapPlottable;

        // 3. Bar Chart State
        private readonly double[] _barValues = { 85, 62, 45, 92, 28 };

        // System / UI Logic
        private DispatcherTimer? _pollTimer;
        private Crosshair? _crosshair;
        private Random _rand = new Random();
        private double _phaseShift = 0;
        private double _lastVoltage = 0;
        private DateTime _lastMouseMoveRender = DateTime.MinValue;

        private ScottPlot.Plottables.BarPlot? _systemBarPlot;
        private ScottPlot.Plottables.Radar? _radarPlottable;
        private readonly double[,] _radarValues = {
            { 100, 80, 65, 45, 80 }, // Node Alpha
            { 45, 60, 100, 80, 50 }  // Node Beta
        };


        public ScottPlotDemoWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDashboard();
                StartTelemetryPoll();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to initialize telemetry dashboard.");
            }
        }

        private void InitializeDashboard()
        {
            // Apply a uniform dark theme to all plots
            WpfPlot[] allPlots = { PlotTelemetry, PlotHeatmap, PlotBarChart, PlotRadar, PlotScatter };
            foreach (var wpfPlot in allPlots)
            {
                wpfPlot.Plot.FigureBackground.Color = Color.FromHex("#1E293B");
                wpfPlot.Plot.DataBackground.Color = Color.FromHex("#0B1120");
                wpfPlot.Plot.Axes.Color(Color.FromHex("#94A3B8"));
                wpfPlot.Plot.Grid.MajorLineColor = Color.FromHex("#334155");
            }

            // Setup Individual Visualizations
            SetupTelemetryPlot();
            SetupHeatmapPlot();
            SetupBarChart();
            SetupRadarChart();
            SetupScatterPlot();

            // Initial Draw
            foreach (var wpfPlot in allPlots) wpfPlot.Refresh();
        }

        // ==========================================
        // INDIVIDUAL PLOT SETUPS
        // ==========================================

        private void SetupTelemetryPlot()
        {
            var plot = PlotTelemetry.Plot;
            SimulateIncomingData();

            var sigVoltage = plot.Add.Signal(_voltageData);
            sigVoltage.Color = Color.FromHex("#10B981");
            sigVoltage.Axes.YAxis = plot.Axes.Left;
            plot.Axes.Left.Label.Text = "Voltage (V)";
            plot.Axes.Left.Label.ForeColor = Color.FromHex("#10B981");

            var sigTemp = plot.Add.Signal(_tempData);
            sigTemp.Color = Color.FromHex("#F59E0B");
            sigTemp.LineWidth = 2;
            sigTemp.Axes.YAxis = plot.Axes.Right;
            plot.Axes.Right.Label.Text = "Temp (°C)";
            plot.Axes.Right.Label.ForeColor = Color.FromHex("#F59E0B");
            plot.Axes.Right.IsVisible = true;

            plot.Axes.Title.Label.Text = "Primary Engine Telemetry";
            plot.Axes.Title.Label.ForeColor = Colors.White;

            // DPI-Aware Interactive Crosshair
            _crosshair = plot.Add.Crosshair(0, 0);
            _crosshair.LineColor = Color.FromHex("#E2E8F0");
            _crosshair.TextColor = Color.FromHex("#0F172A");
            _crosshair.TextBackgroundColor = Color.FromHex("#E2E8F0");
            PlotTelemetry.MouseMove += PlotTelemetry_MouseMove;

            plot.Axes.SetLimits(0, PointCount, -100, 100, plot.Axes.Bottom, plot.Axes.Left);
            plot.Axes.SetLimits(0, PointCount, 50, 150, plot.Axes.Bottom, plot.Axes.Right);
        }

        private void SetupHeatmapPlot()
        {
            var plot = PlotHeatmap.Plot;

            for (int y = 0; y < HeatmapSize; y++)
                for (int x = 0; x < HeatmapSize; x++)
                    _heatmapData[y, x] = _rand.NextDouble() * 50;

            _heatmapPlottable = plot.Add.Heatmap(_heatmapData);
            _heatmapPlottable.Colormap = new ScottPlot.Colormaps.Turbo();

            plot.Axes.Title.Label.Text = "Thermal Array Matrix";
            plot.Axes.Title.Label.ForeColor = Colors.White;
            plot.Axes.SetLimits(-0.5, HeatmapSize - 0.5, -0.5, HeatmapSize - 0.5);
            plot.Axes.Frameless();
        }

        private void SetupBarChart()
        {
            var plot = PlotBarChart.Plot;

            // Save the reference so we can animate it later
            _systemBarPlot = plot.Add.Bars(_barValues);

            // Add a critical threshold warning line
            var limitLine = plot.Add.HorizontalLine(85);
            limitLine.Color = Color.FromHex("#EF4444"); // Red
            limitLine.LinePattern = LinePattern.Dashed;
            limitLine.LineWidth = 2;

            Tick[] ticks = {
                new Tick(0, "CPU"), new Tick(1, "RAM"), new Tick(2, "IO"),
                new Tick(3, "NET"), new Tick(4, "GPU")
            };
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

            plot.Axes.Title.Label.Text = "Subsystem Load (%)";
            plot.Axes.Title.Label.ForeColor = Colors.White;
            plot.Axes.SetLimitsY(0, 100);
        }


        private void SetupRadarChart()
        {
            var plot = PlotRadar.Plot;
            plot.Axes.Frameless();
            plot.Grid.IsVisible = false;

            _radarPlottable = plot.Add.Radar(_radarValues);

            _radarPlottable.Series[0].FillColor = Color.FromHex("#10B981").WithAlpha(0.3);
            _radarPlottable.Series[0].LineColor = Color.FromHex("#10B981");
            _radarPlottable.Series[0].LineWidth = 2.5f;

            _radarPlottable.Series[1].FillColor = Color.FromHex("#8B5CF6").WithAlpha(0.3);
            _radarPlottable.Series[1].LineColor = Color.FromHex("#8B5CF6");
            _radarPlottable.Series[1].LineWidth = 2.5f;

            plot.Axes.SetLimits(-120, 120, -120, 120);

            plot.Axes.Title.Label.Text = "Cluster Synchronization";
            plot.Axes.Title.Label.ForeColor = Colors.White;
        }


        private void SetupScatterPlot()
        {
            // Simulates an RF Signal Quality Constellation diagram (QAM)
            var plot = PlotScatter.Plot;

            int pointCount = 2000;
            double[] xs = new double[pointCount];
            double[] ys = new double[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                // Generate 4 clusters
                double cx = (_rand.Next(2) == 0) ? -5 : 5;
                double cy = (_rand.Next(2) == 0) ? -5 : 5;

                xs[i] = cx + (_rand.NextDouble() - 0.5) * 4;
                ys[i] = cy + (_rand.NextDouble() - 0.5) * 4;
            }

            var scatter = plot.Add.Scatter(xs, ys);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 4;
            scatter.MarkerColor = Color.FromHex("#EC4899").WithAlpha(.6);

            plot.Axes.Title.Label.Text = "RF Constellation (QAM)";
            plot.Axes.Title.Label.ForeColor = Colors.White;
            plot.Axes.SetLimits(-10, 10, -10, 10);
        }

        // ==========================================
        // DYNAMIC UPDATES & EVENTS
        // ==========================================

        private void StartTelemetryPoll()
        {
            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(RefreshSlider.Value);
            _pollTimer.Tick += (s, e) =>
            {
                SimulateIncomingData();
                UpdateOtherPlots();

                PlotTelemetry.Refresh();
                PlotHeatmap.Refresh();
                PlotBarChart.Refresh();
            };
            _pollTimer.Start();
        }

        private void RefreshSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_pollTimer != null)
                _pollTimer.Interval = TimeSpan.FromSeconds(e.NewValue);
        }

        private void SimulateIncomingData()
        {
            // 1. Update Telemetry Signal Arrays (In-place for memory efficiency)
            _phaseShift += 0.15;
            for (int i = 0; i < PointCount; i++)
            {
                _tempData[i] = (Math.Sin((i * 0.02) + _phaseShift) * 20) + 100 + (_rand.NextDouble() * 4 - 2);
                _lastVoltage += (_rand.NextDouble() - 0.5) * 3;

                if (_lastVoltage > 80) _lastVoltage -= 5;
                if (_lastVoltage < -80) _lastVoltage += 5;
                _voltageData[i] = _lastVoltage;
            }
        }

        private void UpdateOtherPlots()
        {
            // 1. Heatmap Updates
            int hotX = _rand.Next(0, HeatmapSize);
            int hotY = _rand.Next(0, HeatmapSize);
            _heatmapData[hotY, hotX] = 100;
            for (int y = 0; y < HeatmapSize; y++)
                for (int x = 0; x < HeatmapSize; x++)
                    _heatmapData[y, x] = Math.Max(0, _heatmapData[y, x] - 3);
            _heatmapPlottable?.Update();

            if (_systemBarPlot != null)
            {
                // 2. Bar Chart: Dynamic Values AND Dynamic Colors
                for (int i = 0; i < _barValues.Length; i++)
                {
                    // Jitter the values
                    _barValues[i] += (_rand.NextDouble() - 0.5) * 15;
                    _barValues[i] = Math.Clamp(_barValues[i], 10, 98);

                    // Update the bar properties
                    var bar = _systemBarPlot.Bars[i];
                    bar.Value = _barValues[i];

                    // Color code based on severity
                    if (bar.Value >= 85)
                        bar.FillColor = Color.FromHex("#EF4444"); // Critical (Red)
                    else if (bar.Value >= 70)
                        bar.FillColor = Color.FromHex("#F59E0B"); // Warning (Yellow)
                    else
                        bar.FillColor = Color.FromHex("#3B82F6"); // Normal (Blue)
                }
             }

            // 3. Radar Chart: Make it "breathe" dynamically
            for (int series = 0; series < 2; series++)
            {
                for (int node = 0; node < 5; node++)
                {
                    _radarValues[series, node] += (_rand.NextDouble() - 0.5) * 10;
                    _radarValues[series, node] = Math.Clamp(_radarValues[series, node], 20, 100);
                }
            }
            // Trigger redraw of the radar
            PlotRadar.Refresh();
        }

        private void PlotTelemetry_MouseMove(object sender, MouseEventArgs e)
        {
            if (_crosshair == null) return;

            // Throttle to 60fps equivalent to prevent SDL3 buffer lag
            if ((DateTime.Now - _lastMouseMoveRender).TotalMilliseconds < 16)
                return;

            _lastMouseMoveRender = DateTime.Now;

            // Fix for Linux display scaling: Multiply by active DPI
            float dpiScale = (float)PlotTelemetry.DisplayScale;
            Point mousePos = e.GetPosition(PlotTelemetry);

            Pixel physicalPixel = new Pixel(
                (float)mousePos.X * dpiScale,
                (float)mousePos.Y * dpiScale
            );

            Coordinates coords = PlotTelemetry.Plot.GetCoordinates(physicalPixel);
            _crosshair.Position = coords;

            PlotTelemetry.Refresh();
        }
    }
}