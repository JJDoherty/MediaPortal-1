using System;
using System.Text;
using System.Collections;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using MediaPortal.Playlists;
using MediaPortal.Radio.Database;

namespace MediaPortal.GUI.Alarm
{
	/// <summary>
	/// Alarm Class
	/// </summary>
	public class Alarm
	{
		#region Private Variables
			private System.Windows.Forms.Timer _AlarmTimer = new System.Windows.Forms.Timer();
			private System.Windows.Forms.Timer _VolumeFadeTimer = new  System.Windows.Forms.Timer();
			private int _Id;
			private bool _Enabled;
			private string _Name;
			private DateTime _Time;
			private AlarmType _Type;
			private bool _Mon;
			private bool _Tue;
			private bool _Wed;
			private bool _Thu;
			private bool _Fri;
			private bool _Sat;
			private bool _Sun;
			private string _Sound;
			private PlayType _PlayType;
			private bool _VolumeFade;
			private GUIListItem _SelectedItem;
		#endregion

		#region Public Enumerations
			public enum AlarmType
			{
				Alarm = 1,
				SleepTimer = 2
			}
			public enum PlayType
			{
				PlayList =0,
				Radio = 1,
				Alarm = 2	
			}
		#endregion

		#region Constructor
			public Alarm(int id,string name,int playType, bool enabled,DateTime time,AlarmType type,bool mon,bool tue,bool wed,bool thu,bool fri,bool sat,bool sun,string sound,bool volumeFade)
			{
				_Id = id;
				_Name = name;
				_PlayType = (PlayType)playType;
				_Enabled = enabled;
				_Time = time;
				_Type = type;
				_Mon = mon;
				_Tue = tue;
				_Wed = wed;
				_Thu = thu;
				_Fri = fri;
				_Sat = sat;
				_Sun = sun;
				_Sound = sound;
				_VolumeFade = volumeFade;


				InitializeTimer();

			}

			public Alarm(int id)
			{
				_Id = id;
				_Name = "NewAlarm" + _Id.ToString();
			}
		#endregion

		#region Properties	
			public string Name
			{
				get{return _Name;}
				set{_Name = value;}
			}
			public string DaysEnabled
			{
				get
				{
					StringBuilder sb= new StringBuilder("-------");

					if(_Sun)
						sb.Replace("-","S",0,1);
					if(_Mon)
						sb.Replace("-","M",1,1);
					if(_Tue)
						sb.Replace("-","T",2,1);
					if(_Wed)
						sb.Replace("-","W",3,1);
					if(_Thu)
						sb.Replace("-","T",4,1);
					if(_Fri)
						sb.Replace("-","F",5,1);
					if(_Sat)
						sb.Replace("-","S",6,1);

					return sb.ToString();

				}

			}
			public PlayType AlarmPlayType
			{
				get{return _PlayType;}
				set{_PlayType = value;}
			}
			public bool Enabled
			{
				get{return _Enabled;}
				set{
					_Enabled = value;
					_AlarmTimer.Enabled = value;
					}
			}
			public DateTime Time
			{
				get{return _Time;}
				set{_Time = value;}
			}
			public AlarmType Type
			{
				get{return _Type;}
				set{_Type = value;}
			}
			public string Sound
			{
				get{return _Sound;}
				set{_Sound = value;}
			}
			public int Id
			{
				get{return _Id;}
			}
			public bool Mon
			{
				get{return _Mon;}
				set{_Mon = value;}
			}
			public bool Tue
			{
				get{return _Tue;}
				set{_Tue = value;}
			}
			public bool Wed
			{
				get{return _Wed;}
				set{_Wed = value;}
			}
			public bool Thu
			{
				get{return _Thu;}
				set{_Thu = value;}
			}
			public bool Fri
			{
				get{return _Fri;}
				set{_Fri = value;}
			}
			public bool Sat
			{
				get{return _Sat;}
				set{_Sat = value;}
			}
			public bool Sun
			{
				get{return _Sun;}
				set{_Sun = value;}
			}
			public bool VolumeFade
			{
				get{return _VolumeFade;}
				set{_VolumeFade = value;}
			}
			public GUIListItem SelectedItem
			{
				get{return _SelectedItem;}
				set{_SelectedItem = value;}
			}
		#endregion

