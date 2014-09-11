using System;
using System.Collections.Generic;
using Bespoke.Common.Osc;
using Bespoke.Common.Net;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OSCDebugger
{
	public class Receiver : IDisposable
	{
		public Queue<OscPacket> PacketQueue = new Queue<OscPacket> ();
		public int QueueLength = 50;
		public bool IsPaused { get; set; }

		OscServer server;

		Queue<OscPacket> producerQueue = new Queue<OscPacket>();
		Queue<OscPacket> consumerQueue = new Queue<OscPacket>();

		public IPAddress GetLocalAddress ()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily.ToString() == "InterNetwork") {
					return ip;
				}
			}
			return null;
		}

//		public IPAddress GetSubnet (IPAddress address)
//		{
//			foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
//				foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses) {
//					if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
//						if (address.Equals(unicastIPAddressInformation.Address)) {
//							return unicastIPAddressInformation.IPv4Mask;
//						}
//					}
//				}
//			}
//			throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
//		}

		int port = 9000;
		public int Port {
			get {
				return port;
			}
			set {
				if (value != port) {
					port = value;
					Initialize ();
				}
			}
		}

		void HandleMessageReceived (object sender, OscMessageReceivedEventArgs e)
		{
			if (!IsPaused) {
				lock (producerQueue) {
					producerQueue.Enqueue(e.Message);
				}
			}
		}

		public void Update()
		{
			if (producerQueue.Count > 0) {
				lock (producerQueue) {
					while (producerQueue.Count > 0) {
						consumerQueue.Enqueue(producerQueue.Dequeue());
					}
				}
				while (consumerQueue.Count > 0) {
					PacketQueue.Enqueue (consumerQueue.Dequeue ());
				}
				// Discard packets that overflow the queue
				while (PacketQueue.Count > QueueLength) {
					PacketQueue.Dequeue ();
				}
			}
		}

//		public IPAddress BroadcastAddress {
//			get {
//				return IPAddress.Broadcast;
//			}
//		}



		public void Initialize ()
		{
			if (server != null) {
				if (server.IsRunning) {
					server.Stop ();
				}
			}
			server = new OscServer (IPAddress.Parse ("255.255.255.255"), Port, TransmissionType.Broadcast);
			server.FilterRegisteredMethods = false;
			server.Start ();
			server.MessageReceived += HandleMessageReceived;
		}

		public void Dispose ()
		{
			server.MessageReceived -= HandleMessageReceived;
		}
	}
}

