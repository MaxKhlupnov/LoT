using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Platform.ManagedWifi
{
    public class WifiHelper
    {
        public static string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        public static string MakeProfile(string profileName, Wlan.WlanAvailableNetwork network, string key)
        {
            switch (network.dot11DefaultAuthAlgorithm)
            {
                case Wlan.Dot11AuthAlgorithm.WPA_PSK:
                    return MakeWpaPskProfile(profileName, network, key);
                case Wlan.Dot11AuthAlgorithm.RSNA_PSK:
                    return MakeWpa2PskProfile(profileName, network, key);
                case Wlan.Dot11AuthAlgorithm.IEEE80211_Open:
                    return MakeOpenProfile(profileName, network);
                default:
                    throw new Exception("unsupported auth algorithm: " + network.dot11DefaultAuthAlgorithm);
            }
        }

        static string MakeOpenProfile(string profileName, Wlan.WlanAvailableNetwork network)
        {

            string ssid = GetStringForSSID(network.dot11Ssid);
            string encryption = CipherToStringInProfile(network.dot11DefaultCipherAlgorithm);

            string profile = string.Format(
@"<?xml version=""1.0"" encoding=""US-ASCII""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{0}</name>
    <SSIDConfig>
        <SSID>
            <name>{1}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <autoSwitch>false</autoSwitch>
    <MSM>
        <security>
            <authEncryption>
                <authentication>open</authentication>
                <encryption>{2}</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
        </security>
    </MSM>
</WLANProfile>
", profileName, ssid, encryption);

            return profile;
        }

        static string MakeWpaPskProfile(string profileName, Wlan.WlanAvailableNetwork network, string key)
        {

            string ssid = GetStringForSSID(network.dot11Ssid);
            string encryption = CipherToStringInProfile(network.dot11DefaultCipherAlgorithm);

            string profile = string.Format(
@"<?xml version=""1.0"" encoding=""US-ASCII""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{0}</name>
    <SSIDConfig>
        <SSID>
            <name>{1}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <autoSwitch>false</autoSwitch>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPAPSK</authentication>
                <encryption>{2}</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
               <keyType>passPhrase</keyType>
               <protected>false</protected>
               <keyMaterial>{3}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>
", profileName, ssid, encryption, key);

            return profile;
        }

        static string MakeWpa2PskProfile(string profileName, Wlan.WlanAvailableNetwork network, string key)
        {

            string ssid = GetStringForSSID(network.dot11Ssid);
            string encryption = CipherToStringInProfile(network.dot11DefaultCipherAlgorithm);

            string profile = string.Format(
@"<?xml version=""1.0"" encoding=""US-ASCII""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{0}</name>
    <SSIDConfig>
        <SSID>
            <name>{1}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <autoSwitch>false</autoSwitch>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>{2}</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
               <keyType>passPhrase</keyType>
               <protected>false</protected>
               <keyMaterial>{3}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>
", profileName, ssid, encryption, key);

            return profile;
        }

        static string CipherToStringInProfile(Wlan.Dot11CipherAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case Wlan.Dot11CipherAlgorithm.None:
                    return "none";
                case Wlan.Dot11CipherAlgorithm.WEP:
                case Wlan.Dot11CipherAlgorithm.WEP104:
                case Wlan.Dot11CipherAlgorithm.WEP40:
                    return "WEP";
                case Wlan.Dot11CipherAlgorithm.TKIP:
                    return "TKIP";
                case Wlan.Dot11CipherAlgorithm.CCMP:
                    return "AES";
                default:
                    throw new Exception("Unsupported cipher algorithm: " + algorithm);
            }

        }

    }
}
