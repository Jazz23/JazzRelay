using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JazzRelay.DataTypes
{
    public class Proxy
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        public Proxy(string username, string password, string ip, int port)
        {
            Username = username;
            Password = password;
            Ip = ip;
            Port = port;
        }

        public Proxy(string proxyString)
        {
            var split = proxyString.Split(':');

            // This can throw an error if int.Parse can't parse the port.
            try
            {
                Username = split[2];
                Password = split[3];
                Ip = split[0] ?? "";
                Port = int.Parse(split[1]);
            }
            catch
            {
                Console.WriteLine("Proxy line formatted wrong {proxyString}", proxyString);
                Ip = "";
            }
        }

        public bool IsGood()
        {
            return Username != default && Password != default && Ip != default && Port != default;
        }

        public static async Task LoadWebshare(string url, List<Proxy> backend, List<Proxy> frontend)
        {
            using var web = new HttpClient();
            var result = await web.GetStringAsync(url);
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var proxy = new Proxy(lines[i]);
                frontend.Add(proxy);
            }
        }
    }
}
