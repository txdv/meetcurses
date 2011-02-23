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
        System.Environment.Exit(0);

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
  
  class MessageWidget : Widget
  {
    public MessageWidget()
      : this(0, 0, 0, 0)
    {
    }

    public MessageWidget(int x, int y, int w, int h)
      : base(x, y, w, h)
    {
      ShowRealName = true;
    }

    TwitterStatusCollection collection = null;

    public void Update(TwitterStatusCollection collection)
    {
      this.collection = collection;
      Redraw();
    }

    public void UpdatePublicTimeLine()
    {

      Update(TwitterTimeline.PublicTimeline(MainClass.config.GetOAuthTokens()).ResponseObject);
    }

    public void UpdateHomeTimeline()
    {
      TimelineOptions to = new TimelineOptions() {
        Count = h - y
      };

      Update(TwitterTimeline.HomeTimeline(MainClass.config.GetOAuthTokens(), to).ResponseObject);
    }

    public bool ShowRealName { get; set; }

    protected string GetName(TwitterStatus status)
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

      var friends = MainClass.config.GetFriends();

      if (status.User.ScreenName == MainClass.config.ScreenName) {
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

    protected int DrawStatus(int line, int nickWidth, TwitterStatus status)
    {
      Curses.attrset(Application.ColorNormal);

      int lineWidth = w - x - nickWidth - 3;

      int times = status.Text.Length / lineWidth;

      if (status.Text.Length % lineWidth != 0)
        times++;

      int j;
      for (j = 0; j < times; j++) {
        BaseMove(line + j, 0);
        if (j == 0) {
          DrawName(line, nickWidth, status);
        } else {
          DrawWhiteSpaces(nickWidth);
        }

        Curses.addstr(" | ");
        int min = Math.Min(lineWidth, status.Text.Length - j * lineWidth);
        Curses.addstr(status.Text.Substring(j * lineWidth, min));

        DrawWhiteSpaces(lineWidth - min);
      }
      return j;
    }

    public override void Redraw()
    {
      if (collection == null) {
        Clear();
        string info = "No tweets loaded";
        BaseMove(Application.Lines/2, Application.Cols/2 - info.Length/2);
        Curses.addstr(info);
        return;
      }

      int nickWidth = 0;
      foreach (var entry in collection) {
        nickWidth = Math.Max(nickWidth, GetName(entry).Length);
      }

      int line = y;
      foreach (var status in collection) {
        line += DrawStatus(line, nickWidth, status);
      }
    }
  }

  class ConfigurationManager : Container
  {
    public ConfigurationManager(Config config)
      : this(0, 0, 0, 0, config)
    {
    }

    public ConfigurationManager(int x, int y, int w, int h, Config config)
      : base(x, y, w, h)
    {
      Config = config;

      Add(new Label(7, 1, "ConsumerKey: " + Config.ConsumerKey));

      Add(new Label(4, 3, "ConsumerSecret: " + Config.ConsumerSecret));

      Add(new Label(7, 5, "AccessToken: " + Config.AccessToken));

      Add(new Label(1, 7, "AccessTokenSecret: " + Config.AccessTokenSecret));
    }

    public Config Config { get; set; }

    public override void Redraw()
    {
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