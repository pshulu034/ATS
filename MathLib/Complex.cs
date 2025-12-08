using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

/// <summary>
/// 高性能、可序列化的复数结构体，支持射频工程常用格式（j 在前）、i/j 切换、JSON/二进制序列化
/// </summary>
[Serializable]
[JsonConverter(typeof(ComplexJsonConverter))]
public struct Complex : IEquatable<Complex>, IFormattable, ISerializable
{
    public double Real { get; }
    public double Imag { get; }


    // 常用静态常量
    public static Complex Zero => new Complex(0, 0);
    public static Complex One => new Complex(1, 0);
    public static Complex I => new Complex(0, 1);
    public static Complex NaN => new Complex(double.NaN, double.NaN);

    //虚数单位
    public static ImaginaryUnit DefaultUnit { get; set; } = ImaginaryUnit.J;  // 全局默认虚数单位（射频默认 J）
    public ImaginaryUnit Unit { get; init; } = DefaultUnit;
    private string GetUnitSymbol() => Unit == ImaginaryUnit.I ? "i" : "j";

    //构造函数
    public Complex() : this(0, 0) { }
    public Complex(double real) : this(real, 0) { }
    public Complex(double real, double imag, ImaginaryUnit unit = ImaginaryUnit.J)
    {
        Real = real;
        Imag = imag;
        Unit = unit;
    }
    public Complex(double real, double imag) : this(real, imag, DefaultUnit) { }

    //复数四则运算
    public static Complex operator +(Complex a, Complex b) => new Complex(a.Real + b.Real, a.Imag + b.Imag, a.Unit);
    public static Complex operator -(Complex a, Complex b) => new Complex(a.Real - b.Real, a.Imag - b.Imag, a.Unit);
    public static Complex operator *(Complex a, Complex b)
        => new Complex(a.Real * b.Real - a.Imag * b.Imag,
                       a.Real * b.Imag + a.Imag * b.Real, a.Unit);
    public static Complex operator /(Complex a, Complex b)
    {
        double denominator = b.Real * b.Real + b.Imag * b.Imag;
        if (denominator == 0) return NaN;
        return new Complex(
            (a.Real * b.Real + a.Imag * b.Imag) / denominator,
            (a.Imag * b.Real - a.Real * b.Imag) / denominator, a.Unit);
    }

    // 标量乘除
    public static Complex operator *(Complex a, double b) => new Complex(a.Real * b, a.Imag * b, a.Unit);
    public static Complex operator *(double a, Complex b) => b * a;
    public static Complex operator /(Complex a, double b) => new Complex(a.Real / b, a.Imag / b, a.Unit);

    // 隐式转换 double -> Complex   
    public static implicit operator Complex(double real) => new Complex(real, 0);       

