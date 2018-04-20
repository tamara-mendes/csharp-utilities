using System.Collections.Generic;
using System.IO;
using Ionic.Zip;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System;

namespace Adadev.Gdal {

    public class GdalUtilities {

        private static readonly string UTM_ZONE_24S =
            "PROJCS[\"UTM_Zone_24_Southern_Hemisphere\",GEOGCS[\"GCS_GRS 1980(IUGG, 1980)\",DATUM[\"unknown\",SPHEROID[\"GRS80\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-39],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",50000000],PARAMETER[\"false_northing\",1000000000],UNIT[\"Centimeter\",0.01]]";

        public const int NO_DATA_LABEL = -32768;

        public GdalUtilities() {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();
        }

        public bool JsonToShapeFile(string jsonFilePath, string shapeFilePath) {

            OSGeo.OGR.Driver jsonFileDriver = Ogr.GetDriverByName("GeoJSON");
            DataSource jsonFile = Ogr.Open(jsonFilePath, 0);
            if(jsonFile == null) {
                return false;
            }

            string filesPathName = shapeFilePath.Substring(0, shapeFilePath.Length - 4);
            RemoveShapeFileIfExists(filesPathName);

            Layer jsonLayer = jsonFile.GetLayerByIndex(0);

            OSGeo.OGR.Driver esriShapeFileDriver = Ogr.GetDriverByName("ESRI Shapefile");
            DataSource shapeFile = Ogr.Open(shapeFilePath, 0);

            shapeFile = esriShapeFileDriver.CreateDataSource(shapeFilePath, new string[] { });
            Layer shplayer = shapeFile.CreateLayer(jsonLayer.GetName(), GetSirgas2000CurrentUTMReferenceInCentimeters(), jsonLayer.GetGeomType(), new string[] { });

            // cria os campos (propriedades) na layer 
            Feature feature = jsonLayer.GetNextFeature();
            for(int i = 0; i < feature.GetFieldCount(); i++) {
                FieldDefn fieldDefn = new FieldDefn(getValidFieldName(feature.GetFieldDefnRef(i)), feature.GetFieldDefnRef(i).GetFieldType());
                shplayer.CreateField(fieldDefn, 1);
            }

            while(feature != null) {
                Geometry geometry = feature.GetGeometryRef();
                double[] point = convertWgs84ToSirgas2000UtmZone24(geometry.GetX(0), geometry.GetY(0));
                Feature shpFeature = createPointInLayer(shplayer, point[0], point[1], GetSirgas2000CurrentUTMReferenceInCentimeters());

                // copia os valores de cada campo
                for(int i = 0; i < feature.GetFieldCount(); i++) {
                    if(FieldType.OFTInteger == feature.GetFieldDefnRef(i).GetFieldType()) {
                        shpFeature.SetField(getValidFieldName(feature.GetFieldDefnRef(i)), feature.GetFieldAsInteger(i));
                    } else if(FieldType.OFTReal == feature.GetFieldDefnRef(i).GetFieldType()) {
                        shpFeature.SetField(getValidFieldName(feature.GetFieldDefnRef(i)), feature.GetFieldAsDouble(i));
                    } else {
                        shpFeature.SetField(getValidFieldName(feature.GetFieldDefnRef(i)), feature.GetFieldAsString(i));
                    }
                }
                shplayer.SetFeature(shpFeature);

                feature = jsonLayer.GetNextFeature();
            }

            shapeFile.Dispose();

            // gerar zip dos arquivos gerados
            string zipName = filesPathName + ".zip";
            CompressToZipFile(new List<string>() { shapeFilePath, filesPathName + ".dbf", filesPathName + ".prj", filesPathName + ".shx" }, zipName);

            return true;
        }

        private Feature createPointInLayer(Layer layer, double x, double y, SpatialReference reference) {
            Feature feature = new Feature(layer.GetLayerDefn());

            string point = "POINT(" + x + " " + y + ")";
            point = point.Replace(",", ".");
            Geometry geometry = Geometry.CreateFromWkt(point);
            geometry.AssignSpatialReference(reference);

            feature.SetGeometry(geometry);
            layer.CreateFeature(feature);

            return feature;
        }

        /// <summary>
        /// Gerar um nome válido para o campo
        /// Os nomes dos campos em arquivos shapefile têm limite de 10 caracteres
        /// </summary>
        private string getValidFieldName(FieldDefn fieldDefn) {
            string fieldName = fieldDefn.GetName();
            return fieldName.Length > 10 ? fieldName.Substring(0, 10) : fieldName;
        }

        private void RemoveShapeFileIfExists(string filesPathName) {
            RemoveFileIfExists(filesPathName + ".shp");
            RemoveFileIfExists(filesPathName + ".shx");
            RemoveFileIfExists(filesPathName + ".prj");
            RemoveFileIfExists(filesPathName + ".zip");
        }

        public static bool RemoveFileIfExists(string filePath) {
            if(File.Exists(filePath)) {
                File.Delete(filePath);
                return true;
            }
            return false;
        }

        public static SpatialReference GetSirgas2000CurrentUTMReferenceInCentimeters() {
            // string epsg_31984_sirgas_proj4 = @"+proj=utm +zone=24 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=cm +no_defs";
            // reference.ImportFromEPSG(31984);
            // reference.ImportFromProj4(epsg_31984_sirgas_proj4);
            SpatialReference reference = new SpatialReference("");
            string ppszInput = UTM_ZONE_24S;
            reference.ImportFromWkt(ref ppszInput);
            return reference;
        }

        public double[] convertWgs84ToSirgas2000UtmZone24(double x, double y) {
            SpatialReference currentReference = getWgs84Reference();
            SpatialReference newReference = GetSirgas2000CurrentUTMReferenceInCentimeters();

            CoordinateTransformation ct = new CoordinateTransformation(currentReference, newReference);
            double[] point = new double[2] { x, y };
            ct.TransformPoint(point);

            return point;
        }

        public double[] convertSirgas200UtmZone24ToWgs84(double x, double y) {
            SpatialReference reference = GetSirgas2000CurrentUTMReferenceInCentimeters();
            SpatialReference newReference = getWgs84Reference();

            CoordinateTransformation ct = new CoordinateTransformation(reference, newReference);
            double[] point = new double[] { x, y };
            ct.TransformPoint(point);
            return point;
        }

        public static SpatialReference getWgs84Reference() {
            string epsg_wgs1984_proj4 = @"+proj=latlong +datum=WGS84 +no_defs";
            SpatialReference reference = new SpatialReference("");
            reference.ImportFromProj4(epsg_wgs1984_proj4);

            return reference;
        }

        /// <summary>
        /// Converter as coordenadas de WGS 84 / UTM zone 23S para latitude e longitude em graus
        /// </summary>
        /// <returns>Array com as coordenadas geodésicas do canto superior esquerdo da imagem e inferior direito, respectivamente</returns>
        public static double[][] TifCoordinatesToLonLat(double[] tifGeoTransformerData, string tifProjectionReference, int imgWidth, int imgHeight) {
            // referência atual em WGS 84 / UTM zone 23S
            SpatialReference currentReference = new SpatialReference(tifProjectionReference);
            // referência em latitude e longitude
            SpatialReference newReference = getWgs84Reference();

            CoordinateTransformation ct = new CoordinateTransformation(currentReference, newReference);

            double[] northWestPoint = new double[2] { tifGeoTransformerData[0], tifGeoTransformerData[3] };
            ct.TransformPoint(northWestPoint);
            double[] northwest = new double[]{
                northWestPoint[0],// x
                northWestPoint[1] // y
            };

            double[] southEastPoint = new double[2] {
                tifGeoTransformerData[0] + tifGeoTransformerData[1] * imgWidth,
                tifGeoTransformerData[3] + tifGeoTransformerData[5] * imgHeight
            };
            ct.TransformPoint(southEastPoint);
            double[] southeast = new double[] {
                southEastPoint[0],// x
                southEastPoint[1] // y
            };

            return new double[][] { northwest, southeast };
        }

        /// <summary>
        /// Converter as coordenadas de WGS 84 / UTM zone 23S para latitude e longitude em graus
        /// </summary>
        /// <param name="lonLat">Array de vetor com as duas coordenadas Geodesicas</param>
        /// <param name="tifProjectionReference"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static double[] LonLatToTifCoordinates(double[][] lonLat, string tifProjectionReference, int imgWidth, int imgHeight) {
            // referência do arquivo tif em WGS 84 / UTM zone 23S
            SpatialReference newReference = new SpatialReference(tifProjectionReference);
            // referência em latitude e longitude
            SpatialReference currentReference = GdalUtilities.getWgs84Reference();

            CoordinateTransformation ct = new CoordinateTransformation(currentReference, newReference);

            double[] tifGeoTransformerData = new double[6];
            double[] northwest = new double[2] { lonLat[0][0], lonLat[0][1] };
            double[] southeast = new double[2] { lonLat[1][0], lonLat[1][1] };

            ct.TransformPoint(northwest);
            ct.TransformPoint(southeast);
            southeast = new double[2] {
                (southeast[0] - northwest[0]) / imgWidth,
                (southeast[1] - northwest[1]) / imgHeight
            };

            tifGeoTransformerData[0] = northwest[0];
            tifGeoTransformerData[1] = southeast[0];
            tifGeoTransformerData[2] = 0;
            tifGeoTransformerData[3] = northwest[1];
            tifGeoTransformerData[4] = 0;
            tifGeoTransformerData[5] = southeast[1];

            return tifGeoTransformerData;
        }


