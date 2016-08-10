using System;
using Urho;

namespace UrhoExample
{
  public static class QuaternionExtensions
  {
    const double EPS = 1e-6;

    /// <summary>
    /// Converts a quaternion to a rotation matrix, following the logic here:
    /// http://grepcode.com/file/repo1.maven.org/maven2/org.robolectric/android-all/4.4_r1-robolectric-1/android/hardware/SensorManager.java
    /// </summary>
    /// <param name="self">Self.</param>
    /// <param name="output">Output.</param>
    public static void ToRotationMatrix(this Quaternion rotationVector, ref Matrix4 R)
    {
      float q0 = rotationVector.W;
      float q1 = rotationVector.X;
      float q2 = rotationVector.Y;
      float q3 = rotationVector.Z;

      float sq_q1 = 2 * q1 * q1;
      float sq_q2 = 2 * q2 * q2;
      float sq_q3 = 2 * q3 * q3;
      float q1_q2 = 2 * q1 * q2;
      float q3_q0 = 2 * q3 * q0;
      float q1_q3 = 2 * q1 * q3;
      float q2_q0 = 2 * q2 * q0;
      float q2_q3 = 2 * q2 * q3;
      float q1_q0 = 2 * q1 * q0;

      R.M11 = 1 - sq_q2 - sq_q3;
      R.M12 = q1_q2 - q3_q0;
      R.M13 = q1_q3 + q2_q0;
      R.M14 = 0.0f;

      R.M21 = q1_q2 + q3_q0;
      R.M22 = 1 - sq_q1 - sq_q3;
      R.M23 = q2_q3 - q1_q0;
      R.M24 = 0.0f;

      R.M31 = q1_q3 - q2_q0;
      R.M32 = q2_q3 + q1_q0;
      R.M33 = 1 - sq_q1 - sq_q2;
      R.M34 = 0.0f;

      R.M41 = R.M42 = R.M43 = 0.0f;
      R.M44 = 1.0f;
    }

    public static Matrix4 ToRotationMatrix(this Quaternion rotationVector)
    {
      Matrix4 ret = new Matrix4();
      rotationVector.ToRotationMatrix(ref ret);
      return ret;
    }

    /// <summary>
    /// see cref="http://lolengine.net/blog/2014/02/24/quaternion-from-two-vectors-final"/> 
    /// assumes direction vectors vFrom and vTo are normalized
    /// </summary>
    /// <returns>The from unit vectors.</returns>
    /// <param name="vFrom">V from.</param>
    /// <param name="vTo">V to.</param>
    public static Quaternion FromUnitVectors(Vector3 vFrom, Vector3 vTo)
    {
      var r = Vector3.Dot(vFrom, vTo) + 1.0;
      Vector3 v1;

      if (r < EPS)
      {
        r = 0;
        if (Math.Abs(vFrom.X) > Math.Abs(vFrom.Z))
        {
          v1 = new Vector3(-vFrom.Y, vFrom.X, 0);
        }
        else 
        {
          v1 = new Vector3(0, -vFrom.Z, vFrom.Y);
        }
      }
      else 
      {
        v1 = Vector3.Cross(vFrom, vTo);
      }

      Quaternion q = new Quaternion(v1.X, v1.Y, v1.Z, (float)r);

      q.Normalize();

      return q;
    }

    /// <summary>
    /// Created to replace Quartenion.FromAxisAngle, which returns incorrect value
    /// </summary>
    /// <returns>The axis angle.</returns>
    /// <param name="axis">Axis.</param>
    /// <param name="angle">Angle.</param>
    public static Quaternion FromAxisAngle(Vector3 axis, float angle)
    {
      var halfAngle = angle / 2f;
      var s = (float)Math.Sin(halfAngle);

      return new Quaternion(
        axis.X * s,
        axis.Y * s,
        axis.Z * s,
        (float)Math.Cos(halfAngle));
    }

    public static Vector3 ApplyToVector(this Quaternion q, Vector3 v)
    {
      var x = v.X;
      var y = v.Y;
      var z = v.Z;

      var qx = q.X;
      var qy = q.Y;
      var qz = q.Z;
      var qw = q.W;

      // calculate quat * vector

      var ix = qw * x + qy * z - qz * y;
      var iy = qw * y + qz * x - qx * z;
      var iz = qw * z + qx * y - qy * x;
      var iw = -qx * x - qy * y - qz * z;

      // calculate result * inverse quat

      return new Vector3(
        ix * qw + iw * -qx + iy * -qz - iz * -qy,
        iy * qw + iw * -qy + iz * -qx - ix * -qz,
        iz * qw + iw * -qz + ix * -qy - iy * -qx);
    }

    public static Quaternion Flip(this Quaternion q, bool x = false, bool y = false, bool z = false)
    {
      Vector3 axis;
      float angle;
      q.ToAxisAngle(out axis, out angle);
      if (double.IsNaN(axis.X) || double.IsNaN(axis.Y) || double.IsNaN(axis.Z)) { return q; }

      if (x) { axis.X = -axis.X; }
      if (y) { axis.Y = -axis.Y; }
      if (z) { axis.Z = -axis.Z; }
      return FromAxisAngle(axis, angle);
    }
  }
}

