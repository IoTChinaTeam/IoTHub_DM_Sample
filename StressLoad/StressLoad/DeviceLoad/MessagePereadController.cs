using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLoad
{
    public class MessagePereadController
    {
        private IBlobDataReader _blobReader;
        private double _secondsOffset;
        CancellationTokenSource _cancellationTokenSource;
        Dictionary<string, ConcurrentQueue<MessageData>> _messagePool;
        public MessagePereadController(IBlobDataReader blobReader)
        {
            _blobReader = blobReader;
            _messagePool = new Dictionary<string, ConcurrentQueue<MessageData>>();
        }
        private IEnumerable<string> GetAfterString(string deviceId, ref long startOffset, uint count)
        {
            return _blobReader.ReadBlobLines(deviceId, ref startOffset, count);
        }

        private async void TransportDataElaspe(string deviceId, CancellationToken cancellationToken)
        {
            var messsageList = _messagePool[deviceId];
            long startOffset = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime orgUtc = DateTime.Now.AddSeconds(-_secondsOffset);
                if (messsageList.Count < 500)
                {
                    var messages = GetAfterString(deviceId, ref startOffset, Convert.ToUInt32(1000 - messsageList.Count));
                    foreach (var message in messages)
                    {
                        var data = new MessageData();
                        if (data.Convert(message))
                            _messagePool[deviceId].Enqueue(data);
                    }
                }
                await Task.Delay(1000);
            }
        }

        public IEnumerable<string> GetMessages(string deviceId, DateTime deadLine)
        {
            try
            {
                if (!_messagePool.ContainsKey(deviceId))
                    return null;
                List<string> result = new List<string>();
                MessageData item;
                lock (_messagePool[deviceId])
                {
                    while (_messagePool[deviceId].TryPeek(out item))
                    {
                        if (item.Utc <= deadLine.AddSeconds(-_secondsOffset))
                        {
                            _messagePool[deviceId].TryDequeue(out item);
                            foreach (var data in item.MessageContent)
                            {
                                long orgUtc = 0;
                                try
                                {
                                    data["s"] = long.TryParse(data["s"].ToString(), out orgUtc) ? (orgUtc + Convert.ToInt64((_secondsOffset * 10000000))).ToString() : deadLine.ToFileTimeUtc().ToString();
                                }
                                catch { }
                            }
                            Message message = new Message(Encoding.ASCII.GetBytes(item.MessageContent.ToString()));
                            result.Add(item.MessageContent.ToString());
                        }
                        else
                            break;
                    }
                }
                return result;
            }
            catch
            {

            }
            return null;
        }

        public async Task StartTransportAsync()
        {
            try
            {
                var devices = await Task.Run(() => _blobReader.GetDevices());
                _secondsOffset = _blobReader.GetSecondOffset(DateTime.Now);
                _cancellationTokenSource = new CancellationTokenSource();
                foreach (var deviceId in devices)
                {
                    _messagePool.Add(deviceId, new ConcurrentQueue<MessageData>());
                    TransportDataElaspe(deviceId, _cancellationTokenSource.Token);
                    Console.WriteLine("device-" + deviceId + ": start read thread");
                }
            }
            catch
            {

            }
        }

        public IEnumerable<string> Devices
        {
            get
            {
                if (_messagePool != null)
                    return _messagePool.Keys;
                return null;
            }
        }

        public void StopTransport()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _blobReader.CloseStreamReaders();
            }
            catch
            {
            }
        }
    }

    public class MessageData
    {
        public string deviceId { get; set; }

        public DateTime Utc { get; set; }

        public JToken MessageContent { get; set; }

        public bool Convert(string srcString)
        {
            try
            {

                JToken token = JToken.Parse(srcString);
                deviceId = token["deviceId"].ToString();
                Utc = DateTime.FromFileTimeUtc(long.Parse(token["utc"].ToString()));
                MessageContent = JToken.Parse(token["data"].ToString());
                return true;
            }
            catch (Exception)
            {

            }
            return false;
        }
    }
}
