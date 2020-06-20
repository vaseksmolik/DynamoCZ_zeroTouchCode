using Autodesk.DesignScript.Runtime;
using DSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamoCZ
{
    /// <summary>
    /// Výpočet podle ČSN EN 12 831-1
    /// </summary>
    public class TepelneZtratyObalkovouMetodou
    {
        private TepelneZtratyObalkovouMetodou() { }

        /// <summary>
        ///  Výpočet tepelné ztráty budovy podle ČSN EN 12 831-1
        /// </summary>
        /// <param name="tepelnaZtrataProstupem"></param>
        /// <param name="tepelnaZtrataVetranim"></param>
        /// <param name="tepelnyZisk"></param>
        /// <returns></returns>
        public static string Celkem(
            double tepelnaZtrataProstupem,
            double tepelnaZtrataVetranim,
            double tepelnyZisk
            )
        {
            double tepelnaZtrataCelkem = tepelnaZtrataProstupem + tepelnaZtrataVetranim - tepelnyZisk;
            return tepelnaZtrataCelkem.ToString() + " W";
        }

        /// <summary>
        /// Výpočet tepelné ztráty větráním podle ČSN EN 12 831-1 {Qv = (Vmin*r*c) * (qi-qe) * (1-n)}
        /// </summary>
        /// <param name="teplotaInterier">Návrhová teplota v interiéru [C]</param>
        /// <param name="teplotaExterier">Teplota exteriéru [C]</param>
        /// <param name="objemBudovy">Objem vzduchu v budově [m^2]</param>
        /// <param name="ucinnostRekuperace">účinnost zpětného zisku tepla [-] (Bez rekuperace tepla = 0)</param>
        /// <param name="intenzitaVetrani">intenzita výměny vzduchu [1/h] (Běžně = 0,4 ; Minimálně = 0,3 ; u netěsných staveb = 1 i více.)</param>
        /// <returns>Tepelná ztráta přirozeným větráním [W]</returns>
        public static double Vetranim(double teplotaInterier, double teplotaExterier, double objemBudovy, double ucinnostRekuperace = 0, double intenzitaVetrani = 0.4)
        {
            double vinfi = 2 * objemBudovy * 4 * 0.05 * 1;
            double vmini = intenzitaVetrani * objemBudovy;
            double vetranyObjem = System.Math.Max(vinfi, vmini);

            return (vetranyObjem * 0.34) * (teplotaInterier - teplotaExterier) * (1 - ucinnostRekuperace);
        }

        /// <summary>
        /// Výpočet tepelné ztráty prostupem podle ČSN EN 12 831-1 {QT = SUM(Sk*Uk) * (qi-qe)}
        /// </summary>
        /// <param name="dataKonstrukci"></param>
        /// <param name="soucLinTepMostu">Součinitel lineárních tepelných mostů, vztažený na ochlazovanou plochu [W/m2K] (konstrukce téměř bez tepelných mostů = 0,02 ; s mírnými tepelnýmy mosty = 0,05 ; a běžnými tepelnými mosty = 0,10 ; s výraznými tepelnými mosty = 0,15 )</param>
        /// <returns>Tepelná ztráta prostupem [W]</returns>
        public static double Prostupem(List<List<string>> dataKonstrukci, double soucLinTepMostu = 0.02)
        {

            // načti data
            // zjisti řazení sloupců
            var zahlaviTabulky = dataKonstrukci[0];
            dataKonstrukci.RemoveAt(0);
            int indexSoucProstupuTepla = -1;
            int indexPlocha = -1;
            int indexCinitelTeplotniRedukce = -1;
            try
            {
                indexSoucProstupuTepla = zahlaviTabulky.IndexOf("Součinitel prostupu tepla (U)");
                indexPlocha = zahlaviTabulky.IndexOf("Plocha");
            }
            catch
            {
                throw new Exception("Ve vstupních datech nejsou správně definovány parametry \"součinitel prostupu tepla (U)\" , \"Plocha\"");
            }

            if (zahlaviTabulky.Contains("Činitel teplotní redukce (b)"))
                indexCinitelTeplotniRedukce = zahlaviTabulky.IndexOf("Činitel teplotní redukce (b)");

            double celkovaZtrata = 0;
            double celkovaPlocha = 0;

            foreach (List<string> radek in dataKonstrukci)
            {
                // kdyby náhodou v datech chyběla poslední buňka, tak jí doplň
                while (radek.Count() < zahlaviTabulky.Count())
                    radek.Add("");

                double plocha = double.Parse(Regex.Match(radek[indexPlocha], @"\d+").Value);
                double soucinitelProstupuTepla = 0;
                double.TryParse(Regex.Match(radek[indexSoucProstupuTepla], @"\d+").Value, out soucinitelProstupuTepla);

                double cinitelTeplotniRedukce;
                if (indexCinitelTeplotniRedukce != -1)
                    double.TryParse(Regex.Match(radek[indexCinitelTeplotniRedukce], @"\d+").Value, out cinitelTeplotniRedukce);
                else
                    cinitelTeplotniRedukce = 1;

                double ztrata = plocha * soucinitelProstupuTepla;
                celkovaZtrata += ztrata;
                celkovaPlocha += plocha;
            }

            return celkovaZtrata + celkovaPlocha * soucLinTepMostu;
        }
    }
}
