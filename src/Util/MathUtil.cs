using System;
using System.Collections.Generic;
using System.Globalization;

namespace Adadev.Util {
    public class MathMethods {
    
		    /// <summary>
        /// Converte graus para radianos
        /// </summary>
        private static double degree2Rad(double degree) {
            return degree * Math.PI / 180;
        }

        /// <summary>
        /// Calcula a distância entre um ponto e uma reta
        /// </summary>
        /// <param name="xPoint">longitude do ponto</param>
        /// <param name="yPoint">latitude do ponto</param>
        /// <param name="xLine1">x do primeiro ponto da reta</param>
        /// <param name="yLine1">y do primeiro ponto da reta</param>
        /// <param name="xLine2">x do segundo ponto da reta</param>
        /// <param name="yLine2">y do segundo ponto da reta</param>
        /// <returns>a distância ou -1 se os pontos da reta forem inválidos</returns>
        public static double DistanceBetweenPointAndLine(double xPoint, double yPoint, double xLine1, double yLine1, double xLine2, double yLine2) {
            // erro: devem ser fornecidos dois pontos diferentes da reta 
            if(xLine1 == xLine2 && yLine1 == yLine2) {
                return -1;
            }

            double a = 1;
            double b = 0;
            double c = 0;

            double[] lineEquation = LineEquation(xLine1, yLine1, xLine2, yLine2);

            if(lineEquation != null) {
                // equação reduzida da reta a partir de dois pontos => y = Ax + B
                double A = lineEquation[0];
                double B = lineEquation[1];

                // equação geral da reta a partir da reduzida => ax + by + c = 0
                a = -A;
                b = 1;
                c = -B;
            }

            // distância entre ponto e reta
            double d = Math.Abs(a * xPoint + b * yPoint + c) / Math.Sqrt(a * a + b * b);

            return d;
        }

        /// <summary>
        /// Distância entre ponto e semi reta
        /// </summary>
        /// <param name="xPoint">longitude do ponto</param>
        /// <param name="yPoint">latitude do ponto</param>
        /// <param name="xLine1">x do primeiro ponto da semireta</param>
        /// <param name="yLine1">y do primeiro ponto da semireta</param>
        /// <param name="xLine2">x do segundo ponto da semireta</param>
        /// <param name="yLine2">y do segundo ponto da semireta</param>
        /// <returns>a distância ou -1 se os pontos da semireta forem inválidos</returns>
        public static double DistanceBetweenPointAndRay(double xPoint, double yPoint, double xLine1, double yLine1, double xLine2, double yLine2) {
            double[] intersectionPoint = null;
            double[] line = LineEquation(xLine1, yLine1, xLine2, yLine2);
            if(line != null) {
                double[] perpendicular = PerpendicularLine(line[0], line[1], xPoint, yPoint);
                if(perpendicular != null) {
                    intersectionPoint = IntersectionBetweenLines(line[0], line[1], perpendicular[0], perpendicular[1]);
                }
            }

            if(intersectionPoint == null) {
                return -1;
            }

            if(IsLinePointInRay(intersectionPoint[0], intersectionPoint[1], xLine1, yLine1, xLine2, yLine2)) {
                return DistanceBetweenPoints(xPoint, yPoint, intersectionPoint[0], intersectionPoint[1]);
            }

            double d1 = DistanceBetweenPoints(xPoint, yPoint, xLine1, yLine1);
            double d2 = DistanceBetweenPoints(xPoint, yPoint, xLine2, yLine2);
            if(d1 < d2) {
                return d1;
            }

            return d2;
        }

        /// <summary>
        /// Determina se ponto pertence a polígono(está dentro ou na aresta) baseado no algoritmo do número winding
        /// </summary>
        /// <param name="polygon">Array de array com pontos do polígono</param>
        /// <param name="p">Array com coordenadas do ponto</param>
        /// <returns></returns>
        public static bool IsPoinInConvexPolygon(int[,] polygon, int[] p) {
			// erro
			if(polygon.GetLength(0) < 3 || p.Length < 2){
				return false;
			}
			
            int sign = 0;
            for(int i = 0; i < polygon.GetLength(0) - 1; i++) {
                int d = (p[0] - polygon[i, 0]) * (polygon[i + 1, 1] - polygon[i, 1]) - (polygon[i + 1, 0] - polygon[i, 0]) * (p[1] - polygon[i, 1]);
                // se o valor de d tiver o mesmo sinal para todos segmentos de reta, o ponto é interno
                int currentSign = d < 0 ? -1 : d > 0 ? 1 : 0;
                if(Math.Abs(sign - currentSign) == 2) {// se trocar de sinal o ponto é externo
                    return false;
                }
                sign = currentSign;
            }
            int i1 = polygon.GetLength(0) - 1;
            int d1 = (p[0] - polygon[i1, 0]) * (polygon[0, 1] - polygon[i1, 1]) - (polygon[0, 0] - polygon[i1, 0]) * (p[1] - polygon[i1, 1]);
            // se o valor de d tiver o mesmo sinal para todos segmentos de reta, o ponto é interno
            int currentSign1 = d1 < 0 ? -1 : d1 > 0 ? 1 : 0;
            if(Math.Abs(sign - currentSign1) == 2) {// se trocar de sinal o ponto é externo
                return false;
            }

            return true;
        }

        /// <summary>
        /// Dados dois pontos de uma reta, retorna os coeficientes da reta
        /// </summary>
        /// <param name="xLine1">x do primeiro ponto da reta</param>
        /// <param name="yLine1">y do primeiro ponto da reta</param>
        /// <param name="xLine2">x do segundo ponto da reta</param>
        /// <param name="yLine2">y do segundo ponto da reta</param>
        /// <returns>Array onde o primeiro elemento é o coeficiente angular e o segundo, o coeficiente linear. Nulo se os parâmetros forem inválidos.</returns>

