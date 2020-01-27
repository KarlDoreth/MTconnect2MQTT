using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class ConfigHandling
    {
        Dictionary<string, string> ConfigParamters = new Dictionary<string, string>();

        public void LoadConfig()
        {
            ConfigParamters.Clear();
            System.IO.StreamReader SR = new System.IO.StreamReader("config.ini");
            while (SR.EndOfStream == false)
            {
                string Zeile = SR.ReadLine();
                Zeile = Zeile.Trim();
                string[] ZeileGeteilt = Zeile.Split('=');
                string Schluessel = ZeileGeteilt[0].Trim();
                string Wert = ZeileGeteilt[1].Trim();

                ConfigParamters.Add(Schluessel, Wert);
            }
            SR.Close();
        }

  
        public string GetConfigValue(string Schluessel)
        {
            return ConfigParamters[Schluessel];
        }


    }
}