    //常用运算
    public double Magnitude => Math.Sqrt(Real * Real + Imag * Imag);
    public double Phase => Math.Atan2(Imag, Real);
    public double MagnitudeSquared => Real * Real + Imag * Imag;
    public Complex Conjugate => new Complex(Real, -Imag, Unit);
    public static Complex FromPolar(double magnitude, double phase, ImaginaryUnit unit = ImaginaryUnit.J)
        => new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase), unit);

    // ==================== 其它运算 ====================
    public static Complex operator -(Complex a) => new Complex(-a.Real, -a.Imag, a.Unit);

    public static Complex Pow(Complex value, double exponent)
    {
        if (value == Zero) return Zero;
        double r = Math.Pow(value.Magnitude, exponent);
        double theta = value.Phase * exponent;
        return FromPolar(r, theta, value.Unit);
    }

    public static Complex Exp(Complex z)
        => FromPolar(Math.Exp(z.Real), z.Imag, z.Unit);

    public static Complex Log(Complex z)
        => new Complex(Math.Log(z.Magnitude), z.Phase, z.Unit);

    // ==================== 相等性 ====================
    public bool Equals(Complex other)
        => Real.Equals(other.Real) && Imag.Equals(other.Imag);

    public override bool Equals(object? obj) => obj is Complex c && Equals(c);

    public override int GetHashCode() => HashCode.Combine(Real, Imag);

    public static bool operator ==(Complex left, Complex right) => left.Equals(right);
    public static bool operator !=(Complex left, Complex right) => !left.Equals(right);

    // ==================== ToString 支持 i/j 切换 ====================
    public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

    /// <summary>
    /// 带格式化参数的重载（兼容 IFormattable）
    /// 使用方式：z.ToString("RF4") → 保留4位小数
    ///           z.ToString("RF2") → 保留2位小数
    ///           z.ToString("RF")  → 默认4位
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format)) format = "G";
        formatProvider ??= CultureInfo.CurrentCulture;

        // 解析 RF 后面跟着的数字，如 "RF3" → roundDigits = 3
        if (format.StartsWith("RF", StringComparison.OrdinalIgnoreCase))
        {
            string digitPart = format.Substring(2);
            int digits = 4; // 默认4位（和仪器一致）
            if (int.TryParse(digitPart, out int parsed))
                digits = parsed;
            return ToStringRf(digits);
        }

        return format.ToUpperInvariant() switch
        {
            "G" or "g" => ToStringStandard(),
            "RECT" => $"{Real:R} + {Imag:R}{GetUnitSymbol()}",
            "POLAR" => $"{Magnitude:R} ∠ {Phase * 180 / Math.PI:R}°",
            _ => ToStringStandard()
        };
    }

    private string ToStringStandard()
    {
        if (double.IsNaN(Real) || double.IsNaN(Imag)) return "NaN";
        if (Imag == 0) return Real.ToString("G");
        if (Real == 0) return Imag == 1 ? GetUnitSymbol() : Imag == -1 ? $"-{GetUnitSymbol()}" : $"{Imag:G}{GetUnitSymbol()}";

        string sign = Imag > 0 ? " + " : " - ";
        string imagPart = Math.Abs(Imag) == 1 ? "" : $"{Math.Abs(Imag):G}";
        return $"{Real:G}{sign}{imagPart}{GetUnitSymbol()}";
    }

    /// <summary>
    /// 射频工程师专用格式（j 在前），支持四舍五入小数位数控制
    /// 示例：
    ///   0.1234 + j0.5678  →  "0.123+j0.568"   (roundDigits = 3)
    ///   0 + j27.336       →  "j27.34"         (roundDigits = 2)
    ///   0 - j0.95         →  "－j0.95"
    /// </summary>
    /// <param name="roundDigits">保留小数位数，-1 表示不四舍五入（显示完整精度）</param>
    public string ToStringRf(int roundDigits = 4)
    {
        if (double.IsNaN(Real) || double.IsNaN(Imag)) return "NaN";
        if (Real == 0 && Imag == 0) return "0";

        // 四舍五入函数
        double Round(double v) => roundDigits < 0 ? v : Math.Round(v, roundDigits);

        double re = Round(Real);
        double im = Round(Math.Abs(Imag));   // 先取绝对值用于显示

        string unit = Unit == ImaginaryUnit.I ? "i" : "j";

        // 纯虚数
        if (re == 0)
            return Imag >= 0 ? $"{unit}{im:G}" : $"－{unit}{im:G}";

        // 纯实数
        if (Imag == 0) return $"{re:G}";

        // 实部 + 虚部
        string sign = Imag >= 0 ? "+" : "－";
        return $"{re:G}{sign}{unit}{im:G}";
    }


    // ==================== 解析支持 i 和 j ====================
    public static Complex Parse(string s) => Parse(s, DefaultUnit);

    public static Complex Parse(string s, ImaginaryUnit unit)
    {
        if (TryParse(s, unit, out var result)) return result;
        throw new FormatException($"无法解析为复数: {s}");
    }

    public static bool TryParse(string s, out Complex result) => TryParse(s, DefaultUnit, out result);

    public static bool TryParse(string s, ImaginaryUnit unit, out Complex result)
    {
        result = Zero;
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim().Replace(" ", "").ToLowerInvariant();

        // 支持的单位符号
        string unitSymbol = unit == ImaginaryUnit.I ? "i" : "j";

        // 正则匹配：支持 j在前、标准格式、纯实/虚数
        var regex = new Regex(
            @"^([+-]?\d*\.?\d+(?:[eE][+-]?\d+)?)?\s*([+-]?\s*)?([ij]?)(\d*\.?\d+(?:[eE][+-]?\d+)?)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        var match = regex.Match(s);
        if (!match.Success) return false;

        double re = 0, im = 0;

        // 实部
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
        {
            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out re))
                return false;
        }

        // 符号
        string sign = match.Groups[2].Value.Replace(" ", "");
        bool negative = sign.Contains("-");

        // 单位
        string parsedUnit = match.Groups[3].Value.ToLowerInvariant();
        if (!string.IsNullOrEmpty(parsedUnit) && parsedUnit != unitSymbol) return false;  // 单位不匹配

        // 虚部数值
        string imStr = match.Groups[4].Value;
        if (string.IsNullOrEmpty(imStr))
        {
            if (!string.IsNullOrEmpty(parsedUnit)) im = 1;  // 如 "j" 或 "+j"
        }
        else
        {
            if (!double.TryParse(imStr, NumberStyles.Any, CultureInfo.InvariantCulture, out im))
                return false;
        }

        if (negative) im = -im;

        // 如果没有实部但有单位，交换为纯虚数
        if (string.IsNullOrEmpty(match.Groups[1].Value) && !string.IsNullOrEmpty(parsedUnit))
        {
            re = 0;
        }

        result = new Complex(re, im, unit);
        return true;
    }

    // ==================== 二进制序列化 ====================
    private Complex(SerializationInfo info, StreamingContext context)
    {
        Real = info.GetDouble(nameof(Real));
        Imag = info.GetDouble(nameof(Imag));
        Unit = (ImaginaryUnit)info.GetInt32(nameof(Unit));  // 支持序列化单位
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Real), Real);
        info.AddValue(nameof(Imag), Imag);
        info.AddValue(nameof(Unit), (int)Unit);
    }

    // ==================== JSON 序列化支持 ====================
    public class ComplexJsonConverter : JsonConverter<Complex>
    {
        public override Complex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Parse(reader.GetString()!);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            double real = 0, imag = 0;
            ImaginaryUnit unit = DefaultUnit;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Complex(real, imag, unit);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string prop = reader.GetString()!.ToLowerInvariant();
                    reader.Read();
                    if (prop is "real" or "re") real = reader.GetDouble();
                    if (prop is "imag" or "im" or "j") imag = reader.GetDouble();
                    if (prop == "unit") unit = reader.GetString()!.ToLowerInvariant() == "i" ? ImaginaryUnit.I : ImaginaryUnit.J;
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Complex value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("real", value.Real);
            writer.WriteNumber("imag", value.Imag);
            writer.WriteString("unit", value.Unit.ToString().ToLowerInvariant());
            writer.WriteEndObject();
            // 或者直接写字符串：writer.WriteStringValue(value.ToStringRf());
        }
    }

}

public enum ImaginaryUnit
{
    I,
    J
}