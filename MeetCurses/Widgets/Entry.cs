using System;
using System.Collections.Generic;
using Mono.Terminal;

namespace MeetCurses
{
	public class Entry : Widget
	{
		private int position = 0;
		public int Position {
			get {
				return position;
			}
			set {
				int oldPosition = position;
				if (value > Text.Length) {
					position = text.Length;
					OnPositionChanged(oldPosition);
				} else if (value <= 0) {
					position = 0;
					OnPositionChanged(oldPosition);
				} else if (position != value) {
					position = value;
					OnPositionChanged(oldPosition);
				}
			}
		}

		private string text = "";
		public string Text {
			get {
				return text;
			}
			set {
				position = Math.Min(position, (value.Length > 0 ? value.Length + 1 : 0));
				string oldText = text;
				text = value;
				OnTextChanged(oldText);
			}
		}

		public Entry()
			: base()
		{
		}

		public override bool CanFocus {
			get {
				return true;
			}
		}

		public override void Redraw()
		{
			base.Redraw();
			Fill(Text);
			SetCursorPosition();
		}

		protected int GetFirstNonWhiteSpace()
		{
			int i = Position;
			while (i >= Text.Length) {
				i--;
			}

			for (; i > 0; i--) {
				if (Text[i] != ' ') {
					return i;
				}
			}

			return 0;
		}

		protected int GetFirstWhiteSpace()
		{
			int i = Position;
			while (i >= Text.Length) {
				i--;
			}

			for (; i > 0; i--) {
				if (Text[i] == ' ') {
					return i;
				}
			}

			return 0;
		}

		protected int GetFirst()
		{
			if (Text[Position - 1] == ' ') {
				return GetFirstNonWhiteSpace();
			} else {
				return GetFirstWhiteSpace();
			}
		}

		const int ctrl = 96;

		public override bool ProcessKey(int key)
		{
			char ch = (char)key;

			Invalid = true;

			switch (key) {
			case (int)'a' - ctrl:
				Position = 0;
				return true;
			case (int)'e' - ctrl:
				Position = Text.Length;
				return true;
			case (int)'w' - ctrl:
				if (Position == 1) {
					Text = Text.Substring(1);
					Position = 0;
				} else if (Position != 0) {
					int i = GetFirst() + 1;
					if (i == 1) {
						Text = Text.Substring(Position);
						Position = 0;
					} else {
						Text = Text.Substring(0, i);
						Position = i;
					}
				}
				return true;
			case 126:
				if (Position < Text.Length) {
					Text = Text.Substring(0, Text.Length - 1);
				}
				return true;
			case 127:
			case Curses.Key.Backspace:
				if (Position > 0) {
					Text = Text.Substring(0, Position - 1) + Text.Substring(Position);
					Position--;
				}
				return true;
			case Curses.Key.Left:
				if (Position > 0) {
					Position--;
				}
				return true;
			case Curses.Key.Right:
				if (Position < Text.Length) {
					Position++;
				}
				return true;
			case Curses.Key.Delete:
				if (Position < Text.Length) {
					Text = Text.Substring(0, Position) + Text.Substring(Position + 1);
				}
				return true;
			case 10:
				OnEnter();
				return true;
			default:
				if (key < 32 || key > 255) {
					return false;
				} else {
					Text = Text.Substring(0, Position) + ch + Text.Substring(Position);
					Position++;
					return true;
				}
			}
		}

		public override void SetCursorPosition()
		{
			Move(Position, 0);
		}

		protected virtual void OnTextChanged(string oldText)
		{
			if (TextChanged != null) {
				TextChanged(oldText);
			}
		}

		public event Action<string> TextChanged;

		protected virtual void OnPositionChanged(int oldPosition)
		{
			if (PositionChanged != null) {
				PositionChanged(oldPosition);
			}
		}

		public event Action<int> PositionChanged;

		protected virtual void OnEnter()
		{
			if (Enter != null) {
				Enter();
			}
		}

		public event Action Enter;
	}
}