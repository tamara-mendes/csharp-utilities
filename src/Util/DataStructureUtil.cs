using System;

namespace Adadev.Util {
    
	public class DataStructureUtil {
	
		public static float[][] ArrayToMatrix(float[] array, int width) {
            int height = array.Length / width;

            float[][] matrix = new float[height][];

            int i = 0;
            for(int h = 0; h < height; h++) {
                matrix[h] = new float[width];
                for(int w = 0; w < width; w++) {
                    matrix[h][w] = array[i];
                    i++;
                }
            }

            return matrix;
        }
	}
}