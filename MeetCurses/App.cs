using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Manos.IO;
using Mono.Terminal;
using Twitterizer;

namespace MeetCurses
{
	public class Line
	{
		public static void DrawH(Widget widget, int x, int y, int width, ColorPair color)
		{
			Curses.attron(color.Attribute);
			DrawH(widget, x, y, width);
			Curses.attron(ColorPair.From(-1, -1).Attribute);
		}

		public static void DrawH(Widget widget, int x, int y, int width)
		{
			for (int i = 0; i < width; i++) {
				widget.Set(x + i, y, ACS.HLINE);
			}
		}

		public static void DrawV(Widget widget, int x, int y, int height, ColorPair color)
		{
			Curses.attron(color.Attribute);
			DrawV(widget, x, y, height);
			Curses.attron(ColorPair.From(-1, -1).Attribute);
		}

		public static void DrawV(Widget widget, int x, int y, int height)
		{
			for (int i = 0; i < height; i++) {
				widget.Set(x, y + i, ACS.VLINE);
			}
		}
	}

	public class App
	{
		public static int Key { get; set; }

		static bool updating;
		public static bool Updating {
			get {
				return updating;
			}
			set {
				updating = value;
				MainWindow.Statusbar.Invalid = true;
			}
		}

		static bool tweeting;
		public static bool Tweeting {
			get {
				return tweeting;
			}
			set {
				tweeting = value;
				MainWindow.Statusbar.Invalid = true;
			}
		}

		static TwitterUser userInformation;
		public static TwitterUser UserInformation {

			get {
				return userInformation;
			}
			set {
				userInformation = value;
				MainWindow.Statusbar.Invalid = true;
			}
		}

		public static readonly int Accent = 202;
		public static readonly int Normal = 255;
		public static readonly int Brace = 241;
		public static readonly int Background = 237;

		public static MainWindow MainWindow { get; protected set; }

		public static ManosTwitter ManosTwitter { get; private set; }

		private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;

			if (sslPolicyErrors == SslPolicyErrors.None) {
				return true;
			}

			// HACK: Mono's certificate chain validator is buggy
			if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && sender is HttpWebRequest) {
				var request = (HttpWebRequest)sender;
				if (request.RequestUri.Host == "api.twitter.com" &&
					certificate.Issuer == "OU=Equifax Secure Certificate Authority, O=Equifax, C=US") {
					return true;
				}
			}

			return false;
		}

		public static Configuration Configuration = (Configuration)Configuration.Deserialize("MeetCurses.xml", typeof(Configuration));

		public static void Main(string[] args)
		{
			ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;

			if (Configuration.User.AccessToken == null) {
				Console.WriteLine("Retrieving Configuration Token ...");

				var token = Configuration.User.GetRequestToken();
				Uri uri = OAuthUtility.BuildAuthorizationUri(token.Token);

				Console.WriteLine(uri);

				Console.Write("Enter verficiation code: ");
				Configuration.User.Update(token, Console.ReadLine());

				Configuration.Serialize("MeetCurses.xml", Configuration);
			}



			Application.Init(Context.Create(Backend.Poll));

			MainWindow = new MainWindow();
			MainWindow.Timeline.Load("homeline.xml");

			ManosTwitter = new ManosTwitter(Application.Context, App.Configuration.User.GetOAuthTokens());
			Application.Run(new FullsizeContainer(App.MainWindow));

			MainWindow.Timeline.Save("homeline.xml");
		}
	}
}

