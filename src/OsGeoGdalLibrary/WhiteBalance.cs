﻿using System;
using OSGeo.GDAL;

// usando GdalUtilities.cs

namespace Adadev.Gdal {

    public class WhiteBalance {

		/*** O balanço de branco é um filtro que melhora a qualidade de imagens foscas ***/
		
        private double percentForBalance = 0.7;

        public WhiteBalance() {
            GdalConfiguration.ConfigureGdal();
        }

        public WhiteBalance(double percentForBalance) {
            this.percentForBalance = percentForBalance;
            GdalConfiguration.ConfigureGdal();
        }

        public void AplyWhiteBalance(string imagePath, string outImagePath) {

            using(Dataset image = Gdal.Open(imagePath, Access.GA_ReadOnly)) {

                Band redBand = GdalUtilities.GetBand(image, ColorInterp.GCI_RedBand);
                Band greenBand = GdalUtilities.GetBand(image, ColorInterp.GCI_GreenBand);
                Band blueBand = GdalUtilities.GetBand(image, ColorInterp.GCI_BlueBand);

                if(redBand == null || greenBand == null || blueBand == null) {
                    throw new NullReferenceException("One or more bands are not available.");
                }

                int width = redBand.XSize;
                int height = redBand.YSize;

                using(Dataset outImage = Gdal.GetDriverByName("GTiff").Create(outImagePath, width, height, 3, DataType.GDT_Byte, null)) {

                    GdalUtilities.CopyGeoProjection(image, outImage);

                    Band outRedBand = outImage.GetRasterBand(1);
                    Band outGreenBand = outImage.GetRasterBand(2);
                    Band outBlueBand = outImage.GetRasterBand(3);
                    GdalUtilities.SetBandsColorInterpretation(outRedBand, outGreenBand, outBlueBand);

                    int[] red = new int[width * height];
                    int[] green = new int[width * height];
                    int[] blue = new int[width * height];
                    redBand.ReadRaster(0, 0, width, height, red, width, height, 0, 0);
                    greenBand.ReadRaster(0, 0, width, height, green, width, height, 0, 0);
                    blueBand.ReadRaster(0, 0, width, height, blue, width, height, 0, 0);

                    byte[] outRed = WhiteBalanceBand(red);
                    byte[] outGreen = WhiteBalanceBand(green);
                    byte[] outBlue = WhiteBalanceBand(blue);
                    outRedBand.WriteRaster(0, 0, width, height, outRed, width, height, 0, 0);
                    outGreenBand.WriteRaster(0, 0, width, height, outGreen, width, height, 0, 0);
                    outBlueBand.WriteRaster(0, 0, width, height, outBlue, width, height, 0, 0);

                    outImage.FlushCache();
                }
            }
        }

        public byte[] WhiteBalanceBand(int[] band) {
            int[] sortedBand = new int[band.Length];
            Array.Copy(band, sortedBand, band.Length);
            Array.Sort(sortedBand);

            double perc05 = Percentile(sortedBand, percentForBalance);
            double perc95 = Percentile(sortedBand, 100.0 - percentForBalance);

            byte[] bandBalanced = new byte[band.Length];

            for(int i = 0; i < band.Length; i++) {

                double valueBalanced = (band[i] - perc05) * 255.0 / (perc95 - perc05);
                bandBalanced[i] = LimitToByte(valueBalanced);
            }

            return bandBalanced;
        }

        public double Percentile(int[] sequence, double percentile) {

            int nSequence = sequence.Length;
            double nPercent = (nSequence + 1) * percentile / 100d;
            if(nPercent == 1d) {
                return sequence[0];
            } else if(nPercent == nSequence) {
                return sequence[nSequence - 1];
            } else {
                int intNPercent = (int)nPercent;
                double d = nPercent - intNPercent;
                return sequence[intNPercent - 1] + d * (sequence[intNPercent] - sequence[intNPercent - 1]);
            }
        }

        private byte LimitToByte(double value) {
            byte newValue;

            if(value < 0) {
                newValue = 0;
            } else if(value > 255) {
                newValue = 255;
            } else {
                newValue = (byte)value;
            }

            return newValue;
        }
    }
}
