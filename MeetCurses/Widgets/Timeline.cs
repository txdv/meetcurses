using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Twitterizer;
using Twitterizer.Streaming;
using Mono.Terminal;
using Manos.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MeetCurses
{
	public class TimelineDataConfiguration : BaseConfiguration
	{
		public TwitterStatus[] StatusCollection { get; set; }
	}

	public class Timeline : Widget
	{
		int position;
		public int Position {
			get {
				return position;
			}
			protected set {
				int val = 0;
				if (value >= 0) {
					val = value;
				}

				if (position != val) {
					int old = position;
					position = val;
					OnChangePosition(old);
				}
			}
		}

		public Action<int> ChangePosition;

		protected void OnChangePosition(int pos)
		{
			if (ChangePosition != null) {
				ChangePosition(pos);
			}
		}

		SortedList<decimal, TwitterStatus> stats = new SortedList<decimal, TwitterStatus>();
		public Timeline()
			: base()
		{
			position = 0;
			ChangePosition += (_) => {
				Invalid = true;
				App.MainWindow.Statusbar.Invalid = true;
			};
		}

		void Update()
		{
			Update(1);
		}

		void Update(int page, int count = 100)
		{
			if (!App.Updating) {
				App.Updating = true;

				var options = new TimelineOptions();

				options.Page = page;

				if (stats.Count == 0) {
					options.Count = count;
				} else {
					options.SinceStatusId = stats.Last().Value.Id;
				}

				App.ManosTwitter.HomeTimeline(options, (response) => {
					App.Updating = false;
					if (response.Result == RequestResult.Success) {
						foreach (var status in response.ResponseObject) {
							if (!stats.ContainsKey(status.Id)) {
								stats.Add(status.Id, status);
							}
						}
						StartStream();
					}
					Invalid = true;
				});
			}
		}

		TwitterStream stream = null;
		public bool Streaming {
			get {
				return stream != null;
			}
		}

		void StartStream()
		{
			if (stream == null) {
				App.ManosTwitter.UserStream((s, callbacks) => {
					stream = s;
					callbacks.StatusCreated += Add;
				});
			}
		}

		void StopStream()
		{
			stream.EndStream();
			stream = null;
		}

		public void Add(TwitterStatus status)
		{
			if (!stats.ContainsKey(status.Id)) {
				stats.Add(status.Id, status);
				Invalid = true;
			}
		}

		int LongestUserName(SortedList<decimal, TwitterStatus> collection)
		{
			return collection.OrderBy((status) => status.Value.User.Name.Length)
				.Select((status) => status.Value.User.Name.Length)
				.LastOrDefault();
		}

		public override void Redraw()
		{
			if (stats.Count == 0) {
				return;
			}

			int length = LongestUserName(stats);

			int space = 1;

			Line.DrawV(this, length + space, 0, Height, ColorPair.From(App.Background, -1));

			int textStart = length + 1 + space * 2;
			int textWidth = Width - textStart;

			int height = Height - 1;

			for (int i = stats.Values.Count - 1 - Position; i >= 0; i--) {
				var status = stats.Values[i];
				string text = status.Text;
				int h = (int)Math.Ceiling((double)text.Length / textWidth);
				Fill(status.User.Name, 0, height, length, h);
				Fill(text, textStart, height, textWidth, h);
				height -= h;
			}

			Fill(' ', 0, 0, length, height + 1);
			Fill(' ', textStart, 0, textWidth, height + 1);
		}

		void UpdateUserInformation()
		{
			App.ManosTwitter.User((response) => {
				if (response.Result == RequestResult.Success) {
					App.UserInformation = response.ResponseObject;
				}
			});
		}

		public override bool ProcessKey(int key)
		{
			App.Key = key;

			if (key == 339) {
				// up
				Position += (int)(Height * 0.8);
				return true;
			} else if (key == 338) {
				// down
				Position -= (int)(Height * 0.8);
				return true;
			} else if (key == 21) {
				Update();
				UpdateUserInformation();
				return true;
			} else {
				return base.ProcessKey(key);
			}
		}
		public void Save(string filename)
		{
			var file = File.Open(filename, FileMode.Create);
			var sw = new StreamWriter(file);
			sw.Write(JsonConvert.SerializeObject(stats));
			sw.Flush();
			file.Close();
		}

		public void Load(string filename)
		{
			if (File.Exists(filename)) {
				var file = File.Open(filename, FileMode.Open);
				var sr = new StreamReader(file);
				var text = sr.ReadToEnd();
				foreach (var kvp in JsonConvert.DeserializeObject<SortedList<decimal, TwitterStatus>>(text)) {
					stats.Add(kvp.Key, kvp.Value);
				}
				file.Close();
			}
			Invalid = true;
		}
	}
}

