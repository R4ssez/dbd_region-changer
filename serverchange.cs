using DnsClient;
using Newtonsoft.Json;
using Sharprompt;
using Sharprompt.Fluent;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace serverchange
{
    class DBDRegionChange
    {
        const string Hostfile = @"C:/Windows/System32/drivers/etc/hosts";
        public static void Main()
        {
            Console.Clear();
            Print(CheckServer(), ConsoleColor.Green);
            Print("(Ohjaa komentoja nuolinäppäimillä)", ConsoleColor.DarkBlue);
            Prompt.ColorSchema.Select = ConsoleColor.DarkRed;
            var select = Prompt.Select<string>(o => o.WithMessage("Dead By Daylight: Region Changer")
                                       .WithItems(new[] { "Vaihda palvelinta", "Poista muokkaukset", "Tarkista muokkaukset", "Poistu" })
                                       .WithDefaultValue("Vaihda palvelinta"));




            if (select == "Vaihda palvelinta")
            {
                ChangeServer();
            }
            else if (select == "Poista muokkaukset")
            {
                RemoveEdits();
            }
            else if (select == "Tarkista muokkaukset")
            {
                if (CheckEdits())
                {
                    Print("Muokkauksia löytyi", ConsoleColor.Green);
                } 
                else
                {
                    Print("Muokkauksia ei löytynyt", ConsoleColor.Red);
                }

                Thread.Sleep(5000);
                Main();
            }
            else if (select == "Poistu")
            {
                System.Environment.Exit(1);
            }
        }


        public static void RemoveEdits()
        {
            bool written = false;
            int firstindex = -1;
            List<string> file = File.ReadAllLines(Hostfile).ToList();
            Regex pattern = new(@"^.*(#RMEDIT#).*$");


            for (int i = 0; i < file.Count; i++)
            {
                Match m = pattern.Match(file[i]);
                if (m.Success)
                {
                    if (written)
                    {
                        file.RemoveRange(firstindex - 2, (i - firstindex) + 3);
                        File.WriteAllLines(Hostfile, file.ToArray());
                        StoreData(null, null);
                        Print("Muokkaukset poistettu. Käynnistä peli uudestaan!", ConsoleColor.Blue);
                        Thread.Sleep(5000);
                        Main();
                    }


                    if (!written) { firstindex = i; written = true; }
                }

            }

        }

        public static bool CheckEdits()
        {
            List<string> file = File.ReadAllLines(Hostfile).ToList();
            Regex pattern = new(@"^.*(#RMEDIT#).*$");


            for (int i = 0; i < file.Count; i++)
            {
                Match m = pattern.Match(file[i]);
                if (m.Success)
                {
                    return true;
                }

            }
            return false;
        }

        public static void ChangeServer()
        {
            List<string> Choices = new();
            var lookup = new LookupClient();
            string? country = null;

            if (CheckEdits())
            {
                Console.WriteLine("Muokkauksia löydetty, poistetaan...");
                RemoveEdits();
                Thread.Sleep(3000);
                Process.Start("./dbd_region-changer.exe");
                Environment.Exit(0);
                return;
            }

            var data = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@"data.json"));

            foreach (var d in data.CountryList)
            {
                if (d.Name != null)
                Choices.Add(d.Name);
            }

            var select = Prompt.Select("Valitse Alue", Choices);

            foreach (var d in data.CountryList)
            {
                if (d.Name == select.ToString())
                {
                    country = d.Server;
                    StoreData(d.Name, d.Server);
                }

            }
            

            List<string> file = File.ReadAllLines(Hostfile).ToList();

            var result = lookup.Query(country, QueryType.A);
            var record = result.Answers.ARecords().FirstOrDefault();
            var ip = record?.Address;

            file.Add("\n\n#RMEDIT#");

            foreach (var d in data.CountryList) file.Add(ip + " " + d.Server);

            file.Add("#RMEDIT#");
            File.WriteAllLines(Hostfile, file.ToArray());
            Print("Vaihdettu palvelimelle " + select.ToString() + ". Käynnistä peli uudelleen.", ConsoleColor.Blue);
            Thread.Sleep(5000);
            Main();

        }


        public static void StoreData(string ServerName, string CurrentIP)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@"data.json"));

            config!.CurrentServer = ServerName;
            config.CurrentIP = CurrentIP;

            File.WriteAllText(@"data.json", JsonConvert.SerializeObject(config));

        }

        public static string CheckServer()
        {
            var data = JsonConvert.DeserializeObject<Config>(File.ReadAllText(@"data.json"));
            if (data.CurrentServer == null)
            {
                return "Et ole millään palvelimella";
            }
            else
            {
                return "Olet palvelimella " + data.CurrentServer;
            }
        }

        public static void Print(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(" {0}", text);
            Console.ResetColor();
        }

        public class Config
        {
            public string? CurrentServer { get; set; }
            public object? CurrentIP { get; set; }
            public Countrylist[]? CountryList { get; set; }
        }

        public class Countrylist
        {
            public string? Name { get; set; }
            public string? Server { get; set; }
        }

    }
}
