using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

public class TrueTime : MonoBehaviourAssistant<TrueTime> {

    static List<ScheduledTask> tasks = new List<ScheduledTask>();
    static List<ScheduledTask> unscheduledTasks = new List<ScheduledTask>();
    static TimeSpan timeZone;
    static DateTime? now = null;

    public static Action<DateTime> onGetTime = delegate {};


    public static bool IsKnown {
        get {
            return now.HasValue;
        }
    }

    public static TimeSpan Zone {
        get {
            return timeZone;
        }
    }

    public static DateTime Now {
        get {
            if (IsKnown) return now.Value;
            throw new Exception("Current time is unknow. NTP servers are not avaliable.");
        }
    }

    public static DateTime NowLocal {
        get {
            if (IsKnown)
                return now.Value + timeZone;
            throw new Exception("Current time is unknow. NTP servers are not avaliable.");
        }
    }

    void Awake () {
        StartCoroutine(Rountine());		
	}

    IEnumerator Rountine() {
        timeZone = DateTime.Now - DateTime.UtcNow;
        while (true) {
            var task = GetTime();
            while (!task.IsCompleted)
                yield return 0;

            if (!task.IsFaulted && !task.IsCanceled) {
                now = task.Result;
                break;
            }

            yield return new WaitForSeconds(20);
        }

        #if UNITY_EDITOR
        TimeSpan timeOffset = new TimeSpan(ProfileAssistant.main.debugTimeOffset);
        now += timeOffset;
        #endif

        onGetTime.Invoke(Now);
        ItemCounter.RefreshAll();

        while (true) {
            now = now.Value.AddSeconds(Time.unscaledDeltaTime);
            if (unscheduledTasks.Count > 0) {
                unscheduledTasks.ForEach(task => tasks.Remove(task));
                unscheduledTasks.Clear();
            }
            if (tasks.Count > 0)
                tasks.ForEach(x => x.Update(Now));
            yield return 0;
        }
    }

    Task<DateTime> GetTime() {
        return Task.Run<DateTime>(() => {
            foreach (string server in ntpServers) {
                DateTime? time = GetNetworkTime(server);
                if (time.HasValue)
                    return time.Value;
            }
            throw new Exception("All NTP servers are not avaliable");
        });
    }

    static readonly string[] ntpServers = {
        "pool.ntp.org",
        "time.windows.com"
    };

    public static DateTime? GetNetworkTime(string server) {
        try {
            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(server).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long) milliseconds);

            return networkDateTime;
        } catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }

    static uint SwapEndianness(ulong x) {
        return (uint) (((x & 0x000000ff) << 24) +
                       ((x & 0x0000ff00) << 8) +
                       ((x & 0x00ff0000) >> 8) +
                       ((x & 0xff000000) >> 24));
    }

    public static void Schedule(ScheduledTask task) {
        if (!Application.isPlaying) return;
        if (IsKnown) {
            if (task.uniqueID != null) tasks.RemoveAll(x => x.uniqueID == task.uniqueID);
            tasks.Add(task);
            task.Initialize(Now);
        } else
            onGetTime += x => Schedule(task);
    }

    public static void Unschedule(ScheduledTask task) {
        if (!Application.isPlaying) return;
        unscheduledTasks.Add(task);
    }

    void Update() {
        if (Application.isEditor || Debug.isDebugBuild) {
            DebugPanel.Log("True Time", "System", IsKnown ? Now.ToString() : "Is Unknown");
            DebugPanel.Log("True Time Local", "System", IsKnown ? NowLocal.ToString() : "Is Unknown");
            DebugPanel.Log("Time Zone", "System", timeZone);
        }
    }
}

public abstract class ScheduledTask {
    public string uniqueID;

    protected NextCall nextCall;

    public void Initialize(DateTime dateTime) {
        nextCall = OnStart(dateTime);
    }

    public abstract NextCall OnStart(DateTime time);

    public void Update(DateTime currentTime) {
        if (nextCall == null) {
            TrueTime.Unschedule(this);
            return;
        }
        if (nextCall.time <= currentTime) {
            try {
                nextCall.action.Invoke();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }

    public virtual void Serialize(XElement xml) {}

    public virtual void Deserialize(XElement xml) {}

    public virtual Dictionary<string, object> Serialize() {
        return new Dictionary<string, object>();
    }

    public virtual void Deserialize(Dictionary<string, object> json) {}

    public void SetNextCall(Action action, DateTime time) {
        nextCall = new NextCall(action, time);
    }

    public void SetNextCall(Action action, TimeSpan span) {
        nextCall = new NextCall(action, span);
    }

    public void SetNextCall(Action action, float spanSeconds) {
        nextCall = new NextCall(action, spanSeconds);
    }

    public class NextCall {
        public NextCall(Action action, DateTime time) {
            this.action = action;
            this.time = time;
        }

        public NextCall(Action action) : this(action, new DateTime()) {
            action();
        }

        public NextCall(Action action, TimeSpan span) : this(action, TrueTime.Now + span) {}
        public NextCall(Action action, float spanSeconds) : this(action, new TimeSpan(0, 0, 0, 0, Mathf.RoundToInt(spanSeconds * 1000))) {}

        public DateTime time;
        public Action action;
    }
}