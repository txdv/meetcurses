using System;
using Mono.Terminal;

namespace MeetCurses
{
	public class Statusbar : Widget
	{
		public Statusbar()
		{
			Invalid = true;
		}

		public override void Redraw()
		{
			string text = "";

			if (App.UserInformation != null) {
				text += string.Format("[{0}] {1} tweets, {2} following, {3} followers ",
					App.UserInformation.ScreenName,
					App.UserInformation.NumberOfStatuses,
					App.UserInformation.NumberOfFollowers,
					App.UserInformation.NumberOfFriends);
			}
			text += string.Format("[{0}] ", 140 - App.MainWindow.Entry.Text.Length);
			if (App.Updating) {
				text += "[Updating] ";
			}
			if (App.Tweeting) {
				text += "[Tweeting] ";
			}

			text += App.Key;
			text += " ";
			ColorString.Escape(text, (ch) => {
				switch (ch) {
				case '[':
				case ']':
				case ',':
					return ColorPair.From(App.Accent, App.Background);
				}
				return ColorPair.From(App.Normal, App.Background);

			}).Fill(this);
			Curses.attron(ColorPair.From(-1, -1).Attribute);
		}
	}
}