		#region Private Methods
		
		/// <summary>
		/// Initializes the timer object
		/// </summary>
		private void InitializeTimer()
		{
			_AlarmTimer.Tick += new EventHandler(OnTimer);
			_AlarmTimer.Interval = 1000; //second	
			_VolumeFadeTimer.Tick += new EventHandler(OnTimer);
			_VolumeFadeTimer.Interval = 3000; //3 seconds	

			if(_Enabled)
				_AlarmTimer.Enabled = true;
		}

		/// <summary>
		/// Executes on the interval of the timer object.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnTimer(Object sender, EventArgs e)
		{
			if(sender == _AlarmTimer)
			{
				if(DateTime.Now.Hour == _Time.Hour && DateTime.Now.Minute == _Time.Minute && IsDayEnabled())
				{
					Log.Write("Alarm {0} fired at {1}",_Name,DateTime.Now);

					if (!GUIGraphicsContext.IsFullScreenVideo)
					{
						Play();
						//enable fade timer if selected
						if(_VolumeFade)
						{
							g_Player.Volume = 0;
							_VolumeFadeTimer.Enabled = true;
						}
							
						GUIWindowManager.ActivateWindow(GUIAlarm.WINDOW_ALARM);
					}

					//disable the timer.
					_AlarmTimer.Enabled = false;
				}
			}
			if(sender == _VolumeFadeTimer)
			{
				if(g_Player.Volume < 99)
				{
					g_Player.Volume +=1;

				}
				else
				{
					_VolumeFadeTimer.Enabled = false;
				}
			}
			
		}

		

		/// <summary>
		/// Checks if the current dayofweek for the alarm is enabled
		/// </summary>
		/// <returns>true if current dayofweek is enabled</returns>
		private bool IsDayEnabled()
		{
			switch(DateTime.Now.DayOfWeek)
			{
				case DayOfWeek.Monday:
					return _Mon;
				case DayOfWeek.Tuesday:
					return _Tue;
				case DayOfWeek.Wednesday:
					return _Wed;
				case DayOfWeek.Thursday:
					return _Thu;
				case DayOfWeek.Friday:
					return _Fri;
				case DayOfWeek.Saturday:
					return _Sat;
				case DayOfWeek.Sunday:
					return _Sun;
			}
			return false;
		}

		/// <summary>
		/// Plays the selected play type
		/// </summary>
		private void Play()
		{
			switch(_PlayType)
			{
				case PlayType.PlayList:
					if(PlayListFactory.IsPlayList(_Sound))
					{
						PlayList playlist = PlayListFactory.Create(GUIAlarm.PlayListPath + "\\" + _Sound);
						if(playlist==null) return;
						if(!playlist.Load(GUIAlarm.PlayListPath + "\\" +  _Sound))
						{
							ShowErrorDialog();
							return;
						}
						if(playlist.Count == 1)
						{
							g_Player.Play(playlist[0].FileName);
							g_Player.Volume=99;
							return;
						}
						for(int i=0; i<playlist.Count; ++i)
						{
							PlayList.PlayListItem playListItem = playlist[i];
							PlayListPlayer.GetPlaylist(PlayListPlayer.PlayListType.PLAYLIST_MUSIC).Add(playListItem);
						}
						if(PlayListPlayer.GetPlaylist(PlayListPlayer.PlayListType.PLAYLIST_MUSIC).Count>0)
						{
							PlayListPlayer.CurrentPlaylist = PlayListPlayer.PlayListType.PLAYLIST_MUSIC;
							PlayListPlayer.Reset();
							if(_Sound.Length == 0)
							{
								PlayListPlayer.Play(0);
								g_Player.Volume=99;
							}
							else
							{
								PlayListPlayer.Play(_Sound);
							}
						
						}
					}
					else
					{
						ShowErrorDialog();
					}
					break;
				case PlayType.Radio:
					ArrayList stations = new ArrayList();
					RadioDatabase.GetStations(ref stations);
					foreach (RadioStation station in stations)
					{
						if(station.Name == _Sound)
						{
							g_Player.Play(station.URL);
							g_Player.Volume=99;
						}
						break;
					}
					break;
				case PlayType.Alarm:
					try
					{
						g_Player.Play(GUIAlarm.AlarmSoundPath + "\\" +  _Sound);
						g_Player.Volume=99;
					}
					catch
					{
						ShowErrorDialog();
					}
				
					break;
			}

		}
		/// <summary>
		/// Shows the Error Dialog
		/// </summary>
		private void ShowErrorDialog()
		{
			GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
			if(dlgOK !=null)
			{
				dlgOK.SetHeading(6);
				dlgOK.SetLine(0,"");
				dlgOK.SetLine(1,477);
				dlgOK.SetLine(2,"");
				dlgOK.DoModal(GUIAlarm.WINDOW_ALARM);
			}
			return;
		}
		#endregion

