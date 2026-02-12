using TMPro;
using UnityEngine;
using Mirror;
using System.Net;

public class IPAddressUI : MonoBehaviour
{
    public static IPAddressUI Instance;

    [SerializeField] private TextMeshProUGUI ipText;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowLocalIP()
    {
        string ip = GetLocalIPAddress();
        ipText.text = $"Current IP: {ip}";
    }

    public void Clear()
    {
        ipText.text = "";
    }

    private string GetLocalIPAddress()
    {
        string localIP = "Unknown";

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch { }

        return localIP;
    }

    public void ShowRemainingTime(int seconds)
    {
        ipText.text = $"Remaining Time: {seconds} seconds";
    }

}
