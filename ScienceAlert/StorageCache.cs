using System;
using System.Collections;
using System.Collections.Generic;
using ReeperCommon;
using System.Linq;
using UnityEngine;

namespace ScienceAlert
{
	public class StorageCache : MonoBehaviour
	{
		protected List<IScienceDataContainer> storage = new List<IScienceDataContainer>();
		protected MagicDataTransmitter magicTransmitter;
		protected Vessel vessel;

		public int StorageContainerCount => storage.Count;

	    public bool IsBusy
		{
			get;
			private set;
		}

		public void Start()
		{
			GameEvents.onVesselChange.Add(OnVesselChange);
			GameEvents.onVesselWasModified.Add(OnVesselModified);
			GameEvents.onVesselDestroy.Add(OnVesselDestroyed);
			vessel = FlightGlobals.ActiveVessel;
			ScheduleRebuild();
		}

		public void OnDestroy()
		{
			GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);
			GameEvents.onVesselWasModified.Remove(OnVesselModified);
			GameEvents.onVesselChange.Remove(OnVesselChange);
			RemoveMagicTransmitter(false);
			Log.Debug("ALERT:StorageCache destroyed");
		}

		public void OnVesselChange(Vessel v)
		{
			RemoveMagicTransmitter();
			vessel = v;
			ScheduleRebuild();
		}

		public void OnVesselModified(Vessel v)
		{
			if (vessel != v)
			{
				OnVesselChange(v);
				return;
			}
			ScheduleRebuild();
		}

		public void OnVesselDestroyed(Vessel v)
		{
		    if (vessel != v) return;
		    storage = new List<IScienceDataContainer>();
		    magicTransmitter = null;
		    vessel = null;
		}

		public void ScheduleRebuild()
		{
			if (IsBusy)
			{
				try
				{
					StopCoroutine("Rebuild");
				}
			    catch (Exception)
			    {
			        // ignored
			    }
			}
			StartCoroutine("Rebuild");
		}

		private IEnumerator Rebuild()
		{
			IsBusy = true;
			storage.Clear();
			List<KeyValuePair<List<ScienceData>, Callback>> queuedData = magicTransmitter != null ? magicTransmitter.GetQueuedData() : new List<KeyValuePair<List<ScienceData>, Callback>>();
			magicTransmitter = null;
			yield return new WaitForFixedUpdate();
			if (FlightGlobals.ActiveVessel != vessel)
			{
				RemoveMagicTransmitter();
			}
			while (FlightGlobals.ActiveVessel != null && !vessel.loaded || !FlightGlobals.ready)
			{
				yield return new WaitForFixedUpdate();
			}
			if (FlightGlobals.ActiveVessel == null)
			{
				IsBusy = false;
			}
			else
			{
				vessel = FlightGlobals.ActiveVessel;
				storage = vessel.FindPartModulesImplementing<IScienceDataContainer>();
				List<IScienceDataTransmitter> source = (from tx in vessel.FindPartModulesImplementing<IScienceDataTransmitter>()
				where !(tx is MagicDataTransmitter)
				select tx).ToList();
				if (source.Any())
				{
					magicTransmitter = vessel.rootPart.gameObject.GetComponent<MagicDataTransmitter>();
					if (magicTransmitter != null)
					{
						magicTransmitter.RefreshTransmitterQueues(queuedData);
					}
					else
					{
					    magicTransmitter = vessel.rootPart.AddModule("MagicDataTransmitter") as MagicDataTransmitter;
					    if (magicTransmitter != null)
					        magicTransmitter.cacheOwner = this;
					}
				}
				else
				{
					RemoveMagicTransmitter(false);
					Log.Debug("ALERT:Vessel {0} has no transmitters; no magic transmitter added", vessel.name);
				}
				IsBusy = false;
				Log.Debug("ALERT:Rebuilt StorageCache");
			}
		}

		private void RemoveMagicTransmitter(bool rootOnly = true)
		{
			magicTransmitter = null;
		    if (vessel == null || vessel.rootPart == null) return;
		    try
		    {
		        if (vessel.rootPart.Modules.Contains("MagicDataTransmitter"))
		        {
		            vessel.rootPart.RemoveModule(vessel.rootPart.Modules.OfType<MagicDataTransmitter>().Single());
		        }
		        if (rootOnly) return;
		        foreach (Part current in vessel.Parts)
		        {
		            if (current.Modules.Contains("MagicDataTransmitter"))
		            {
		                current.RemoveModule(current.Modules.OfType<MagicDataTransmitter>().First());
		            }
		        }
		    }
		    catch (Exception ex)
		    {
		        Log.Warning("RemoveMagicTransmitter: caught exception {0}", ex);
		    }
		}

		public List<ScienceData> FindStoredData(string subjectid)
		{
			List<ScienceData> list = new List<ScienceData>();
			foreach (IScienceDataContainer current in storage)
			{
			    if (current.GetScienceCount() <= 0) continue;
			    ScienceData[] data = current.GetData();
			    for (int i = 0; i < data.Length; i++)
			    {
			        ScienceData scienceData = data[i];
			        if (scienceData.subjectID == subjectid)
			        {
			            list.Add(scienceData);
			        }
			    }
			}
		    if (magicTransmitter == null) return list;
		    foreach (ScienceData current2 in magicTransmitter.QueuedData)
		    {
		        if (current2.subjectID != subjectid) continue;
		        list.Add(current2);
		        Log.Debug("ALERT:Found stored data in transmitter queue");
		    }
		    return list;
		}
	}
}
