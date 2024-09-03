using PCSC;
using PCSC.Iso7816;
using System.Diagnostics;

namespace FrApp42.ACR122U
{
    internal class Card
    {
        private const byte CUSTOM_CLA = 0xFF;
        private IsoReader _isoReader;

        public Card(ISCardContext cardContext, string device)
        {
            try
            {
                this._isoReader = new IsoReader(cardContext, device, SCardShareMode.Shared, SCardProtocol.Any, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static bool IsSuccess(Response response) =>
            (response.SW1 == (byte)SW1Code.Normal) &&
            (response.SW2 == 0x00);

        #region Key & Auth

        public bool LoadKey(KeyStructure keyStructure, byte keyNumber, byte[] key)
        {
            var loadKeyCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.ExternalAuthenticate,
                P1 = (byte)keyStructure,
                P2 = keyNumber,
                Data = key
            };

            bool isSuccess = false;

            try
            {
                if (_isoReader != null)
                {
                    Debug.WriteLine($"Load Authentication Keys: {BitConverter.ToString(loadKeyCmd.ToArray())}");
                    Response response = _isoReader.Transmit(loadKeyCmd);
                    Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2}");
                    isSuccess = IsSuccess(response);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return isSuccess;
        }

        public bool Authenticate(byte block = 0x08, KeyType keyType = KeyType.KeyA, byte keyNumber = 0x00)
        {
            var authBlock = new GeneralAuthenticate
            {
                Msb = 0x00,
                Lsb = block,
                KeyNumber = keyNumber,
                KeyType = keyType
            };

            var authKeyCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.InternalAuthenticate,
                P1 = 0x00,
                P2 = 0x00,
                Data = authBlock.ToArray()
            };

            bool result = false;
            try
            {
                if (_isoReader != null)
                {
                    Debug.WriteLine($"General Authenticate: {BitConverter.ToString(authKeyCmd.ToArray())}");
                    Response response = _isoReader.Transmit(authKeyCmd);
                    Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2}");
                    result = (response.SW1 == 0x90) && (response.SW2 == 0x00);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }

        #endregion

        #region Binary

        public byte[] ReadBinary(byte P1, byte P2, int Le)
        {
            unchecked
            {
                var readBinaryCmd = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any)
                {
                    CLA = CUSTOM_CLA,
                    Instruction = InstructionCode.ReadBinary,
                    P1 = P1,
                    P2 = P2,
                    Le = Le
                };


                byte[] datas = new byte[0];
                try
                {
                    if (_isoReader != null)
                    {
                        Debug.WriteLine($"Read Binary: {BitConverter.ToString(readBinaryCmd.ToArray())}");
                        var response = _isoReader.Transmit(readBinaryCmd);
                        var data = response.GetData();
                        if (data != null)
                        {
                            Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2} Data: {BitConverter.ToString(data)}");
                        }
                        datas = IsSuccess(response) ? response.GetData() : new byte[0];
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                return datas;
            }
        }

        public bool UpdateBinary(byte msb, byte lsb, byte[] data)
        {
            var updateBinaryCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.UpdateBinary,
                P1 = msb,
                P2 = lsb,
                Data = data
            };

            Debug.WriteLine($"Update Binary: {BitConverter.ToString(updateBinaryCmd.ToArray())}");
            var response = _isoReader.Transmit(updateBinaryCmd);
            Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2}");

            return IsSuccess(response);
        }

        #endregion

        #region Block
        public byte[] GetData()
        {
            var getDataCmd = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.GetData,
                P1 = 0x00,
                P2 = 0x00
            };

            var response = _isoReader.Transmit(getDataCmd);
            return IsSuccess(response)
                    ? response.GetData() ?? new byte[0]
                    : null;
        }
        #endregion

        #region Inc & Dec
        public bool Decrement(byte msb, byte lsb, byte[] data)
        {
            var decrementCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.Decrement,
                P1 = 0x00,
                P2 = lsb,
                Data = data,
            };


            Debug.WriteLine($"Decrement Binary: {BitConverter.ToString(decrementCmd.ToArray())}");
            var response = _isoReader.Transmit(decrementCmd);
            Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2}");

            return IsSuccess(response);
        }


        public bool Increment(byte msb, byte lsb, byte[] data)
        {
            var incrementCmd = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any)
            {
                CLA = CUSTOM_CLA,
                Instruction = InstructionCode.Increment,
                P1 = 0x00,
                P2 = lsb,
                Data = data
            };


            Debug.WriteLine($"Increment Binary: {BitConverter.ToString(incrementCmd.ToArray())}");
            var response = _isoReader.Transmit(incrementCmd);
            Debug.WriteLine($"SW1 SW2 = {response.SW1:X2} {response.SW2:X2}");

            return IsSuccess(response);
        }

        #endregion
    }
}
