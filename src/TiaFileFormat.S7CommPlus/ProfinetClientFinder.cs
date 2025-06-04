using PacketDotNet;
using SharpPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace TiaFileFormat.S7CommPlus
{
    public class ProfinetClientFinder : IDisposable
    {
        public Action<ProfinetClient> ProfinetClientFound { get; set; }

        public class ProfinetClient
        {
            public PhysicalAddress Mac { get; set; }
            public IPAddress IPAddress { get; set; }
            public IPAddress Subnetmask { get; set; }
            public IPAddress Gateway { get; set; }
            public string StationType { get; set; }
            public string StationName { get; set; }

            public override string ToString()
            {
                return string.Join(':', Mac.GetAddressBytes().Select(x => x.ToString("X2"))) + " (" + (IPAddress?.ToString() ?? "") + ", " + (StationType ?? "") + ", " + (StationName ?? "") + ")";
            }
        }

        private List<ILiveDevice> filteredDevices;

        public void Find()
        {
            // Netzwerkschnittstelle auswählen
            var devices = CaptureDeviceList.Instance;
            filteredDevices = devices.Where(x => x.MacAddress != null).ToList();
            Task.Run(() =>
            {
                Parallel.ForEach(filteredDevices.Skip(3).Take(1), device =>
                {
                    FindOnNetworkDevice(device);
                });
            });
        }

        private void FindOnNetworkDevice(ILiveDevice device)
        {
            try
            {
                device.Open(DeviceModes.Promiscuous);

                var mac = device.MacAddress.GetAddressBytes();
                // DCP-Identify-Request (Broadcast) senden
                byte[] dcpRequest = new byte[]
                {
                    0x01, 0x0E, 0xCF, 0x00, 0x00, 0x00, // PROFINET Multicast MAC
                    mac[0], mac[1], mac[2], mac[3], mac[4], mac[5], // Eigene MAC (anpassen!)
                    0x88, 0x92, // PROFINET Ethertype
                    0xFE, 0xFE, 0x05, 0x00, // DCP
                    0x11, 0x01, 0x00, 0x0b,
                    0x00, 0x80,
                    0x00, 0x04,
                    0xff, 0xff, 0x00, 0x00
                };

                device.SendPacket(dcpRequest);

                device.OnPacketArrival += (sender, e) =>
                {
                    var rawPaket = e.GetPacket();
                    var packet = Packet.ParsePacket(rawPaket.LinkLayerType, rawPaket.Data);
                    var ethPacket = (EthernetPacket)packet;

                    if (ethPacket.Type == EthernetType.Profinet)
                    {
                        var rawData = ethPacket.PayloadData;
                        if (rawData.Length > 12) // Minimale Länge prüfen
                        {
                            var pc = new ProfinetClient() { Mac = ethPacket.SourceHardwareAddress };
                            ParseDcpResponse(pc, rawData);
                            if (pc.IPAddress != null && pc.StationType != null && pc.StationName != null)
                            {
                                ProfinetClientFound?.Invoke(pc);
                            }
                        }
                    }
                };

                device.StartCapture();
            }
            catch (Exception)
            {
            }
        }

        private void ParseDcpResponse(ProfinetClient pc, byte[] data)
        {
            int index = 12; // Profinet Header & DCP-Header überspringen

            while (index < data.Length)
            {
                if (index + 4 > data.Length) break;

                byte option = data[index];      // Option (z. B. 0x02 = Name of Station)
                byte subOption = data[index + 1]; // SubOption
                ushort length = BitConverter.ToUInt16(data.Skip(index + 2).Take(2).Reverse().ToArray(), 0);
                var blockinfo = data.Skip(index + 4).Take(2).ToArray();
                index += 4;

                if (index + length > data.Length) break;
                byte[] value = data.Skip(index).Take(length).ToArray();
                length += (ushort)(length % 2);
                index += length;

                if (option == 0x02 && subOption == 0x01)
                {
                    string stationType = Encoding.ASCII.GetString(value).Trim('\0');
                    pc.StationType = stationType;
                }
                else if (option == 0x02 && subOption == 0x02)
                {
                    string stationName = Encoding.ASCII.GetString(value).Trim('\0');
                    pc.StationName = stationName;
                }
                else if (option == 0x01 && subOption == 0x02 && blockinfo[0] == 0x00 && blockinfo[1] == 0x01 && length == 14)
                {
                    pc.IPAddress = new IPAddress(value.Skip(2).Take(4).ToArray());
                    pc.Subnetmask = new IPAddress(value.Skip(6).Take(4).ToArray());
                    pc.Gateway = new IPAddress(value.Skip(10).Take(4).ToArray());
                }
            }
        }

        public void Dispose()
        {
            if (filteredDevices != null)
            {
                foreach (var d in filteredDevices)
                {
                    try
                    {
                        d.Close();
                    }
                    catch { }
                }
            }
        }
    }
}
