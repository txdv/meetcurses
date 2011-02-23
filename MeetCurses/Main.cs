using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Mono.Terminal;
using Twitterizer;

namespace MeetCurses
{
  [Serializable]
  public class Configuration
  {
    public static void Serialize(string file, Configuration c)
    {
      System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(c.GetType());
      StreamWriter sw = File.CreateText(file);
      xs.Serialize(sw, c);
      sw.Flush();
      sw.Close();
    }

    public static object Deserialize(string file, Type type)
    {
      System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(type);
      StreamReader sr = File.OpenText(file);
      object ret = xs.Deserialize(sr);
      sr.Close();
      return ret;
    }
  }

  public class Config : Configuration
  {
    public string  ConsumerKey       { get; set; }
    public string  ConsumerSecret    { get; set; }
    public string  AccessToken       { get; set; }
    public string  AccessTokenSecret { get; set; }
    public string  ScreenName        { get; set; }
    public Decimal UserId            { get; set; }

    public OAuthTokens GetOAuthTokens()
    {
      OAuthTokens tokens = new OAuthTokens();
      tokens.ConsumerKey       = ConsumerKey;
      tokens.ConsumerSecret    = ConsumerSecret;
      tokens.AccessToken       = AccessToken;
      tokens.AccessTokenSecret = AccessTokenSecret;
      return tokens;
    }

    public OAuthTokenResponse GetRequestToken()
    {
      return OAuthUtility.GetRequestToken(ConsumerKey, ConsumerSecret, "oob");
    }

    public OAuthTokenResponse GetAccessToken(OAuthTokenResponse authorizationToken, string verifier)
    {
      return OAuthUtility.GetAccessToken(ConsumerKey, ConsumerSecret, authorizationToken.Token, verifier);
    }

    public bool Update(OAuthTokenResponse token, string verifier)
    {
      try {
        var accessToken = GetAccessToken(token, verifier);
        AccessToken       = accessToken.Token;
        AccessTokenSecret = accessToken.TokenSecret;
        ScreenName        = accessToken.ScreenName;
        UserId            = accessToken.UserId;
      } catch {
        return false;
      }
      return true;
    }

    TwitterUserCollection friends = null;
    public TwitterUserCollection GetFriends()
    {
      if (friends == null) {
        var response = TwitterFriendship.Friends(MainClass.config.GetOAuthTokens());
        if (response.Result == RequestResult.Success) {
          friends = response.ResponseObject;
        }
      }
      return friends;
    }
  }

  class MainClass
  {
    public static int FriendsColor;
    public static int SelfColor;

    private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None)
        return true;

      // HACK: Mono's certificate chain validator is buggy
      if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && sender is HttpWebRequest) {
      var request = (HttpWebRequest)sender;
      if (request.RequestUri.Host == "api.twitter.com" && certificate.Issuer == "OU=Equifax Secure Certificate Authority, O=Equifax, C=US")
        return true;
      }
      return false;
    }

    public static Config config = (Config)Configuration.Deserialize("MeetCurses.xml", typeof(Config));

    public static void Main(string[] args)
    {
      ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;

      if (config.AccessToken == null) {
        var token = config.GetRequestToken();
        Uri uri = OAuthUtility.BuildAuthorizationUri(token.Token);

        Console.WriteLine(uri);

        config.Update(token, Console.ReadLine());

        Configuration.Serialize("MeetCurses.xml", config);
      }

      Application.Init(false);

      FriendsColor = Application.MakeColor(Curses.COLOR_BLUE,  Curses.COLOR_BLACK);
      SelfColor    = Application.MakeColor(Curses.COLOR_WHITE, Curses.COLOR_BLACK);
      
      Application.ColorNormal = Application.MakeColor(Curses.COLOR_WHITE, Curses.COLOR_BLACK);

      Frames f = new Frames();

      FullWindowContainer fwc = new FullWindowContainer(f);

      MessageWidget publicTimeLine = new MessageWidget();

      f.AddFrame('1', publicTimeLine);
      
      f.AddFrame('2', new ConfigurationManager(MainClass.config));

      f.KeyPressed += delegate(int obj) {
        switch (obj) {
        case 'u':
          publicTimeLine.UpdateHomeTimeline();
          fwc.Redraw();
          break;
        case 'n':
          f.NextFrame();
          break;
        case 'p':
          f.PreviousFrame();
          break;
        default:
          break;
        }
        f.Redraw();
      };

      Application.Run(fwc);
    }
  }
}

