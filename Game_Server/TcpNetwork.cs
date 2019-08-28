//NOTE: SERVER DOESN'T CURRENTLY USE THIS CLASS!!! IT'S HERE MOSTLY FOR WHEN IMPLMENTING ENCRYPTION
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
//using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
//using vtortola.WebSockets;
//using vtortola.WebSockets.Deflate;
//using vtortola.WebSockets.Rfc6455;

namespace Game_Server
{
    //All of the TCP networking stuff
    internal class TcpNetwork
    {
        //Networking
        //private TcpClient _client;
		private WebSocket _client;
        //private ClientWebSocket _client;
		//private Socket _client;
        private int BLKSIZE = 4096;
        private byte[] _buffer;
        private ArraySegment<byte> _bufferSeg;
        private ArraySegment<byte> _bufferSegUnencrypted;


        //Encryption
        private byte[] _key = new byte[32];
        private byte[] _authKey = new byte[32];
        private byte[] _iv = new byte[32];
        private byte[] _hmac = new byte[32];

        //Logging
        private Logger logger;

        //Misc
        public bool MessageSent;
        public string Username;
        //private string _udpUniqueId;
        private int _playerId;
        private string _serverVersion;
        private static Encoding _encoder = Encoding.ASCII;

        //--------------------
        //  Public functions
        //--------------------

        //Constructor
        public TcpNetwork(/*Socket TcpClient */ WebSocket inClient, string username, /*string udpUniqueId,*/ int playerId)
        {
            //First initialize the variables
            _buffer = new byte[BLKSIZE];
            _client = inClient;
            //_client.ReceiveTimeout = 30000;
            //_client.SendTimeout = 30000;
            logger = Server.logger;
            Username = username;
            //_udpUniqueId = udpUniqueId;
            _playerId = playerId;
            _serverVersion = Server.ServerVersion;
            MessageSent = false;
        }

        //Initialize the connection, must be run before any messages are sent or received
        public void Initialize()
        {
            //Perform handshake to set up encrypted communications
            //Handshake();
			
        }

        //Receive a message and return it as a parsed list of list of strings
        public List<List<string>> ReceiveMessage()
        {
            byte[] receivedInput = null;// = ReceiveMessageBytes();
            if (receivedInput == null) return null;
            return ParseInput(receivedInput);
        }
        
