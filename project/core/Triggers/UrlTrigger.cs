namespace ThoughtWorks.CruiseControl.Core.Triggers
{
    using System;
    using Exortech.NetReflector;
    using ThoughtWorks.CruiseControl.Core.Util;
    using ThoughtWorks.CruiseControl.Remote;

    /// <summary>
    /// <para>
    /// The Url Trigger is used to trigger a CCNet build when the page at a particular url changes. The Url Trigger will poll the specified url according 
    /// to a configured polling interval to detect if the last modified date of the page has changed since the last integration.
    /// </para>
    /// <para>
    /// This trigger is especially useful in reducing the load on your source control system caused by the polling for modifications performed by an Interval
    /// Trigger. If your source control system supports trigger scripts (such as the use of commitinfo scripts in CVS), you can use create a trigger to touch
    /// the page that is being monitored by CCNet to start a new integration.
    /// </para>
    /// <para type="info">
    /// Like all triggers, the urlTrigger must be enclosed within a triggers element in the appropriate <link>Project Configuration Block</link>.
    /// </para>
    /// </summary>
    /// <title>URL Trigger</title>
    /// <version>1.0</version>
    /// <remarks>
    /// <para type="warning">
    /// There is currently a limitation in this trigger such that it will not persist the url's last modified date when the server restarts. This means
    /// that the trigger will always attempt to start a new integration when the first interval expires after the server starts up.
    /// </para>
    /// <para type="warning">
    /// The UrlTrigger will only work with pages that return a reliable LastModified date HTTP Header, such as static html pages or well-behaved dynamic
    /// pages. Using static html pages is the most reliable way to use this trigger.
    /// </para>
    /// <para>
    /// This trigger has been contributed by Steve Norman.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code title="Minimalist example">
    /// &lt;urlTrigger url="http://server/page.html"  /&gt;
    /// </code>
    /// <code title="Full example">
    /// &lt;urlTrigger url="http://server/page.html" seconds="30" buildCondition="ForceBuild" /&gt;
    /// </code>
    /// </example>
    [ReflectorType("urlTrigger")]
	public class UrlTrigger : IntervalTrigger
	{
		private HttpWrapper httpRequest;
		private DateTime lastModifiedTimeAtLastBuild = DateTime.MinValue;
        private DateTime lastModified = DateTime.MinValue;
        private Uri uri;
        private bool fireOnStartup = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlTrigger"/> class.
        /// </summary>
		public UrlTrigger() : this(new DateTimeProvider(), new HttpWrapper())
		{}

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlTrigger"/> class.
        /// </summary>
        /// <param name="dtProvider">The dt provider.</param>
        /// <param name="httpWrapper">The HTTP wrapper.</param>
		public UrlTrigger(DateTimeProvider dtProvider, HttpWrapper httpWrapper) : base(dtProvider)
		{
			this.httpRequest = httpWrapper;
		}

        /// <summary>
        /// On startup fire the integration request when no pervious known modified time is known.  If set to false then
        /// the first time the url is loaded the modifiedDate will be saved and the first build will be when the modified 
        /// date changes.
        /// </summary>
        /// <version>1.0</version>
        /// <default>n/a</default>
		[ReflectorProperty("fireOnStartup", Required=false)]
		public virtual bool FireOnStartup
        {
            get { return fireOnStartup; }
            set { fireOnStartup = value; }
        }

        /// <summary>
        /// The url to poll for changes.
        /// </summary>
        /// <version>1.0</version>
        /// <default>n/a</default>
        [ReflectorProperty("url", Required = true)]
        public virtual string Url
        {
            get { return uri.ToString(); }
            set { uri = new Uri(value); }
        }

        /// <summary>
        /// Fires the trigger.
        /// </summary>
        /// <returns>
        /// A new <see cref="IntegrationResult"/> if the trigger has been fired; <c>null</c> otherwise.
        /// </returns>
		public override IntegrationRequest Fire()
		{
			IntegrationRequest request = base.Fire();
			if (request ==  null) return null;
			
            IncrementNextBuildTime();
            if (HasUrlChanged())
			{
				return new IntegrationRequest(BuildCondition, Name, null);
			}
            
            return null;
		}

        /// <summary>
        /// Integrations the completed.	
        /// </summary>
        /// <remarks></remarks>
        public override void IntegrationCompleted()
        {
            base.IntegrationCompleted();
            lastModifiedTimeAtLastBuild = lastModified;
        }

        /// <summary>
        /// Determines whether the URL has changed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the URL has changed; otherwise, <c>false</c>.
        /// </returns>
		private bool HasUrlChanged()
		{
			try
			{
                Log.Debug(string.Format(System.Globalization.CultureInfo.CurrentCulture, "More than {0} seconds since last integration, checking url.", IntervalSeconds));
                Log.Debug(String.Format("Getting last modified time stamp from url: {0}", uri));
				DateTime newModifiedTime = httpRequest.GetLastModifiedTimeFor(uri, lastModifiedTimeAtLastBuild);

				if (newModifiedTime > lastModifiedTimeAtLastBuild)
				{
                    Log.Debug(String.Format("lastModified = {0}", newModifiedTime));
                    Log.Debug(String.Format("lastModifiedAtLastBuild = {0}", lastModifiedTimeAtLastBuild));

                    bool ret = true;

                    if (!fireOnStartup && lastModifiedTimeAtLastBuild == DateTime.MinValue)
                    { 
                        ret = false;
                        Log.Debug(String.Format("Setting lastModifiedTimeAtLastBuild to lastModified"));
                        Log.Debug(String.Format("Skipping build"));
                        lastModifiedTimeAtLastBuild = newModifiedTime;
                    }

                    lastModified = newModifiedTime;

                    return ret;
				}
			}
			catch (Exception e)
			{
				Log.Error("Error loading last modified time from url: " + uri);
				Log.Error(String.Format("{0}", e));
			}
			return false;
		}
	}
}