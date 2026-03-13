using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WifiManager.Management.Base;
using WifiManager.Management.Helper;

namespace WifiManager.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region ICommand
        public ICommand WifiEnabledCommand { get; private set; }
        public ICommand ConnectNetworkCommand { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Object> AvailableNetworkList {  get; private set; }

        private bool _isWifiChecked = false;

        // Dictionary for xml special character replacements
        Dictionary<char, string> dictionaryXmlSpecialCharacter = new()
        {
            {'&', "&amp;"},
            {'<', "&lt;"},
            {'>', "&gt;"},
            {'"', "&quot;"},
            {'\'', "&apos;"}
        };

        public bool IsWifiChecked
        {
            get { return _isWifiChecked; }
            set
            {
                if(_isWifiChecked != value)
                {
                    _isWifiChecked = value;
                    OnPropertyChanged(nameof(IsWifiChecked));
                    EnableWifi(value);
                }
            }
        }

        private int selectedViewIndex;

        public int SelectedViewIndex
        {
            get => selectedViewIndex;
            set
            {
                selectedViewIndex = value;
                OnPropertyChanged(nameof(SelectedViewIndex));
            }
        }


        private bool toggleHotspot;

        public bool ToggleHotspot
        {
            get => toggleHotspot;
            set
            {
                toggleHotspot = value;
                _ = ExecuteToggleHotspot(value);

                OnPropertyChanged(nameof(ToggleHotspot));
            }
        }

        private async Task ExecuteToggleHotspot(bool value)
        {
            try
            {
                var hotspotManager = HotspotManager.GetInstance();
                if (value)
                {
                    await hotspotManager.TurnHotspotOnAsync("HotspotTestinggg", "1+2+3+4=10");
                }
                else
                {
                    await hotspotManager.TurnHotspotOffAsync();
                }

            }
            catch
            {
            }
        }

        private int _selectedBand;

        public int SelectedBand
        {
            get => _selectedBand;
            set
            {
                _selectedBand = value;
                OnPropertyChanged(nameof(SelectedBand));
            }
        }

        private string? _passwordInput;

        public string? PasswordText
        {
            get => _passwordInput;
            set
            {
                _passwordInput = value; OnPropertyChanged(nameof(PasswordText));
            }
        }

        public bool IsEnterpriseNtwk { get; private set; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public MainViewModel()
        {
            AvailableNetworkList = new();
            SelectedViewIndex = 1;
            SelectedBand = 0;
            WifiEnabledCommand = new CommandBase(EnableWifi);
            ConnectNetworkCommand = new CommandBase(ConnectToWIFI);
        }

        #region Command handlers
        private async void ConnectToWIFI(object obj)
        {
            if (obj is not /*NetworkIdentifier*/ string ssid)
            {
                return;
            }

            var result = await WinSDKWifiManager.GetInstance().ConnectWifi(ssid.ToString(), PasswordText);
        }

        #endregion

        #region Windows SDK Methods      

        private async void EnableWifi(object o)
        {
            bool state = false;
            if(o is null)
            {
                state = true;
            }
            else if(o is bool condition)
            {
                state = condition;
            }

            //var interfaces = NativeWifi.EnumerateInterfaces();
            //if (interfaces.Any())
            //{
            //    InterfaceInfo deviceInterface = interfaces.First();  //revisit this? Do we even have more than 1 interface
                
            //}
            var result = await WinSDKWifiManager.GetInstance().SetWifiRadioState(state);
            if (!result)
            {
                await Task.Delay(1000);
            }
            if (state)
            {
                await SetWifiList();
            }
        }

        private async Task SetWifiList()
        {
            List<NetworkData> sourceList = await WinSDKWifiManager.GetInstance().ScanWifiNetworks();

            //remove duplicate, i tried. idk if it works tho, maybe revist later
            IEqualityComparer<NetworkData>? compareBySsid = new NetworkDataEqualityComparer();
            var list = new List<NetworkData>();
            list = sourceList.Distinct(compareBySsid).ToList();

            //sort list by strength
            list.OrderBy(n => n.SignalStrength);

            AvailableNetworkList.Clear();

            switch (SelectedBand)
            {
                case 1: //2.4
                    foreach (var item in list.Where(network => network.Frequency >= 2400000 && network.Frequency < 2500000))
                    {
                        AvailableNetworkList.Add(item);
                    }
                    break;

                case 2: //network.Frequency >= 6000000 && network.Frequency < 7100000 for 6 ghz
                    foreach (var item in list.Where(network => network.Frequency >= 5000000 && network.Frequency < 6000000)) // 5 ghz
                    {
                        AvailableNetworkList.Add(item);
                    }
                    break;

                default: //all
                    foreach (var item in list)
                    {
                        AvailableNetworkList.Add(item);
                    }
                    break;
            }
            
        }

        #endregion

        #region ManagedNative Methods
        private void SetWifiRadio(Guid id, bool state)
        {


            //if (state)
            //{
            //    NativeWifi.TurnOnInterfaceRadio(id);
            //}                                                                                                              
            //else
            //{
            //    NativeWifi.TurnOffInterfaceRadio(id);
            //}
        }

        private async void ConnectToNetwork(object o)
        {
            if (o is not NetworkIdentifier Ssid)
            {
                return;
            }

            string ssid = Ssid.ToString();
            AvailableNetworkPack? availableNetwork = NativeWifi.EnumerateAvailableNetworks()
                .FirstOrDefault(n => n.Ssid.ToString() == ssid);

            if (availableNetwork != null)
            {
                //ProfilePack? profile = NativeWifi.EnumerateProfiles().FirstOrDefault(x => x.Name == ssid);

                if (true)
                {
                    foreach( var profile in NativeWifi.EnumerateProfiles())
                    {
                        if (!profile.Name.Equals("SYKNet"))
                            NativeWifi.DeleteProfile(availableNetwork.Interface.Id, profile.Name);
                    }

                    var count = NativeWifi.EnumerateProfiles().Count();
                    //having issues here, need to revisit later
                    var profileXml = GetWpa2PersonalProfileXml(ssid, PasswordText);

                    // create a profile
                    //var profileXml = CreateAndSetProfileXml(ref availableNetwork, PasswordText);
                    var profileResult = NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true);
                    int newCount = NativeWifi.EnumerateProfiles().Count();
                    if (newCount == 0)
                    {
                        Console.WriteLine("Failed to set profile. Cannot proceed with connection.");
                        return;
                    }
                }
                try
                {
                    ProfilePack? profile = NativeWifi.EnumerateProfiles().FirstOrDefault();
                    
                    if (profile != null)
                    {
                        string profileName = CheckAndConvertXmlSpecialCaharcters(profile.Name);

                        bool result = await NativeWifi.ConnectNetworkAsync(
                            availableNetwork.Interface.Id,
                            profileName,
                            availableNetwork.BssType,
                            TimeSpan.FromSeconds(10));

                        if (result)
                        {
                            Console.WriteLine("Successfully connected to the network.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to connect to the network.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while trying to connect: {ex.Message}");
                }
            }
        }
        void WifiEnableExecute(object o)
        {
            NativeWifi.ScanNetworksAsync(TimeSpan.FromMilliseconds(1000));
            AvailableNetworkList.Clear();
            var listWifi = NativeWifi.EnumerateAvailableNetworkGroups();
            var list = NativeWifi.EnumerateBssNetworks();

            //AvailableNetworkList = NativeWifi.EnumerateAvailableNetworks();

            switch (SelectedBand)
            {
                case 1:
                    foreach (var item in listWifi.Where(n => n.Band.Equals(5)))
                    {
                        //if (string.IsNullOrEmpty(item.Ssid.ToString()) && item.Frequency>0)
                        //{
                        //}
                        //item.Ssid = "Hidden Network";
                        AvailableNetworkList.Add(item);
                    }
                    break;

                case 2:
                    foreach (var item in listWifi.Where(n => n.Band.Equals(2.4f)))
                    {
                        AvailableNetworkList.Add(item);
                    }
                    break;

                default:
                    foreach (var item in listWifi)
                    {
                        AvailableNetworkList.Add(item);
                    }
                    break;
            }
        }

        private string CreateAndSetProfileXml(ref AvailableNetworkPack? availableNetwork, string? password)
        {
            string profileXml = string.Empty;
            string eapXml = string.Empty; //What are you??

            if (availableNetwork is not null)
            {
                bool isHidden = false;

                if (string.IsNullOrEmpty(availableNetwork.Ssid.ToString()) && availableNetwork.SignalQuality > 0)
                {
                    isHidden = true;
                }
                string userName = CheckAndConvertXmlSpecialCaharcters(availableNetwork.ProfileName);
                string securityKey = CheckAndConvertXmlSpecialCaharcters(password);
                string isNetworkHidden = isHidden.ToString().ToLower();
                string ssid = CheckAndConvertXmlSpecialCaharcters(availableNetwork.Ssid.ToString());
                string ssidHex = ConvertToHex(availableNetwork.Ssid.ToString());
                string connectionMode = "auto"; //keeping automatic as default for now
                string autoSwitch = "true";
                string encryption = ConvertCipherAlgorithmToString(availableNetwork.CipherAlgorithm);

                try
                {
                    switch (availableNetwork.AuthenticationAlgorithm)
                    {
                        //WEP - OPen
                        case AuthenticationAlgorithm.Open:
                            if (String.IsNullOrEmpty(securityKey))
                            {
                                profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{2}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{3}</connectionMode><MSM><security><authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", ssid, ssidHex, isNetworkHidden, connectionMode);
                            }
                            else
                            {
                                profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{3}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{4}</connectionMode><MSM><security><authEncryption><authentication>open</authentication><encryption>WEP</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>networkKey</keyType><protected>false</protected><keyMaterial>{2}</keyMaterial></sharedKey></security></MSM></WLANProfile>", ssid, ssidHex, securityKey, isNetworkHidden, connectionMode);
                            }

                            //none is AllUser
                            if (NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_RSNA(Wlan.WlanSetProfile)");
                            }
                            break;


                        //WPA2
                        case AuthenticationAlgorithm.RSNA:
                            profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{3}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{4}</connectionMode><autoSwitch>{5}</autoSwitch><MSM><security><authEncryption><authentication>WPA2</authentication><encryption>{2}</encryption><useOneX>true</useOneX><FIPSMode xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v2\">false</FIPSMode></authEncryption><PMKCacheMode>enabled</PMKCacheMode><PMKCacheTTL>720</PMKCacheTTL><PMKCacheSize>128</PMKCacheSize><preAuthMode>disabled</preAuthMode><OneX xmlns=\"http://www.microsoft.com/networking/OneX/v1\"><cacheUserData>true</cacheUserData><authMode>user</authMode><EAPConfig><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>false</DisableUserPromptForServerValidation><ServerNames></ServerNames></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>26</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2ConnectionPropertiesV1\"><UseWinLogonCredentials>false</UseWinLogonCredentials></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></EAPConfig></OneX></security></MSM></WLANProfile>", ssid, ssidHex, encryption, isNetworkHidden, connectionMode, autoSwitch);
                            eapXml = string.Format("<?xml version=\"1.0\"?><EapHostUserCredentials xmlns=\"http://www.microsoft.com/provisioning/EapHostUserCredentials\" xmlns:eapCommon=\"http://www.microsoft.com/provisioning/EapCommon\" xmlns:baseEap=\"http://www.microsoft.com/provisioning/BaseEapMethodUserCredentials\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Credentials><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\"><Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\"><RoutingIdentity xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\">{0}</RoutingIdentity><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\"><Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">26</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\"><Username xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">{0}</Username><Password xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">{1}</Password><LogonDomain xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\" /></EapType></Eap></EapType></Eap></Credentials></EapHostUserCredentials>", userName, securityKey);

                            if (NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_RSNA(Wlan.WlanSetProfile)");
                            }
                            //EapXmlType AllUsers is 1
                            if(NativeWifi.SetProfileEapXmlUserData(availableNetwork.Interface.Id, availableNetwork.ProfileName, EapXmlType.AllUsers, eapXml) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_RSNA(Wlan.WlanSetProfileEapXmlUserData)");
                            }
                            break;

                        //WPA2-PSK.   
                        case AuthenticationAlgorithm.RSNA_PSK:
                            profileXml = string.Format("<?xml version=\"1.0\"?>" +
                                "<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
                                "<name>{0}</name>" +
                                "<SSIDConfig>" +
                                "<SSID><hex>{1}</hex><name>{0}</name></SSID>" +
                                "<nonBroadcast>{4}</nonBroadcast>" +
                                "</SSIDConfig>" +
                                "<connectionType>ESS</connectionType>" +
                                "<connectionMode>{5}</connectionMode>" +
                                "<autoSwitch>{6}</autoSwitch>" +
                                "<MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>{2}</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{3}</keyMaterial></sharedKey></security></MSM></WLANProfile>", ssid, ssidHex, encryption, securityKey, isNetworkHidden, connectionMode, autoSwitch);

                            // profileXml contains sensitive passwords, do not write to log
                            //Debug.WriteLine(profileXml);
                            if (NativeWifi.SetProfile(availableNetwork.Interface.Id,ProfileType.AllUser,profileXml,null,true) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_RSNA_PS(Wlan.WlanSetProfile)");
                            }
                            break;

                        //WPA
                        case AuthenticationAlgorithm.WPA:
                            profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{3}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{4}</connectionMode><autoSwitch>{5}</autoSwitch><MSM><security><authEncryption><authentication>WPA</authentication><encryption>{2}</encryption><useOneX>true</useOneX><FIPSMode xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v2\">false</FIPSMode></authEncryption><PMKCacheMode>enabled</PMKCacheMode><PMKCacheTTL>720</PMKCacheTTL><PMKCacheSize>128</PMKCacheSize><preAuthMode>disabled</preAuthMode><OneX xmlns=\"http://www.microsoft.com/networking/OneX/v1\"><cacheUserData>true</cacheUserData><authMode>user</authMode><EAPConfig><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>false</DisableUserPromptForServerValidation><ServerNames></ServerNames></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>26</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2ConnectionPropertiesV1\"><UseWinLogonCredentials>false</UseWinLogonCredentials></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></EAPConfig></OneX></security></MSM></WLANProfile>", ssid, ssidHex, encryption, isNetworkHidden, connectionMode, autoSwitch);
                            eapXml = string.Format("<?xml version=\"1.0\"?><EapHostUserCredentials xmlns=\"http://www.microsoft.com/provisioning/EapHostUserCredentials\" xmlns:eapCommon=\"http://www.microsoft.com/provisioning/EapCommon\" xmlns:baseEap=\"http://www.microsoft.com/provisioning/BaseEapMethodUserCredentials\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Credentials><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\"><Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\"><RoutingIdentity xmlns=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\">{0}</RoutingIdentity><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\"><Type xmlns=\"http://www.microsoft.com/provisioning/BaseEapUserPropertiesV1\">26</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">" +
                                "<Username xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">{0}</Username><Password xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">{1}</Password><LogonDomain xmlns=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\" /></EapType></Eap></EapType></Eap></Credentials></EapHostUserCredentials>", userName, securityKey);
                            if (NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_WPA(Wlan.WlanSetProfile)");
                            }

                            if (NativeWifi.SetProfileEapXmlUserData(availableNetwork.Interface.Id, availableNetwork.ProfileName, EapXmlType.AllUsers, eapXml) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_WPA(Wlan.WlanSetProfileEapXmlUserData)");
                            }
                            break;

                        //WPA-PSK
                        case AuthenticationAlgorithm.WPA_PSK:

                            profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID><nonBroadcast>{4}</nonBroadcast></SSIDConfig><connectionType>ESS</connectionType><connectionMode>{5}</connectionMode><autoSwitch>{6}</autoSwitch><MSM><security><authEncryption><authentication>WPAPSK</authentication><encryption>{2}</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{3}</keyMaterial></sharedKey></security></MSM></WLANProfile>", ssid, ssidHex, encryption, securityKey, isNetworkHidden, connectionMode, autoSwitch);
                            if (NativeWifi.SetProfile(availableNetwork.Interface.Id, ProfileType.AllUser, profileXml, null, true) == false)
                            {
                                Debug.WriteLine("Error encountered in WifiSetProfileAndConnect in case Wlan.DOT11_AUTH_ALGORITHM.DOT11_AUTH_ALGO_WPA_PSK(Wlan.WlanSetProfile)");
                            }
                            break;
                    }
                }
                catch(Exception exception)
                {
                    throw new Exception(exception.Message, exception);
                }
                finally
                {

                }
            }

            return profileXml;
        }

        private string ConvertCipherAlgorithmToString(CipherAlgorithm cipherAlgorithm)
        {
            switch (cipherAlgorithm)
            {
                case CipherAlgorithm.CCMP:
                    return "AES";
                case CipherAlgorithm.TKIP:
                    return "TKIP";
                case CipherAlgorithm.WEP:
                case CipherAlgorithm.WEP104:
                case CipherAlgorithm.WEP40:
                    return "WEP";
                default:
                    return "NA";
            }
        }

        private string CheckAndConvertXmlSpecialCaharcters(string? input)
        {
            if(string.IsNullOrEmpty(input)) return string.Empty;

            StringBuilder result = new StringBuilder();
            string? xmlcharacter;
            foreach (char c in input)
            {
                xmlcharacter = string.Empty;
                if (dictionaryXmlSpecialCharacter.TryGetValue(c, out xmlcharacter))
                {
                    result.Append(xmlcharacter);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static string ConvertToHex(string asciiString)
        {
            string hex = String.Empty;
            try
            {
                foreach (char c in asciiString)
                {
                    int tmp = c;
                    hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
                }
                return hex.ToUpper();
            }
            finally
            {
            }
        }

        public static string GetWpa2PersonalProfileXml(string? ssid, string? password)
        {
            var profileXml = $@"<?xml version=""1.0""?>
    <WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
        <name>{ssid}</name>
        <SSIDConfig>
            <SSID>
                <name>{ssid}</name>
            </SSID>
        </SSIDConfig>
        <connectionType>ESS</connectionType>
        <connectionMode>auto</connectionMode>
        <MSM>
            <security>
                <authEncryption>
                    <authentication>WPA2PSK</authentication>
                    <encryption>AES</encryption>
                    <useOneX>false</useOneX>
                </authEncryption>
                <sharedKey>
                    <keyType>passPhrase</keyType>
                    <protected>false</protected>
                    <keyMaterial>{password}</keyMaterial>
                </sharedKey>
            </security>
        </MSM>
    </WLANProfile>";
            return profileXml;
        }

        #endregion

        #region NotifyProperty
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(propertyName, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class NetworkDataEqualityComparer : IEqualityComparer<NetworkData>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(NetworkData? x, NetworkData? y)
        {
            if (x == null || y == null)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            return x.Ssid == y.Ssid;
        }

        public int GetHashCode(NetworkData obj)
        {
            if (obj == null)
                return 0;

            return obj.Ssid == null ? 0 : obj.Ssid.GetHashCode();
        }
    }

}