		public void Dispose()
		{
			_AlarmTimer.Enabled=false;
			_AlarmTimer.Dispose();
		} 

		#region Static Methods
			/// <summary>
			/// Loads all of the alarms from the profile xml
			/// </summary>
			/// <returns>ArrayList of Alarm Objects</returns>
			public static ArrayList LoadAll()
			{
				ArrayList Alarms = new ArrayList();

				using(AMS.Profile.Xml xmlreader = new AMS.Profile.Xml("MediaPortal.xml"))
				{
					for (int i=0; i < 20; i++)
					{
						string NameTag=String.Format("alarmName{0}",i);
						string PlayTypeTag=String.Format("alarmPlayType{0}",i);
						string TimeTag=String.Format("alarmTime{0}",i);
						string EnabledTag=String.Format("alarmEnabled{0}",i);
						string MonTag =  String.Format("alarmMon{0}",i);
						string TueTag =  String.Format("alarmTue{0}",i);
						string WedTag =  String.Format("alarmWed{0}",i);
						string ThuTag =  String.Format("alarmThu{0}",i);
						string FriTag =  String.Format("alarmFri{0}",i);
						string SatTag =  String.Format("alarmSat{0}",i);
						string SunTag =  String.Format("alarmSun{0}",i);
						string SoundTag =  String.Format("alarmSound{0}",i);
						string VolumeFadeTag = String.Format("alarmVolumeFade{0}",i);

						string AlarmName=xmlreader.GetValueAsString("alarm",NameTag,"");

						if (AlarmName.Length>0)
						{
							bool AlarmEnabled=xmlreader.GetValueAsBool("alarm",EnabledTag,false);
							int AlarmPlayType =xmlreader.GetValueAsInt("alarm",PlayTypeTag,1);
							DateTime AlarmTime =DateTime.Parse(xmlreader.GetValueAsString("alarm",TimeTag,string.Empty));
							bool AlarmMon = xmlreader.GetValueAsBool("alarm",MonTag,false);
							bool AlarmTue = xmlreader.GetValueAsBool("alarm",TueTag,false);
							bool AlarmWed = xmlreader.GetValueAsBool("alarm",WedTag,false);
							bool AlarmThu = xmlreader.GetValueAsBool("alarm",ThuTag,false);
							bool AlarmFri = xmlreader.GetValueAsBool("alarm",FriTag,false);
							bool AlarmSat = xmlreader.GetValueAsBool("alarm",SatTag,false);
							bool AlarmSun = xmlreader.GetValueAsBool("alarm",SunTag,false);
							string AlarmSound = xmlreader.GetValueAsString("alarm",SoundTag,string.Empty);
							bool AlarmVolumeFade = xmlreader.GetValueAsBool("alarm",VolumeFadeTag,false);

								
							Alarm objAlarm = new Alarm(i,AlarmName,AlarmPlayType,AlarmEnabled,AlarmTime,
								Alarm.AlarmType.Alarm,AlarmMon,AlarmTue,AlarmWed,AlarmThu,
								AlarmFri,AlarmSat,AlarmSun,AlarmSound,AlarmVolumeFade);

							Alarms.Add(objAlarm);
						}
					}	
				}
				return Alarms;

			}
			public static bool SaveAlarm(Alarm alarmToSave)
			{
				int id = alarmToSave.Id;
				try
				{
					using(AMS.Profile.Xml xmlwriter = new AMS.Profile.Xml("MediaPortal.xml"))
					{
						
						xmlwriter.SetValue("alarm","alarmName"+id,alarmToSave.Name);
						xmlwriter.SetValue("alarm","alarmPlayType"+id,(int)alarmToSave.AlarmPlayType);
						xmlwriter.SetValueAsBool("alarm","alarmEnabled"+id,alarmToSave.Enabled);
						xmlwriter.SetValue("alarm","alarmTime"+id,alarmToSave.Time);
						xmlwriter.SetValueAsBool("alarm","alarmMon"+id,alarmToSave.Mon);   
						xmlwriter.SetValueAsBool("alarm","alarmTue"+id,alarmToSave.Tue);   
						xmlwriter.SetValueAsBool("alarm","alarmWed"+id,alarmToSave.Wed);   
						xmlwriter.SetValueAsBool("alarm","alarmThu"+id,alarmToSave.Thu);   
						xmlwriter.SetValueAsBool("alarm","alarmFri"+id,alarmToSave.Fri);   
						xmlwriter.SetValueAsBool("alarm","alarmSat"+id,alarmToSave.Sat); 
						xmlwriter.SetValueAsBool("alarm","alarmSun"+id,alarmToSave.Sun); 
						xmlwriter.SetValue("alarm","alarmSound"+id,alarmToSave.Sound);
						xmlwriter.SetValueAsBool("alarm","alarmVolumeFade"+id,alarmToSave.VolumeFade); 
					}
					return true;
				}
				catch
				{
					return false;
				}
			
			}
			public static bool DeleteAlarm(int id)
			{
				using(AMS.Profile.Xml xmlwriter = new AMS.Profile.Xml("MediaPortal.xml"))
				{
					xmlwriter.RemoveEntry("alarm","alarmName"+id);
					xmlwriter.RemoveEntry("alarm","alarmEnabled"+id);
					xmlwriter.RemoveEntry("alarm","alarmTime"+id);
					xmlwriter.RemoveEntry("alarm","alarmMon"+id);   
					xmlwriter.RemoveEntry("alarm","alarmTue"+id);   
					xmlwriter.RemoveEntry("alarm","alarmWed"+id);   
					xmlwriter.RemoveEntry("alarm","alarmThu"+id);   
					xmlwriter.RemoveEntry("alarm","alarmFri"+id);   
					xmlwriter.RemoveEntry("alarm","alarmSat"+id); 
					xmlwriter.RemoveEntry("alarm","alarmSun"+id); 
					xmlwriter.RemoveEntry("alarm","alarmSound"+id);
					xmlwriter.RemoveEntry("alarm","alarmPlayType"+id);
					xmlwriter.RemoveEntry("alarm","alarmVolumeFade"+id);
				}
				return true;
			} 
			public static int GetNextId()
			{
				string tempText;
				for (int i=0; i<20; i++)
				{
					using(AMS.Profile.Xml   xmlreader=new AMS.Profile.Xml("MediaPortal.xml"))
					{
						tempText = xmlreader.GetValueAsString("alarm","alarmName"+i,"");
						if (tempText == "")
						{
							return i;
						}
					}	
				}
				return -1;
			}
		#endregion

	}
}
