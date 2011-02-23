using System;
using System.Collections.Generic;
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

      Widget.Redraw();
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
      Clear();
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

    public string GetName(TwitterStatus status)
    {
      if (ShowRealName)
        return status.User.Name;
      else
        return status.User.ScreenName;
    }

    public override void Redraw()
    {
      if (collection == null)
        return;

      int maxLen = 0;
      foreach (var entry in collection) {
        int newLen = GetName(entry).Length;
        if (newLen > maxLen) maxLen = newLen;
      }

      int line = y;
      foreach (var entry in collection) {
        string name = GetName(entry);
        BaseMove(line, 0);
        for (int i = 0; i < maxLen - name.Length; i++)
          Curses.addch(' ');

        Curses.addstr(name);

        int lineWidth = w - x - maxLen - 3;

        int times = entry.Text.Length / lineWidth;

        if (entry.Text.Length % lineWidth != 0)
          times++;

        for (int i = 0; i < times; i++) {
          BaseMove(line, maxLen);
          Curses.addstr(" | ");
          int min = Math.Min(lineWidth, entry.Text.Length - i * lineWidth);
          Curses.addstr(entry.Text.Substring(i * lineWidth, min));

          line++;
        }
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