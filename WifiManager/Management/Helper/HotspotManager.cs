using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.Devices.WiFi;
using Windows.Networking;
using Windows.Security.Credentials;
using Windows.System;
using Windows.Devices.Radios;
using Windows.Data.Xml.Dom;
using System.Xml.Linq;
using System.Security.Cryptography.Xml;
using System.IO;
using System.Net;
using ManagedNativeWifi;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WifiManager.Management.Helper
{
    public class HotspotManager
    {
        private static readonly object _lock = new();
        private static volatile HotspotManager? hotspotManagerObject;
        private static NetworkOperatorTetheringManager? networkOperatorTetheringManager;              

        private HotspotManager()
        {
            InitializeTetheringManager();
        }

        #region Instance
        public static HotspotManager GetInstance()
        {
            try
            {
                if (hotspotManagerObject != null) return hotspotManagerObject;
                lock (_lock)
                {
                    if (hotspotManagerObject == null)
                        hotspotManagerObject = new();
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Can not create instance: " + exception.Message);
            }
            return hotspotManagerObject;
        }

        #endregion

        #region Hotspot Methods
        private void InitializeTetheringManager()
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();

            if (connectionProfile == null)
            {
                connectionProfile = NetworkInformation.GetConnectionProfiles().FirstOrDefault();
            }

            if (connectionProfile != null)
            {
                networkOperatorTetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
            }
        }

        public async Task<bool> TurnHotspotOnAsync(string ssid, string pass)
        {
            bool sucess = false;
            try
            {
                if (networkOperatorTetheringManager != null)
                {
                    await TurnHotspotOffAsync(); //recomended by microsoft

                    if (networkOperatorTetheringManager.TetheringOperationalState == TetheringOperationalState.Off)
                    {
                        NetworkOperatorTetheringAccessPointConfiguration TetheringAccessPoint = new NetworkOperatorTetheringAccessPointConfiguration();
                        TetheringAccessPoint.Ssid = ssid;
                        TetheringAccessPoint.Passphrase = pass;
                        TetheringAccessPoint.Band = TetheringWiFiBand.Auto; //TetheringWiFiBand.FiveGigahertz or TetheringWiFiBand.SixGigahertz

                        //TetheringAccessPoint.AuthenticationKind = TetheringWiFiAuthenticationKind.Wpa2;
                        //var CurrentTetheringAccessPoint = networkOperatorTetheringManager.GetCurrentAccessPointConfiguration();
                        //bool isBand6 = await CurrentTetheringAccessPoint.IsBandSupportedAsync(TetheringWiFiBand.SixGigahertz);
                        await networkOperatorTetheringManager.ConfigureAccessPointAsync(TetheringAccessPoint);
                        var result = await networkOperatorTetheringManager.StartTetheringAsync();
                        if (result.Status == TetheringOperationStatus.Success)
                        {
                            sucess = true;
                            //if (IsBandSupported(networkOperatorTetheringManager.GetCurrentAccessPointConfiguration(), TetheringWiFiBand.SixGigahertz))
                            //{
                            //    //GetFriendlyName(teth);
                            //}
                            //var m_tetheringManager = TryGetCurrentNetworkOperatorTetheringManager();
                            //NetworkOperatorTetheringAccessPointConfiguration configuration = m_tetheringManager.GetCurrentAccessPointConfiguration();
                        }
                    }
                    //else
                    //{
                    //    sucess = true; //already On
                    //}
                }
            }
            catch (Exception)
            {
                throw new Exception("Can not turn On hotspot");
            }

            return sucess;
        }

        public async Task<bool> TurnHotspotOffAsync()
        {
            bool sucess = false;
            try
            {
                if (networkOperatorTetheringManager != null)
                {
                    if (networkOperatorTetheringManager.TetheringOperationalState == TetheringOperationalState.On)
                    {
                        var result = await networkOperatorTetheringManager.StopTetheringAsync();
                        if (result.Status == TetheringOperationStatus.Success)
                        {
                            return true;
                        }
                        await Task.Delay(500);
                    }
                    int retry = 0;
                    while (networkOperatorTetheringManager.TetheringOperationalState == TetheringOperationalState.InTransition
                        || networkOperatorTetheringManager.TetheringOperationalState == TetheringOperationalState.On)
                    {
                        if (retry++ == 2)
                            throw new Exception("Can not turn off hotspot");
                        if (networkOperatorTetheringManager.TetheringOperationalState == TetheringOperationalState.On)
                        {
                            var result = await networkOperatorTetheringManager.StopTetheringAsync();
                            if (result.Status == TetheringOperationStatus.Success)
                                break;
                            await Task.Delay(500);
                        }
                        await Task.Delay(1000);
                    }

                    sucess = true;
                }
            }
            catch (Exception exception)
            {
                sucess = false;
                throw new Exception(exception.Message, exception);
            }

            return sucess;
        }

        public static NetworkOperatorTetheringManager? TryGetCurrentNetworkOperatorTetheringManager()
        {
            // Get the connection profile
            ConnectionProfile currentConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (currentConnectionProfile == null)
            {
                Debug.WriteLine("System is not connected to the Internet.");
                return null;
            }

            TetheringCapability tetheringCapability =
                NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(currentConnectionProfile);

            if (tetheringCapability != TetheringCapability.Enabled)
            {
                string message;
                switch (tetheringCapability)
                {
                    case TetheringCapability.DisabledByGroupPolicy:
                        message = "Tethering is disabled due to group policy.";
                        break;
                    case TetheringCapability.DisabledByHardwareLimitation:
                        message = "Tethering is not available due to hardware limitations.";
                        break;
                    case TetheringCapability.DisabledByOperator:
                        message = "Tethering operations are disabled for this account by the network operator.";
                        break;
                    case TetheringCapability.DisabledByRequiredAppNotInstalled:
                        message = "An application required for tethering operations is not available.";
                        break;
                    case TetheringCapability.DisabledBySku:
                        message = "Tethering is not supported by the current account services.";
                        break;
                    case TetheringCapability.DisabledBySystemCapability:
                        // This will occur if the "wiFiControl" capability is missing from the App.
                        message = "This app is not configured to access Wi-Fi devices on this machine.";
                        break;
                    default:
                        message = $"Tethering is disabled on this machine. (Code {(int)tetheringCapability}).";
                        break;
                }
                Debug.WriteLine(message);
                return null;
            }

            const int E_NOT_FOUND = unchecked((int)0x80070490); // HRESULT_FROM_WIN32(ERROR_NOT_FOUND)

            try
            {
                return NetworkOperatorTetheringManager.CreateFromConnectionProfile(currentConnectionProfile);
            }
            catch (Exception ex) when (ex.HResult == E_NOT_FOUND)
            {
                Debug.WriteLine("System has no Wi-Fi adapters.");
                return null;
            }
        }

        #endregion

        const int E_INVALID_STATE = unchecked((int)0x8007139f); // HRESULT_FROM_WIN32(ERROR_INVALID_STATE)

        
    }
}
