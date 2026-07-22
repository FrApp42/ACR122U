using PCSC;
using PCSC.Iso7816;
using PCSC.Monitoring;
using System.Diagnostics;

namespace FrApp42.ACR122U
{
    public class Reader
    {
        private string _deviceName;

        private IDeviceMonitorFactory _deviceFactory;
        private IDeviceMonitor _deviceMonitor;

        private IMonitorFactory _cardFactory;
        private ISCardMonitor _cardMonitor;

        private IContextFactory _contextFactory;
        private ISCardContext _cardContext;
        private ICardReader _cardReader;

        public event ReaderConnectedHandler Connected;
        public delegate void ReaderConnectedHandler(string name);
        public event ReaderDisconnectHandler Disconnected;
        public delegate void ReaderDisconnectHandler(string name);
        public event ReaderExceptionHandler Exeception;
        public delegate void ReaderExceptionHandler(Exception exception);

        public event CardInsertedHandler Inserted;
        public delegate void CardInsertedHandler(string uid);
        public event CardRemovedHandler Removed;
        public delegate void CardRemovedHandler();

        public Reader()
        {
            _deviceFactory = DeviceMonitorFactory.Instance;
            _deviceMonitor = _deviceFactory.Create(SCardScope.System);

            _deviceMonitor.Start();

            _deviceMonitor.Initialized += OnMonitorInitialized;
            _deviceMonitor.StatusChanged += OnMonitorStatusChanged;
            _deviceMonitor.MonitorException += OnMonitorException;

        }

        #region Device Monitor
        private void OnMonitorInitialized(object sender, DeviceChangeEventArgs e)
        {
            foreach (var name in e.AllReaders)
            {
                this.OnMonitorConnected(name);
                break;
            }
        }

        private void OnMonitorStatusChanged(object sender, DeviceChangeEventArgs e)
        {
            foreach (var removed in e.DetachedReaders)
            {
                this.OnMonitorDisconnected(removed);
                break;
            }

            foreach (var added in e.AttachedReaders)
            {
                this.OnMonitorConnected(added);
                break;
            }
        }

        private void OnMonitorConnected(string added)
        {
            Debug.WriteLine($"New reader attached: {added}");
            _deviceName = added;

            _cardFactory = MonitorFactory.Instance;
            _cardMonitor = _cardFactory.Create(SCardScope.System);
            _cardMonitor.CardInserted += OnCardInserted;
            _cardMonitor.CardRemoved += OnCardRemoved;

            _contextFactory = ContextFactory.Instance;
            _cardContext = _contextFactory.Establish(SCardScope.System);

            _cardMonitor.Start(added);
            this.Connected?.Invoke(added);
        }

        private void OnMonitorDisconnected(string removed)
        {
            Debug.WriteLine($"Reader detached: {removed}");
            if (_cardMonitor.Monitoring)
            {
                _cardContext.Cancel();
                _cardContext?.Dispose();

                _cardMonitor.Cancel();
                _cardMonitor?.Dispose();
            }

            _deviceName = string.Empty;
            this.Disconnected?.Invoke(removed);
        }

        private void OnMonitorException(object sender, DeviceMonitorExceptionEventArgs e)
        {
            this.Exeception?.Invoke(e.Exception);
        }

        public string GetDevice()
        {
            return _deviceName;
        }

        #endregion

        #region Card Monitor

        private void OnCardInserted(object sender, CardStatusEventArgs args)
        {
            try
            {
                _cardReader = _cardContext.ConnectReader(_deviceName, SCardShareMode.Shared, SCardProtocol.Any);
                string uid = this.GetUID();
                _cardReader.Disconnect(SCardReaderDisposition.Leave);
                this.Inserted?.Invoke(uid);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }


        }

        private void OnCardRemoved(object sender, CardStatusEventArgs args)
        {
            this.Removed?.Invoke();
        }

        #endregion

        #region Card Reader
        private byte[] GetUIDByte()
        {
            try
            {
                byte[] uid = new byte[10];

                _cardReader.Transmit(new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 }, uid);

                Array.Resize(ref uid, 4);

                return uid;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return new byte[0];
            }

        }

        public string GetUID()
        {
            return BitConverter.ToString(this.GetUIDByte());
        }
        #endregion

        private static bool IsSuccess(Response responses) => (responses.SW1 == (byte)SW1Code.Normal) && (responses.SW2 == 0x00);

        [Obsolete("readBinary is deprecated, please use ReadBinary instead.")]
        public byte[] readBinary(Int32 block, Int32 lenght, byte[]? key = null)
        {
            return ReadBinary(block, lenght, key);
        }

        public byte[] ReadBinary(Int32 block, Int32 lenght, byte[]? key = null)
        {
            return ReadBinary(KeyLocation.Slot0, block, lenght, key);
        }

        public byte[] ReadBinary(KeyLocation slot, Int32 block, Int32 lenght, byte[]? key = null)
        {
            if (key == null)
            {
                key = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            }
            Card card = new Card(_cardContext, _deviceName);
            byte[] result = new byte[lenght];

            KeyType keyType = slot == KeyLocation.Slot1 ? KeyType.KeyB : KeyType.KeyA;

            try
            {
                //_cardReader = _cardContext.ConnectReader(_deviceName, SCardShareMode.Shared, SCardProtocol.Any);
                if (card.LoadKey(KeyStructure.VolatileMemory, (byte)slot, key) && card.Authenticate((byte)block, keyType, (byte)slot))
                {
                    result = card.ReadBinary(0x00, (byte)block, (byte)lenght);
                }

                _cardReader.Disconnect(SCardReaderDisposition.Leave);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }


        public bool WriteBinary(int block, byte[] data, byte[]? key = null)
        {
            return WriteBinary(KeyLocation.Slot0, block, data, key);
        }

        public bool WriteBinary(KeyLocation slot, int block, byte[] data, byte[]? key = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Si la taille est inférieure à 16, on complète avec des 0x00
            if (data.Length < 16)
            {
                var padded = new byte[16];
                Array.Copy(data, padded, data.Length);
                data = padded;
            }
            else if (data.Length > 16)
            {
                throw new ArgumentException("The data to be written must contain a maximum of 16 bytes.");
            }

            if (key == null)
            {
                key = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            }


            bool result = false;
            KeyType keyType = slot == KeyLocation.Slot1 ? KeyType.KeyB : KeyType.KeyA;

            try
            {
                Card card = new Card(_cardContext, _deviceName);
                //byte sector = (byte)(block / 4);
                if (card.LoadKey(KeyStructure.VolatileMemory, (byte)slot, key) && card.Authenticate((byte)block, keyType, (byte)slot))
                {
                    // Écriture des données sur le badge (P1 = 0x00, P2 = block)
                    result = card.UpdateBinary(0x00, (byte)block, data);
                }

                _cardReader.Disconnect(SCardReaderDisposition.Leave);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }
    }
}
