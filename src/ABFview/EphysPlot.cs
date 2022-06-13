using System.Drawing;
using System.Linq;

namespace ABFview;

public static class EphysPlot
{
    public static void Sweep(ScottPlot.Plot plt, AbfSharp.ABF abf, int sweepNumber, bool derivative)
    {
        plt.Clear();

        double[] values = GetSweepValues(abf, sweepNumber, derivative);
        Color color = derivative ? Color.Red : Color.Blue;
        plt.AddSignal(values, abf.Header.SampleRate, color);

        string title = IsGapFree(abf) ? "Full Gap-Free Recording" : $"Sweep {sweepNumber + 1} of {abf.Header.SweepCount}";
        plt.Title(title);
        plt.XLabel("Sweep Time (seconds)");
        plt.AxisAuto(0, .1);
    }

    public static void Stack(ScottPlot.Plot plt, AbfSharp.ABF abf, double yOffset, bool derivative)
    {
        plt.Clear();

        for (int i = 0; i < abf.Header.SweepCount; i++)
        {
            var sig = plt.AddSignal(
                ys: GetSweepValues(abf, i, derivative),
                sampleRate: abf.Header.SampleRate,
                color: derivative ? Color.Red : Color.Blue);

            sig.OffsetY = i * yOffset;
        }

        plt.Title($"Stacked Sweeps");
        plt.XLabel("Sweep Time (seconds)");
        plt.AxisAuto(0, .1);
    }

    public static void Full(ScottPlot.Plot plt, AbfSharp.ABF abf, bool derivative)
    {
        plt.Clear();
        double[] values = GetAllSweepValues(abf, derivative);
        Color color = derivative ? Color.Red : Color.Blue;
        plt.AddSignal(values, abf.Header.SampleRate * 60, color);
        plt.XLabel("Time (minutes)");
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

    private static double[] GetSweepValues(AbfSharp.ABF abf, int sweepNumber, bool derivative)
    {
        float[] valuesRaw = abf.GetSweep(sweepNumber);
        double[] values = new double[valuesRaw.Length];
        for (int i = 0; i < valuesRaw.Length; i++)
            values[i] = valuesRaw[i];
        return derivative ? Diff(values) : values;
    }

    private static double[] GetAllSweepValues(AbfSharp.ABF abf, bool derivative)
    {
        if (IsGapFree(abf))
        {
            return GetSweepValues(abf, 0, derivative);
        }

        double[][] values = new double[abf.Header.SweepCount][];
        for (int i = 0; i < abf.Header.SweepCount; i++)
        {
            values[i] = GetSweepValues(abf, i, derivative);
        }

        return values.SelectMany(a => a).ToArray();
    }

    public static bool IsGapFree(AbfSharp.ABF abf)
    {
        const int GAP_FREE_MODE = 3;
        return abf.Header.nOperationMode == GAP_FREE_MODE;
    }
}
