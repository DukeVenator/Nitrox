﻿using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using NitroxModel.Logger;

namespace NitroxServer
{
    public static class IpLogger
    {
        public static void PrintServerIps()
        {
            NetworkInterface[] allInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface eachInterface in allInterfaces)
            {
                PrintIfHamachi(eachInterface);
                PrintIfLan(eachInterface);
            }

            PrintIfExternal();
        }

        private static void PrintIfHamachi(NetworkInterface _interface)
        {
            if (_interface.Name != "Hamachi")
            {
                return;
            }

            var ips = _interface.GetIPProperties().UnicastAddresses
                .Select(address => address.Address.ToString())
                .Where(address => !address.ToString().Contains("fe80::"));
            Log.Info("If using Hamachi, use this IP: " + string.Join(" or ", ips));
        }

        private static void PrintIfLan(NetworkInterface _interface)
        {
            if (_interface.GetIPProperties().GatewayAddresses.Count == 0)
            {
                return;
            }

            foreach (UnicastIPAddressInformation eachIp in _interface.GetIPProperties().UnicastAddresses)
            {
                string[] splitIpParts = eachIp.Address.ToString().Split('.');
                int secondPart = 0;
                if (splitIpParts.Length > 1)
                {
                    int.TryParse(splitIpParts[1], out secondPart);
                }

                if (splitIpParts[0] == "10" || splitIpParts[0] == "192" && splitIpParts[1] == "168" || splitIpParts[0] == "172" && secondPart > 15 && secondPart < 32) //To get if IP is private
                {
                    Log.Info("If playing on LAN, use this IP: " + eachIp.Address);
                }
            }
        }

        private static void PrintIfExternal()
        {
            using (Ping ping = new Ping())
            {
                ping.PingCompleted += PingOnPingCompleted;
                ping.SendAsync("8.8.8.8", 1000, null);
            }
        }

        private static void PingOnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            if (e.Reply.Status != IPStatus.Success)
            {
                return;
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadStringCompleted += ClientOnDownloadStringCompleted;
                client.DownloadStringAsync(new Uri("http://ipv4bot.whatismyipaddress.com/")); // from https://stackoverflow.com/questions/3253701/get-public-external-ip-address answer by user_v
            }
        }

        private static void ClientOnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Log.Info("If using port forwarding, use this IP: " + e.Result);
        }
    }
}
