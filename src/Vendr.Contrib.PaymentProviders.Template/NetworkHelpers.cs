using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Vendr.Contrib.PaymentProviders.PayNl
{
    public class NetworkHelpers
    {
        internal static string GetIpAddress()
        {
            var context = System.Web.HttpContext.Current;

            if (context == null) return "";
            var ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                var addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return ParseIpAddress(addresses[0]).ToString();
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        /// <summary>
        /// Parse a string to an ip address.  Can do ipv4, ipv6, hostnames, all with or without port numbers.
        /// </summary>
        /// <param name="endpointstring"></param>
        /// <returns></returns>
        /// <remarks>
        /// Original from https://stackoverflow.com/a/12044845/97615
        /// </remarks>
        public static IPAddress ParseIpAddress(string endpointstring)
        {
            return ParseIpAddress(endpointstring, out _);    // declare out as an inline discard variable
        }

        public static IPAddress ParseIpAddress(string endpointstring, out int? port)
        {
            IPAddress ipaddy;
            var values = endpointstring.Split(':');

            // check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                port = values.Length == 1 ? (int?)null : GetPort(values[1]);

                // try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = GetIPfromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                // could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = GetPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpointstring);
                    port = null;
                }
            }
            else
            {
                throw new FormatException($"Invalid endpoint ipaddress '{endpointstring}'");
            }

            return ipaddy;
        }

        private static int GetPort(string p)
        {
            if (!int.TryParse(p, out var port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException($"Invalid end point port '{p}'");
            }

            return port;
        }

        private static IPAddress GetIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException($"Host not found: {p}");

            return hosts[0];
        }
    }
}
