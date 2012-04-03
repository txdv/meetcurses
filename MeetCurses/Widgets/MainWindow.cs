using System;
using Mono.Terminal;
using Twitterizer;

namespace MeetCurses
{
	public class MainWindow : VBox
	{
		public Timeline Timeline { get; protected set; }
		public Statusbar Statusbar { get; protected set; }
		public PrefixEntry Entry { get; protected set; }

		public MainWindow()
		{
			Statusbar = new Statusbar() { Height = 1 };
			Timeline = new Timeline();
			Entry = new PrefixEntry() {
				Height = 1,
				ColorPrefix = Theme.Escape(string.Format("[{0}] ", App.Configuration.User.ScreenName))
			};

			this.Add(Timeline, Box.Setting.Fill);
			this.Add(Statusbar, Box.Setting.Size);
			this.Add(Entry, Box.Setting.Size);

			Entry.Enter += () => {
				App.Tweeting = true;
				App.ManosTwitter.Tweet(Entry.Text, (response) => {
					App.Tweeting = false;
					if (response.Result == RequestResult.Success) {
						Timeline.Add(response.ResponseObject);
						if (App.UserInformation != null) {
							App.UserInformation.NumberOfStatuses++;
						}
					}
				});
				Entry.Text = string.Empty;
			};

			Entry.TextChanged += (_) => {
				Statusbar.Invalid = true;
			};
		}

		public override bool ProcessKey(int key)
		{
			if (!Timeline.ProcessKey(key)) {
				return Entry.ProcessKey(key);
			}
			return false;
		}
	}
}
