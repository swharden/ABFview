using System;
using System.Collections.Generic;
using System.Text;

namespace ABFview
{
    /* This class creates ScottPlots to display ABF data */

    public static class EphysPlot
    {

        public static void Sweep(ScottPlot.Plot plt, ABFsharp.ABF abf, int sweepNumber, bool derivative)
        {
            plt.Clear();

            if (!derivative)
            {
                var sweep = abf.GetSweep((int)sweepNumber);
                plt.PlotSignal(sweep.values, abf.info.sampleRate, color: System.Drawing.Color.Blue);
                plt.YLabel("Membrane Potential (mV)");
            }
            else
            {
                var sweep = abf.GetSweep((int)sweepNumber);
                plt.PlotSignal(Diff(sweep.values), abf.info.sampleRate, color: System.Drawing.Color.Red);
                plt.YLabel("Voltage Derivative (mV/ms)");
            }

            plt.Title($"Sweep {sweepNumber + 1} of {abf.info.sweepCount}");
            plt.XLabel("Sweep Time (seconds)");
            plt.AxisAuto(0, .1);
        }

        public static void Stack(ScottPlot.Plot plt, ABFsharp.ABF abf, double yOffset, bool derivative)
        {
            plt.Clear();

            if (derivative)
            {
                for (int i = 0; i < abf.info.sweepCount; i++)
                {
                    var sweep = abf.GetSweep(i);
                    plt.PlotSignal(Diff(sweep.valuesCopy), abf.info.sampleRate, color: System.Drawing.Color.Red, yOffset: i * yOffset);
                }
                plt.YLabel("Membrane Potential (mV)");
            }
            else
            {
                for (int i = 0; i < abf.info.sweepCount; i++)
                {
                    var sweep = abf.GetSweep(i);
                    plt.PlotSignal(sweep.valuesCopy, abf.info.sampleRate, color: System.Drawing.Color.Blue, yOffset: i * yOffset);
                }
            }

            plt.Title($"Stacked Sweeps");
            plt.XLabel("Sweep Time (seconds)");
            plt.AxisAuto(0, .1);
        }

        public static void Full(ScottPlot.Plot plt, ABFsharp.ABF abf, bool derivative)
        {
            plt.Clear();

            if (!derivative)
            {
                plt.PlotSignal(abf.GetFullRecording(), abf.info.sampleRate * 60, color: System.Drawing.Color.Blue);
                plt.YLabel("Membrane Potential (mV)");
                plt.XLabel("Experiment Time (Minutes)");
            }
            else
            {
                plt.PlotSignal(Diff(abf.GetFullRecording()), abf.info.sampleRate * 60, color: System.Drawing.Color.Red);
                plt.YLabel("Voltage Derivative (mV/ms)");
                plt.XLabel("Experiment Time (Minutes)");
            }

            plt.Title($"Full Recording");
            plt.AxisAuto(0, .1);
        }

        private static double[] Diff(double[] source, int deltaPoints = 1)
        {
            double[] deriv = new double[source.Length];
            for (int i = deltaPoints; i < source.Length; i++)
                deriv[i] = source[i] - source[i - deltaPoints];
            for (int i = 0; i < deltaPoints; i++)
                deriv[i] = deriv[deltaPoints];
            return deriv;
        }
    }
}
