using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathLib
{
    public static class RfInterpolator
    {
        // ==================== 二分查找 ====================
        public static int FindInsertionPoint(IReadOnlyList<double> sorted, double value)
        {
            if (sorted == null) throw new ArgumentNullException(nameof(sorted));
            if (sorted.Count == 0) return 0;
            if (value <= sorted[0]) return 0;
            if (value >= sorted[^1]) return sorted.Count;

            // 使用 Array.BinarySearch（最稳，支持所有平台）
            int index = Array.BinarySearch(sorted.ToArray(), value);
            return index >= 0 ? index : ~index;  // ~index 就是插入点
        }

        // ==================== 1. 线性插值（最常用） ====================
        public static double Linear(double x0, double y0, double x1, double y1, double x)
            => y0 + (y1 - y0) * (x - x0) / (x1 - x0);

        public static double Linear(IReadOnlyList<double> xData, IReadOnlyList<double> yData, double x)
        {
            if (xData.Count != yData.Count) throw new ArgumentException("xData and yData length mismatch");
            if (xData.Count == 0) throw new ArgumentException("Data cannot be empty");
            if (xData.Count == 1) return yData[0];

            if (x <= xData[0]) return yData[0];
            if (x >= xData[^1]) return yData[^1];

            int i = FindInsertionPoint(xData, x);
            if (i == 0) return yData[0];
            if (i >= xData.Count) return yData[^1];

            return Linear(xData[i - 1], yData[i - 1], xData[i], yData[i], x);
        }

        // ==================== 批量线性插值（性能拉满） ====================
        public static double[] BatchLinear(IReadOnlyList<double> xData, IReadOnlyList<double> yData, IReadOnlyList<double> xTarget)
        {
            var result = new double[xTarget.Count];
            for (int i = 0; i < xTarget.Count; i++)
                result[i] = Linear(xData, yData, xTarget[i]);
            return result;
        }

        // ==================== 2. 对数频率线性插值（dB vs log(f) 神器） ====================
        public static double LogLinear(IReadOnlyList<double> freq, IReadOnlyList<double> valueDb, double targetFreq)
        {
            if (targetFreq <= 0) throw new ArgumentException("Frequency must be positive");
            var logFreq = freq.Select(f => Math.Log10(f)).ToArray();
            return Linear(logFreq, valueDb, Math.Log10(targetFreq));
        }

        // ==================== 3. Akima 插值（Keysight PNA 推荐，永不过冲） ====================
        public static double Akima(IReadOnlyList<double> x, IReadOnlyList<double> y, double xi)
        {
            var spline = new AkimaSpline(x, y);
            return spline.Interpolate(xi);
        }

        // ==================== 4. 邻近插值 ====================
        public static double Nearest(IReadOnlyList<double> xData, IReadOnlyList<double> yData, double x)
        {
            if (xData.Count != yData.Count) throw new ArgumentException("Length mismatch");
            if (xData.Count == 0) throw new ArgumentException("Empty");

            if (x <= xData[0]) return yData[0];
            if (x >= xData[^1]) return yData[^1];

            int i = RfInterpolator.FindInsertionPoint(xData, x);  // 我们之前写好的
            if (i == 0) return yData[0];
            if (i >= xData.Count) return yData[^1];

            // 比较左右两个点，谁近用谁
            double leftDist = x - xData[i - 1];
            double rightDist = xData[i] - x;
            return leftDist <= rightDist ? yData[i - 1] : yData[i];
        }

        // ==================== 5. 2D 双线性插值（校准表必备） ====================
        public static double Bilinear(
            IReadOnlyList<double> xGrid,
            IReadOnlyList<double> yGrid,
            IReadOnlyList<IReadOnlyList<double>> zTable,
            double x, double y)
        {
            if (xGrid.Count != zTable.Count || yGrid.Count != zTable[0].Count)
                throw new ArgumentException("Table dimensions do not match grids");

            // 边界处理
            if (x <= xGrid[0]) return Linear(yGrid, zTable[0], y);
            if (x >= xGrid[^1]) return Linear(yGrid, zTable[^1], y);
            if (y <= yGrid[0]) return Linear(xGrid, zTable.Select(row => row[0]).ToArray(), x);
            if (y >= yGrid[^1]) return Linear(xGrid, zTable.Select(row => row[^1]).ToArray(), x);

            int i = FindInsertionPoint(xGrid, x);
            int j = FindInsertionPoint(yGrid, y);
            i--; j--; // 切换到左下角点

            double x0 = xGrid[i], x1 = xGrid[i + 1];
            double y0 = yGrid[j], y1 = yGrid[j + 1];

            double z00 = zTable[i][j];
            double z10 = zTable[i + 1][j];
            double z01 = zTable[i][j + 1];
            double z11 = zTable[i + 1][j + 1];

            double tx = (x - x0) / (x1 - x0);
            double ty = (y - y0) / (y1 - y0);

            double z0 = z00 * (1 - tx) + z10 * tx;
            double z1 = z01 * (1 - tx) + z11 * tx;
            return z0 * (1 - ty) + z1 * ty;
        }

        public static void Test()
        {
            // 衰减器只能 0.5dB 步进
            var attSteps = new double[] { 0, 0.5, 1.0, 1.5, 2.0, 2.5,3.0,3.5,4.0,4.5,5.0,5.5,6.0 };
            var targetAtt = 4.3;
            double actualAtt = RfInterpolator.Nearest(attSteps, attSteps, targetAtt); // → 17.5

            // 温度补偿表只有 -40, -20, 0, 25, 50, 85℃
            var tempPoints = new double[] { -40, -20, 0, 25, 50, 85 };
            var currentTemp = 63.0;
            double idx = RfInterpolator.Nearest(tempPoints, Enumerable.Range(0, 6).Select(i => (double)i).ToArray(), currentTemp);
            string tableName = idx switch { 0 => "-40", 1 => "-20", 3 => "0", 4 => "50C", 5 => "85C", _ => "50C" }; // 直接用 50℃ 表

        }
    }

    // ==================== Akima 插值核心实现（防过冲） ====================
    internal sealed class AkimaSpline
    {
        private readonly double[] _x, _y, _b, _c, _d;

        public AkimaSpline(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            if (x.Count < 5) throw new ArgumentException("Akima spline requires at least 5 points");
            if (x.Count != y.Count) throw new ArgumentException("x and y must have same length");

            int n = x.Count;
            _x = x.ToArray();
            _y = y.ToArray();

            var slope = new double[n - 1];
            for (int i = 0; i < n - 1; i++)
                slope[i] = (_y[i + 1] - _y[i]) / (_x[i + 1] - _x[i]);

            var weight = new double[n + 4];
            for (int i = 0; i < 4; i++)
                weight[i] = weight[n + i] = double.MaxValue;

            for (int i = 0; i < n - 1; i++)
            {
                double s1 = Math.Abs(slope[i]);
                double s0 = i > 0 ? Math.Abs(slope[i - 1]) : s1;
                double s2 = i < n - 2 ? Math.Abs(slope[i + 1]) : s1;
                double s3 = i < n - 3 ? Math.Abs(slope[i + 2]) : s2;

                weight[i + 2] = (s1 == s0 || s2 == s3) ? double.MaxValue : (s0 + s1) / (s1 + s2);
            }

            var deriv = new double[n];
            for (int i = 0; i < n; i++)
            {
                double w1 = weight[i + 2], w2 = weight[i + 3];
                deriv[i] = Math.Abs(w2 - w1) < 1e-10
                    ? (w1 > 1e5 ? slope[i - 1] : slope[i])
                    : (w1 * slope[i - 1] + w2 * slope[i]) / (w1 + w2);
            }

            _b = new double[n - 1];
            _c = new double[n - 1];
            _d = new double[n - 1];

            for (int i = 0; i < n - 1; i++)
            {
                double h = _x[i + 1] - _x[i];
                double s = slope[i];
                double p = deriv[i];
                double q = deriv[i + 1];

                _b[i] = p;
                _c[i] = (3 * s - 2 * p - q) / h;
                _d[i] = (p + q - 2 * s) / (h * h);
            }
        }

        public double Interpolate(double xi)
        {
            if (xi <= _x[0]) return _y[0];
            if (xi >= _x[^1]) return _y[^1];

            int i = RfInterpolator.FindInsertionPoint(_x, xi);
            i--; // 左区间

            double dx = xi - _x[i];
            return _y[i] + _b[i] * dx + _c[i] * dx * dx + _d[i] * dx * dx * dx;
        }
    }
}
