using System;
using System.Collections;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.TV.Recording;
using MediaPortal.TV.Database;
 
namespace MediaPortal.PowerScheduler
{
	/// <summary>
	/// Summary description for NVPPowerScheduler.
	/// </summary>
	//public class NVPPowerScheduler: IPlugin, ISetupForm
	public class NVPPowerScheduler: GUIWindow, IPlugin, ISetupForm
	{
		public static int WINDOW_POWERSCHEDULER = 6039;	// a window ID Won't be needed when converted to non visual plugin
														// Tried it but after one sleep we weren't doing well

		static int		m_iPreRecordInterval = 0;	// Interval to start recording before entered starttime
		static int		m_iStartupInterval = 1;		// 1 minute to give computer time to startup
		static int		m_iShutdownInterval = 3;	// idle minutes before shutdown(hibernate/standby)
		static	long	m_iCurrentStart = 0;		// store current wakeup time (ticks) for comparesing
		static	int		m_iActiveWindow = -1;		// current active window, used to check when WINDOW_HOME is activated
		static string	m_shutdownMode = "None";
		static bool		m_bShutdownEnabled = false;	// shutdown enabled if true
		static DateTime	m_dtShutdownTime = new DateTime();
		static bool		m_bExtensiveLog = false;
		static bool		m_bProgramsChanged = false;
		static bool		m_bRecordingsChanged = false;
		static bool		m_bResetWakeuptime = true;
		static bool		m_bForceShutdown = false;
		static bool		m_bDisableTV = false;
		static bool		m_bDisabled = false;

		// Instanceate a waitabletimer
		static private	WaitableTimer m_Timer = new WaitableTimer();

		// Instanceate a ShutdownTimer
		// Shutdown will only be activated on HOME-window and is prohibited by ongoing and
		// near in time pending recordings
		static private System.Windows.Forms.Timer  m_SDTimer = new System.Windows.Forms.Timer();

		public NVPPowerScheduler()
		{	
			GetID = WINDOW_POWERSCHEDULER;
		}
	
		public override bool Init()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: Init() ");
			LoadSettings();
			
			if (m_bDisabled)
			{
				return false;
			}
			else
			{
				if (m_bExtensiveLog) Log.Write("PowerScheduler: Call Start()");	
				this.Start();
			}
			return true;
		}
    public override void DeInit()
    {
      m_Timer .Close();
    }


		public void Start()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: Start() ");

			if (m_bDisabled)
			{
				if (m_bExtensiveLog) Log.Write(" PowerScheduler: disabled ");
				return;
			}

			// start recorder if needed and option choosed
			if (m_bDisableTV)
				Recorder.Start();

			// if StartupInterval is larger than 0 register startup timer events
			if (m_iStartupInterval > 0)
			{	// Register with PowerManager
				PowerManager.OnPowerUp += new PowerManager.ResumeHandler(this.OnWakeupTimer);

				// Register with WaitableTimer - make sure that processing starts on a different thread
				m_Timer.OnTimerExpired += new WaitableTimer.TimerExpiredHandler(PowerManager.OnResume);
			}

			// Register with TVDatabase
			// listen to OnRecordingsChanged and OnProgramsChanged
			TVDatabase.OnRecordingsChanged += new MediaPortal.TV.Database.TVDatabase.OnChangedHandler(this.OnRecordingsChanged);
			TVDatabase.OnProgramsChanged +=new MediaPortal.TV.Database.TVDatabase.OnChangedHandler(this.OnProgramsChanged);
	
			m_bResetWakeuptime = true;
			m_iCurrentStart = -1;

			m_SDTimer.Tick +=new EventHandler(OnTimer);
			//m_SDTimer.Interval = 30000; // 30 secs between every check 
			m_SDTimer.Interval = 10000; // 10 secs between every check 