        public static double[] LineEquation(double xLine1, double yLine1, double xLine2, double yLine2) {
            // Devem ser fornecidos dois pontos diferentes
            if(xLine1 == xLine2 && yLine1 == yLine2) {
                return null;
            }
            // não é função em x
            if(xLine1 == xLine2) {
                return null;
            }
            // equação reduzida da reta a partir de dois pontos => y = Ax + B
            double A = (yLine1 - yLine2) / (xLine1 - xLine2);
            double B = (xLine1 * yLine2 - xLine2 * yLine1) / (xLine1 - xLine2);

            return new double[] { A, B };
        }

        /// <summary>
        /// Calcula a reta perpendicular a uma reta passando pelo ponto informado
        /// </summary>
        /// <param name="a">Coeficiente angular da reta</param>
        /// <param name="x">x do ponto que a perpendicular intercepta</param>
        /// <param name="y">y do ponto que a perpendicular intercepta</param>
        /// <returns>Coeficientes da reta</returns>
        public static double[] PerpendicularLine(double a, double x, double y) {
            return new double[] { -1 / a, y + x / a };
        }

        /// <summary>
        /// retorna as raízes de uma equação quadrática dados seus coeficientes
        /// </summary>
        /// <returns>Array com as raízes</returns>
        public static double[] Baskara(double a, double b, double c) {
            double delta = b * b - 4 * a * c;
            if(delta < 0) {
                return null;
            }
            if(delta == 0) {
                double x = -b / (2 * a);
                return new[] { x };
            }
            double x1 = (-b + Math.Sqrt(delta)) / (2 * a);
            double x2 = (-b - Math.Sqrt(delta)) / (2 * a);

            return new[] { x1, x2 };
        }

        /// <summary>
        /// Retorna os pontos pertecentes a uma reta que estão a determinada distância do ponto informado 
        /// baseado nas equações:
        /// d^2 = (x? - x)^2 + (y? - y)^2
        /// y? = ax? + b
        /// isolando x? e y? caímos numa equação quadrática e achamos 2 pontos
        /// </summary>
        /// <param name="a">Coeficiente angular da reta</param>
        /// <param name="b">Coeficiente linear da reta</param>
        /// <param name="x">x do ponto</param>
        /// <param name="y">y do ponto</param>
        /// <param name="distance">distância</param>
        /// <returns>ponto(s) encontrado(s)</returns>
        public static double[,] PointOnLineWithDistancePoint(double a, double b, double x, double y, double distance) {
            //raízes da equação
            double[] roots = Baskara(a * a + 1,
                                     2 * a * b - 2 * x - 2 * a * y,
                                     x * x + b * b - 2 * b * y + y * y - distance * distance);
            if(roots == null) {
                return null;
            }

            if(roots.Length == 1) {
                double yout = a * roots[0] + b;
                return new double[1, 2] { { roots[0], yout } };
            }

            double yout1 = a * roots[0] + b;
            double yout2 = a * roots[1] + b;

            return new double[2, 2] { { roots[0], yout1 }, { roots[1], yout2 } };
        }

        /// <summary>
        /// Verifica se o ponto da reta (já pertencente à reta) pertence a uma semi reta
        /// </summary>
        public static bool IsLinePointInRay(double x, double y, double xLine1, double yLine1, double xLine2, double yLine2) {
            if(xLine1 <= x && x <= xLine2 || xLine2 <= x && x <= xLine1 &&
               yLine1 <= y && y <= yLine2 || yLine2 <= y && y <= yLine1) {
                return true;
            }

            return false;
        }

        public static double DistanceBetweenPoints(double x1, double y1, double x2, double y2) {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        public static int Round(double number) {
            return (int)Math.Round(number);
        }

        /// <summary>
        /// Arredonda um decimal sempre para cima
        /// Se for inteiro retorna o mesmo número
        /// </summary>
        public static int RoundUp(double number) {
            int intValue = (int)number;

            if(number - intValue != 0) {
                return intValue + 1;
            }

            return intValue;
        }

        /// <summary>
        /// Fórmula Haversine que calcula a distância entre duas coordenadas geodésicas
        /// Fonte: http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>>
        /// <returns>Distância em metros</returns>
        public static double DistanceBetweenCoordinatesInMeters(double x1, double y1, double x2, double y2) {
            double earthRadius = 6371; // km
            double longitudeDiff = Degree2Rad(x2 - x1);
            double latitudeDiff = Degree2Rad(y2 - y1);

            double a = Math.Sin(latitudeDiff / 2) * Math.Sin(latitudeDiff / 2) +
                Math.Cos(Degree2Rad(y1)) * Math.Cos(Degree2Rad(y2)) *
                Math.Sin(longitudeDiff / 2) * Math.Sin(longitudeDiff / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c * 1000;
        }

        /// <summary>
        /// Compara se dois decimais são iguais até o número de casas decimais informado
        /// </summary>
        public static bool CompareDecimal(double number1, double number2, int decimalPlaces) {
            return Math.Round(number1, decimalPlaces) == Math.Round(number2, decimalPlaces);
        }

        /// <summary>
        /// Calcula o ponto de interseção entre duas retas, dados os coeficientes das mesmas
        /// </summary>
        public static double[] IntersectionBetweenLines(double a1, double b1, double a2, double b2) {
            if(a1 == a2) {// retas paralelas
                return null;
            }

            double x = (b1 - b2) / (a2 - a1);
            double y = a1 * x + b1;
            return new[] { x, y };
        }

    }
}
