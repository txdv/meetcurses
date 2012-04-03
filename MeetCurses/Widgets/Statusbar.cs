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
				text += string.Format("{0} tweets, {1} following, {2} followers ",
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

			Theme.Escape(text, Theme.Background).Fill(this);
			ColorString.Finish();
		}
	}
}

