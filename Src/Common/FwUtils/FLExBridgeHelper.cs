using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Utility methods for FLExBridge interaction
	/// </summary>
	public class FLExBridgeHelper
	{
		#region These are the available '-v' parameter options:
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode for full FLEx data set
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.
		/// Flex Bridge returns 'true', if data changes came in from the S/R, otherwise, 'false' for no changes.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string SendReceive = @"send_receive";
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode for Lift data
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file (which may not actually exist for DB4o projects).
		/// Flex Bridge returns 'true', if data changes came in from the S/R, otherwise, 'false' for no changes.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string SendReceiveLift = @"send_receive_lift";

		/// <summary>
		/// constant for launching the bridge in Obtain project mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the path to the main projects folder where FW projects are located.</para>
		/// <para>Returns the pathname to either the 'fwdata' xml file or the lift file, if FLEx Bridge was able to get a clone. Returns null, if no clone was created.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string Obtain = @"obtain";
		/// <summary>
		/// constant for launching the bridge in Obtain project mode, but only tries to get a new Lift repository for an extant FW project.
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the path to the given (extant) FW project.</para>
		/// <para>Returns the pathname to the lift file, if FLEx Bridge was able to get a clone. Returns null, if no clone was created.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ObtainLift = @"obtain_lift";

		/// <summary>
		/// constant for launching the bridge in the Conflict\Notes Viewer mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.</para>
		/// <para>Nothing is returned, since FLEx Bridge's notes dialog will remain open.
		/// As the user selects some note, a call back to FLEx will be done, with the URL for the item to jump to.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string ConflictViewer = @"view_notes";

		/// <summary>
		/// constant for launching the bridge in the Lift Conflict\Notes Viewer mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file.</para>
		/// <para>Nothing is returned, since FLEx Bridge's notes dialog will remain open.
		/// As the user selects some note, a call back to FLEx will be done, with the URL for the item to jump to.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string LiftConflictViewer = @"view_notes_lift";

		/// <summary>
		/// constant for launching the bridge in the undo export mode
		/// </summary>
		/// <remarks>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file. Nothing is returned.</para>
		/// <para>Flex Bridge restores the local working folder to what is in the lift repository, including deleting any new files.</para>
		/// <para>The '-u' option  (required) will give the user name.</para>
		/// </remarks>
		public const string UndoExportLift = @"undo_export_lift";

		/// <summary>
		/// constant for launching the bridge in the move lift mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to try and move an extant repository from the old location to the new,
		/// if the old one exists. FLEx should not use this option, if the new repository already exists.</para>
		/// <para>The related '-p' option (required) will give the pathname of the xml fwdata file. The new repository location is returned, if it was moved, other wise null is returned.</para>
		/// <para>This option must also use the '-g' command line argument which gives FLEx Bridge the language project's guid,
		/// which is used to find the correct lift repository.</para>
		/// </remarks>
		public const string MoveLift = @"move_lift";

		/// <summary>
		/// constant for launching the bridge in the "Check for Updates" mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to show its "Check for Updates" information.</para>
		/// </remarks>
		public const string CheckForUpdates = @"check_for_updates";

		/// <summary>
		/// constant for launching the bridge in the "About FLEx Bridge" mode
		/// </summary>
		/// <remarks>
		/// <para>Instruct FLEx Bridge to show its "About" information.</para>
		/// </remarks>
		public const string AboutFLExBridge = @"about_flex_bridge";

		#endregion End of available '-v' parameter options:

		// The two paths of a path that locate the Lift repository within a FLEx project.
		/// <summary>
		/// constant for locating the nested lift repository (part 1 of 2)
		/// </summary>
		public const string OtherRepositories = @"OtherRepositories";
		/// <summary>
		/// constant for locating the nested lift repository (part 2 of 2)
		/// </summary>
		public const string LIFT = @"LIFT";

		/// <summary>
		/// Project name grafted to the pipe URI so multiple projects can S/R simultaneously
		/// </summary>
		private static string _sFwProjectName = "";

		/// <summary>
		/// Event handler delegate that passes a jump URL.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public delegate void JumpEventHandler(object sender, FLExJumpEventArgs e);

		/// <summary>
		/// Event to enabled FLExBridgeListener to find out when the Conflict Report title was clicked.
		/// </summary>
		public static event JumpEventHandler FLExJumpUrlChanged;

		private const string FLExBridgeName = @"FLExBridge.exe";

		private static object _waitObject = new object();
		private static bool flexBridgeTerminated;
		private static object _noBlockerHost;
		private static bool _receivedChanges; // true if changes merged via FLExBridgeService.BridgeWorkComplete()
		private static string _projectName; // fw proj path via FLExBridgeService.InformFwProjectName()
		private static string _pipeID;

		/// <summary>
		/// Launches the FLExBridge application with the given commands and locks out the FLEx interface until the bridge
		/// is closed.
		/// </summary>
		/// <param name="projectFolder">The entire FieldWorks project folder path.
		/// Must include the project folder and project name with "fwdata" extension.
		/// Empty is OK if not send_receive command.</param>
		/// <param name="userName">the username to use in Chorus commits</param>
		/// <param name="command">obtain, start, send_receive, view_notes</param>
		/// <param name="projectGuid">Optional Lang Project guid, that is only used with the 'move_lift' command</param>
		/// <param name="liftModelVersionNumber">Version of LIFT schema that is supported by FLEx.</param>
		/// <param name="writingSystemId">The id of the first vernacular writing system</param>
		/// <param name="changesReceived">true if S/R made changes to the project.</param>
		/// <param name="projectName">Name of the project to be opened after launch returns.</param>
		/// <param name="fwmodelVersionNumber">Current FDO model version number</param>
		/// <returns>true if successful, false otherwise</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ServiceHost gets disposed in KillTheHost()")]
		public static bool LaunchFieldworksBridge(string projectFolder, string userName, string command, string projectGuid,
												  int fwmodelVersionNumber, string liftModelVersionNumber, string writingSystemId,
			out bool changesReceived, out string projectName)
		{
			_pipeID = string.Format(@"SendReceive{0}{1}", projectFolder, command);
			flexBridgeTerminated = false;
			changesReceived = false;
			var args = "";
			projectName = "";
			_projectName = "";
			_sFwProjectName = "";
			var userNameActual = userName;
			if (string.IsNullOrEmpty(userName))
				userNameActual = Environment.UserName; // default so we can always pass something.
			if (userNameActual != null) // Paranoia, hopefully never null
			{
				AddArg(ref args, "-u", userNameActual);
			}
			if (!String.IsNullOrEmpty(projectFolder))
			{    // can S/R multiple projects simultaneously
				AddArg(ref args, "-p", projectFolder);
				if (projectFolder != DirectoryFinder.ProjectsDirectory)
					_sFwProjectName = Path.GetFileNameWithoutExtension(projectFolder);
			}

			AddArg(ref args, "-v", command);

			if (command == SendReceive && FixItAppExists)
			{
				AddArg(ref args, "-f", FixItAppPathname);
			}

			if (!String.IsNullOrEmpty(projectGuid))
			{
				AddArg(ref args, "-g", projectGuid);
			}

			// Add two paths: to FW projDir & FW apps folder. Then, FB won't have to look in a zillion registry entries
			AddArg(ref args, "-projDir", DirectoryFinder.ProjectsDirectory);
			AddArg(ref args, "-fwAppsDir", FieldWorksAppsDir);
			// Tell Flex Bridge which model version of data are expected by FLEx.
			AddArg(ref args, "-fwmodel", fwmodelVersionNumber.ToString());
			AddArg(ref args, "-liftmodel", liftModelVersionNumber);
			// current culture may have country etc info after a hyphen. FlexBridge just needs the main language ID.
			// It probably can't ever be null or empty, but let's be as robust as possible.
			var locale = Thread.CurrentThread.CurrentUICulture.Name;
			locale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Split('-')[0];
			AddArg(ref args, "-locale", locale);

			if (_noBlockerHost != null)
			{
				return false;
			}
			AddArg(ref args, "-pipeID", _pipeID);
			if (!String.IsNullOrEmpty(writingSystemId))
			{
				AddArg(ref args, "-ws", writingSystemId);
			}

			// make a new FLExBridge
			ServiceHost host = null;
			try
			{
				var hostPipeBinding = new NetNamedPipeBinding { ReceiveTimeout = TimeSpan.MaxValue };
				host = new ServiceHost(typeof(FLExBridgeService), new[] { new Uri("net.pipe://localhost/FLExBridgeEndpoint" + _pipeID) });

				//open host ready for business
				host.AddServiceEndpoint(typeof(IFLExBridgeService), hostPipeBinding, "FLExPipe");
				host.Open();
			}
			catch (InvalidOperationException)
				// Can happen if Conflict Report is open and we try to run FLExBridge again.
			{
				if (host != null)
					((IDisposable)host).Dispose();
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}
			catch (AddressAlreadyInUseException)
				// Can happen if FLExBridge has been launched and we try to launch FLExBridge again.
			{
				// host is normally not null for this exception, but there is no pipe to dispose
				return false; // Unsuccessful startup. Caller should report duplicate bridge launch.
			}

			LaunchFlexBridge(host, command, args, ref changesReceived, ref projectName);

			return true;
		}

		private static void LaunchFlexBridge(ServiceHost host, string command, string args, ref bool changesReceived, ref string projectName)
		{
			// Launch the bridge process.
			using (Process.Start(FullFieldWorksBridgePath(), args))
			{
			}

			var nonFlexblockers = new HashSet<string>
				{
					ConflictViewer,
					LiftConflictViewer,
					AboutFLExBridge,
					CheckForUpdates
				};
			if (nonFlexblockers.Contains(command))
			{
				// This skips the piping and doesn't pause the Flex UI thread for the
				// two 'view' options and for the 'About Flex Bridge' and 'Check for Updates'.
				_noBlockerHost = host; // so we can kill the host when the bridge quits
			}
			else
			{
				// This uses all the piping and also blocks the Flex UI thread, while Flex Bridge is running.
				Cursor.Current = Cursors.WaitCursor;

				// Pause UI thread until FLEx Bridge terminates:
				Monitor.Enter(_waitObject);
				if (flexBridgeTerminated == false)
					Monitor.Wait(_waitObject, -1);
				Monitor.Exit(_waitObject);

				projectName = _projectName;
				changesReceived = _receivedChanges;
				Cursor.Current = Cursors.Default;
				KillTheHost(host);
			}
		}

		private static void KillTheHost(ServiceHost host)
		{
			// Let the service host cleanup happen in another thread so the user can get on with life.
			var letTheHostDie = new Thread(() =>
												{
													try
													{
														host.Close();
														((IDisposable) host).Dispose();
													}
													catch(Exception)
													{
														//we don't care anymore, just die.
													}
												});
			letTheHostDie.Start();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: It is unclear if disposing the ChannelFactory affects channelClient.")]
		private static void BeginEmergencyExitChute(string pipeID)
		{
			var clientPipeBinding = new NetNamedPipeBinding {ReceiveTimeout = TimeSpan.MaxValue};
			var factory = new ChannelFactory<IFLExServiceChannel>
				(clientPipeBinding, new EndpointAddress("net.pipe://localhost/FLExEndpoint" + pipeID + "/FLExPipe"));
			var channelClient = factory.CreateChannel();
			channelClient.OperationTimeout = TimeSpan.MaxValue;
			channelClient.BeginBridgeWorkOngoing(WorkDoneCallback, channelClient);
		}

		private static void AddArg(ref string extant, string flag, string value)
		{
			if (!string.IsNullOrEmpty(extant))
			{
				extant += " ";
			}
			extant += flag;
			if (!string.IsNullOrEmpty(value))
			{
				bool hasWhitespace;
				if (value.Any(Char.IsWhiteSpace))
				{
					extant += " \"" + value + "\"";
				}
				else
				{
					extant += " " + value;
				}
			}
		}

		/// <summary>
		/// Returns the full path and filename of the FieldWorksBridge executable
		/// </summary>
		/// <returns></returns>
		public static string FullFieldWorksBridgePath()
		{
			return Path.Combine(DirectoryFinder.FlexBridgeFolder, FLExBridgeName);
		}

		/// <summary>
		/// Returns the full path and filename of the FieldWorksBridge executable
		/// </summary>
		/// <returns></returns>
		public static bool FixItAppExists
		{
			get
			{
				var fixitAppPathname = FixItAppPathname;
				return !string.IsNullOrEmpty(fixitAppPathname) && File.Exists(fixitAppPathname);
			}
		}

		/// <summary>
		/// Returns the full path and filename of the FixFwData executable
		/// </summary>
		/// <returns></returns>
		public static string FixItAppPathname
		{
			get
			{
				return Path.Combine(FieldWorksAppsDir, "FixFwData.exe");
			}
		}

		/// <summary>
		/// Returns the full path to where the FieldWorks running apps are located
		/// </summary>
		/// <returns></returns>
		public static string FieldWorksAppsDir
		{
			get
			{
				return Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
			}
		}

		#region Service classes and methods For Bridge calls to FLEx

		/// <summary>
		/// The service class
		/// </summary>
		[ServiceBehavior(UseSynchronizationContext = false)] //Create new threads for the services, don't tie them into the main UI thread.
		private class FLExBridgeService : IFLExBridgeService
		{
			#region Implementation of IFLExBridgeService

			/// <summary>
			/// This method signals that FLExBridge completed normally.
			/// </summary>
			/// <param name="changesReceived">true if the send/receive or other operation resulted in local changes</param>
			public void BridgeWorkComplete(bool changesReceived)
			{
				_receivedChanges = changesReceived;
				AlertFlex();
				if (_noBlockerHost != null)
				{
					KillTheHost((ServiceHost) _noBlockerHost);
					_noBlockerHost = null;
				}
			}

			public void BridgeReady()
			{
				BeginEmergencyExitChute(_pipeID);
			}

			public void InformFwProjectName(string fwProjectName)
			{
				_projectName = fwProjectName;
			}

			/// <summary>
			/// FLExBridge user clicked on the title of a particular conflict in the conflict report.
			/// </summary>
			/// <param name="jumpUrl">Url of the FLEx object to jump to.</param>
			public void BridgeSentJumpUrl(string jumpUrl)
			{
				if (FLExJumpUrlChanged != null)
					FLExJumpUrlChanged(this, new FLExJumpEventArgs(jumpUrl));
			}

			/// <summary>
			/// Acquire the lock on, and then pulse the wait object
			/// </summary>
			private static void AlertFlex()
			{
				Monitor.Enter(_waitObject); //acquire the lock on the _waitObject
				flexBridgeTerminated = true;
				Monitor.Pulse(_waitObject); //notify a thread waiting on _waitObject that it may continue.
				Monitor.Exit(_waitObject); //release the lock on the _waitObject so they actually can continue.
			}
			#endregion
		}

		/// <summary>
		/// Service interface for methods in FLEx that FLExBridge can call.
		/// </summary>
		[ServiceContract]
		private interface IFLExBridgeService
		{
			[OperationContract]
			void BridgeWorkComplete(bool changesReceived);

			[OperationContract]
			void BridgeReady();

			[OperationContract]
			void InformFwProjectName(string fwProjectName);

			[OperationContract]
			void BridgeSentJumpUrl(string jumpUrl);
		}

		#endregion
		#region Service classes and methods for FLEx calls to Bridge
		/// <summary>
		/// Service interface for the methods in FLEXBridge that we can call
		/// </summary>
		[ServiceContract]
		private interface IFLExService
		{
			[OperationContract(AsyncPattern = true)]
			IAsyncResult BeginBridgeWorkOngoing(AsyncCallback callback, object asyncState);

			void EndBridgeWorkOngoing(IAsyncResult result);
		}

		/// <summary>
		/// This interface combines the service and channel objects so a factory can give us a useful oboject
		/// </summary>
		private interface IFLExServiceChannel : IFLExService, IClientChannel
		{
		}

		/// <summary>
		/// This callback mostly serves to help us terminate in exceptional cases.
		/// It is not reliable for return data because it is asynchronous, and FLExBridge might close before we retrieve the data
		/// </summary>
		/// <param name="iar"></param>
		private static void WorkDoneCallback(IAsyncResult iar)
		{
			Monitor.Enter(_waitObject);
			flexBridgeTerminated = true;
			try
			{
				Monitor.Pulse(_waitObject);
				((IFLExServiceChannel)iar.AsyncState).EndBridgeWorkOngoing(iar);
			}
			catch(CommunicationException)
			{
				//Something went wrong with the communication to the Bridge. Possibly it died unexpectedly, wake up FLEx
				Monitor.Pulse(_waitObject);
			}
			catch (Exception e)
			{
				Logger.WriteError(e); //Write the log entry, but likely not important
			}
			finally
			{
				if (_noBlockerHost != null)
				{
					KillTheHost((ServiceHost) _noBlockerHost);
					_noBlockerHost = null;
				}
				Monitor.Exit(_waitObject);
			}
		}

		#endregion
	}

	/// <summary>
	/// Event args plus jump URL
	/// </summary>
	public class FLExJumpEventArgs : EventArgs
	{
		private readonly string _jumpUrl;

		/// <summary>
		/// Set up event args with a URL to jump to.
		/// </summary>
		/// <param name="jumpUrl"></param>
		public FLExJumpEventArgs(string jumpUrl)
		{
			_jumpUrl = jumpUrl;
		}

		/// <summary>
		/// URL that FLEx should jump to when processing this event.
		/// </summary>
		public string JumpUrl
		{
			get { return _jumpUrl; }
		}
	}

}
