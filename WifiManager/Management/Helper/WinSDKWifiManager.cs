using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace WifiManager.Management.Helper
{
    public class WinSDKWifiManager
    {
        #region Fields
        private static readonly object _lock = new();
        private static volatile WinSDKWifiManager? wifiManagerObject;

        private string myPassword = "pass";

        // Dictionary for xml special character replacements
        private Dictionary<char, string> dictionaryXmlSpecialCharacter = new()
        {
            {'&', "&amp;"},
            {'<', "&lt;"},
            {'>', "&gt;"},
            {'"', "&quot;"},
            {'\'', "&apos;"}
        };

        #endregion

        #region Instance
        public static WinSDKWifiManager GetInstance()
        {
            try
            {
                if (wifiManagerObject != null) return wifiManagerObject;
                lock (_lock)
                {
                    if (wifiManagerObject == null)
                        wifiManagerObject = new();
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Can not create instance of WinSDKWifiManager: " + exception.Message);
            }
            return wifiManagerObject;
        }

        #endregion

        #region Wifi Methods

        /// <summary>
        /// Scan Wi-Fi Networks
        /// </summary>
        public async Task<List<NetworkData>> ScanWifiNetworks()
        {
            List<NetworkData> wifiNetworks = new List<NetworkData>();

            var accessStatus = await WiFiAdapter.RequestAccessAsync();
            if (accessStatus == WiFiAccessStatus.Allowed)
            {
                var result = await WiFiAdapter.FindAllAdaptersAsync(); // will not return result if wifi is off

                if (result.Count > 0)
                {
                    var wifiAdapter = result[0];
                    await wifiAdapter.ScanAsync();

                    var availableNetworks = wifiAdapter.NetworkReport.AvailableNetworks;
                    foreach (var network in availableNetworks)
                    {
                        wifiNetworks.Add(new NetworkData
                        {
                            Ssid = network.Ssid,
                            Bssid = network.Bssid,
                            SignalStrength = network.SignalBars,
                            SecurityType = network.PhyKind,
                            NetworkKind = network.NetworkKind,
                            Frequency = network.ChannelCenterFrequencyInKilohertz,
                            SecuritySettings = network.SecuritySettings             // will both authentication type and encryption type
                        });
                    }
                }
            }
            return wifiNetworks; // implement OnNetworkReportChanged
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task<bool> SetWifiRadioState(bool state)
        {
            // Get radios
            var radios = await Radio.GetRadiosAsync();
            Radio? wifiRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.WiFi);

            if (wifiRadio != null)
            {
                if (state)
                {
                    await wifiRadio.SetStateAsync(RadioState.On);
                }
                else
                {
                    await wifiRadio.SetStateAsync(RadioState.Off);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<WiFiAdapter?> GetNetworkAdapter()
        {
            var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
            var wifiAdapter = await WiFiAdapter.FromIdAsync(result[0].Id); //revisit this later
            if (wifiAdapter != null)
            {
                await wifiAdapter.ScanAsync();
                return wifiAdapter;
            }

            return null;
        }

        private bool IsEnterpriseNetwork(NetworkAuthenticationType authType)
        {
            switch (authType)
            {
                // WPA-Enterprise, WPA2-Enterprise, WPA3-Enterprise, or IHV-defined
                case NetworkAuthenticationType.Wpa:
                case NetworkAuthenticationType.Rsna:
                case NetworkAuthenticationType.Ihv:
                case NetworkAuthenticationType.Wpa3:
                case NetworkAuthenticationType.Wpa3Enterprise:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Connect Wi-Fi
        /// </summary>
        /// <param name="ssid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<WifiConnectionResult> ConnectWifi(string ssid, string? password)
        {
            WifiConnectionResult wifiConnectionResult = WifiConnectionResult.UnknownError;
            try
            {
                if (await CheckWiFiAccessAsync())
                {
                    var credential = new PasswordCredential();
                    if (!string.IsNullOrEmpty(password))
                    {
                        credential.Password = password;
                    }
                    WiFiAdapter adapter;

                    var result = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
                    if (result.Count >= 1)
                    {
                        adapter = await WiFiAdapter.FromIdAsync(result[0].Id); //revisit this later
                        if (adapter != null)
                        {
                            await adapter.ScanAsync();
                            var profiles = await adapter.NetworkAdapter.GetConnectedProfileAsync();
                            var networkAccountId = profiles.NetworkAdapter.NetworkAdapterId;
                            WlanConnectionProfileDetails details = profiles.WlanConnectionProfileDetails;

                            WiFiAvailableNetwork? wiFiAvailableNetwork = null;
                            foreach (var network in adapter.NetworkReport.AvailableNetworks)
                            {
                                if (network.Ssid == ssid)
                                {
                                    wiFiAvailableNetwork = network;
                                    break;
                                }
                            }
                            if (wiFiAvailableNetwork != null)
                            {
                                //add check for enterprise networks here

                                var kind = wiFiAvailableNetwork.NetworkKind;
                                var authenticationType = wiFiAvailableNetwork.SecuritySettings.NetworkAuthenticationType;
                                var encryptType = wiFiAvailableNetwork.SecuritySettings.NetworkEncryptionType;

                                if (IsEnterpriseNetwork(authenticationType))
                                {
                                    MessageBox.Show("Enterprise Network!", $"Login is required to connect with :{ssid}");

                                    credential.UserName = "atal.sharma@stryker.com";
                                    credential.Password = myPassword;
                                    //use managed native wifi nuget

                                    AvailableNetworkPack? availableNetwork = NativeWifi.EnumerateAvailableNetworks().FirstOrDefault(n => n.Ssid.ToString() == ssid);
                                    if (availableNetwork != null)
                                    {
                                        //delete profile first
                                        foreach (var profile in NativeWifi.EnumerateProfiles())
                                        {
                                            if (profile.Name.Equals("SYKNet"))
                                            {
                                                NativeWifi.DeleteProfile(availableNetwork.Interface.Id, profile.Name);
                                                break;
                                            }
                                        }

                                        //set profile first
                                        string profileXml = await File.ReadAllTextAsync(@"C:\temp\Wi-Fi-SYKNet.xml");
                                        var profileResult = NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true);

                                        if (profileResult)
                                        {
                                            try
                                            {
                                                ProfilePack? profile = NativeWifi.EnumerateProfiles().FirstOrDefault(n => n.Document.Ssid.ToString() == ssid);

                                                if (profile != null)
                                                {
                                                    string profileName = System.Security.SecurityElement.Escape(profile.Name);

                                                    bool connectionResult = await NativeWifi.ConnectNetworkAsync(
                                                        availableNetwork.Interface.Id,
                                                        profileName,
                                                        availableNetwork.BssType,
                                                        TimeSpan.FromSeconds(10));

                                                    if (connectionResult)
                                                    {
                                                        MessageBox.Show("Successfully connected to the network.");
                                                        return WifiConnectionResult.Success;
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("Failed to connect to the network.");
                                                        return WifiConnectionResult.Failed;
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                return WifiConnectionResult.ManagedNativeError;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        return WifiConnectionResult.NetworkNotFound;
                                    }

                                    //await PrepareForEnterpriseNetwork(networkAccountId, ssid, credential.UserName, credential.Password, encryptType);

                                }
                                else //personal networks
                                {
                                    try
                                    {
                                        var status = await adapter.ConnectAsync(wiFiAvailableNetwork, WiFiReconnectionKind.Automatic, credential);
                                        if (status.ConnectionStatus == WiFiConnectionStatus.Success)
                                        {
                                            //get profile
                                            ConnectionProfile InternetConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                                            var hostname = NetworkInformation.GetHostNames().FirstOrDefault(hn => hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId == InternetConnectionProfile?.NetworkAdapter.NetworkAdapterId);

                                            Debug.WriteLine("OK");
                                            return WifiConnectionResult.Success;
                                        }
                                        else if (status.ConnectionStatus == WiFiConnectionStatus.InvalidCredential)
                                        {
                                            MessageBox.Show("Invalid Password");
                                            return WifiConnectionResult.InvalidPassword;
                                        }
                                        else if (status.ConnectionStatus == WiFiConnectionStatus.Timeout)
                                        {
                                            MessageBox.Show("Timeout");
                                            return WifiConnectionResult.ConnectionTimeout;
                                        }
                                    }
                                    catch
                                    {
                                        return WifiConnectionResult.WifiAdapterError;
                                    }
                                }
                            }
                            else
                            {
                                return WifiConnectionResult.NetworkNotFound;
                            }
                        }
                        else
                        {
                            return WifiConnectionResult.WifiAdapterError;
                        }
                    }
                }
                else
                {
                    return WifiConnectionResult.AccessDenied;
                }
            }
            catch /*(Exception ex)*/
            {
                wifiConnectionResult = WifiConnectionResult.UnknownError;
            }

            return wifiConnectionResult;
        }

        private async Task<bool> CheckWiFiAccessAsync()
        {
            var accessStatus = await WiFiAdapter.RequestAccessAsync(); // required due to recent location permission changes but it is called earlier as well in ScanWifiNetworks when creating list of networks, so do i need this?
            bool result = false;

            switch (accessStatus)
            {
                case WiFiAccessStatus.Allowed:
                    Debug.WriteLine("Access granted");
                    result = true;
                    break;
                case WiFiAccessStatus.DeniedByUser:
                    MessageBox.Show("Access denied by user");
                    break;
                case WiFiAccessStatus.DeniedBySystem:
                    MessageBox.Show("Access denied by system policy");
                    break;
                case WiFiAccessStatus.Unspecified:
                    MessageBox.Show("Unspecified error occurred");
                    break;
            }

            return result;
        }
       
        private string CreateProfileXml(string SSID, string Username, string password, NetworkEncryptionType encryptType)
        {
            string ssid = System.Security.SecurityElement.Escape(SSID);
            string username = System.Security.SecurityElement.Escape(Username);
            string securityKey = System.Security.SecurityElement.Escape(password);

            string isNetworkHidden = "false";
            string ssidHex = ConvertToHex(ssid);
            string connectionMode = "auto"; //keeping automatic as default for now
            string autoSwitch = "true";
            string encryption = ConvertCipherAlgorithmToString(encryptType);

            string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{3}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{4}</connectionMode><autoSwitch>{5}</autoSwitch><MSM><security><authEncryption><authentication>WPA2</authentication><encryption>{2}</encryption><useOneX>true</useOneX><FIPSMode xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v2\">false</FIPSMode></authEncryption><PMKCacheMode>enabled</PMKCacheMode><PMKCacheTTL>720</PMKCacheTTL><PMKCacheSize>128</PMKCacheSize><preAuthMode>disabled</preAuthMode><OneX xmlns=\"http://www.microsoft.com/networking/OneX/v1\"><cacheUserData>true</cacheUserData><authMode>user</authMode><EAPConfig><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>false</DisableUserPromptForServerValidation><ServerNames></ServerNames></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>26</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2ConnectionPropertiesV1\"><UseWinLogonCredentials>false</UseWinLogonCredentials></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></EAPConfig></OneX></security></MSM></WLANProfile>", ssid, ssidHex, encryption, isNetworkHidden, connectionMode, autoSwitch);

            return profileXml;
        }

        private string ConvertCipherAlgorithmToString(NetworkEncryptionType cipherAlgorithm)
        {
            switch (cipherAlgorithm)
            {
                case NetworkEncryptionType.Ccmp:
                    return "AES";
                case NetworkEncryptionType.Tkip:
                    return "TKIP";
                case NetworkEncryptionType.Wep:
                case NetworkEncryptionType.Wep104:
                case NetworkEncryptionType.Wep40:
                    return "WEP";
                default:
                    return "NA";
            }
        }

        private static string ConvertToHex(string asciiString)
        {
            string hex = string.Empty;
            try
            {
                foreach (char c in asciiString)
                {
                    int tmp = c;
                    hex += string.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
                }
                return hex.ToUpper();
            }
            finally
            {
            }
        }
        #endregion

        //private async Task<bool> PrepareForEnterpriseNetwork(Guid networkAccountId, string ssid, string username, string password, NetworkEncryptionType encryptType)
        //{
        //    //create ProvisioningAgent
        //    var provisioningAgent = ProvisioningAgent.CreateFromNetworkAccountId(networkAccountId.ToString());

        //    //var xmlDocument = new XmlDocument();
        //    string wifiProfileXML = string.Empty;
        //    try
        //    {
        //        wifiProfileXML = File.ReadAllText(@"C:\temp\Wi-Fi-SYKNet.xml"); /*CreateProfileXml(ssid, username, password, encryptType)*/
        //        //var res = XDocument.Parse(wifiProfileXML);
        //        //xmlDocument.LoadXml(wifiProfileXML);
        //    }
        //    catch (System.Xml.XmlException ex)
        //    {
        //        Console.WriteLine($"XML Exception: {ex.Message}");
        //        Console.WriteLine(ex.StackTrace);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"General Exception: {ex.Message}");
        //        Console.WriteLine(ex.StackTrace);
        //    }

        //    ProvisionFromXmlDocumentResults results = await provisioningAgent.ProvisionFromXmlDocumentAsync(wifiProfileXML);

        //    if(results.ProvisionResultsXml!=null && results.AllElementsProvisioned)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

    }
}
