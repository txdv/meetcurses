using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Terminal;
using Twitterizer;

namespace MeetCurses
{
  class FullWindowContainer : Container
  {
    public Widget Widget { get; set; }
    
    public FullWindowContainer(Widget widget)
      : base(0, 0, Application.Cols, Application.Lines)
    {
      Add(widget);
      Widget = widget;
      Redraw();
    }
    
    public override bool ProcessKey(int key)
    {
      if (key == 'q')
        Running = false;

      return Widget.ProcessKey(key);
    }
    
    public override void Redraw()
    {
      AdjustWidgetSize();
      Widget.Redraw();
    }

    protected void AdjustWidgetSize()
    {
      x = 0;
      y = 0;
      w = Application.Cols;
      h = Application.Lines;

      foreach (Widget widget in this) {
        widget.x = x;
        widget.y = y;
        widget.w = w;
        widget.h = h;
      }
    }

    public override void DoSizeChanged()
    {
      AdjustWidgetSize();
      base.DoSizeChanged();
    }
  }
  
  class Frames : Widget
  {
    protected class Frame
    {
      public Frame(int key, Widget widget)
      {
        Widget = widget;
        Key    = key;
      }

      public int    Key    { get; set; }
      public Widget Widget { get; set; }
    }

    protected List<Frame> list = new List<Frame>();
    
    public Widget ActiveWidget { get; set; }

    public Frames()
      : base(0, 0, 0, 0)
    {
    }

    public Frames(int x, int y, int w, int h)
      : base(x, y, w, h)
    {
    }
    
    public void AddFrame(Widget widget)
    {
      AddFrame(0, widget);
    }

    public void AddFrame(int key, Widget widget)
    {
      if (list.Count == 0)
        ActiveWidget = widget;

      list.Add(new Frame(key, widget));
    }
    
    public void NextFrame()
    {
      var item = list.Find(frame => frame.Widget == ActiveWidget);

      int i = list.IndexOf(item) + 1;
      i = i % list.Count;
      ActiveWidget = list[i].Widget;
    }
    
    public void PreviousFrame()
    {
      var item = list.Find(frame => frame.Widget == ActiveWidget);

      int i = list.IndexOf(item) - 1;

      if (i == -1)
        i = list.Count - 1;

      ActiveWidget = list[i].Widget;
    }
  
    public override void Redraw()
    {
      if (ActiveWidget != null) {
        ActiveWidget.x = x;
        ActiveWidget.y = y;
        ActiveWidget.w = w;
        ActiveWidget.h = h;

        ActiveWidget.Redraw();
      }
    }

    public override bool ProcessKey(int key)
    {
      foreach (Frame f in list) {
        if (f.Key == key) {
          ActiveWidget = f.Widget;
          Redraw();
          return true;
        }
      }
      return base.ProcessKey(key);
    }
  }

  class StatusWidget : Widget
  {
    public StatusWidget(TwitterStatus status)
      : base(0, 0, 0, 0)
    {
      CanFocus = true;
      Status = status;
    }

    public TwitterStatus Status { get; protected set; }

    public int GetHeight(int nickWidth, int lineWidth)
    {
      int height = Status.Text.Length / lineWidth;

      if (Status.Text.Length % lineWidth != 0)
        height++;

      return height;
    }

    protected static int SeperatorWidth { get { return 3; } }
    protected static void DrawSeperator()
    {
      Curses.addch(' ');
      Curses.addch(Curses.ACS_VLINE);
      Curses.addch(' ');
    }

    protected string GetName()
    {
      if (Container != null && Container is StatusListWidget)
        return (Container as StatusListWidget).GetName(Status);

      return Status.User.Name;
    }

    public int NickWidth {
      get {
        if (Container != null && Container is StatusListWidget)
          return (Container as StatusListWidget).NickWidth;

        return GetName().Length;
      }
    }

    protected virtual int DrawStatus()
    {
      return 0;
    }

    public int LineWidth {
      get {
        return w - NickWidth - SeperatorWidth;
      }
    }

    public override void Redraw()
    {
      for (int i = 0; i < h; i++) {
        BaseMove(y + i, 0);

        if (i == 0)
          DrawName();
        else
          DrawWhiteSpaces(NickWidth);

        DrawSeperator();

        int min = Math.Min(LineWidth, Status.Text.Length - i * LineWidth);
        Curses.addstr(Status.Text.Substring(i * LineWidth, min));

        DrawWhiteSpaces(LineWidth - min);

        if (HasFocus)
          throw new Exception();
      }
    }

    protected bool IsFriend(string screeName)
    {
      var res = from f in MainClass.Configuration.User.GetFriends()
                where f.ScreenName == Status.User.ScreenName
                select f;
      return res.Count() > 0;
    }

    protected virtual void DrawName()
    {
      string name = GetName();
      DrawWhiteSpaces(NickWidth - name.Length);

      string screeName = Status.User.ScreenName;

      Curses.attrset(Application.ColorNormal);

      if (screeName == MainClass.Configuration.User.ScreenName) {
        Curses.attrset(MainClass.SelfColor);
      } else if (IsFriend(screeName)) {
        Curses.attrset(MainClass.FriendsColor);
      }

      Curses.attron(Curses.A_BOLD);
      Curses.addstr(name);
      Curses.attroff(Curses.A_BOLD);

      Curses.attrset(Application.ColorNormal);

    }

