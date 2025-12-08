using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RFToolkit
{
    public static class ImpedanceCalculator
    {
        // 串联
        public static Complex Series(params Complex[] Z)
        {
            Complex sum = new(0, 0);
            foreach (var z in Z) sum += z;
            return sum;
        }

        // 并联
        public static Complex Parallel(params Complex[] Z)
        {
            Complex sum = new(0, 0);
            foreach (var z in Z)
            {
                if (z.Magnitude == 0) continue; // 防止除0
                sum += new Complex(1, 0) / z;
            }
            return new Complex(1, 0) / sum;
        }

        //反射系数
        public static Complex Gamma(Complex zLoad, double z0 = 50.0)
        {
            Complex Z0 = new(z0, 0);
            return (zLoad - Z0) / (zLoad + Z0);
        }

        // 回波损耗 (dB)
        public static double ReturnLoss_dB(Complex gamma) => -20 * Math.Log10(gamma.Magnitude);

        // 驻波比
        public static double VSWR(Complex gamma)
        {
            double mag = gamma.Magnitude;
            if (mag >= 1.0) return double.PositiveInfinity;
            return (1 + mag) / (1 - mag);
        }
    }
}
