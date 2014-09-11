using System;
using System.Collections.Generic;
using ConsoleUI;
using Bespoke.Common.Osc;
using System.Threading;
using System.Linq;

namespace OSCDebugger
{
	class MainClass
	{
		Receiver receiver;
		bool run;
		bool isPaused;
		int selectedIndex;

		string localAddress;
//		string localSubnet;

		public static void Main (string[] args)
		{
			MainClass m = new MainClass ();

			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
				e.Cancel = true;
				m.Stop ();
			};

			m.receiver = new Receiver ();
			m.receiver.Initialize ();
			m.Start ();
		}

		public void Start ()
		{
			// Initial GUI setup
			Console.Title = "OSC Debugger";
			CUI.SetAreaFullScreen ();
			CUI.Clear ();

			var ip = receiver.GetLocalAddress ();
			localAddress = ip.ToString ();
			if (localAddress == "255.255.255.255") {
				localAddress = "Unknown";
			}
//			localSubnet = receiver.GetSubnet (ip).ToString ();

			DrawHeader ();

			// Main loop
			run = true;
			while (run) {
				refreshPacketList = receiver.NewMessagesAvailable;
				receiver.Update ();
				while (Console.KeyAvailable) {
					var key = Console.ReadKey (true);
					ProcessInput (key);
				}
				Tick ();
				Thread.Sleep (100);
			}

			// Cleanup
			Console.WriteLine ("Exiting");
		}

		public void Stop ()
		{
			run = false;
		}

		bool refreshPanels = true;
		bool refreshPacketList;

		void ProcessInput (ConsoleKeyInfo key)
		{
			switch (key.Key) {
			case ConsoleKey.Q:
				run = false;
				break;
			case ConsoleKey.Spacebar:
				selectedIndex = 0;
				isPaused = !isPaused;
				refreshPanels = true;
				refreshPacketList = true;
				receiver.IsPaused = isPaused;
				break;
			case ConsoleKey.UpArrow:
				refreshPanels = true;
				refreshPacketList = true;
				selectedIndex = Clamp (selectedIndex - 1, 0, receiver.PacketQueue.Count - 1);
				break;
			case ConsoleKey.DownArrow:
				refreshPanels = true;
				refreshPacketList = true;
				selectedIndex = Clamp (selectedIndex + 1, 0, receiver.PacketQueue.Count - 1);
				break;
			}
		}

		public void Tick ()
		{
			int panelHeight = Console.WindowHeight - 4;

			// Message panel
			receiver.QueueLength = panelHeight; // account for header and border
			if (!isPaused) {
				DrawUnifiedBox (refreshPanels);
			} else {
				DrawLeftBox (refreshPanels);
			}
			if (refreshPacketList) {
				DisplayOSCMessages ();
			}

			// Packet info panel
			if (isPaused) {
				DrawRightBox (refreshPanels);
				if (receiver.PacketQueue.Count > 0) {
					var packet = receiver.PacketQueue.ElementAt (selectedIndex);
					DisplayPacketInfo (packet);
				}
			}

			if (isPaused) {
				DrawCursor ();
			}

			DrawStatusLine ();

			refreshPanels = false;
			refreshPacketList = false;
		}

		public void DrawHeader ()
		{
			// Draw Header
			CUI.SetArea (1, 0, Console.WindowWidth, 1);
			CUI.DrawString ("IP: " + localAddress + "   Port: " + receiver.Port);
			//			CUI.DrawString (16, 0, "Subnet: " + localSubnet);
		}

		public void DrawStatusLine ()
		{
			CUI.SetArea (1, Console.WindowHeight - 1, Console.WindowWidth, 1);
			CUI.DrawString (isPaused ? "Paused     " : "Running     ");
		}

		public void DrawCursor ()
		{
//			var prevArea = CUI.CurrentArea;
			CUI.SetArea (0, selectedIndex + 2, 1, 1);
//			if (isSelected) {
				CUI.DrawCharacter ('■');
//			} else {
//				CUI.DrawVerticalDivider ();
//			}
//			CUI.SetArea (prevArea);
		}

		public void DrawUnifiedBox (bool refresh)
		{
			CUI.SetArea (0, 1, Console.WindowWidth, Console.WindowHeight - 2);
			if (refresh) {
				CUI.Clear ();
				CUI.LineStyle = LineStyle.Normal;
				CUI.DrawAreaBorder ();
			} else {
				CUI.ScaleAreaCentered (-1, -1);
			}
		}

		public void DrawLeftBox (bool refresh)
		{
			int panelWidth = (Console.WindowWidth / 2);
			CUI.SetArea (0, 1, panelWidth, Console.WindowHeight - 2);
			if (refresh) {
				CUI.Clear ();
				CUI.LineStyle = LineStyle.Normal;
				CUI.DrawAreaBorder ();
			} else {
				CUI.ScaleAreaCentered (-1, -1);
			}
		}

		public void DrawRightBox (bool refresh)
		{
			int panelWidth = (Console.WindowWidth / 2);
			CUI.SetArea (panelWidth, 1, panelWidth, Console.WindowHeight - 2);
			if (refresh) {
				CUI.Clear ();
				CUI.LineStyle = LineStyle.Normal;
				CUI.DrawAreaBorder ();
			} else {
				CUI.ScaleAreaCentered (-1, -1);
			}
		}

		public void DisplayOSCMessages ()
		{
			CUI.CursorPosition = Position.Zero;
			int index = 0;

			CUI.Clear ();

			foreach (var packet in receiver.PacketQueue) {
				var message = packet.Address;
				foreach (var arg in packet.Data) {
					message += " " + arg.ToString ();
				}
				CUI.DrawString (message);
				CUI.MoveCursorDown ();
				index++;
			}
		}

		public void DisplayPacketInfo (OscPacket packet)
		{
			CUI.DrawString ("Source:    " + packet.SourceEndPoint.ToString ());
			CUI.MoveCursorDown (2);
			CUI.DrawString ("IsBundle:    " + packet.IsBundle.ToString ());
			CUI.MoveCursorDown (2);
			CUI.DrawString ("Arguments:");
			CUI.MoveCursorDown (2);
			CUI.MoveCursorRight ();
			CUI.MoveCursorRight ();
			if (packet.Data.Count == 0) {
				CUI.DrawString ("none");
			} else {
				foreach (var arg in packet.Data) {
					if (arg is string) {
						CUI.DrawString ("(string) " + (string)arg);
					} else if (arg is Int32) {
						CUI.DrawString ("(int32)  " + (Int32)arg);
					} else if (arg is Int64) {
						CUI.DrawString ("(int64)  " + (Int64)arg);
					} else if (arg is float) {
						CUI.DrawString ("(float)  " + (float)arg);
					} else if (arg is double) {
						CUI.DrawString ("(double) " + (double)arg);
					} else if (arg is byte[]) {
						CUI.DrawString ("(byte[]) " + ((byte[])arg).Length + " bytes");
					} else if (arg is char) {
						CUI.DrawString ("(char)   " + (char)arg);
					} else if (arg is bool) {
						CUI.DrawString ("(bool)   " + (bool)arg);
					} else {
						CUI.DrawString ("(????)   " + "unknown");
					}
					CUI.MoveCursorDown ();
				}
			}
		}

		int Clamp (int val, int min, int max)
		{
			return Math.Min (Math.Max (val, min), max);
		}
	}
}