    protected void DrawWhiteSpaces(int count)
    {
      for (int i = 0; i < count; i++)
        Curses.addch(' ');
    }
  }

  class StatusListWidget : Container
  {
    public StatusListWidget()
      : this(0, 0, 0, 0)
    {
    }

    public StatusListWidget(int x, int y, int w, int h)
      : base(x, y, w, h)
    {
      ShowRealName = true;
    }

    public int NickWidth {
      get {
        int max = 0;
        foreach (StatusWidget status in this)
           max = Math.Max(max, GetName(status.Status).Length);
        return max;
      }
    }

    public void Update(TwitterStatusCollection collection)
    {

      int nickWidth = NickWidth;

      int height = y;
      foreach (var status in collection) {
        StatusWidget sw = new StatusWidget(status);
        sw.y = height;
        sw.h = sw.GetHeight(nickWidth, w);
        sw.w = w;
        sw.x = x;
        height += sw.h;
        Add(sw);
      }
      Redraw();
    }

    public void UpdatePublicTimeLine()
    {
      Update(TwitterTimeline.PublicTimeline(MainClass.Configuration.User.GetOAuthTokens()).ResponseObject);
    }

    public void UpdateHomeTimeline()
    {
      TimelineOptions to = new TimelineOptions() {
        Count = h - y
      };

      Update(TwitterTimeline.HomeTimeline(MainClass.Configuration.User.GetOAuthTokens(), to).ResponseObject);
    }

    public bool ShowRealName { get; set; }

    public string GetName(TwitterStatus status)
    {
      if (ShowRealName)
        return status.User.Name;
      else
        return status.User.ScreenName;
    }

    protected void DrawName(int line, int nickWidth, TwitterStatus status)
    {
      string name = GetName(status);
      BaseMove(line, 0);

      // draw prefix whitespaces
      for (int i = 0; i < nickWidth - name.Length; i++)
        Curses.addch(' ');

      var friends = MainClass.Configuration.User.GetFriends();

      if (status.User.ScreenName == MainClass.Configuration.User.ScreenName) {
        Curses.attrset(MainClass.SelfColor);
      } else {
        var res = from f in friends
                  where f.ScreenName == status.User.ScreenName
                  select f;

        bool friend = res.Count() > 0;
        if (friend) {
          Curses.attrset(MainClass.FriendsColor);
        }
      }

      Curses.attron(Curses.A_BOLD);
      Curses.addstr(name);
      Curses.attroff(Curses.A_BOLD);

      Curses.attrset(Application.ColorNormal);
    }

    protected void DrawWhiteSpaces(int count)
    {
      for (int i = 0; i < count; i++)
        Curses.addch(' ');
    }

    public override void Redraw()
    {
      // HACK: now this is ugly, we need to get rid of this
      int i = 0;
      foreach (var bla in this)
        i++;

      if (i == 0) {
        Curses.attrset(Application.ColorNormal);
        Clear();
        string info = "No tweets loaded";
        BaseMove(Application.Lines/2, Application.Cols/2 - info.Length/2);
        Curses.addstr(info);
        return;
      }
      base.Redraw();
    }


      }
    }
  }

  class ConfigurationUnit
  {
    public ConfigurationUnit(string label, Widget widget)
    {
      LabelText = label;
      Label = new Label(0, 0, LabelText + " : ");
      Widget = widget;
    }

    public ConfigurationUnit(string label, string text)
      : this(label, new Label(0, 0, text))
    {
    }

    public string LabelText { get; set; }
    public Label Label { get; set; }
    public Widget Widget { get; set; }
  }

  class ConfigurationManager : Container
  {
    private List<ConfigurationUnit> list = new List<ConfigurationUnit>();

    public ConfigurationManager(Configuration configuration)
      : this(0, 0, 0, 0, configuration)
    {
    }

    public ConfigurationManager(int x, int y, int w, int h, Configuration configuration)
      : base(x, y, w, h)
    {
      Configuration = configuration;

      list.Add(new ConfigurationUnit("ConsumerKey",       Configuration.User.ConsumerKey));
      list.Add(new ConfigurationUnit("ConsumerSecret",    Configuration.User.ConsumerSecret));
      list.Add(new ConfigurationUnit("AccessToken",       Configuration.User.AccessToken));
      list.Add(new ConfigurationUnit("AccessTokenSecret", Configuration.User.AccessTokenSecret));

      int length = 0;
      foreach (var cu in list) {
        length = Math.Max(length, cu.LabelText.Length);
      }

      int height = 1;
      foreach (var cu in list) {
        cu.Label.y = height;
        cu.Label.x = 1 + length - cu.LabelText.Length;
        cu.Widget.y = height;
        cu.Widget.x = 1 + length + 3;

        Add(cu.Label);
        Add(cu.Widget);
        height += 2;
      }
    }

    public Configuration Configuration { get; set; }

    public override void Redraw()
    {
      Curses.attrset(Application.ColorNormal);
      Clear();
      base.Redraw();
    }
  }
  
  public class Box : Container
  {
    public Box(int x, int y, int w, int h)
      : base(x, y, w, h)
    {
    }
  }
  
  public class VBox : Box
  {
    public VBox(int x, int y, int w, int h)
      : base(x, y, w, h)
    {
    }
  }
}