using System;
using Twitterizer;
using Twitterizer.Streaming;
using Manos.IO;
using Mono.Terminal;
using System.Threading.Tasks;

namespace MeetCurses
{
	public class ManosTwitter
	{
		public Context Context { get; protected set; }
		public TaskScheduler TaskScheduler { get; protected set; }
		public OAuthTokens OAuthTokens { get; protected set; }

		AsyncWatcher<Action> callbacks;

		public ManosTwitter(Context context, OAuthTokens oauthtokens)
			: this(context, oauthtokens, TaskScheduler.Default)
		{
		}

		public ManosTwitter(Context context, OAuthTokens oauthtokens, TaskScheduler scheduler)
		{
			Context = context;
			TaskScheduler = scheduler;
			OAuthTokens = oauthtokens;

			callbacks = new AsyncWatcher<Action>(Context, (callback) => {
				callback();
			});
			callbacks.Start();
		}

		public void HomeTimeline(Action<TwitterResponse<TwitterStatusCollection>> callback)
		{
			HomeTimeline(new TimelineOptions(), callback);
		}

		public void HomeTimeline(TimelineOptions options, Action<TwitterResponse<TwitterStatusCollection>> callback)
		{
			new Task(() => {
				TwitterResponse<TwitterStatusCollection> timeline = TwitterTimeline.HomeTimeline(OAuthTokens, options);
				callbacks.Send(() => {
					callback(timeline);
				});
			}).Start(TaskScheduler);
		}

		public void Tweet(string text, Action<TwitterResponse<TwitterStatus>> callback)
		{
			new Task(() => {
				var response = TwitterStatus.Update(OAuthTokens, text);
				callbacks.Send(() => {
					callback(response);
				});
			}).Start(TaskScheduler);
		}

		public void User(Action<TwitterResponse<TwitterUser>> callback)
		{
			new Task(() => {
				var user = TwitterUser.Show(App.Configuration.User.UserId);
				callbacks.Send(() => {
					callback(user);
				});
			}).Start(TaskScheduler);
		}

		public void UserStream(StreamOptions streamOptions, Action<TwitterStream> streamStarted, Action<TwitterIdCollection> friendsCallback, Action<StopReasons> streamErrorCallback,
			Action<TwitterStatus> statusCreatedCallback, Action<TwitterStreamDeletedEvent> statusDeletedCallback,
			Action<TwitterDirectMessage> directMessageCreatedCallback, Action<TwitterStreamDeletedEvent> directMessageDeletedCallback,
			Action<TwitterStreamEvent> eventCallback, Action<string> rawJsonCallback = null)
		{
			var stream = new TwitterStream(OAuthTokens, "MeetCurses", streamOptions);
			new Task(() => {
				stream.StartUserStream((friendsId) => {
					callbacks.Send(() => friendsCallback(friendsId));
				}, (stopreason) => {
					callbacks.Send(() => streamErrorCallback(stopreason));
				}, (status) => {
					callbacks.Send(() => statusCreatedCallback(status));
				}, (status) => {
					callbacks.Send(() => statusDeletedCallback(status));
				}, (status) => {
					callbacks.Send(() => directMessageCreatedCallback(status));
				}, (status) => {
					callbacks.Send(() => directMessageDeletedCallback(status));
				}, (twitterStreamEvent) => {
					callbacks.Send(() => eventCallback(twitterStreamEvent));
				}, (rawJson) => {
					callbacks.Send(() => {
						if (rawJsonCallback != null) {
							rawJsonCallback(rawJson);
						}
					});
				});
				callbacks.Send(() => streamStarted(stream));
			}).Start(TaskScheduler);
		}

		public void UserStream(Action<TwitterStream> streamStarted, Action<TwitterIdCollection> friendsCallback, Action<StopReasons> streamErrorCallback,
			Action<TwitterStatus> statusCreatedCallback, Action<TwitterStreamDeletedEvent> statusDeletedCallback,
			Action<TwitterDirectMessage> directMessageCreatedCallback, Action<TwitterStreamDeletedEvent> directMessageDeletedCallback,
			Action<TwitterStreamEvent> eventCallback, Action<string> rawJsonCallback = null)
		{
			UserStream(new StreamOptions(), streamStarted, friendsCallback, streamErrorCallback, statusCreatedCallback,
				statusDeletedCallback, directMessageCreatedCallback, directMessageDeletedCallback,
				eventCallback, rawJsonCallback);
		}

