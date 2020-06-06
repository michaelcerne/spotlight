using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CitizenFX.Core;

namespace Spotlight_client
{
    class Utilities
    {

        public static double DirectionToAngle(Vector2 vector)
        {
            return DirectionToAngle(vector.X, vector.Y);
        }

        public static double DirectionToAngle(double x, double y) // credit to Aidan Ferry
        {
            double[] angles = new double[4];
            double newRad = Math.Sqrt(x * x + y * y);
            x /= newRad;
            y /= newRad;
            angles[0] = RadianToDegree(Math.Acos(x));
            angles[1] = (x == 1 || x == -1) ? angles[0] : 360 - angles[0];
            angles[2] = RadianToDegree(Math.Asin(y));
            angles[3] = (y == 1 || y == -1) ? angles[2] : 180 - angles[2];
            if (angles[2] < 0.0) angles[2] += 360.0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (i != j && IsApproximatelyEqual(angles[i], angles[j])) return (angles[i] + angles[j]) / 2;
                }
            }
            return -1;
        }

        public static Vector3 RotationToDirection(Vector3 Rotation) // credit to LETSPLAYORDY
        {
            float z = Rotation.Z;
            float num = z * 0.0174532924f;
            float x = Rotation.X;
            float num2 = x * 0.0174532924f;
            float num3 = Math.Abs((float)Math.Cos(num2));
            return new Vector3
            {
                X = (float)(-(float)Math.Sin(num) * (double)num3),
                Y = (float)((float)Math.Cos(num) * (double)num3),
                Z = (float)Math.Sin(num2)
            };
        }

        private static double RadianToDegree(double rad)
        {
            return rad / Math.PI * 180.0f;
        }

        private static bool IsApproximatelyEqual(double a, double b)
        {
            return Math.Abs(a - b) < 0.25;
        }
    }
}