        public static void CompressToZipFile(List<string> files, string zipPath) {
            using(ZipFile zip = new ZipFile()) {
                foreach(string file in files) {
                    zip.AddFile(file, "");
                }
                zip.Save(zipPath);
            }
        }

        /// <summary>
        /// Retorna a banda para determinada cor (Red, Green, Blue ou NIR)
        /// O dataset deve ter 4 bandas
        /// </summary>
        public static Band GetBand(Dataset SatelliteImageDataSet, ColorInterp colorInterp) {
            if(colorInterp.Equals(SatelliteImageDataSet.GetRasterBand(1).GetRasterColorInterpretation())) {
                return SatelliteImageDataSet.GetRasterBand(1);
            } else if(colorInterp.Equals(SatelliteImageDataSet.GetRasterBand(2).GetRasterColorInterpretation())) {
                return SatelliteImageDataSet.GetRasterBand(2);
            } else if(colorInterp.Equals(SatelliteImageDataSet.GetRasterBand(3).GetRasterColorInterpretation())) {
                return SatelliteImageDataSet.GetRasterBand(3);
            } else {
                return SatelliteImageDataSet.GetRasterBand(4);
            }
        }
        /// <summary>
        /// Copia a projeção de uma imagem para outra
        /// </summary>
        public static void CopyGeoProjection(Dataset image, Dataset newImage) {
            double[] geoTransformerData = new double[6];
            image.GetGeoTransform(geoTransformerData);
            newImage.SetGeoTransform(geoTransformerData);
            newImage.SetProjection(image.GetProjection());
        }

