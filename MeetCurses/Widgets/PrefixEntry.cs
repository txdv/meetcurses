using System;
using Mono.Terminal;

namespace MeetCurses
{
	public class PrefixEntry : Entry
	{
		public string Prefix {
			get { return ColorPrefix.String; }
			set { ColorPrefix = new ColorString(value); }
		}

		ColorString prefix;
		public ColorString ColorPrefix {
			get { return prefix; }
			set { prefix = value; Invalid = true; }
		}

		public override void Redraw()
		{
			Invalid = false;
			int length;
			if (ColorPrefix != null) {
				ColorPrefix.Draw(this, 0, 0, ColorPrefix.Length, 1);
				ColorString.Finish();
				length = ColorPrefix.Length;
			} else {
				length = 0;
			}
			Fill(Text, length, 0);
		}

		public override void SetCursorPosition()
		{
			Move((ColorPrefix == null ? 0 : ColorPrefix.Length) + Text.Length, 0);
		}
	}
}
