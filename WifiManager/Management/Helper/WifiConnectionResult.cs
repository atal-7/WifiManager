using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiManager.Management.Helper
{
    public enum WifiConnectionResult
    {
        Success,                // Connection successful
        AccessDenied,           // Access to network denied
        PasswordRequired,       // Password required for network
        InvalidPassword,        // Password incorrect
        Failed,                 // Connection failed
        WifiAdapterError,       // Error occurred with Wi-Fi adapter
        ManagedNativeError,     // Error specific to Managed Native library
        NetworkNotFound,        // The specified network not found
        ConnectionTimeout,      // Connection time out in WiFi adapter
        UnknownError
    }

}