		public void UserStream(StreamOptions streamOptions, UserStreamCallbacks callbacks, Action<TwitterStream> streamStart)
		{
			UserStream(streamOptions, streamStart,
				callbacks.OnFriends, callbacks.OnStreamError, callbacks.OnStatusCreated,
				callbacks.OnStatusDeleted, callbacks.OnDirectedMessageCreated, callbacks.OnDirectMessageDeleted,
				callbacks.OnEvent, callbacks.OnRawJson);
		}

		public void UserStream(UserStreamCallbacks callbacks, Action<TwitterStream> streamStart)
		{
			UserStream(new StreamOptions(), callbacks, streamStart);
		}

		public void UserStream(StreamOptions streamOptions, Action<TwitterStream, UserStreamCallbacks> streamStart)
		{
			UserStreamCallbacks callbacks = new UserStreamCallbacks();
			UserStream(streamOptions, callbacks, (stream) => {
				streamStart(stream, callbacks);
			});
		}

		public void UserStream(Action<TwitterStream, UserStreamCallbacks> streamStart)
		{
			UserStream(new StreamOptions(), streamStart);
		}

		public void PublicStream(StreamOptions streamOptions, Action<TwitterStream> streamStarted,
			Action<StopReasons> streamErrorCallback,
			Action<TwitterStatus> statusCreatedCallback, Action<TwitterStreamDeletedEvent> statusDeletedCallback,
			Action<TwitterStreamEvent> eventCallback, Action<string> rawJsonCallback = null)
		{
			var stream = new TwitterStream(OAuthTokens, "MeetCurses", streamOptions);
			new Task(() => {
				stream.StartPublicStream((stopreason) => {
					callbacks.Send(() => streamErrorCallback(stopreason));
				}, (status) => {
					callbacks.Send(() => statusCreatedCallback(status));
				}, (status) => {
					callbacks.Send(() => statusDeletedCallback(status));
				}, (twitterStreamEvent) => {
					callbacks.Send(() => eventCallback(twitterStreamEvent));
				}, (rawJson) => {
					callbacks.Send(() => {
						if (rawJsonCallback != null) {
							rawJsonCallback(rawJson);
						}
					});
				});
			}).Start(TaskScheduler);
		}

		public void PublicStream(StreamOptions options, PublicStreamCallbacks callbacks, Action<TwitterStream> streamStarted)
		{
			PublicStream(options, streamStarted, callbacks.OnStreamError,
				callbacks.OnStatusCreated, callbacks.OnStatusDeleted,
				callbacks.OnEvent, callbacks.OnRawJson);
		}

		public void PublicStream(StreamOptions options, Action<TwitterStream, PublicStreamCallbacks> streamStarted)
		{
			PublicStreamCallbacks callbacks = new PublicStreamCallbacks();
			PublicStream(options, callbacks, (stream) => {
				streamStarted(stream, callbacks);
			});
		}
	}

	public class PublicStreamCallbacks
	{
		internal void OnStreamError(StopReasons reasons)
		{
			if (StreamError != null) {
				StreamError(reasons);
			}
		}
		public Action<StopReasons> StreamError;

		internal void OnStatusCreated(TwitterStatus status)
		{
			if (StatusCreated != null) {
				StatusCreated(status);
			}
		}
		public Action<TwitterStatus> StatusCreated;

		internal void OnStatusDeleted(TwitterStreamDeletedEvent deleted)
		{
			if (StatusDeleted != null) {
				StatusDeleted(deleted);
			}
		}
		public Action<TwitterStreamDeletedEvent> StatusDeleted;

		internal void OnEvent(TwitterStreamEvent @event)
		{
			if (Event != null) {
				Event(@event);
			}
		}
		public Action<TwitterStreamEvent> Event;

		internal void OnRawJson(string rawJson)
		{
			if (RawJson != null) {
				RawJson(rawJson);
			}
		}
		public Action<string> RawJson;
	}

	public class UserStreamCallbacks : PublicStreamCallbacks
	{
		internal void OnFriends(TwitterIdCollection friendsId)
		{
			if (Friends != null) {
				Friends(friendsId);
			}
		}
		public Action<TwitterIdCollection> Friends;

		internal void OnDirectedMessageCreated(TwitterDirectMessage directMessage)
		{
			if (DirectMessageCreated != null) {
				DirectMessageCreated(directMessage);
			}
		}
		public Action<TwitterDirectMessage> DirectMessageCreated;

		internal void OnDirectMessageDeleted(TwitterStreamDeletedEvent streamDeletedEvent)
		{
			if (DirectMessageDeleted != null) {
				DirectMessageDeleted(streamDeletedEvent);
			}
		}
		public Action<TwitterStreamDeletedEvent> DirectMessageDeleted;
	}
}

