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
			Console.Title = "OSC Debugger";
			CUI.SetAreaFullScreen ();
			CUI.Clear ();
			run = true;

			var ip = receiver.GetLocalAddress ();
			localAddress = ip.ToString ();
//			localSubnet = receiver.GetSubnet (ip).ToString ();

			// Main loop
			while (run) {
				receiver.Update ();
				while (Console.KeyAvailable) {
					var key = Console.ReadKey (true);
					ProcessInput (key);
				}
				Draw ();
				Thread.Sleep (100);
			}

			// Cleanup
			Console.WriteLine ("Exiting gracefully");

			receiver.GetLocalAddress ();
		}

		public void Stop ()
		{
			run = false;
		}

		void ProcessInput (ConsoleKeyInfo key)
		{
			switch (key.Key) {
			case ConsoleKey.Spacebar:
				selectedIndex = 0;
				isPaused = !isPaused;
				receiver.IsPaused = isPaused;
				break;
			case ConsoleKey.UpArrow:
				selectedIndex = Clamp (selectedIndex - 1, 0, receiver.PacketQueue.Count - 1);
				break;
			case ConsoleKey.DownArrow:
				selectedIndex = Clamp (selectedIndex + 1, 0, receiver.PacketQueue.Count - 1);
				break;
			}
		}

		int Clamp (int val, int min, int max)
		{
			return Math.Min (Math.Max (val, min), max);
		}

		public void Draw ()
		{
			int panelHeight = Console.WindowHeight - 4;
			int panelWidth = isPaused ? (Console.WindowWidth / 2) : Console.WindowWidth;

			// Header
			CUI.SetArea (1, 0, Console.WindowWidth, 1);
			CUI.Clear ();
			CUI.DrawString ("IP: " + localAddress + "   Port: " + receiver.Port);
//			CUI.DrawString (16, 0, "Subnet: " + localSubnet);

			// Left message panel
			receiver.QueueLength = panelHeight; // account for header and border
			CUI.SetArea (0, 1, panelWidth, Console.WindowHeight - 2);
			CUI.Clear ();
			CUI.LineStyle = LineStyle.Normal;
			CUI.DrawAreaBorder ();

			// Display OSC messages
			CUI.CursorPosition = Position.Zero;
			int index = 0;

			foreach (var packet in receiver.PacketQueue) {
				// Display selection cursor
				if (isPaused && index == selectedIndex) {
					CUI.MoveArea (-1, 0);
					CUI.DrawCharacter ('■');
					CUI.MoveArea (1, 0);
				}
				var message = packet.Address;
				foreach (var arg in packet.Data) {
					message += " " + arg.ToString ();
				}
				CUI.DrawString (message);
				CUI.MoveCursorDown ();
				index++;
			}

			// Right inspector panel
			if (isPaused) {
				CUI.SetArea (panelWidth, 1, panelWidth, Console.WindowHeight - 2);
				CUI.Clear ();
				CUI.LineStyle = LineStyle.Normal;
				CUI.DrawAreaBorder ();
				// Display packet info
				if (receiver.PacketQueue.Count > 0) {
					var packet = receiver.PacketQueue.ElementAt (selectedIndex);
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
			}

			// Status line
			CUI.SetArea (1, Console.WindowHeight - 1, Console.WindowWidth, 1);
//			CUI.Clear ();
			CUI.DrawString (isPaused ? "Paused     " : "Running     ");
		}
	}
}
