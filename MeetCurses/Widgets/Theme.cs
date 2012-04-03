using System;
using Mono.Terminal;

namespace MeetCurses
{
	public class Theme
	{
		public static readonly int Accent = 202;
		public static readonly int Normal = 255;
		public static readonly int Brace = 241;
		public static readonly int Background = 237;

		public static ColorString Escape(string text, int background = -1)
		{
			return ColorString.Escape(text, (ch) => {
				switch (ch) {
				case '[':
				case ']':
				case ',':
					return ColorPair.From(Accent, background);
				}
				return ColorPair.From(Normal, background);
			});
		}
	}
}