			if (m_iShutdownInterval > 0)
			{
				m_SDTimer.Start();
				ResetShutdownTimer(m_iShutdownInterval);	// set and enable the shutdown timer
			}
			else
			{	
				ResetShutdownTimer(0);
				m_SDTimer.Stop();
			}
		
		}

		public void Stop()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: Stop() ");
			m_Timer.SecondsToWait = -1;
		}
 
		/// <summary>
		/// The wakeup timer has expired
		/// </summary>
		void OnWakeupTimer()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: OnWakeupTimer() ");

			Log.Write("PowerScheduler: Wakeup timer expired ");	
			m_bResetWakeuptime = true;
			m_iCurrentStart = -1;
			SetPowerUpTimer();

			// start recorder if needed
			if (m_bDisableTV)
				Recorder.Start();
		}

		/// <summary>
		/// Recordings has been changed, flag for a rescan
		/// </summary>
		void OnRecordingsChanged()
		{	
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: OnRecordingsChanged() ");
			m_bRecordingsChanged = true;
		}

		/// <summary>
		/// TVGuide has been updated, flag for a rescan
		/// </summary>
		void OnProgramsChanged()
		{	
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: OnProgramsChanged() ");
			m_bProgramsChanged = true;
		}
		
		/// <summary>
		/// Check every X seconds if time to do something 
		/// </summary>
		private void OnTimer(Object sender, EventArgs e)
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: OnTimer() ");

			if (m_bExtensiveLog) Log.Write(" PowerScheduler: Active window {0}, {1}, {2}",GUIWindowManager.ActiveWindow,m_iActiveWindow, m_bShutdownEnabled);
			
			// Manages when to wake up
			WakeupManager();

			// manages when to shutdown
			ShutdownManager();
		}

	
		# region WakeupManager
		/// <summary>
		///  Handles setting the wakeup
		/// </summary>
		private void WakeupManager()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: WakeupManager() ");

			if (m_bProgramsChanged || m_bRecordingsChanged || m_bResetWakeuptime || (DateTime.Now.Ticks >  m_iCurrentStart))
			{
				// reset the flags
				m_bProgramsChanged = false;
				m_bRecordingsChanged = false;
				m_bResetWakeuptime = false;

				if (m_bRecordingsChanged)
				{
					Log.Write("PowerScheduler: Recordings has changed - rescan recordings ");	
				}
				if (m_bProgramsChanged)
				{
					Log.Write("PowerScheduler: TVguide has been updated - recalculate recordings");
				}
				SetPowerUpTimer();
			}
		}
		
		/// <summary>
		/// This function looks at pending recordings and set the wakeup timer
		/// to next recording which starttime is in the future. 
		/// </summary>
		static private void SetPowerUpTimer()
		{ 
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: SetPowerUpTimer() ");

			if (m_iStartupInterval == 0)
			{
				m_Timer.SecondsToWait = -1;
				return;
			}

			// Find the next pending recording 
			DateTime nextStart = GetNextRecordingStarttime(m_iStartupInterval + m_iPreRecordInterval);
			
			// if Starttime differs set new starttime
			if (nextStart.Ticks != m_iCurrentStart) 
			{	
				m_iCurrentStart = nextStart.Ticks;
		
				// make sure the timer is set to a (future) pending recording
				if (nextStart.CompareTo(DateTime.Now) > 0)
				{	
					Log.Write ("PowerScheduler: next scheduled recording starttime: {0} ", nextStart);
					
					// calculate when to set wakeup timer
					nextStart = nextStart.AddMinutes(-m_iStartupInterval);
					
					// convert to seconds and set the timer
					TimeSpan tDelta = nextStart.Subtract(DateTime.Now);		
					m_Timer.SecondsToWait = tDelta.TotalSeconds;
					Log.Write("PowerScheduler: Set wakeup timer at {0}", nextStart);
				}
				else
				{	// disable timer
					m_Timer.SecondsToWait = -1;
					Log.Write("PowerScheduler: No pending recordings scheduled, disable wakeup timer (might be pending recordings too near in time to use wakeup)");
				}
			}
			else
			{
				//Log.Write("PowerScheduler: Nothing to change");
			}
		}

		#endregion

		#region ShutdownManager
		/// <summary>
		///  Handles shutdown, hibernate, stand by 
		/// </summary>
		private void ShutdownManager()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: ShutdownManager() ");

			// when the active window has changed check if 
			// to enable or disable shutdown
			if (m_iActiveWindow != (int) GUIWindowManager.ActiveWindow)
			{
				m_iActiveWindow = GUIWindowManager.ActiveWindow;

				if (m_iActiveWindow == (int) GUIWindow.Window.WINDOW_HOME)
				{
					if (!m_bShutdownEnabled)
					{
						// Entered HOME - enable shutdown
						ResetShutdownTimer(m_iShutdownInterval);
					}
				}
				else
				{
					if (m_bShutdownEnabled)
					{
						// Left HOME - disable shutdown
						ResetShutdownTimer(0);	
					}
				}
			}
			ShutdownCheck();	// check if time to shutdown
		}

		/// <summary>
		/// Check prior to shutdown if, shutdown is canceled by any 
		/// ongoing or pending recording. 
		/// </summary>
		private bool PreShutdownCheck()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: PreShutdownCheck() ");

			if (GUIWindowManager.ActiveWindow != 0)
			{
				Log.Write("PowerScheduler: Shutdown process aborted - home is not the active window");
				ResetShutdownTimer(0);
				return false;
			}

			if (Recorder.IsRecording)
			{
				Log.Write("PowerScheduler: Shutdown process aborted - TVrecording in progress");
				ResetShutdownTimer(m_iShutdownInterval);
				return false;
			}

			// Find the next pending recording
			// If it's due in less than the "ShutdownInterval" cancel shutdown
			DateTime nextStart = new DateTime();
			nextStart = GetNextRecordingStarttime(0);

			if (nextStart.CompareTo(DateTime.Now) > 0)
			{
				TimeSpan tDelta = nextStart.Subtract(DateTime.Now);		

				// use same interval as "idle minutes before shutdown" to decide if it's ok
				// to shutdown before the next recording.
				int compvar = (int)Convert.ChangeType(tDelta.TotalMinutes, typeof(int));
				if (compvar <= m_iShutdownInterval)
				{
					Log.Write("PowerScheduler: Shutdown process aborted - pending recording within {0} minutes", tDelta.Minutes );
					ResetShutdownTimer(m_iShutdownInterval);
					return false;
				}
			}
			return true;
		}


		/// <summary>
		///  Performs shutdown, hibernate, stand by when it's time
		/// </summary>
		private void ShutdownCheck()
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: ShutdownCheck() ");

			TimeSpan tDelta = m_dtShutdownTime.Subtract(DateTime.Now);			
			if (m_bShutdownEnabled && (DateTime.Now.CompareTo(m_dtShutdownTime)>0) )
			{
				Log.Write("PowerScheduler: Shutdown timer expired");
				if (PreShutdownCheck())
				{
					// about to shutdown - disable shutdown timer
					ResetShutdownTimer(0);
					m_SDTimer.Stop();
					
					if (m_bDisableTV)
					{
						Log.Write("PowerScheduler: Prepare for shutdown, disable any TVcard ");
						Recorder.Stop();
					}

					if (m_shutdownMode.StartsWith("None"))
					{
						Log.Write("PowerScheduler: No shutdown");	
					}
						
					if (m_shutdownMode.StartsWith("Suspend"))
					{
						Log.Write("PowerScheduler: Suspend system");
						WindowsController.ExitWindows(RestartOptions.Suspend, m_bForceShutdown);
					}

					if (m_shutdownMode.StartsWith("Hibernate"))
					{
						Log.Write("PowerScheduler: Hibernate system");
						WindowsController.ExitWindows(RestartOptions.Hibernate, m_bForceShutdown);
					}
							
					if (m_shutdownMode.StartsWith("Shutdown"))
					{
						Log.Write("PowerScheduler: System shutdown");
						WindowsController.ExitWindows(RestartOptions.ShutDown, m_bForceShutdown);
					}
				}
			}
		}

		/// <summary>
		/// Reset the shutdown timer
		/// </summary>
		static void ResetShutdownTimer(int aMinutes)
		{	
			Log.Write("PowerScheduler: ResetShutdownTimer() ");

			if (aMinutes == 0)
			{	// shutdown disabled (set shutdown time 1 year into the future)
				// - this is probably not the best solution
				m_dtShutdownTime = DateTime.Now.AddYears(1);

				if (m_bShutdownEnabled)
				{
					Log.Write("PowerScheduler: Shutdown timer deactivated");
				}
				m_bShutdownEnabled = false;
			}
			else
			{
				// set shutdown time N minutes from now
				m_dtShutdownTime = DateTime.Now.AddMinutes(aMinutes);
				if (!m_bShutdownEnabled)
				{
					Log.Write("PowerScheduler: Shutdown timer activated, automatic shutdown in {0} minutes", m_iShutdownInterval);
				}
				m_bShutdownEnabled = true;
			}
		}

		#endregion

		#region Settings

		void LoadSettings()
		{
			Log.Write("PowerScheduler: 0.1 beta(3)");

			using(AMS.Profile.Xml xmlreader = new AMS.Profile.Xml("MediaPortal.xml"))
			{
				m_iStartupInterval = xmlreader.GetValueAsInt("powerscheduler","wakeupinterval",1);
				m_iShutdownInterval = xmlreader.GetValueAsInt("powerscheduler","shutdowninterval",3);
				m_shutdownMode = xmlreader.GetValueAsString("powerscheduler","shutdownmode","Suspend");
				m_bExtensiveLog = xmlreader.GetValueAsBool("powerscheduler","extensivelogging",false);
				m_bForceShutdown = xmlreader.GetValueAsBool("powerscheduler","forcedshutdown",false);
				m_bDisableTV = xmlreader.GetValueAsBool("powerscheduler","disabletv",false);
				
				m_bDisabled = xmlreader.GetValueAsString("plugins","Power Scheduler","yes") == "no";
				
				if (m_bDisabled)
				{
					Log.Write("PowerScheduler: Disabled");	
				}
				else
				{
					if (m_bForceShutdown)
					{
						Log.Write("PowerScheduler: Settings loaded - wakeup {0}, shutdown {1}, mode {2} - Forced",m_iStartupInterval, m_iShutdownInterval, m_shutdownMode);	
					}
					else
					{
						Log.Write("PowerScheduler: Settings loaded - wakeup {0}, shutdown {1}, mode {2}",m_iStartupInterval, m_iShutdownInterval, m_shutdownMode);	
					}
					if (m_bExtensiveLog) Log.Write("PowerScheduler: Extensive logging");
					if (m_iStartupInterval ==0) Log.Write("PowerScheduler: Wakeup from hibernate/standby - disabled");
					if (m_iShutdownInterval ==0) Log.Write("PowerScheduler: Shutdown on idle - disabled");
				}
				
				m_iPreRecordInterval = xmlreader.GetValueAsInt("capture","prerecord",0);
			}
		}
		#endregion

		#region Interfacing TVdatabase and TVrecorder
	
		/// <summary>
		/// This function gets the starttime of the next pending recording
		/// </summary>
		static private DateTime GetNextRecordingStarttime(int minutes)
		{
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: GetNextRecordingStarttime() ");

			// get starttime of the first recording which is the next to due
			// care not to set it to the current recording (due in a minute or so) 
			ArrayList recordings = new ArrayList();
			ArrayList TVGuideRecordings = new ArrayList();
			DateTime nextStarttime = DateTime.Now.AddMonths(1);
			DateTime earliestStarttime = DateTime.Now.AddMinutes(minutes);	

			recordings.Clear();
			TVGuideRecordings.Clear();

			if (TVDatabase.GetRecordings(ref recordings)) 
			{
				foreach (TVRecording recording in recordings)
				{
					DateTime tmpNextStarttime = new DateTime();

					if (recording.Canceled > 0)
					{
						continue;
					}
					
					switch (recording.RecType)
					{
						case (TVRecording.RecordingType.Once):
						{						
							tmpNextStarttime = recording.StartTime.AddMinutes(- m_iPreRecordInterval);
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  Once - next starttime {0} ", tmpNextStarttime);
							break;
						}
						case (TVRecording.RecordingType.Daily):
						{
							tmpNextStarttime = recording.StartTime.AddMinutes(- m_iPreRecordInterval);
							double days = - tmpNextStarttime.Subtract(earliestStarttime).TotalDays ;	
							tmpNextStarttime = tmpNextStarttime.AddDays(Math.Round(days,0) + 1);
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  Daily - next starttime {0} ", tmpNextStarttime);
							break;
						}
						case (TVRecording.RecordingType.WeekDays):
						{
							tmpNextStarttime = recording.StartTime.AddMinutes(- m_iPreRecordInterval);
							double days = - tmpNextStarttime.Subtract(earliestStarttime).TotalDays ;	
							tmpNextStarttime = tmpNextStarttime.AddDays(Math.Round(days,0) + 1);

							while (tmpNextStarttime.DayOfWeek == System.DayOfWeek.Saturday || tmpNextStarttime.DayOfWeek == System.DayOfWeek.Sunday)
							{
								tmpNextStarttime = tmpNextStarttime.AddDays(1);
							}
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  WeekDays next starttime {0} ", tmpNextStarttime);
							break;
						}
						case (TVRecording.RecordingType.Weekly):
						{
							tmpNextStarttime = recording.StartTime.AddMinutes(- m_iPreRecordInterval);
							System.DayOfWeek day = tmpNextStarttime.DayOfWeek;

							double days = - Math.Round(tmpNextStarttime.Subtract(earliestStarttime).TotalDays,0) ;	
							double weeks = Math.Round((days / 7),0);
							tmpNextStarttime = tmpNextStarttime.AddDays(weeks * 7 + 7);
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  Weekly next starttime {0} ", tmpNextStarttime);
							break;
						}
						case (TVRecording.RecordingType.EveryTimeOnEveryChannel):
						{
							TVGuideRecordings.Add(recording);
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  EveryTimeOnEveryChannel ");
							break;
						}
						case (TVRecording.RecordingType.EveryTimeOnThisChannel):
						{
							TVGuideRecordings.Add(recording);
							if (m_bExtensiveLog) Log.Write(" PowerScheduler:  EveryTimeOnThisChannel ");
							break;
						}
					}

					if (tmpNextStarttime.Ticks > earliestStarttime.Ticks && tmpNextStarttime.Ticks < nextStarttime.Ticks)
					{
						nextStarttime = new DateTime(tmpNextStarttime.Ticks);
					}
				}

				if (TVGuideRecordings.Count > 0)
				{
					if (m_bExtensiveLog) Log.Write(" PowerScheduler: Evaluate TVGuide recordings ");
					
					ArrayList tvPrograms = new ArrayList();
					bool programfound = false;
					DateTime tmpNextStarttime = new DateTime();
					tvPrograms.Clear();
					
					if (TVDatabase.GetPrograms(Utils.datetolong(earliestStarttime), Utils.datetolong(nextStarttime),ref tvPrograms)) 
					{
						foreach (TVProgram program in tvPrograms)
						{
							foreach (TVRecording rec in TVGuideRecordings)
							{
		
								switch (rec.RecType)
								{
									case (TVRecording.RecordingType.EveryTimeOnEveryChannel):
									{		
										if (program.Title == rec.Title)
										{
											tmpNextStarttime = program.StartTime.AddMinutes(- m_iPreRecordInterval);
											programfound = true;
											if (m_bExtensiveLog) Log.Write(" PowerScheduler:  TVGuide {0} {1} {2} ", program.Title, program.Channel, tmpNextStarttime);
										}
										break;
									}
									case (TVRecording.RecordingType.EveryTimeOnThisChannel):
									{		
										if (program.Title == rec.Title && program.Channel == rec.Channel)
										{
											tmpNextStarttime = program.StartTime.AddMinutes(- m_iPreRecordInterval);
											programfound = true;
											if (m_bExtensiveLog) Log.Write(" PowerScheduler:  TVGuide {0} {1} {2} ", program.Title, program.Channel, tmpNextStarttime);
										}
										break;
									}
								}
								if (programfound)
									break;
							}
							if (programfound)
								break;
						}
					}
					if (tmpNextStarttime.Ticks > earliestStarttime.Ticks && tmpNextStarttime.Ticks < nextStarttime.Ticks)
					{
						nextStarttime = new DateTime(tmpNextStarttime.Ticks);
					}
				}
			}
			if (m_bExtensiveLog) Log.Write(" PowerScheduler: GetNextRecordingStarttime() starttime {0} ", nextStarttime);
			return nextStarttime;
		}


		#endregion
		
		#region ISetupForm Members

		public bool CanEnable()
		{
			return true;
		}

		public string PluginName()
		{
			return "Power Scheduler";
		}

		public bool HasSetup()
		{
			return true;
		}
		public bool DefaultEnabled()
		{
			return true;
		}

		public int GetWindowId()
		{
			return WINDOW_POWERSCHEDULER;
		}

		public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
		{
			strButtonText = "Power Scheduler";
			strButtonImage = "";
			strButtonImageFocus = "";
			strPictureImage = "";
			return false;
		}

		public string Author()
		{
			return "Fred";
		}

		public string Description()
		{
			return "Power manager plugin (hibernate/resume)";
		}

		public void ShowPlugin() // show the setup dialog
		{
			System.Windows.Forms.Form setup=new MediaPortal.PowerScheduler.PowerSchedulerSetupForm();
			setup.ShowDialog();
		}
		

		#endregion
	}
}
