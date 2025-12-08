using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathLib
{
    /// <summary>
    /// 射频常用计算核心套件（三兄弟）
    /// 1. RfLoadForwardCalculator      → 已知负载阻抗，正向推一切（仿真/设计用）
    /// 2. RfViPhaseCalculator          → 已知负载端电压+电流+相位差（电压/电流探头最常用）
    /// 3. RfVfVrPhaseCalculator        → 已知双向耦合器检波电压 Vf/Vr + 相位（双向检波板最常用）
    /// </summary>
    public static class RfCalc
    {
        // ====================== 1. 已知负载阻抗 → 正向计算 ======================
        public sealed class RfLoadForwardCalculator
        {
            public Complex LoadImpedance { get; init; }
            public double Z0 { get; init; } = 50.0;
            public double ForwardPowerW { get; init; } = 1.0;   // 入射功率（W）

            public RfLoadForwardCalculator(Complex loadImpedance, double z0 = 50.0, double forwardPowerW = 1.0)
            {
                LoadImpedance = loadImpedance;
                Z0 = z0;
                ForwardPowerW = forwardPowerW;
            }

            public Complex Gamma => (LoadImpedance - Z0) / (LoadImpedance + Z0);
            public double VSWR => Gamma.Magnitude >= 1 ? double.PositiveInfinity : (1 + Gamma.Magnitude) / (1 - Gamma.Magnitude);
            public double ReflectedPowerW => ForwardPowerW * Gamma.MagnitudeSquared;
            public double DeliveredPowerW => ForwardPowerW - ReflectedPowerW;
            public Complex VoltagePeak => new Complex(Math.Sqrt(2 * ForwardPowerW * Z0), 0) * (1 + Gamma);
            public Complex CurrentPeak => VoltagePeak / LoadImpedance;
        }

        // ====================== 2. 已知电压+电流+相位差（产线王者） ======================
        public sealed class RfViPhaseCalculator
        {
            public double VoltageRms { get; }
            public double CurrentRms { get; }
            public double PhaseDeg { get; }        // V 超前 I 的角度（°），感性为正，容性为负
            public double Z0 { get; }

            public RfViPhaseCalculator(double voltageRms, double currentRms, double phaseDeg, double z0 = 50.0)
            {
                VoltageRms = voltageRms;
                CurrentRms = currentRms;
                PhaseDeg = phaseDeg;
                Z0 = z0;
            }

            private double PhaseRad => PhaseDeg * Math.PI / 180.0;
            public Complex LoadImpedance => CurrentRms == 0 ? Complex.NaN : Complex.FromPolar(VoltageRms / CurrentRms, PhaseRad);
            public Complex Gamma => (LoadImpedance - Z0) / (LoadImpedance + Z0);
            public double VSWR => Gamma.Magnitude >= 1 ? double.PositiveInfinity : (1 + Gamma.Magnitude) / (1 - Gamma.Magnitude);
            public double ActivePowerW => VoltageRms * CurrentRms * Math.Cos(PhaseRad);
            public double ReflectedPowerW => ActivePowerW * Gamma.MagnitudeSquared / (1 - Gamma.MagnitudeSquared);
            public double ForwardPowerW => ActivePowerW / (1 - Gamma.MagnitudeSquared);
            public double DeliveredPowerW => ActivePowerW;
            public double PowerFactor => Math.Cos(PhaseRad);
        }

        // ====================== 3. 已知双向检波器 Vf、Vr + 相位差（第二常用） ======================
        public sealed class RfVfVrPhaseCalculator
        {
            public double Vf { get; }   // 正向检波电压（V）
            public double Vr { get; }   // 反向检波电压（V）
            public double PhaseDeg { get; }     // Vf 与 Vr 的相位差（°）
            public double CouplingFactorDb { get; }
            public double Z0 { get; }

            public RfVfVrPhaseCalculator(double vf, double vr, double phaseDeg, double couplingDb = 30.0, double z0 = 50.0)
            {
                Vf = vf; Vr = vr; PhaseDeg = phaseDeg; CouplingFactorDb = couplingDb; Z0 = z0;
            }

            private double Scale => Math.Pow(10, CouplingFactorDb / 20.0);
            private double VfMain => Vf * Scale;
            private double VrMain => Vr * Scale;
            private double PhiRad => PhaseDeg * Math.PI / 180.0;

            public Complex Gamma => new Complex(VrMain / VfMain * Math.Cos(PhiRad), VrMain / VfMain * Math.Sin(PhiRad));
            public Complex LoadImpedance => Z0 * (1 + Gamma) / (1 - Gamma);
            public double VSWR => Gamma.Magnitude >= 1 ? double.PositiveInfinity : (1 + Gamma.Magnitude) / (1 - Gamma.Magnitude);
            public double ForwardPowerW => VfMain * VfMain / (2 * Z0);
            public double ReflectedPowerW => VrMain * VrMain / (2 * Z0);
            public double DeliveredPowerW => ForwardPowerW - ReflectedPowerW;
        }

        public static void Test()
        {
            Console.WriteLine("=== 1. 已知负载阻抗 ===");
            var c1 = new RfCalc.RfLoadForwardCalculator(new Complex(75, 25));
            Console.WriteLine($"Gamma = {c1.Gamma.ToStringRf(4)}");
            Console.WriteLine($"VSWR = {c1.VSWR:F3}\n");

            Console.WriteLine("=== 2. 电压电流探头（产线最常用） ===");
            var c2 = new RfCalc.RfViPhaseCalculator(28.5, 0.42, 18.7);
            Console.WriteLine($"Z = {c2.LoadImpedance.ToStringRf(3)} Ω");
            Console.WriteLine($"VSWR = {c2.VSWR:F3}");
            Console.WriteLine($"有功功率 = {c2.ActivePowerW:F2} W\n");

            Console.WriteLine("=== 3. 双向检波板（Vf/Vr） ===");
            var c3 = new RfCalc.RfVfVrPhaseCalculator(0.95, 0.32, -42.5, couplingDb: 30.0);
            Console.WriteLine($"Z = {c3.LoadImpedance.ToStringRf(3)} Ω");
            Console.WriteLine($"VSWR = {c3.VSWR:F3}");
            Console.WriteLine($"入射功率 = {c3.ForwardPowerW:F2} W");
        }
    }

    // ====================== 使用示例（直接复制可跑） ======================

}