        //Send single message with embedded status code. If message is longer than the block size, it will send it in chunks
        //Format: int32 size, int32 status, byte[16] auth, byte[16] iv, byte[size] data
        public void SendMessage(Int32 status, string message)
        {
            //First, encrypt the message
            byte[] messageBytes = _encoder.GetBytes(message);
            byte[] encryptedMessage = Encrypt(messageBytes);

            //Hash the encrypted text and IV			
            _hmac = SignMessage(encryptedMessage, encryptedMessage.Length, status);

            //Generate a header
            List<byte> header = new List<byte>();
            //Add message length (number of bytes after this and the status code)
            byte[] intBuf = BitConverter.GetBytes(encryptedMessage.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
            header.AddRange(intBuf);
            //Add status code
            Array.Clear(intBuf, 0, 4);
            intBuf = BitConverter.GetBytes(status);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
            header.AddRange(intBuf);
            //Add auth code
            header.AddRange(_hmac);
            //Add IV
            header.AddRange(_iv);

            //SendMessage(header.ToArray(), encryptedMessage);
            logger.Log("> send " + status + " " + Username + ": " + (encryptedMessage.Length + 40) + " bytes");

            MessageSent = true;
        }


        //Closes all resources
        public void Close()
        {
			//tcpclient?
            //_client.GetStream().Close();
            _client.Close();
			
			//socket
			
			
			//websockets
            //_client.CloseAsync(WebSocketCloseStatus.None,"Closed normally.", CancellationToken.None);
        }

        //--------------------
        // Private functions
        //--------------------

        //Receive input
        //Format: int32 size, byte[16] auth, byte[16] iv, byte[size] data
        /*
        private byte[] ReceiveMessageBytes()
        {
            Array.Clear(_buffer, 0, BLKSIZE);
            int size = 1;
            int startIndex = 0;
            int bytesRead = 0;
            int bytesToRead = 0;
            bool headerRead = false;
            int readIntoBuffer = 0;
            byte[] decrypted = null;

            using (MemoryStream byteBuffer = new MemoryStream())
            {
                //Don't crash upon receive error
                try
                {
                    //Task<WebSocketReceiveResult> received_info = _client.ReceiveAsync(_bufferSeg, CancellationToken.None);

                    //First four bytes is size, rest are encrypted bytes
                    while (((readIntoBuffer = _client.GetStream().Read(_buffer,0,BLKSIZE)) != 0) && bytesRead < size)
                    {
                        startIndex = 0;
                        //Array.Copy(_bufferSeg.Array, _buffer);
                        //_buffer = _bufferSeg.ToArray<byte>();
                        //Read header
                        if (!headerRead)
                        {
                            //Size comes first
                            byte[] intBuf = new byte[4];
                            Array.Copy(_buffer, 0, intBuf, 0, 4);
                            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                            size = BitConverter.ToInt32(intBuf, 0);
                            //Get auth
                            Array.Copy(_buffer, 4, _hmac, 0, 32);
                            //Get IV
                            Array.Copy(_buffer, 36, _iv, 0, 32);
                            startIndex = 68;
                            headerRead = true;
                            byteBuffer.Capacity = size;
                            //_logger.Debug("< recv++ " + Username + ": " + (size + 36) + " bytes");
                        }
                        //Calculate how many bytes are remaining
                        bytesToRead = (size - bytesRead + startIndex < BLKSIZE ? size - bytesRead : BLKSIZE - startIndex);
                        bytesToRead = (bytesToRead > (readIntoBuffer - startIndex) ? (readIntoBuffer - startIndex) : bytesToRead);
                        //_logger.Debug("Size: " + size + " bytesRead: " + bytesRead + " startIndex " + startIndex + " bytesToRead " + bytesToRead);
                        //byteBuffer.Write(_bufferSeg.ToArray<byte>(), startIndex, bytesToRead);
                        byteBuffer.Write(_buffer, startIndex, bytesToRead);
                        bytesRead += bytesToRead;
                        //_logger.Debug("Read " + bytesToRead + " of " + size + " bytes");
                        if (bytesRead >= size) break;
                        Array.Clear(_buffer, 0, BLKSIZE);
                        //received_info = _client.ReceiveAsync(_bufferSeg, CancellationToken.None);
                    }

                    //Now decrypt the bytes
                    byte[] encrypted = byteBuffer.ToArray();
                    decrypted = Decrypt(encrypted);

                    bool isValid = VerifyMessage(encrypted, size);

                    //If valid, return the decrypted data
                    if (isValid)
                    {
                        logger.Log("< recv - " + Username + ": " + (size + 36) + " bytes");
                        return decrypted;
                    }
                    elsef
                    {
                        logger.Log("< invalid message received from " + Username);
                        throw new Exception("Data failed verification");
                    }
                }
                catch (Exception)
                {
                    logger.Log("ERROR: " + Username + " timed out");
                }
                finally
                {
                    decrypted = null;
                }
            }
            return decrypted;
        }

        //*/


        /*
        //Used by Handshake() to receive plaintext
        private byte[] ReceiveMessageUnencrypted()
        {
            //Initialize variables
            Array.Clear(_buffer, 0, BLKSIZE);
            int size = 1;
            int startIndex = 0;
            int bytesRead = 0;
            int bytesToRead = 0;
            bool firstBlockRead = false;

            using (MemoryStream byteBuffer = new MemoryStream())
            {
                //Task<WebSocketReceiveResult> result = _client.ReceiveAsync(_bufferSegUnencrypted, CancellationToken.None);
                
				//Read size and status code, then read the remaining data into the byte buffer
                while ((_client.GetStream().Read(_buffer, 0, _buffer.Length) != 0) && bytesRead < size) //(result.Result.Count != 0 && bytesRead < size )
                {
                    startIndex = 0;
                    //_buffer = _bufferSegUnencrypted.ToArray<byte>();
                    //Read header
                    if (!firstBlockRead)
                    {
                        //Size comes first
                        byte[] intBuf = new byte[4];
                        Array.Copy(_buffer, 0, intBuf, 0, 4);
                        if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                        size = BitConverter.ToInt32(intBuf, 0);
                        //Update values
                        startIndex = 4;
                        firstBlockRead = true;
                        //Debug.Log("Receiving " + size + " bytes");
                    }
                    //Calculate how many bytes are remaining then read them
                    bytesToRead = (size - bytesRead + startIndex < BLKSIZE ? size - bytesRead : BLKSIZE - startIndex);
                    byteBuffer.Write(_buffer, startIndex, bytesToRead);
                    bytesRead += bytesToRead;
                    if (bytesRead >= size) break;
                    //result = _client.ReceiveAsync(_bufferSegUnencrypted, CancellationToken.None);
                }
                return byteBuffer.ToArray();
            }
        }
        //*/

        //Sends the actual byte arrays to the server
        /*
        private void SendMessage(byte[] header, byte[] data)
        {
            Array.Clear(_buffer, 0, BLKSIZE);
            int startIndex = 0;
            int s = 0;
            bool headerSent = false;
            int headerSize = header.Length;

            while (startIndex < data.Length)
            {
                s = 0;
                //Calculate number of bytes to send in this chunk
                int toSend = data.Length - startIndex;
                if (!headerSent && toSend > BLKSIZE - headerSize) toSend = BLKSIZE - headerSize;
                if (toSend > BLKSIZE) toSend = BLKSIZE;
                //If header hasn't been sent, buffer it
                if (!headerSent)
                {
                    Array.Copy(header, 0, _buffer, 0, headerSize);
                    Array.Copy(data, 0, _buffer, headerSize, toSend);
                    s = headerSize;
                    headerSent = true;
                    //logger.Log("Sending " + data.Length + " bytes");
                }
                //Else just buffer the next block
                else
                {
                    Array.Copy(data, startIndex, _buffer, 0, toSend);
                }
                //Write buffer to stream
                _client.GetStream().Write(_buffer, 0, toSend + s);
				//_client.SendAsync(new ArraySegment<byte>(_buffer, 0, toSend + s));


                //Increment counter and clear the buffer
                startIndex += toSend;
                Array.Clear(_buffer, 0, BLKSIZE);
            }
        }

        //*/

        //Run when the clients first connect. The server creates a new key and auth key, then the client sends a public key with which to encrypt these new keys
        private void Handshake()
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            {
                //Generate two new keys, Key and AuthKey
                rm.BlockSize = 256;
                _key = rm.Key;
                rm.GenerateKey();
                _authKey = rm.Key;

                //Receive the public key
                string clientPublicKey = "";// _encoder.GetString(ReceiveMessageUnencrypted());
                var csp = new RSACryptoServiceProvider(2048);
                csp.FromXmlString(clientPublicKey);

                //Encrypt and send a five part message wih the Key, AuthKey, UDP unique ID, player ID, and server version
                byte[] encryptedKey = csp.Encrypt(_key, false);
                byte[] encryptedAuthKey = csp.Encrypt(_authKey, false);
                //byte[] encryptedUniqueId = csp.Encrypt(_encoder.GetBytes(_udpUniqueId), false);
                byte[] playerIdBytes = BitConverter.GetBytes(_playerId);
                if (BitConverter.IsLittleEndian) Array.Reverse(playerIdBytes);
                byte[] encryptedPlayerId = csp.Encrypt(playerIdBytes, false);
                byte[] encryptedServerVersion = csp.Encrypt(_encoder.GetBytes(_serverVersion), false);

                List<byte> header = new List<byte>();
                byte[] intBuf = BitConverter.GetBytes(encryptedKey.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                header.AddRange(intBuf);
                intBuf = BitConverter.GetBytes(encryptedAuthKey.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                header.AddRange(intBuf);
                //intBuf = BitConverter.GetBytes(encryptedUniqueId.Length);
                //if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                //header.AddRange(intBuf);
                intBuf = BitConverter.GetBytes(encryptedPlayerId.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                header.AddRange(intBuf);
                intBuf = BitConverter.GetBytes(encryptedServerVersion.Length);
                if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
                header.AddRange(intBuf);

                List<byte> message = new List<byte>();
                message.AddRange(encryptedKey);
                message.AddRange(encryptedAuthKey);
                //message.AddRange(encryptedUniqueId);
                message.AddRange(encryptedPlayerId);
                message.AddRange(encryptedServerVersion);
                //SendMessage(header.ToArray(), message.ToArray());
            }
        }

        private byte[] Crypto(ICryptoTransform cryptoTransform, byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }

        private byte[] Decrypt(byte[] data)
        {
            using (var rm = new RijndaelManaged())
            {
                rm.BlockSize = 256;
                var decryptor = rm.CreateDecryptor(_key, _iv);
                return Crypto(decryptor, data);
            }
        }

        private byte[] Encrypt(byte[] data)
        {
            using (var rm = new RijndaelManaged())
            {
                rm.BlockSize = 256;
                rm.GenerateIV();
                _iv = rm.IV;
                var encryptor = rm.CreateEncryptor(_key, _iv);
                return Crypto(encryptor, data);
            }
        }

        //Signs a file with _authKey. Follows encrypt-then-MAC
        private byte[] SignMessage(byte[] data, Int32 size, Int32 status)
        {
            byte[] intBuf1 = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf1);
            byte[] intBuf2 = BitConverter.GetBytes(status);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf2);
            using (HMACSHA256 hmac = new HMACSHA256(_authKey))
            {
                byte[] buffer2 = new byte[intBuf1.Length + intBuf2.Length + _iv.Length + data.Length];
                Array.Copy(intBuf1, 0, buffer2, 0, intBuf1.Length);
                Array.Copy(intBuf2, 0, buffer2, intBuf1.Length, intBuf2.Length);
                Array.Copy(_iv, 0, buffer2, intBuf1.Length + intBuf2.Length, _iv.Length);
                Array.Copy(data, 0, buffer2, intBuf1.Length + intBuf2.Length + _iv.Length, data.Length);
                return hmac.ComputeHash(buffer2);
            }
        }

        //Verifies a file, returns true or false. "False" means the message should be ignored, because someone is trying to hack the network
        private bool VerifyMessage(byte[] data, Int32 size)
        {
            bool IsValid = true;
            byte[] intBuf = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian) Array.Reverse(intBuf);
            using (HMACSHA256 hmac = new HMACSHA256(_authKey))
            {
                byte[] buffer2 = new byte[intBuf.Length + _iv.Length + data.Length];
                Array.Copy(intBuf, 0, buffer2, 0, intBuf.Length);
                Array.Copy(_iv, 0, buffer2, intBuf.Length, _iv.Length);
                Array.Copy(data, 0, buffer2, intBuf.Length + _iv.Length, data.Length);
                byte[] computedHash = hmac.ComputeHash(buffer2);
                //Check all values even if an error is found. This prevents timing attacks
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (_hmac[i] != computedHash[i])
                    {
                        IsValid = false;
                    }
                }
                return IsValid;
            }
        }

