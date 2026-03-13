using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedNativeWifi;

namespace WifiManager.Management.Helper
{
}
//    /// <summary>
//    /// This class is for the Wlan connection information
//    /// </summary>
//    public class WLANConnectionInfo
//    {
//        #region PRIVATE VARIABLE

//        private AvailableNetworkPack Wlan;

//        /// <summary>
//        /// Holds the network SSId
//        /// </summary>

//        private String ssid;
//        /// <summary>
//        /// holds default Algorithm for authentication
//        /// </summary>

//        private Wlan.DOT11_AUTH_ALGORITHM defaultAuthAlgorithm;
//        /// <summary>
//        /// Holds Cipher algorithm for authentication
//        /// </summary>

//        private Wlan.DOT11_CIPHER_ALGORITHM defaultCipherAlgorithm;
//        /// <summary>
//        /// Holds username for the wifi network connection
//        /// </summary>

//        private String userName;
//        /// <summary>
//        /// Hold the password for the wifi network connection
//        /// </summary>

//        private String password;
//        /// <summary>
//        /// Holds the connection mode
//        /// </summary>

//        private Wlan.WlanConnectionMode wlanConnectionMode;
//        /// <summary>
//        /// Holds profile name of the wifi network
//        /// </summary>

//        private AvailableNetworkPack profile;
//        /// <summary>
//        /// Holds the SSid Ptr
//        /// </summary>

//        private IntPtr dot11SsidPtr;
//        /// <summary>
//        /// Holds the desiredBssidListPtr
//        /// </summary>

//        private IntPtr desiredBssidListPtr;
//        /// <summary>
//        /// Hold bss type of the wifi network
//        /// </summary>

//        private Wlan.DOT11_BSS_TYPE dot11BssType;
//        /// <summary>
//        /// Holds the connection status
//        /// </summary>

//        private Wlan.WlanConnectionFlags flags;
//        /// <summary>
//        /// Holds whether the network is secured or not
//        /// </summary>

//        private Boolean isSecured;

//        #endregion

//        #region CONSTRUCTOR

//        /// <summary>
//        /// Constructor for WlanConnectionInfo
//        /// </summary>
//        /// <summary>
//        /// Initializes a new instance of the <see cref="WlanConnectionInfo"/> class.
//        /// </summary>
//        public WLANConnectionInfo()
//        {
//            ssid = String.Empty;
//            userName = String.Empty;
//            password = String.Empty;
//            wlanConnectionMode = Wlan.WlanConnectionMode.Profile;
//            profile = String.Empty;
//            dot11SsidPtr = IntPtr.Zero;
//            desiredBssidListPtr = IntPtr.Zero; ;
//            dot11BssType = Wlan.DOT11_BSS_TYPE.dot11_BSS_type_any;
//            flags = 0;
//            defaultAuthAlgorithm = Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_80211_OPEN;
//            defaultCipherAlgorithm = Wlan.DOT11_CIPHER_ALGORITHM.DOT11_CIPHER_ALGO_NONE;
//            IsHidden = false;
//            SecurityKey = "";
//            IsManualConfigured = false;
//        }

//        #endregion

//        #region PROPERTIES

//        /// <summary>
//        /// Property to get and set the ssid value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the ssid.
//        /// </summary>
//        /// <value>The ssid.</value>
//        public String Ssid
//        {
//            get { return ssid; }
//            set { ssid = value; }
//        }


//        /// <summary>
//        /// Property to get and set the username value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the name of the user.
//        /// </summary>
//        /// <value>The name of the user.</value>
//        public String UserName
//        {
//            get { return userName; }
//            set { userName = value; }
//        }


//        /// <summary>
//        /// Property to get and set the password value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the password.
//        /// </summary>
//        /// <value>The password.</value>
//        public String Password
//        {
//            get { return password; }
//            set { password = value; }
//        }


//        /// <summary>
//        /// Property to get and set the WlanConnectionMode value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the wlan connection mode.
//        /// </summary>
//        /// <value>The wlan connection mode.</value>
//        public Wlan.WlanConnectionMode WlanConnectionMode
//        {
//            get { return wlanConnectionMode; }
//            set { wlanConnectionMode = value; }
//        }


//        /// <summary>
//        /// Property to get and set the Profile value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the profile.
//        /// </summary>
//        /// <value>The profile.</value>
//        public String Profile
//        {
//            get { return profile; }
//            set { profile = value; }
//        }


//        /// <summary>
//        /// Property to get and set the Dot11AsidPtr value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the dot11 ssid PTR.
//        /// </summary>
//        /// <value>The dot11 ssid PTR.</value>
//        public IntPtr Dot11SsidPtr
//        {
//            get { return dot11SsidPtr; }
//            set { dot11SsidPtr = value; }
//        }


//        /// <summary>
//        /// Property to get and set the DesiredBssisListPtr value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the desired bssid list PTR.
//        /// </summary>
//        /// <value>The desired bssid list PTR.</value>
//        public IntPtr DesiredBssidListPtr
//        {
//            get { return desiredBssidListPtr; }
//            set { desiredBssidListPtr = value; }
//        }


//        /// <summary>
//        /// Property to get and set the Dot11BssType value.
//        /// </summary>
//        /// <summary>
//        /// Gets or sets the type of the dot11 BSS.
//        /// </summary>
//        /// <value>The type of the dot11 BSS.</value>
//        public Wlan.DOT11_BSS_TYPE Dot11BssType
//        {
//            get { return dot11BssType; }
//            set { dot11BssType = value; }
//        }

//        /// <summary>
//        /// Property to get and set the flag value.
//        /// </summary>
//        /// <value>The flags.</value>
//        public Wlan.WlanConnectionFlags Flags
//        {
//            get { return flags; }
//            set { flags = value; }
//        }



//        /// <summary>
//        /// Property to get and set the defaultalgo value.
//        /// </summary>
//        /// <value>The default authentication algorithm.</value>
//        public Wlan.DOT11_AUTH_ALGORITHM DefaultAuthAlgorithm
//        {
//            get { return defaultAuthAlgorithm; }
//            set { defaultAuthAlgorithm = value; }
//        }


//        /// <summary>
//        /// Property to get and set the defaultCipherAlgo value.
//        /// </summary>
//        /// <value>The default cipher algorithm.</value>
//        public Wlan.DOT11_CIPHER_ALGORITHM DefaultCipherAlgorithm
//        {
//            get { return defaultCipherAlgorithm; }
//            set { defaultCipherAlgorithm = value; }
//        }


//        /// <summary>
//        /// Property to get and set the IsSecured value.
//        /// </summary>
//        /// <value>The is secured.</value>
//        public Boolean IsSecured
//        {
//            get { return isSecured; }
//            set { isSecured = value; }
//        }

//        /// <summary>
//        /// Property to get and set hidden value
//        /// </summary>
//        /// <value>The is hidden.</value>
//        public Boolean IsHidden { get; set; }

//        /// <summary>
//        /// Property to get and set Security value
//        /// </summary>
//        /// <value>The security key.</value>
//        public string SecurityKey { get; set; }

//        /// <summary>
//        /// Property to get and set manual configured value
//        /// </summary>
//        /// <value>The is manual configured.</value>
//        public Boolean IsManualConfigured { get; set; }

//        /// <summary>
//        /// Property to get and set manual order
//        /// </summary>
//        /// <value>The is manual order specified.</value>
//        public Boolean IsManualOrderSpecified { get; set; }

//        #endregion

//    }
//}
