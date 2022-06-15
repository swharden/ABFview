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
        string units = derivative ? abf.Header.sADCUnits[0] + "/ms" : abf.Header.sADCUnits[0];

        plt.Title(title);
        plt.YLabel($"{abf.Header.sADCChannelName[0]} ({units})");
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

        string units = derivative ? abf.Header.sADCUnits[0] + "/ms" : abf.Header.sADCUnits[0];
        plt.YLabel($"{abf.Header.sADCChannelName[0]} ({units})");
        plt.XLabel("Sweep Time (seconds)");
        plt.AxisAuto(0, .1);
    }

    public static void Full(ScottPlot.Plot plt, AbfSharp.ABF abf, bool derivative)
    {
        plt.Clear();
        double[] values = GetFullRecording(abf, derivative);
        Color color = derivative ? Color.Red : Color.Blue;
        plt.AddSignal(values, abf.Header.SampleRate * 60, color);
        plt.XLabel("Time (minutes)");
        string units = derivative ? abf.Header.sADCUnits[0] + "/ms" : abf.Header.sADCUnits[0];
        plt.YLabel($"{abf.Header.sADCChannelName[0]} ({units})");
        plt.Title($"Full Recording");
        plt.AxisAuto(0, .1);
    }

    private static double[] Diff(double[] source, double scale = 1, int deltaPoints = 1)
    {
        double[] deriv = new double[source.Length];
        for (int i = deltaPoints; i < source.Length; i++)
            deriv[i] = (source[i] - source[i - deltaPoints]) * scale;
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
        return derivative ? Diff(values, abf.Header.SampleRate / 1000) : values;
    }

    private static double[] GetFullRecording(AbfSharp.ABF abf, bool derivative)
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

    public static void SaveCSV(AbfSharp.ABF abf, string filePath)
    {
        string yName = $"{abf.Header.sADCChannelName[0]} ({abf.Header.sADCUnits[0]})";
        string xName = "Time (seconds)";
        double[] ys = GetFullRecording(abf, false);

        System.Text.StringBuilder sb = new();

        sb.AppendLine($"\"{xName}\", \"{yName}\"");

        for (int i = 0; i < ys.Length; i++)
        {
            double x = i / abf.Header.SampleRate;
            sb.AppendLine($"{x:0.000000}, {ys[i]}");
        }

        System.IO.File.WriteAllText(filePath, sb.ToString());
    }
}
