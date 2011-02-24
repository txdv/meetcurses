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
  public abstract class BaseConfiguration
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

  public class User
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
        var response = TwitterFriendship.Friends(GetOAuthTokens());
        if (response.Result == RequestResult.Success) {
          friends = response.ResponseObject;
        }
      }
      return friends;
    }
  }

  public class Configuration : BaseConfiguration
  {
    public User User { get; set; }
  }

  class MainClass
  {
    public static int FriendsColor;
    public static int SelfColor;
    public static short BackgroundColor;

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

    public static Configuration Configuration = (Configuration)Configuration.Deserialize("MeetCurses.xml", typeof(Configuration));

    public static void Main(string[] args)
    {
      ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;

      if (Configuration.User.AccessToken == null) {
        Console.WriteLine("Retrieving Configuration Token ..");

        var token = Configuration.User.GetRequestToken();
        Uri uri = OAuthUtility.BuildAuthorizationUri(token.Token);

        Console.WriteLine(uri);

        Console.Write("Enter verficiation code: ");
        Configuration.User.Update(token, Console.ReadLine());

        Configuration.Serialize("MeetCurses.xml", Configuration);
      }

      Application.Init(false);

      BackgroundColor = -1;
      FriendsColor = Application.MakeColor(Curses.COLOR_BLUE,  BackgroundColor);
      SelfColor    = Application.MakeColor(Curses.COLOR_WHITE, BackgroundColor);

      Application.ColorNormal = Application.MakeColor(Curses.COLOR_WHITE, BackgroundColor);

      Frames f = new Frames();

      FullWindowContainer fwc = new FullWindowContainer(f);

      StatusListWidget publicTimeLine = new StatusListWidget();

      f.AddFrame('1', publicTimeLine);
      
      f.AddFrame('2', new ConfigurationManager(MainClass.Configuration));

      f.KeyPressed += delegate(int obj) {
        switch (obj) {
        case 'u':
          publicTimeLine.UpdateHomeTimeline();
          fwc.Redraw();
          break;
        case 'n':
          f.NextFrame();
          f.Redraw();
          break;
        case 'p':
          f.PreviousFrame();
          f.Redraw();
          break;
        default:
          break;
        }
      };

      Application.Run(fwc);
    }
  }
}

