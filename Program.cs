using System;
using System.Collections.Generic;
using ConsoleUI;
using Bespoke.Common.Osc;
using System.Threading;

namespace OSCDebugger
{
	class MainClass
	{
		Receiver receiver;
		bool run;
		bool isPaused;

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
		}

		public void Stop ()
		{
			run = false;
		}

		void ProcessInput (ConsoleKeyInfo key)
		{
			switch (key.Key) {
			case ConsoleKey.Spacebar:
				isPaused = !isPaused;
				receiver.IsPaused = isPaused;
				break;
			}
		}

		public void Draw ()
		{
			receiver.QueueLength = Console.WindowHeight - 3; // account for header and border

			// Header
			CUI.SetArea (0, 0, Console.WindowWidth, 1);
			CUI.Clear ();
			CUI.DrawString (isPaused ? "Paused" : "Running");

			int leftPanelWidth = (Console.WindowWidth / 2) - 2;
//			if (!isPaused) {
				leftPanelWidth = Console.WindowWidth - 1;
//			}

			// Draw Left panel
			CUI.SetArea (1, 1, leftPanelWidth, Console.WindowHeight - 1);
			CUI.Clear ();
			CUI.LineStyle = LineStyle.Normal;
			CUI.DrawAreaBorder ();

			// Display OSC messages
			CUI.CursorPosition = new Position (0, 0);
			foreach (var packet in receiver.PacketQueue) {
				var message = packet.Address;
				foreach (var arg in packet.Data) {
					message += " " + arg.ToString ();
				}
				CUI.DrawString (message);
				CUI.MoveCursorDown ();
			}

//			if (isPaused) {
//				// Draw right panel
//				var width = Console.WindowWidth / 2;
//				CUI.SetArea (width, 1, width, Console.WindowHeight - 1);
//				CUI.Clear ();
//				CUI.LineStyle = LineStyle.Normal;
//				CUI.DrawAreaBorder ();
//			}
		}
	}
}
