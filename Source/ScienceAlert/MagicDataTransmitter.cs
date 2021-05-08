using ReeperCommon;
using System.Collections.Generic;
using System.Linq;

namespace ScienceAlert
{
    public class NonexistentTransmitterException : System.Exception
    {
    }

    public class MagicDataTransmitter : PartModule, IScienceDataTransmitter
    {
        private Dictionary<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>> realTransmitters = new Dictionary<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>>();
        private Dictionary<IScienceDataTransmitter, Queue<KeyValuePair<List<ScienceData>, Callback>>> toBeTransmitted = new Dictionary<IScienceDataTransmitter, Queue<KeyValuePair<List<ScienceData>, Callback>>>();
        internal StorageCache cacheOwner;

        float IScienceDataTransmitter.DataRate => 3.40282347E+38f;

        double IScienceDataTransmitter.DataResourceCost => 0.0;

        public List<ScienceData> QueuedData
        {
            get
            {
                List<ScienceData> list = new List<ScienceData>();
                bool flag = false;
                try
                {
                    foreach (KeyValuePair<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>> current in realTransmitters)
                    {
                        if (current.Key == null)
                        {
                            Log.Debug("[ScienceAlert]:MagicDataTransmitter: Encountered a bad transmitter value.");
                            flag = true;
                        }
                        else
                        {
                            if (!current.Key.IsBusy() && current.Value.Key != null)
                            {
                                current.Value.Key.Clear();
                            }
                            if (current.Value.Key != null)
                            {
                                list.AddRange(current.Value.Key);
                            }
                            list.AddRange(toBeTransmitted[current.Key].SelectMany(transmitterData => transmitterData.Key));
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    flag = true;
                    Log.Debug("[ScienceAlert]:Exception occurred in MagicDataTransmitter.QueuedData: {0}", ex);
                }
                if (flag)
                {
                    Log.Warning("Resetting MagicDataTransmitter due to bad transmitter key or value");
                    cacheOwner.ScheduleRebuild();
                }
                return list;
            }
        }

        public void Start()
        {
            Log.Debug("ALERT:MagicDataTransmitter started");
            RefreshTransmitterQueues(new List<KeyValuePair<List<ScienceData>, Callback>>());
        }

        public List<KeyValuePair<List<ScienceData>, Callback>> GetQueuedData()
        {
            return toBeTransmitted.Values.SelectMany(q => q.ToArray()).ToList();
        }

        public void RefreshTransmitterQueues(List<KeyValuePair<List<ScienceData>, Callback>> queuedData)
        {
            if (queuedData == null)
            {
                throw new System.ArgumentNullException("queuedData");
            }
            Dictionary<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>> dictionary = new Dictionary<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>>(realTransmitters);
            realTransmitters.Clear();
            toBeTransmitted.Clear();
            List<IScienceDataTransmitter> list = (from tx in FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataTransmitter>()
            where !(tx is MagicDataTransmitter)
            select tx).ToList();
            if (!list.Any())
            {
                Destroy(this);
                cacheOwner.ScheduleRebuild();
            }
            for (int i = list.Count - 1; i >=0; i--)
            {
                IScienceDataTransmitter current = list[i];

                realTransmitters.Add(current, default(KeyValuePair<List<ScienceData>, Callback>));
                toBeTransmitted.Add(current, new Queue<KeyValuePair<List<ScienceData>, Callback>>());
            }
            Log.Debug("ALERT:MagicDataTransmitter has found {0} useable transmitters", list.Count);
            foreach (IScienceDataTransmitter current2 in dictionary.Keys)
            {
                if (realTransmitters.ContainsKey(current2))
                {
                    realTransmitters[current2] = dictionary[current2];
                }
            }
            if (!queuedData.Any()) return;
            foreach (KeyValuePair<List<ScienceData>, Callback> current3 in queuedData)
            {
                TransmitData(current3.Key, current3.Value);
            }
        }

        private void BeginTransmissionWithRealTransmitter(IScienceDataTransmitter transmitter, List<ScienceData> science, Callback callback)
        {
            if (transmitter == null)
            {
                throw new System.ArgumentNullException("transmitter");
            }
            if (science == null)
            {
                throw new System.ArgumentNullException("science");
            }
            if ((PartModule)transmitter == null)
            {
                TransmitData(science, callback);
                throw new NonexistentTransmitterException();
            }
            Log.Debug(string.Concat("Beginning real transmission of ", science.Count, " science reports on transmitter ", ((PartModule)transmitter).part.flightID));
            if (callback != null) return;
            transmitter.TransmitData(science);
        }

        public void Update()
        {
            Dictionary<IScienceDataTransmitter, Queue<KeyValuePair<List<ScienceData>, Callback>>>.KeyCollection keys = toBeTransmitted.Keys;
            try
            {
                foreach (IScienceDataTransmitter current in keys)
                {
                    if (toBeTransmitted[current].Count > 0 && !current.IsBusy() && current.CanTransmit())
                    {
                        KeyValuePair<List<ScienceData>, Callback> value = toBeTransmitted[current].Dequeue();
                        Log.Debug("ALERT:Dispatching " + value.Key.Count + " science data entries to transmitter");
                        realTransmitters[current] = value;
                        BeginTransmissionWithRealTransmitter(current, value.Key, value.Value);
                    }
                }
            }
            catch (NonexistentTransmitterException)
            {
                Log.Warning("MagicDataTransmitter: Nonexistent transmitter encountered. Rescanning vessel and re-queuing transmissions");
                realTransmitters.Clear();
                RefreshTransmitterQueues(GetQueuedData());
                if (!realTransmitters.Any())
                {
                    Log.Warning("MagicDataTransmitter: No real transmitters found. Data will stay queued. If the vessel is switched or scenes are changed before it is dispatched, it will be lost.");
                }
            }
            catch (KeyNotFoundException)
            {
                Log.Debug("[ScienceAlert]:MagicDataTransmitter appears to be out of date. Any queued data might have been lost.");
                toBeTransmitted.Clear();
                realTransmitters.Clear();
                cacheOwner.ScheduleRebuild();
            }
        }

        public override void OnSave(ConfigNode node)
        {
            node.ClearData();
        }

        public override void OnLoad(ConfigNode node)
        {
        }

        private void QueueTransmission(List<ScienceData> data, IScienceDataTransmitter transmitter, Callback callback)
        {
            if (data.Count == 0)
            {
                return;
            }
            Log.Debug("ALERT:Queued " + data.Count + " science reports for transmission");
            toBeTransmitted[transmitter].Enqueue(new KeyValuePair<List<ScienceData>, Callback>(new List<ScienceData>(data), callback));
        }

        void IScienceDataTransmitter.TransmitData(List<ScienceData> data)
        {
            TransmitData(data, null);
        }

        public void TransmitData(List<ScienceData> dataQueue, Callback callback)
        {
            Log.Debug("ALERT:MagicTransmitter: received {0} ScienceData entries", dataQueue.Count);
            Log.Debug(callback == null ? "ALERT: with no callback" : "ALERT:With callback");
            List<IScienceDataTransmitter> list = new List<IScienceDataTransmitter>();
            foreach (KeyValuePair<IScienceDataTransmitter, KeyValuePair<List<ScienceData>, Callback>> current in realTransmitters)
            {
                list.Add(current.Key);
            }
            if (list.Any())
            {
                list = (from potential in list
                orderby ScienceUtil.GetTransmitterScore(potential)
                select potential).ToList();
                QueueTransmission(dataQueue, list.First(), callback);
                return;
            }
            Log.Debug("[ScienceAlert]:MagicDataTransmitter: Did not find any real transmitters");
        }

        bool IScienceDataTransmitter.IsBusy()
        {
            return false;
        }

        bool IScienceDataTransmitter.CanTransmit()
        {
            return realTransmitters.Any(pair => pair.Key.CanTransmit());
        }

        private void TransmissionComplete(IScienceDataTransmitter transmitter, Callback original)
        {
            Log.Debug("ALERT:Received TransmissionComplete callback from " + transmitter.GetType().Name);
            if (original != null)
            {
                original();
            }
        }

        public override string ToString()
        {
            return
                $"MagicDataTransmitter attached to {FlightGlobals.ActiveVessel.rootPart.name}; {QueuedData.Count} entries in queue";
        }
    }
}
