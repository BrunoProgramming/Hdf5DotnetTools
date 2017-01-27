﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hdf5DotNetTools;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HDF.PInvoke;

namespace Hdf5UnitTests
{
    public partial class Hdf5UnitTests
    {
        private Hdf5AcquisitionFile fillHeader(Hdf5AcquisitionFile header)
        {
            header.Patient.Name = "Robert";
            header.Patient.Gender = "Male";
            header.Patient.BirthDate = new DateTime(1969, 1, 12);
            header.Patient.Id = "8475805";
            header.Recording.NrOfChannels = 5;
            header.Recording.SampleRate = 200;
            for (int i = 0; i < header.Recording.NrOfChannels; i++)
            {
                var chn = header.Channels[i];
                chn.Label = $"DC{(i + 1):D2}";
                chn.Dimension = "V";
                chn.Offset = 0;
                chn.Amplification = (double)(10 - -10) / (short.MaxValue - short.MinValue);
                chn.SamplingRate = header.Recording.SampleRate;
            }
            return header;

        }

        [TestMethod]
        public void WriteAndReadNoDataAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testNoDataAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var data = fillHeader(writer.Header);
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Channels.Select(c=>c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }

        /// <summary>
        /// an acquisition file is created that has 5 channels of data
        /// for each channel 100 samples are written in two steps of 50 samples to the file
        /// The total number of samples is written to the channels and recording objects
        /// </summary>
        [TestMethod]
        public void WriteAndReadWithDataAcquisitionFile()
        {
            string filename = Path.Combine(folder, "testWithDataAcquisition.H5");
            try
            {
                using (var writer = new Hdf5AcquisitionFileWriter(filename))
                {
                    var header = fillHeader(writer.Header);
                    var signals = new List<double[]>(header.Recording.NrOfChannels);
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        signals.Add(Enumerable.Range(i * 50, 50).Select(j => j / 50.0).ToArray());
                    }

                    writer.Write(signals);
                    signals = new List<double[]>(header.Recording.NrOfChannels);
                    for (int i = 0; i < header.Recording.NrOfChannels; i++)
                    {
                        signals.Add(Enumerable.Range((i + 1) * 50, 50).Select(j => j / 50.0).ToArray());
                    }
                    writer.Write(signals);
                    header.Recording.NrOfSamples = 100;
                    header.Channels.Select(c => c.NrOfSamples =  100);
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }

            try
            {
                using (var reader = new Hdf5AcquisitionFileReader(filename))
                {
                    var header = reader.Header;
                    Assert.IsTrue(header.Patient.Name == "Robert");
                    Assert.IsTrue(header.Recording.NrOfSamples == 100);
                    Assert.IsTrue(header.Channels.Select(c => c.Label).SequenceEqual(new string[] { "DC01", "DC02", "DC03", "DC04", "DC05" }));
                    Assert.IsTrue(header.Channels.Select(c => c.NrOfSamples).SequenceEqual(new int[] { 100, 100, 100, 100, 100 }));
                    var data = reader.ReadDouble(0, 49);
                    var sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 0, 1 / 50f, 2 / 50f, 3 / 50f, 4 / 50f }));
                    data = reader.ReadDouble(50, 99);
                    sig = data.First().Take(5);
                    Assert.IsTrue(sig.Similar(new double[] { 50 / 50f, 51 / 50f, 52 / 50f, 53 / 50f, 54 / 50f }));
                }
            }
            catch (Exception ex)
            {
                CreateExceptionAssert(ex);
            }
        }
    }
}