﻿using System;
using System.Runtime.InteropServices;

namespace Discord.Audio
{
    internal unsafe class OpusDecoder : OpusConverter
    {
        public OpusDecoder()
        {
            _ptr = CreateDecoder(SamplingRate, Channels, out var error);
            CheckError(error);
        }

        [DllImport("opus", EntryPoint = "opus_decoder_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateDecoder(int Fs, int channels, out OpusError error);

        [DllImport("opus", EntryPoint = "opus_decoder_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DestroyDecoder(IntPtr decoder);

        [DllImport("opus", EntryPoint = "opus_decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode(IntPtr st, byte* data, int len, byte* pcm, int max_frame_size, int decode_fec);

        [DllImport("opus", EntryPoint = "opus_decoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DecoderCtl(IntPtr st, OpusCtl request, int value);

        public int DecodeFrame(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset,
            bool decodeFEC)
        {
            int result;
            fixed (byte* inPtr = input)
            fixed (byte* outPtr = output)
                result = Decode(_ptr, inPtr + inputOffset, inputCount, outPtr + outputOffset, FrameSamplesPerChannel,
                    decodeFEC ? 1 : 0);
            CheckError(result);
            return result * SampleBytes;
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (_ptr != IntPtr.Zero)
                DestroyDecoder(_ptr);
            base.Dispose(disposing);
        }
    }
}
