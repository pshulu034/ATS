using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RFToolkit
{
    /// <summary>
    /// 拟合误差统计信息
    /// </summary>
    public class FittingErrorMetrics
    {
        /// <summary>决定系数 R² (越接近1越好)</summary>
        public double RSquared { get; set; }

        /// <summary>均方根误差 RMSE</summary>
        public double RMSE { get; set; }

        /// <summary>平均绝对误差 MAE</summary>
        public double MAE { get; set; }

        /// <summary>最大绝对误差</summary>
        public double MaxError { get; set; }

        /// <summary>平均相对误差百分比</summary>
        public double MeanRelativeError { get; set; }

        /// <summary>残差平方和 SSE</summary>
        public double SSE { get; set; }

        public override string ToString()
        {
            return $"R² = {RSquared:F6}, RMSE = {RMSE:F6}, MAE = {MAE:F6}, MaxError = {MaxError:F6}";
        }
    }

    /// <summary>
    /// 有理式拟合结果
    /// </summary>
    public class RationalFitResult
    {
        /// <summary>分子多项式系数（从低次到高次）</summary>
        public double[] NumeratorCoeffs { get; set; }

        /// <summary>分母多项式系数（从低次到高次）</summary>
        public double[] DenominatorCoeffs { get; set; }

        /// <summary>分子多项式次数</summary>
        public int NumeratorDegree => NumeratorCoeffs?.Length - 1 ?? 0;

        /// <summary>分母多项式次数</summary>
        public int DenominatorDegree => DenominatorCoeffs?.Length - 1 ?? 0;
    }

    /// <summary>
    /// 向量拟合结果
    /// </summary>
    public class VectorFitResult
    {
        /// <summary>每个分量的拟合系数</summary>
        public List<double[]> ComponentCoeffs { get; set; }

        /// <summary>拟合的多项式次数</summary>
        public int Degree { get; set; }

        /// <summary>向量维度</summary>
        public int Dimension => ComponentCoeffs?.Count ?? 0;
    }

    /// <summary>
    /// 综合拟合算法类
    /// 包含多项式拟合、有理式拟合、向量拟合和误差计算
    /// </summary>
    public class FittingAlgorithms
    {
        #region 多项式拟合

        /// <summary>
        /// 多项式拟合
        /// </summary>
        /// <param name="x">x坐标数组</param>
        /// <param name="y">y坐标数组</param>
        /// <param name="degree">多项式次数</param>
        /// <returns>多项式系数数组，从低次到高次</returns>
        public static double[] PolynomialFit(double[] x, double[] y, int degree)
        {
            ValidateInput(x, y);

            if (x.Length < degree + 1)
                throw new ArgumentException($"数据点数量({x.Length})必须大于等于多项式次数+1({degree + 1})");

            if (degree < 0)
                throw new ArgumentException("多项式次数必须大于等于0");

            int n = x.Length;
            int m = degree + 1;

            // 构建范德蒙德矩阵
            double[,] A = new double[n, m];
            double[] b = new double[n];

            for (int i = 0; i < n; i++)
            {
                b[i] = y[i];
                for (int j = 0; j < m; j++)
                {
                    A[i, j] = Math.Pow(x[i], j);
                }
            }

            // 求解正规方程: A^T * A * coeffs = A^T * b
            double[,] AtA = MatrixMultiplyTranspose(A, A);
            double[] Atb = MatrixVectorMultiplyTranspose(A, b);

            return SolveLinearSystem(AtA, Atb);
        }

        /// <summary>
        /// 使用拟合的多项式计算给定x值的y值
        /// </summary>
        public static double EvaluatePolynomial(double[] coeffs, double x)
        {
            if (coeffs == null || coeffs.Length == 0)
                return 0;

            double result = 0;
            for (int i = 0; i < coeffs.Length; i++)
            {
                result += coeffs[i] * Math.Pow(x, i);
            }
            return result;
        }

        #endregion

        #region 有理式拟合

        /// <summary>
        /// 有理式拟合：拟合形如 P(x)/Q(x) 的函数
        /// 使用迭代最小二乘法
        /// </summary>
        /// <param name="x">x坐标数组</param>
        /// <param name="y">y坐标数组</param>
        /// <param name="numDegree">分子多项式次数</param>
        /// <param name="denDegree">分母多项式次数</param>
        /// <param name="maxIterations">最大迭代次数（默认100）</param>
        /// <param name="tolerance">收敛容差（默认1e-6）</param>
        /// <returns>有理式拟合结果</returns>
        public static RationalFitResult RationalFit(double[] x, double[] y, int numDegree, int denDegree,
            int maxIterations = 100, double tolerance = 1e-6)
        {
            ValidateInput(x, y);

            int n = x.Length;
            int totalParams = numDegree + denDegree + 1;

            if (n < totalParams)
                throw new ArgumentException($"数据点数量({n})必须大于等于参数数量({totalParams})");

            // 初始化：使用多项式拟合作为初始值
            double[] initialDen = new double[denDegree + 1];
            initialDen[0] = 1.0;  // 分母常数项设为1

            // 迭代求解
            double[] numCoeffs = new double[numDegree + 1];
            double[] denCoeffs = (double[])initialDen.Clone();

            double prevError = double.MaxValue;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // 计算当前分母值
                double[] q = new double[n];
                for (int i = 0; i < n; i++)
                {
                    q[i] = EvaluatePolynomial(denCoeffs, x[i]);
                    if (Math.Abs(q[i]) < 1e-10)
                        q[i] = 1e-10;  // 避免除零
                }

                // 构建线性化系统: y*Q(x) = P(x)
                // 即: y*Q(x) - P(x) = 0
                // 线性化为: y*Q_old(x) ≈ P(x) - y*Q_new(x) + y*Q_old(x)

                // 构建矩阵：求解 P(x) 和 Q(x) 的系数
                double[,] A = new double[n, totalParams];
                double[] b = new double[n];

                for (int i = 0; i < n; i++)
                {
                    // P(x) 的系数（前 numDegree+1 列）
                    for (int j = 0; j <= numDegree; j++)
                    {
                        A[i, j] = Math.Pow(x[i], j) / q[i];
                    }

                    // Q(x) 的系数（后 denDegree 列，常数项固定为1）
                    for (int j = 1; j <= denDegree; j++)
                    {
                        A[i, numDegree + j] = -y[i] * Math.Pow(x[i], j) / q[i];
                    }

                    b[i] = y[i];
                }

                // 求解线性系统
                double[,] AtA = MatrixMultiplyTranspose(A, A);
                double[] Atb = MatrixVectorMultiplyTranspose(A, b);

                double[] params_new = SolveLinearSystem(AtA, Atb);

                // 提取系数
                for (int i = 0; i <= numDegree; i++)
                {
                    numCoeffs[i] = params_new[i];
                }

                denCoeffs[0] = 1.0;  // 固定分母常数项为1
                for (int i = 1; i <= denDegree; i++)
                {
                    denCoeffs[i] = params_new[numDegree + i];
                }

                // 检查收敛
                double currentError = CalculateSSE(x, y, (xi) => EvaluateRational(numCoeffs, denCoeffs, xi));
                if (Math.Abs(prevError - currentError) < tolerance)
                    break;

                prevError = currentError;
            }

            return new RationalFitResult
            {
                NumeratorCoeffs = numCoeffs,
                DenominatorCoeffs = denCoeffs
            };
        }

        /// <summary>
        /// 计算有理式的值
        /// </summary>
        public static double EvaluateRational(double[] numCoeffs, double[] denCoeffs, double x)
        {
            double num = EvaluatePolynomial(numCoeffs, x);
            double den = EvaluatePolynomial(denCoeffs, x);

            if (Math.Abs(den) < 1e-10)
                throw new ArithmeticException("分母接近零，无法计算");

            return num / den;
        }

        #endregion

        #region 向量拟合

        /// <summary>
        /// 向量拟合：对多维向量数据进行多项式拟合
        /// </summary>
        /// <param name="x">x坐标数组</param>
        /// <param name="vectors">向量数组，每个元素是一个向量（double数组）</param>
        /// <param name="degree">多项式次数</param>
        /// <returns>向量拟合结果</returns>
        public static VectorFitResult VectorFit(double[] x, double[][] vectors, int degree)
        {
            if (x == null || vectors == null)
                throw new ArgumentNullException("输入参数不能为空");

            if (x.Length != vectors.Length)
                throw new ArgumentException("x数组和向量数组长度必须相等");

            if (vectors.Length == 0)
                throw new ArgumentException("向量数组不能为空");

            int dimension = vectors[0].Length;
            if (dimension == 0)
                throw new ArgumentException("向量维度不能为零");

            // 验证所有向量维度相同
            for (int i = 1; i < vectors.Length; i++)
            {
                if (vectors[i].Length != dimension)
                    throw new ArgumentException($"所有向量必须具有相同的维度");
            }

            // 对每个分量分别进行多项式拟合
            List<double[]> componentCoeffs = new List<double[]>();

            for (int d = 0; d < dimension; d++)
            {
                double[] y_component = new double[x.Length];
                for (int i = 0; i < x.Length; i++)
                {
                    y_component[i] = vectors[i][d];
                }

                double[] coeffs = PolynomialFit(x, y_component, degree);
                componentCoeffs.Add(coeffs);
            }

            return new VectorFitResult
            {
                ComponentCoeffs = componentCoeffs,
                Degree = degree
            };
        }

        /// <summary>
        /// 计算向量拟合在给定x处的值
        /// </summary>
        public static double[] EvaluateVector(VectorFitResult result, double x)
        {
            if (result == null || result.ComponentCoeffs == null)
                throw new ArgumentNullException("拟合结果不能为空");

            int dimension = result.Dimension;
            double[] vector = new double[dimension];

            for (int d = 0; d < dimension; d++)
            {
                vector[d] = EvaluatePolynomial(result.ComponentCoeffs[d], x);
            }

            return vector;
        }

        #endregion

        #region 误差计算

        /// <summary>
        /// 计算拟合误差指标
        /// </summary>
        /// <param name="x">原始x坐标数组</param>
        /// <param name="y">原始y坐标数组</param>
        /// <param name="predictFunc">预测函数，输入x值返回预测的y值</param>
        /// <returns>拟合误差指标</returns>
        public static FittingErrorMetrics CalculateErrorMetrics(double[] x, double[] y, Func<double, double> predictFunc)
        {
            ValidateInput(x, y);

            if (predictFunc == null)
                throw new ArgumentNullException("预测函数不能为空");

            int n = x.Length;
            double[] predicted = new double[n];
            double[] errors = new double[n];
            double[] relativeErrors = new double[n];

            double yMean = y.Average();
            double sumSquaredError = 0;
            double sumAbsoluteError = 0;
            double maxError = 0;
            double sumRelativeError = 0;
            int validRelativeCount = 0;

            for (int i = 0; i < n; i++)
            {
                predicted[i] = predictFunc(x[i]);
                errors[i] = y[i] - predicted[i];

                double absError = Math.Abs(errors[i]);
                sumSquaredError += errors[i] * errors[i];
                sumAbsoluteError += absError;
                maxError = Math.Max(maxError, absError);

                if (Math.Abs(y[i]) > 1e-10)
                {
                    relativeErrors[i] = absError / Math.Abs(y[i]);
                    sumRelativeError += relativeErrors[i];
                    validRelativeCount++;
                }
            }

            // 计算R²
            double ssTotal = 0;
            for (int i = 0; i < n; i++)
            {
                ssTotal += Math.Pow(y[i] - yMean, 2);
            }

            double rSquared = ssTotal < 1e-10 ? 1.0 : 1 - (sumSquaredError / ssTotal);

            return new FittingErrorMetrics
            {
                RSquared = rSquared,
                RMSE = Math.Sqrt(sumSquaredError / n),
                MAE = sumAbsoluteError / n,
                MaxError = maxError,
                MeanRelativeError = validRelativeCount > 0 ? (sumRelativeError / validRelativeCount) * 100 : 0,
                SSE = sumSquaredError
            };
        }

        /// <summary>
        /// 计算多项式拟合的误差指标
        /// </summary>
        public static FittingErrorMetrics CalculatePolynomialError(double[] x, double[] y, double[] coeffs)
        {
            return CalculateErrorMetrics(x, y, (xi) => EvaluatePolynomial(coeffs, xi));
        }

        /// <summary>
        /// 计算有理式拟合的误差指标
        /// </summary>
        public static FittingErrorMetrics CalculateRationalError(double[] x, double[] y, RationalFitResult result)
        {
            if (result == null)
                throw new ArgumentNullException("拟合结果不能为空");

            return CalculateErrorMetrics(x, y, (xi) => EvaluateRational(result.NumeratorCoeffs, result.DenominatorCoeffs, xi));
        }

        /// <summary>
        /// 计算向量拟合的误差指标
        /// </summary>
        public static FittingErrorMetrics CalculateVectorError(double[] x, double[][] vectors, VectorFitResult result)
        {
            if (result == null || vectors == null)
                throw new ArgumentNullException("输入参数不能为空");

            if (x.Length != vectors.Length)
                throw new ArgumentException("x数组和向量数组长度必须相等");

            int n = x.Length;
            int dimension = result.Dimension;

            double sumSquaredError = 0;
            double sumAbsoluteError = 0;
            double maxError = 0;

            for (int i = 0; i < n; i++)
            {
                double[] predicted = EvaluateVector(result, x[i]);

                for (int d = 0; d < dimension; d++)
                {
                    double error = vectors[i][d] - predicted[d];
                    double absError = Math.Abs(error);
                    sumSquaredError += error * error;
                    sumAbsoluteError += absError;
                    maxError = Math.Max(maxError, absError);
                }
            }

            // 计算R²（对所有分量）
            double ssTotal = 0;
            for (int i = 0; i < n; i++)
            {
                for (int d = 0; d < dimension; d++)
                {
                    double mean = vectors.Select(v => v[d]).Average();
                    ssTotal += Math.Pow(vectors[i][d] - mean, 2);
                }
            }

            double rSquared = ssTotal < 1e-10 ? 1.0 : 1 - (sumSquaredError / ssTotal);

            return new FittingErrorMetrics
            {
                RSquared = rSquared,
                RMSE = Math.Sqrt(sumSquaredError / (n * dimension)),
                MAE = sumAbsoluteError / (n * dimension),
                MaxError = maxError,
                MeanRelativeError = 0,  // 向量拟合不计算相对误差
                SSE = sumSquaredError
            };
        }

        #endregion

        #region 辅助方法

        private static void ValidateInput(double[] x, double[] y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException("输入数组不能为空");

            if (x.Length != y.Length)
                throw new ArgumentException("x和y数组长度必须相等");

            if (x.Length == 0)
                throw new ArgumentException("输入数组不能为空");
        }

        private static double CalculateSSE(double[] x, double[] y, Func<double, double> predictFunc)
        {
            double sse = 0;
            for (int i = 0; i < x.Length; i++)
            {
                double error = y[i] - predictFunc(x[i]);
                sse += error * error;
            }
            return sse;
        }

        private static double[,] MatrixMultiplyTranspose(double[,] A, double[,] B)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);
            int p = B.GetLength(1);

            double[,] result = new double[m, p];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < p; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < n; k++)
                    {
                        sum += A[k, i] * B[k, j];
                    }
                    result[i, j] = sum;
                }
            }

            return result;
        }

        private static double[] MatrixVectorMultiplyTranspose(double[,] A, double[] v)
        {
            int n = A.GetLength(0);
            int m = A.GetLength(1);

            double[] result = new double[m];

            for (int i = 0; i < m; i++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++)
                {
                    sum += A[k, i] * v[k];
                }
                result[i] = sum;
            }

            return result;
        }

        private static double[] SolveLinearSystem(double[,] A, double[] b)
        {
            int n = b.Length;
            double[,] augmented = new double[n, n + 1];

            // 构建增广矩阵
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = A[i, j];
                }
                augmented[i, n] = b[i];
            }

            // 前向消元（带部分主元选择）
            for (int i = 0; i < n; i++)
            {
                // 部分主元选择
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(augmented[k, i]) > Math.Abs(augmented[maxRow, i]))
                        maxRow = k;
                }

                // 交换行
                if (maxRow != i)
                {
                    for (int k = i; k <= n; k++)
                    {
                        double temp = augmented[i, k];
                        augmented[i, k] = augmented[maxRow, k];
                        augmented[maxRow, k] = temp;
                    }
                }

                // 消元
                for (int k = i + 1; k < n; k++)
                {
                    if (Math.Abs(augmented[i, i]) < 1e-10)
                        throw new InvalidOperationException("矩阵奇异，无法求解");

                    double factor = augmented[k, i] / augmented[i, i];
                    for (int j = i; j <= n; j++)
                    {
                        augmented[k, j] -= factor * augmented[i, j];
                    }
                }
            }

            // 回代
            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = augmented[i, n];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= augmented[i, j] * x[j];
                }
                x[i] /= augmented[i, i];
            }

            return x;
        }

        #endregion
    }

    /// <summary>
    /// 测试程序
    /// </summary>
    static class Sample
    {
        public static void Test()
        {
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("综合拟合算法示例");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine();

            // 示例1: 多项式拟合
            Console.WriteLine("示例1: 多项式拟合");
            Console.WriteLine("-".PadRight(70, '-'));
            double[] x1 = { 0, 1, 2, 3, 4, 5 };
            double[] y1 = { 1, 3, 7, 13, 21, 31 };

            int degree1 = 2;
            double[] polyCoeffs = FittingAlgorithms.PolynomialFit(x1, y1, degree1);
            FittingErrorMetrics polyError = FittingAlgorithms.CalculatePolynomialError(x1, y1, polyCoeffs);

            Console.WriteLine($"拟合多项式 (次数={degree1}):");
            Console.WriteLine($"  系数: [{string.Join(", ", polyCoeffs.Select(c => c.ToString("F4")))}]");
            Console.WriteLine($"误差指标: {polyError}");
            Console.WriteLine();

            // 示例2: 有理式拟合
            Console.WriteLine("示例2: 有理式拟合");
            Console.WriteLine("-".PadRight(70, '-'));
            double[] x2 = { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0 };
            double[] y2 = { 2.0, 1.5, 1.2, 1.0, 0.857, 0.75, 0.667, 0.6 };  // 近似 1/x

            int numDegree = 1;
            int denDegree = 1;
            RationalFitResult rationalResult = FittingAlgorithms.RationalFit(x2, y2, numDegree, denDegree);
            FittingErrorMetrics rationalError = FittingAlgorithms.CalculateRationalError(x2, y2, rationalResult);

            Console.WriteLine($"有理式拟合 (分子次数={numDegree}, 分母次数={denDegree}):");
            Console.WriteLine($"  分子系数: [{string.Join(", ", rationalResult.NumeratorCoeffs.Select(c => c.ToString("F4")))}]");
            Console.WriteLine($"  分母系数: [{string.Join(", ", rationalResult.DenominatorCoeffs.Select(c => c.ToString("F4")))}]");
            Console.WriteLine($"误差指标: {rationalError}");
            Console.WriteLine();

            // 示例3: 向量拟合
            Console.WriteLine("示例3: 向量拟合（2D向量）");
            Console.WriteLine("-".PadRight(70, '-'));
            double[] x3 = { 0, 1, 2, 3, 4 };
            double[][] vectors = new double[][]
            {
                new double[] { 0, 0 },
                new double[] { 1, 2 },
                new double[] { 2, 4 },
                new double[] { 3, 6 },
                new double[] { 4, 8 }
            };

            int degree3 = 1;
            VectorFitResult vectorResult = FittingAlgorithms.VectorFit(x3, vectors, degree3);
            FittingErrorMetrics vectorError = FittingAlgorithms.CalculateVectorError(x3, vectors, vectorResult);

            Console.WriteLine($"向量拟合 (次数={degree3}, 维度={vectorResult.Dimension}):");
            for (int d = 0; d < vectorResult.Dimension; d++)
            {
                Console.WriteLine($"  分量{d}系数: [{string.Join(", ", vectorResult.ComponentCoeffs[d].Select(c => c.ToString("F4")))}]");
            }
            Console.WriteLine($"误差指标: {vectorError}");
            Console.WriteLine();

            // 示例4: 向量拟合（3D向量）
            Console.WriteLine("示例4: 向量拟合（3D向量）");
            Console.WriteLine("-".PadRight(70, '-'));
            double[] x4 = { 0, 1, 2, 3 };
            double[][] vectors3D = new double[][]
            {
                new double[] { 0, 0, 0 },
                new double[] { 1, 1, 1 },
                new double[] { 2, 4, 8 },
                new double[] { 3, 9, 27 }
            };

            int degree4 = 2;
            VectorFitResult vectorResult3D = FittingAlgorithms.VectorFit(x4, vectors3D, degree4);
            FittingErrorMetrics vectorError3D = FittingAlgorithms.CalculateVectorError(x4, vectors3D, vectorResult3D);

            Console.WriteLine($"向量拟合 (次数={degree4}, 维度={vectorResult3D.Dimension}):");
            for (int d = 0; d < vectorResult3D.Dimension; d++)
            {
                Console.WriteLine($"  分量{d}系数: [{string.Join(", ", vectorResult3D.ComponentCoeffs[d].Select(c => c.ToString("F4")))}]");
            }
            Console.WriteLine($"误差指标: {vectorError3D}");
            Console.WriteLine();

            // 测试预测
            Console.WriteLine("预测测试:");
            Console.WriteLine("-".PadRight(70, '-'));
            double testX = 2.5;
            double polyPred = FittingAlgorithms.EvaluatePolynomial(polyCoeffs, testX);
            double rationalPred = FittingAlgorithms.EvaluateRational(rationalResult.NumeratorCoeffs, rationalResult.DenominatorCoeffs, testX);
            double[] vectorPred = FittingAlgorithms.EvaluateVector(vectorResult, testX);

            Console.WriteLine($"x = {testX}:");
            Console.WriteLine($"  多项式预测: {polyPred:F4}");
            Console.WriteLine($"  有理式预测: {rationalPred:F4}");
            Console.WriteLine($"  向量预测: [{string.Join(", ", vectorPred.Select(v => v.ToString("F4")))}]");
            Console.WriteLine();

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}