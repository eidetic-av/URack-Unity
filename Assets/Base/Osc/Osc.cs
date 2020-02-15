/*
 * Osc.cs - A minimal OSC receiver implementation for Unity.
 * https://github.com/keijiro/unity-osc
 */
using System;

namespace Eidetic.URack.Osc
{
    using MessageQueue = System.Collections.Generic.Queue<Message>;
    
    public struct Message
    {
        public string path;
        public object[] data;
        
        public override string ToString ()
        {
            var temp = path + ":";
            foreach (var o in data) {
                temp += o + ":";
            }
            return temp;
        }
    }
    
    public class Parser
    {
        #region General private members
        MessageQueue messageBuffer;
        #endregion
        
        #region Temporary read buffer
        Byte[] readBuffer;
        int readPoint;
        #endregion
        
        #region Public members
        public int MessageCount {
            get { return messageBuffer.Count; }
        }
        
        public Parser ()
        {
            messageBuffer = new MessageQueue ();
        }
        
        public Message PopMessage ()
        {
            return messageBuffer.Dequeue ();
        }
        
        public void FeedData (Byte[] data)
        {
            readBuffer = data;
            readPoint = 0;
            
            ReadMessage ();
            
            readBuffer = null;
        }
        #endregion
        
        #region Private methods
        void ReadMessage ()
        {
            var path = ReadString ();
            
            if (path == "#bundle") {
                ReadInt64 ();
                
                while (true) {
                    if (readPoint >= readBuffer.Length) {
                        return;
                    }
                    var peek = readBuffer [readPoint];
                    if (peek == '/' || peek == '#') {
                        ReadMessage ();
                        return;
                    }
                    var bundleEnd = readPoint + ReadInt32 ();
                    while (readPoint < bundleEnd) {
                        ReadMessage ();
                    }
                }
            }
            
            var temp = new Message ();
            temp.path = path;
            
            var types = ReadString ();
            temp.data = new object[types.Length - 1];
            
            for (var i = 0; i < types.Length - 1; i++) {
                switch (types [i + 1]) {
                case 'f':
                    temp.data [i] = ReadFloat32 ();
                    break;
                case 'i':
                    temp.data [i] = ReadInt32 ();
                    break;
                case 's':
                    temp.data [i] = ReadString ();
                    break;
                case 'b':
                    temp.data [i] = ReadBlob ();
                    break;
                }
            }
            
            messageBuffer.Enqueue (temp);
        }
        
        float ReadFloat32 ()
        {
            Byte[] temp = {
                readBuffer [readPoint + 3],
                readBuffer [readPoint + 2],
                readBuffer [readPoint + 1],
                readBuffer [readPoint]
            };
            readPoint += 4;
            return BitConverter.ToSingle (temp, 0);
        }
        
        int ReadInt32 ()
        {
            int temp =
                (readBuffer [readPoint + 0] << 24) +
                (readBuffer [readPoint + 1] << 16) +
                (readBuffer [readPoint + 2] << 8) +
                (readBuffer [readPoint + 3]);
            readPoint += 4;
            return temp;
        }
        
        long ReadInt64 ()
        {
            long temp =
                ((long)readBuffer [readPoint + 0] << 56) +
                ((long)readBuffer [readPoint + 1] << 48) +
                ((long)readBuffer [readPoint + 2] << 40) +
                ((long)readBuffer [readPoint + 3] << 32) +
                ((long)readBuffer [readPoint + 4] << 24) +
                ((long)readBuffer [readPoint + 5] << 16) +
                ((long)readBuffer [readPoint + 6] << 8) +
                ((long)readBuffer [readPoint + 7]);
            readPoint += 8;
            return temp;
        }
        
        string ReadString ()
        {
            var offset = 0;
            while (readBuffer[readPoint + offset] != 0) {
                offset++;
            }
            var s = System.Text.Encoding.UTF8.GetString (readBuffer, readPoint, offset);
            readPoint += (offset + 4) & ~3;
            return s;
        }
        
        Byte[] ReadBlob ()
        {
            var length = ReadInt32 ();
            var temp = new Byte[length];
            Array.Copy (readBuffer, readPoint, temp, 0, length);
            readPoint += (length + 3) & ~3;
            return temp;
        }
        #endregion
    }
    public class Encoder
    {
        public Byte[] Buffer { get { return _buffer; } }
        public int Length { get { return _length; } }

        public void Clear()
        {
            _length = 0;
        }

        public void Append(string data)
        {
            var len = data.Length;
            for (var i = 0; i < len; i++)
                _buffer[_length++] = (Byte)data[i];

            var len4 = Align4(len + 1);
            for (var i = len; i < len4; i++)
                _buffer[_length++] = 0;
        }

        public void Append(int data)
        {
            _buffer[_length++] = (Byte)(data >> 24);
            _buffer[_length++] = (Byte)(data >> 16);
            _buffer[_length++] = (Byte)(data >> 8);
            _buffer[_length++] = (Byte)(data);
        }

        public void Append(float data)
        {
            _tempFloat[0] = data;
            System.Buffer.BlockCopy(_tempFloat, 0, _tempByte, 0, 4);
            _buffer[_length++] = _tempByte[3];
            _buffer[_length++] = _tempByte[2];
            _buffer[_length++] = _tempByte[1];
            _buffer[_length++] = _tempByte[0];
        }

        public int Align4(int length)
        {
            return (length + 3) & ~3;
        }

        Byte[] _buffer = new Byte[4096];
        int _length;

        // Used to change byte order
        static float[] _tempFloat = new float[1];
        static Byte[] _tempByte = new Byte[4];
    }

}