        public static void SetBandsColorInterpretation(Band redBand, Band greenBand, Band blueBand) {
            redBand.SetRasterColorInterpretation(ColorInterp.GCI_RedBand);
            greenBand.SetRasterColorInterpretation(ColorInterp.GCI_GreenBand);
            blueBand.SetRasterColorInterpretation(ColorInterp.GCI_BlueBand);
        }

        public int[] GetImageSize(string filePath) {
            using(Dataset image = Gdal.Open(filePath, Access.GA_ReadOnly)) {
                Band band = image.GetRasterBand(1);
                if(band == null) {
                    return null;
                }
                return new int[] { band.XSize, band.YSize };
            }
        }

        public static double[][] readImageCoordinatesBounds(Dataset imageDataset) {
            Band band = imageDataset.GetRasterBand(1);
            int width = band.XSize;
            int height = band.YSize;

            double[] geoTransformerData = new double[6];
            imageDataset.GetGeoTransform(geoTransformerData);

            // converter as coordenadas de WGS 84 / UTM zone 23S para latitude e longitude em graus
            return TifCoordinatesToLonLat(geoTransformerData, imageDataset.GetProjectionRef(), width, height);
        }

        public float[][] readOneBandTiffToMatrix(string imagePath) {
            float[][] matrixImage;
            int width, height;
            using(Dataset image = Gdal.Open(imagePath, Access.GA_ReadOnly)) {
                Band band = image.GetRasterBand(1);

                if(band == null) {
                    throw new NullReferenceException("One or more bands are not available.");
                }

                width = band.XSize;
                height = band.YSize;

                matrixImage = new float[height][];
                for(int h = 0; h < height; h++) {
                    matrixImage[h] = new float[width];

                    band.ReadRaster(0, h, width, 1, matrixImage[h], width, 1, 0, 0);
                }
            }
            return matrixImage;
        }

