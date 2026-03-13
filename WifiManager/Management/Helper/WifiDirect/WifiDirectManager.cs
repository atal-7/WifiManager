using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.NetworkOperators;

namespace WifiManager.Management.Helper.WifiDirect
{
    public class WifiDirectManager
    {
        private static readonly object _lock = new();
        private static volatile WifiDirectManager? wifiDirectManager;

        private WifiDirectManager()
        {
            
        }

        #region Instance
        public static WifiDirectManager GetInstance()
        {
            try
            {
                if (wifiDirectManager != null) return wifiDirectManager;
                lock (_lock)
                {
                    if (wifiDirectManager == null)
                        wifiDirectManager = new();
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Can not create instance: " + exception.Message);
            }
            return wifiDirectManager;
        }

        #endregion
    }
}
