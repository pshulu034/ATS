using RFToolkit;

namespace MathLib
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Sample.Test();

            //TemperatureCompensator.Test();

            //Console.WriteLine($"30 dBm = {RfConverter.DbmToWatt(30):F6} W");           // 1.000000 W
            //Console.WriteLine($"1 W = {RfConverter.WattToDbm(1):F2} dBm");           // 30.00 dBm
            //Console.WriteLine($"30 dBm @50¦¸ = {RfConverter.DbmToVpp(30):F2} Vpp");   // 28.28 Vpp
            //Console.WriteLine($"10 Vpp @50¦¸ = {RfConverter.VppToDbm(10):F2} dBm");   // 23.01 dBm
            //Console.WriteLine($"VSWR 1.5 ¡ú RL = {RfConverter.VswrToReturnLossDb(1.5):F2} dB"); // 13.98 dB
            //Console.WriteLine($"2.4 GHz ²¨³¤ = {RfConverter.GhzToMm(2.4):F1} mm");   // 125.0 mm


        }
    }
}