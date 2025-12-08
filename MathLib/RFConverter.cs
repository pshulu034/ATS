using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//射频自动化单位转换器
namespace RFToolkit
{
    public static class RfConverter
    {
        // ==================== 功率转换（最常用） ====================
        /// <summary>dBm ↔ 瓦特</summary>
        public static double DbmToWatt(double dbm) => Math.Pow(10, (dbm - 30) / 10.0);
        public static double WattToDbm(double watt) => 10 * Math.Log10(watt) + 30;

        /// <summary>dBm ↔ 毫瓦</summary>
        public static double DbmToMw(double dbm) => Math.Pow(10, dbm / 10.0);
        public static double MwToDbm(double mw) => 10 * Math.Log10(mw);

        // ==================== 电压转换（50Ω 为默认，也支持任意 Z0） ====================
        /// <summary>dBm → RMS 电压（默认 50Ω）</summary>
        public static double DbmToVRms(double dbm, double z0 = 50.0)
            => Math.Sqrt(DbmToWatt(dbm) * z0 * 1000);   // *1000 是因为 Watt → mW 匹配

        /// <summary>dBm → 峰值电压</summary>
        public static double DbmToVPeak(double dbm, double z0 = 50.0)
            => DbmToVRms(dbm, z0) * Math.Sqrt(2);

        /// <summary>dBm → 峰峰值电压（示波器最常用）</summary>
        public static double DbmToVpp(double dbm, double z0 = 50.0)
            => DbmToVPeak(dbm, z0) * 2;

        // 反向转换
        public static double VRmsToDbm(double vRms, double z0 = 50.0)
            => WattToDbm(vRms * vRms / z0);
        public static double VPeakToDbm(double vPeak, double z0 = 50.0)
            => VRmsToDbm(vPeak / Math.Sqrt(2), z0);
        public static double VppToDbm(double vpp, double z0 = 50.0)
            => VRmsToDbm(vpp / (2 * Math.Sqrt(2)), z0);

        // ==================== dB/dBc/dBc/Hz 等常用 ====================
        public static double RatioToDb(double ratio) => 10 * Math.Log10(ratio);
        public static double DbToRatio(double db) => Math.Pow(10, db / 10.0);
        public static double RatioToDb20(double ratio) => 20 * Math.Log10(ratio); // 电压/场强用

        // dBc/Hz → 绝对功率（常用于相位噪声）
        public static double DbcHzToDbm(double dbcHz, double rbwHz = 1.0, double carrierDbm = 30.0)
            => dbcHz + 10 * Math.Log10(rbwHz) + carrierDbm;

        // ==================== 频率/波长转换 ====================
        public static double FreqToWavelengthMeter(double freqHz, double c = 299792458.0)
            => c / freqHz;
        public static double FreqToWavelengthMm(double freqHz)
            => FreqToWavelengthMeter(freqHz) * 1000;
        public static double GhzToMm(double freqGhz)
            => 300.0 / freqGhz;   // 近似，空气中

        // ==================== 温度转换（产线常用） ====================
        public static double CelsiusToKelvin(double c) => c + 273.15;
        public static double KelvinToCelsius(double k) => k - 273.15;
        public static double CelsiusToFahrenheit(double c) => c * 9 / 5.0 + 32;
        public static double FahrenheitToCelsius(double f) => (f - 32) * 5 / 9.0;

        // ==================== VSWR ↔ Return Loss ↔ |Γ| ====================
        public static double VswrToReturnLossDb(double vswr)
            => vswr <= 1.0 ? double.PositiveInfinity : 20 * Math.Log10((vswr - 1) / (vswr + 1));
        public static double ReturnLossDbToVswr(double rlDb)
            => rlDb >= 100 ? 1.0 : (1 + Math.Pow(10, -rlDb / 20.0)) / (1 - Math.Pow(10, -rlDb / 20.0));
        public static double VswrToGamma(double vswr)
            => vswr <= 1.0 ? 0.0 : (vswr - 1.0) / (vswr + 1.0);
        public static double GammaToVswr(double gamma)
            => Math.Abs(gamma) >= 1.0 ? double.PositiveInfinity : (1 + Math.Abs(gamma)) / (1 - Math.Abs(gamma));

    }
}
