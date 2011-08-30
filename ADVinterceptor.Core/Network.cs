/*
 * This file is part of ADVinterceptor
 * Copyright (c) 2011 - ADVTOOLS SARL
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Management;

namespace Advtools.AdvInterceptor
{
    internal static class Network
    {
        public static IPAddress[] GetIps()
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            return entry.AddressList;
        }

        public static IPAddress[] GetIpsByWmi()
        {
            List<IPAddress> ips = new List<IPAddress>();

            using(ManagementObjectSearcher adapterSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter where NetConnectionStatus=2"))
            {
                foreach(ManagementObject adapter in adapterSearcher.Get())
                {
                    int index = Convert.ToInt32(adapter["index"]);
                    string query = string.Format("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE Index={0} AND IPEnabled='true'", index);
                    using(ManagementObjectSearcher confSearcher = new ManagementObjectSearcher(query))
                    {
                        foreach(ManagementObject config in confSearcher.Get())
                        {
                            // List all IP addresses of the current network interface
                            string[] addressList = (string[])config["IPAddress"];

                            foreach(string address in addressList)
                                ips.Add(IPAddress.Parse(address));
                        }
                    }
                }
                return ips.ToArray();
            }
        }
    }
}