        public void writeMatrixInExistingOneBandTiff(string imagePath, float[][] matrixImage, string referencialPath) {
            int width = matrixImage[0].Length;
            int height = matrixImage.Length;

            using(Dataset image = Gdal.GetDriverByName("GTiff").Create(imagePath, width, height, 1, DataType.GDT_Int32, null)) {

                using(Dataset imageRef = Gdal.Open(referencialPath, Access.GA_ReadOnly)) {
                    CopyGeoProjection(imageRef, image);
                }

                Band DTMBand = image.GetRasterBand(1);

                for(int h = 0; h < height; h++) {

                    DTMBand.WriteRaster(0, h, width, 1, matrixImage[h], width, 1, 0, 0);
                }
                image.FlushCache();
            }
        }

        /// <summary>
        /// Em cada pixel de uma imagem onde não houver dado, anula a mesma posição na matriz
        /// </summary>
        public float[][] copyNoDataToImage(string noDataImagePath, float[][] newImage) {
            using(Dataset noDataImage = Gdal.Open(noDataImagePath, Access.GA_ReadOnly)) {
                Band band = noDataImage.GetRasterBand(1);
                int width = band.XSize;
                int height = band.YSize;

                for(int h = 0; h < height; h++) {
                    float[] values = new float[width];
                    band.ReadRaster(0, h, width, 1, values, width, 1, 0, 0);

                    for(int w = 0; w < width; w++) {
                        if(values[w] == NO_DATA_LABEL) {
                            newImage[h][w] = NO_DATA_LABEL;
                        }
                    }
                }
            }
            return newImage;
        }

        /// <summary>
        /// Percorre as três bandas de uma imagem RGB e retorna o maior valor de pixel encontrado
        /// </summary>
        /// <param name="filePath">Diretório da imagem</param>
        /// <param name="width">Variavel para armazenar a largura da imagem</param>
        /// <param name="height">Variavel para armazenar a altura da imagem</param>
        /// <returns>inteiro com maior valor de pixel da imagem</returns>
        public int GetMaxPixelValueOfRGBImage(string filePath, ref int width, ref int height) {
            int maxPixelValue = int.MinValue;

            using(Dataset imageDataSet = Gdal.Open(filePath, Access.GA_ReadOnly)) {

                if(imageDataSet == null || imageDataSet.RasterCount < 3) {
                    throw new Exception("Imagem invalida " + filePath);
                }

                Band redBand = GetBand(imageDataSet, ColorInterp.GCI_RedBand);
                Band greenBand = GetBand(imageDataSet, ColorInterp.GCI_GreenBand);
                Band blueBand = GetBand(imageDataSet, ColorInterp.GCI_BlueBand);
                if(redBand == null || greenBand == null || blueBand == null) {
                    throw new Exception("Uma ou mais bandas de cor nao estao disponiveis para a imagem " + filePath);
                }
                width = redBand.XSize;
                height = redBand.YSize;

                for(int h = 0; h < height; h++) {
                    short[] red = new short[width];
                    short[] green = new short[width];
                    short[] blue = new short[width];
                    redBand.ReadRaster(0, h, width, 1, red, width, 1, 0, 0);
                    greenBand.ReadRaster(0, h, width, 1, green, width, 1, 0, 0);
                    blueBand.ReadRaster(0, h, width, 1, blue, width, 1, 0, 0);

                    for(int w = 0; w < width; w++) {
                        if(red[w] > maxPixelValue) {
                            maxPixelValue = red[w];
                        }
                        if(green[w] > maxPixelValue) {
                            maxPixelValue = green[w];
                        }
                        if(blue[w] > maxPixelValue) {
                            maxPixelValue = blue[w];
                        }
                    }
                }
            }

            return maxPixelValue;
        }

    }
}
