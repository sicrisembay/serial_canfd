using System;
using System.Collections.Generic;
using System.Text;

namespace libSerialCanFD
{
    internal class FrameParser
    {
        #region Members
        private const int BUFFER_SIZE = 1024;
        private byte[] _buffer = new byte[BUFFER_SIZE];
        private int _readIndex = 0;
        private int _writeIndex = 0;
        public const int FRAME_OVERHEAD = 10;
        public const byte TAG_START_OF_FRAME = 0xFF;
        #endregion // Members

        #region Events
        public event EventHandler<byte[]>? FrameReceived;
        #endregion // Events

        #region Methods
        public FrameParser()
        {

        }

        private int GetAvailableDataLength()
        {
            if (_writeIndex >= _readIndex)
                return _writeIndex - _readIndex;
            else
                return BUFFER_SIZE - _readIndex + _writeIndex;
        }

        public void Parse(byte[] data, int length)
        {
            if (length <= 0 || length > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            for (int i = 0; i < length; i++)
            {
                _buffer[_writeIndex] = data[i];
                _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;
            }

            if(GetAvailableDataLength() < FRAME_OVERHEAD)
            {
                return;
            }

            // Parse frames from the buffer
            while(_writeIndex != _readIndex)
            {
                if (_buffer[_readIndex] != TAG_START_OF_FRAME)
                {
                    _readIndex = (_readIndex + 1) % BUFFER_SIZE;
                    continue;
                }

                int availableDataLength = GetAvailableDataLength();

                if (availableDataLength < FRAME_OVERHEAD)
                {
                    break;
                }

                int frameLength = _buffer[(_readIndex + 1) % BUFFER_SIZE];
                frameLength += _buffer[(_readIndex + 2) % BUFFER_SIZE] << 8;

                if (frameLength < FRAME_OVERHEAD || frameLength > BUFFER_SIZE)
                {
                    _readIndex = (_readIndex + 1) % BUFFER_SIZE;
                    continue;
                }

                if (availableDataLength < frameLength)
                {
                    break;
                }

                // Calculate the checksum
                int checksum = 0;
                for (int i = 0; i < frameLength; i++)
                {
                    checksum += _buffer[(_readIndex + i) % BUFFER_SIZE];
                }

                if((checksum & 0xFF) != 0)
                {
                    _readIndex = (_readIndex + 1) % BUFFER_SIZE;
                    continue;
                }

                // Valid frame found, extract it
                byte[] frame = new byte[frameLength];
                for (int i = 0; i < frameLength; i++)
                {
                    frame[i] = _buffer[(_readIndex + i) % BUFFER_SIZE];
                }

                FrameReceived?.Invoke(this, frame);

                // Done with this frame, move the read index forward
                _readIndex = (_readIndex + frameLength) % BUFFER_SIZE;
            }
        }
        #endregion // Methods
    }
}
