using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiManager.Management.Helper
{
    public class NetworkData
    {
        public int StausId { get; set; }
        public string? Ssid { get; set; }
        public int IpAddress { get; set; }
        public string? GatewayAddress { get; set; }
        public object? NativeObject { get; set; }
        public object? Bssid { get; set; }
        public byte? SignalStrength { get; set; }
        public object? SecurityType { get; set; }
        public object? NetworkKind { get; set; }
        public int Frequency { get; set; }
        public object? SecuritySettings { get; set; }
    }
}