        //Parse input. Input is in the following form:
        // (0x01)CMD(0x02)var1(0x02)var2(0x01)KEY(0x03)                 single command
        // (0x01)CMD(0x02)var1...(0x01)CMD(0x02)var1...(0x01)KEY(0x03)  multiple commands
        //The key will be ignored if making a login request, as the user needs to login to get the key
        private List<List<string>> ParseInput(byte[] input)
        {
            if (input == null) return null; //If an error was hit, don't bother parsing
            List<List<string>> commandList = new List<List<string>>();
            StringBuilder sb = new StringBuilder();

            //Very complicated but very fast single-pass parsing (probably isn't possible to be faster than this). Assume this is demonic magic
            try
            {
                //Parse this as a list of 1 or more commands
                int startIndex = 0;
                int endIndex = 0;

                if (input.Length == 0) return null;
                
                while (input[endIndex] != 3)
                {
                    //Create an object List to store the objects in this command
                    List<string> newCommand = new List<string>();

                    //Get indices for first 0x01 and next 0x01.
                    startIndex = Array.IndexOf(input, (byte)1, startIndex + 1);
                    endIndex = Array.IndexOf(input, (byte)1, endIndex + 1);
                    //If these point to the same place, move startIndex back to 0
                    if (startIndex == endIndex) startIndex = 0;
                    //If endIndex is not valid, set it to the first 0x03, aka the end of the command
                    if (endIndex < 0) endIndex = Array.IndexOf(input, (byte)3, startIndex + 1);
                    //_logger.Log("Command segment from " + startIndex + " to " + endIndex);
                    //Loop through all bytes in this segment in a similar fashion, except for each item add it to the new command list
                    int startIndexSub = 0;
                    int endIndexSub = 0;
                    while (endIndexSub < endIndex - startIndex)
                    {
                        //Clear StringBuilder
                        sb.Length = 0;
                        //Find first (byte)2, delimiter
                        startIndexSub = Array.IndexOf(input, (byte)2, startIndex + startIndexSub + 1) - startIndex;
                        endIndexSub = Array.IndexOf(input, (byte)2, startIndex + endIndexSub + 1) - startIndex;
                        //_logger.Log("Subsection from " + startIndexSub + " to " + endIndexSub);
                        //If these point to the same place, move startIndexSub back to 0
                        if (startIndexSub == endIndexSub) startIndexSub = 0;
                        //If endIndexSub is past endIndex or < 0, point to the end of this segment
                        if ((startIndex + endIndexSub > endIndex) || endIndexSub < 0)
                        {
                            endIndexSub = endIndex - startIndex;
                        }
                        //_logger.Log("Subsection from " + startIndexSub + " to " + endIndexSub);
                        //_logger.Log("Parsing from " + (startIndex + startIndexSub + 1) + ", length " + (endIndexSub - startIndexSub - 1));
                        while (endIndexSub - startIndexSub - 1 > 0)
                        {
                            int len = endIndexSub - startIndexSub - 1;
                            if (len > BLKSIZE) len = BLKSIZE;
                            sb.Append(_encoder.GetString(input, (startIndex + startIndexSub + 1), len));
                            startIndexSub += len;
                        }
                        //Convert to a string and add this to the new command list
                        //_logger.Log("Parsed: " + sb.ToString());
                        //If this is equal to the null string, replace it with null
                        if (sb.ToString() == "~><~>~<~><~")
                        {
                            newCommand.Add(null);
                        }
                        else
                        {
                            newCommand.Add(sb.ToString());
                        }
                    }
                    commandList.Add(newCommand);
                }
                return commandList;
            }
            //If an exception is thrown, the input was not properly formatted
            catch (Exception e)
            {
                logger.Log(e);
                SendMessage(-3, "FORMATERROR");
                //In which case, we return null
                return null;
            }
        }
    }
}
