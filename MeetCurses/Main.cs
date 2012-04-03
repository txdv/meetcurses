using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Twitterizer;

namespace MeetCurses
{
	[Serializable]
	public abstract class BaseConfiguration
	{
		public static void Serialize(string file, BaseConfiguration c)
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
	}

	public class Configuration : BaseConfiguration
	{
		public User User { get; set; }
	}
}

