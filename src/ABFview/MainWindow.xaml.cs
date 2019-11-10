using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ABFview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string demoAbfFolder = System.IO.Path.GetFullPath("../../../../../dev/abfs/");
            string demoAbfFile = System.IO.Path.Combine(demoAbfFolder, "ic-ramp-ap.abf");

            if (IntPtr.Size != 4)
                throw new Exception("Must build as 32-bit (x86)");

            gbSweepNav.Visibility = Visibility.Collapsed;
            gbSweepMeasure.Visibility = Visibility.Collapsed;
            gbStackSettings.Visibility = Visibility.Collapsed;
            gbView.Visibility = Visibility.Collapsed;
            btnNextAbf.Visibility = Visibility.Collapsed;
            btnPreviousAbf.Visibility = Visibility.Collapsed;

            wpfPlot1.plt.AxisAuto();
            wpfPlot1.Render();

            LoadAbf(demoAbfFile);
        }

        ABFsharp.ABF abf;
        private void LoadAbf(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return;

            if (abf != null)
            {
                Debug.WriteLine($"Closing old ABF...");
                abf.Close();
            }

            Debug.WriteLine($"Loading ABF: {filePath}");
            abf = new ABFsharp.ABF(filePath);
            Title = $"ABFview {abf.info.fileName}";
            SetViewMode("sweep");
            SetSweep(0);

            gbView.Visibility = Visibility.Visible;
            btnNextAbf.Visibility = Visibility.Visible;
            btnPreviousAbf.Visibility = Visibility.Visible;

            cbMeasure_Unhecked(null, null);
        }

        private double[] Diff(double[] source, int deltaPoints = 1)
        {
            double[] deriv = new double[source.Length];
            for (int i = deltaPoints; i < source.Length; i++)
                deriv[i] = source[i] - source[i - deltaPoints];
            for (int i = 0; i < deltaPoints; i++)
                deriv[i] = deriv[deltaPoints];
            return deriv;
        }

        private void SetSweep(int? sweepNumber = null)
        {
            if (sweepNumber is null)
                sweepNumber = abf.sweep.number;

            abf.SetSweep((int)sweepNumber);

            wpfPlot1.plt.Clear();
            wpfPlot1.plt.Title($"Sweep {sweepNumber + 1} of {abf.info.sweepCount}");

            if (cbDerivative.IsChecked == false)
            {
                wpfPlot1.plt.PlotSignal(abf.sweep.values, abf.info.sampleRate, color: System.Drawing.Color.Blue);
                wpfPlot1.plt.YLabel("Membrane Potential (mV)");
            }
            else
            {
                wpfPlot1.plt.PlotSignal(Diff(abf.sweep.values), abf.info.sampleRate, color: System.Drawing.Color.Red);
                wpfPlot1.plt.YLabel("Voltage Derivative (mV/ms)");
            }

            wpfPlot1.plt.XLabel("Sweep Time (seconds)");
            wpfPlot1.plt.AxisAuto(0, .1);
            wpfPlot1.Render();

            lblSweep.Content = $"{sweepNumber + 1} of {abf.info.sweepCount}";
        }

        private void SetStack()
        {
            if (!double.TryParse(tbVertSep.Text, out double yOffset))
                yOffset = 0;

            wpfPlot1.plt.Clear();
            wpfPlot1.plt.Title($"Stacked Sweeps");

            if (cbDerivative.IsChecked == true)
            {
                for (int i = 0; i < abf.info.sweepCount; i++)
                {
                    abf.SetSweep(i);
                    wpfPlot1.plt.PlotSignal(Diff(abf.sweep.valuesCopy), abf.info.sampleRate, color: System.Drawing.Color.Red, yOffset: i * yOffset);
                }
                wpfPlot1.plt.YLabel("Membrane Potential (mV)");
            }
            else
            {
                for (int i = 0; i < abf.info.sweepCount; i++)
                {
                    abf.SetSweep(i);
                    wpfPlot1.plt.PlotSignal(abf.sweep.valuesCopy, abf.info.sampleRate, color: System.Drawing.Color.Blue, yOffset: i * yOffset);
                }
            }

            wpfPlot1.plt.XLabel("Sweep Time (seconds)");
            wpfPlot1.plt.AxisAuto(0, .1);
            wpfPlot1.Render();
        }

        private void SetFull()
        {
            wpfPlot1.plt.Clear();

            if (cbDerivative.IsChecked == false)
            {
                wpfPlot1.plt.PlotSignal(abf.GetFullRecording(), abf.info.sampleRate * 60, color: System.Drawing.Color.Blue);
                wpfPlot1.plt.YLabel("Membrane Potential (mV)");
                wpfPlot1.plt.XLabel("Experiment Time (Minutes)");
            }
            else
            {
                wpfPlot1.plt.PlotSignal(Diff(abf.GetFullRecording()), abf.info.sampleRate * 60, color: System.Drawing.Color.Red);
                wpfPlot1.plt.YLabel("Voltage Derivative (mV/ms)");
                wpfPlot1.plt.XLabel("Experiment Time (Minutes)");
            }

            wpfPlot1.plt.Title($"Full Recording");
            wpfPlot1.plt.AxisAuto(0, .1);
            wpfPlot1.Render();
        }

        private void btnSweepPrevious_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(Math.Max(0, abf.sweep.number - 1));
        }

        private void btnSweepNext_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(Math.Min(abf.info.sweepCount - 1, abf.sweep.number + 1));
        }

        private void btnSweepFirst_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(0);
        }

        private void btnSweepLast_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(abf.info.sweepCount - 1);
        }

        private void cbDelta_CheckChanged(object sender, RoutedEventArgs e)
        {
            double x1 = wpfPlot1.plt.Axis()[0];
            double x2 = wpfPlot1.plt.Axis()[1];
            SetViewMode();
            wpfPlot1.plt.Axis(x1: x1, x2: x2);
            wpfPlot1.Render();
        }

        private void SetViewMode(string viewMode = null)
        {
            if (wpfPlot1 is null)
                return;

            if (viewMode is null)
                viewMode = cbView.Text;

            // start by hiding all custom panels
            gbSweepNav.Visibility = Visibility.Collapsed;
            gbSweepMeasure.Visibility = Visibility.Collapsed;
            gbStackSettings.Visibility = Visibility.Collapsed;

            // reveal relevant panels only
            switch (viewMode)
            {
                case "sweep":
                    SetSweep();
                    gbSweepNav.Visibility = Visibility.Visible;
                    gbSweepMeasure.Visibility = Visibility.Visible;
                    break;

                case "stack":
                    SetStack();
                    gbStackSettings.Visibility = Visibility.Visible;
                    break;

                case "full":
                    SetFull();
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void cbView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string viewMode = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
            if (wpfPlot1 != null && viewMode != null)
                SetViewMode(viewMode);
        }

        private void btnLoadAbf_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.DefaultExt = ".abf";
            openFileDlg.Filter = "ABF Files|*.abf";
            if (openFileDlg.ShowDialog() == true)
            {
                string filePath = System.IO.Path.GetFullPath(openFileDlg.FileName);
                LoadAbf(filePath);
            }
        }

        private void tbVertSep_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetViewMode();
        }

        private string GetPathAdjacentAbf(string pathAbf, bool nextAbf = true)
        {
            pathAbf = System.IO.Path.GetFullPath(pathAbf);
            string abfFolder = System.IO.Path.GetDirectoryName(pathAbf);

            string[] abfsInFolder = System.IO.Directory.GetFiles(abfFolder, "*.abf");
            Array.Sort(abfsInFolder);
            for (int i = 0; i < abfsInFolder.Length; i++)
                abfsInFolder[i] = abfsInFolder[i].ToUpper();

            int index = Array.IndexOf(abfsInFolder, pathAbf.ToUpper());

            if (nextAbf)
                index += 1;
            else
                index -= 1;

            if ((index >= 0) && (index < abfsInFolder.Length))
                return abfsInFolder[index];
            else
                return null;
        }

        private void btnPreviousAbf_Click(object sender, RoutedEventArgs e)
        {
            string previousAbf = GetPathAdjacentAbf(abf.info.filePath, nextAbf: false);
            if (previousAbf != null)
                LoadAbf(previousAbf);
        }

        private void btnNextAbf_Click(object sender, RoutedEventArgs e)
        {
            string nextAbf = GetPathAdjacentAbf(abf.info.filePath, nextAbf: true);
            if (nextAbf != null)
                LoadAbf(nextAbf);
        }

        private void cbMeasure_Checked(object sender, RoutedEventArgs e)
        {
            cbBaseline.Visibility = Visibility.Visible;
            cbMeasurement.Visibility = Visibility.Visible;
            btnMeasure.Visibility = Visibility.Visible;

            wpfPlot1.plt.Clear(signalPlots: false);
            wpfPlot1.plt.PlotVLine(.5, System.Drawing.Color.Black, lineWidth: 2, draggable: true);
            wpfPlot1.plt.PlotVLine(.6, System.Drawing.Color.Black, lineWidth: 2, draggable: true);
            wpfPlot1.Render();
        }

        private void cbMeasure_Unhecked(object sender, RoutedEventArgs e)
        {
            cbBaseline.Visibility = Visibility.Collapsed;
            cbMeasurement.Visibility = Visibility.Collapsed;
            btnMeasure.Visibility = Visibility.Collapsed;

            cbBaseline.IsChecked = false;
            wpfPlot1.plt.Clear(signalPlots: false);
            wpfPlot1.Render();
        }

        private void cbBaseline_Checked(object sender, RoutedEventArgs e)
        {
            wpfPlot1.plt.PlotVLine(.1, System.Drawing.Color.Gray, lineWidth: 2, draggable: true);
            wpfPlot1.plt.PlotVLine(.2, System.Drawing.Color.Gray, lineWidth: 2, draggable: true);
            wpfPlot1.Render();
        }

        private void cbBaseline_Unchecked(object sender, RoutedEventArgs e)
        {
            cbMeasure_Checked(null, null);
        }

        private void btnMeasure_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("perform measurement");
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files[0].ToUpper().EndsWith("ABF"))
                    LoadAbf(files[0]);
            }
        }
    }
}
